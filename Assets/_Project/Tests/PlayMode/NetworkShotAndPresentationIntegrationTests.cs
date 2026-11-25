#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Mirror;
using NUnit.Framework;
using SmallArena.Client;
using SmallArena.Client.UI;
using SmallArena.Domain;
using SmallArena.Input;
using SmallArena.Networking;
using SmallArena.Protocol;
using SmallArena.Server.Sessions;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SmallArena.Tests.PlayMode
{
    public sealed class NetworkShotAndPresentationIntegrationTests : NetworkGameplayFixture
    {
        private ServerMatchLifecycleController lifecycle;
        private NetworkShotIntentAdapter sink;
        private NetworkPlayerPresentationAdapter source;
        private IEnumerator Begin(int target = 0, double cooldown = .1, bool waitForLive = true)
        {
            yield return StartWorld();
            var floor = Track(GameObject.CreatePrimitive(PrimitiveType.Cube)); floor.layer = 6;
            floor.transform.position = new Vector3(1000, -.5f, 1000); floor.transform.localScale = new Vector3(20, 1, 20);
            var markers = NetworkManager.startPositions.Where(t => t.name == "Combat start").ToArray();
            markers[0].position = new Vector3(997, 1, 997); markers[1].position = new Vector3(1003, 1, 1003);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Network/MatchState.prefab"); NetworkClient.RegisterPrefab(prefab);
            lifecycle = manager.gameObject.AddComponent<ServerMatchLifecycleController>();
            lifecycle.Configure(new GameplayConfiguration(maxHealth: 10, shotDamage: 1, shotCooldownSeconds: cooldown,
                shotRangeMetres: 3, countdownDurationSeconds: .2, matchDurationSeconds: 30, respawnDelaySeconds: .3,
                minimumSafeSpawnDistanceMetres: .5, spawnProtectionSeconds: .01, targetKills: target), prefab, floor.GetComponent<BoxCollider>());
            lifecycle.BeginSession("adapter-session", new[] { new LobbyMember { ConnectionId = 1, SlotIndex = 0 }, new LobbyMember { ConnectionId = 2, SlotIndex = 1 } });
            sink = NetworkClient.localPlayer.GetComponent<NetworkShotIntentAdapter>();
            source = NetworkClient.localPlayer.GetComponent<NetworkPlayerPresentationAdapter>();
            yield return Until(() => owner.IsLifecycleBound && source.Current != null);
            if (waitForLive) yield return Until(() => sink.CanSubmit && other.CanAct && other.LifeId == 1);
            SideTarget();
        }
        private void SideTarget()
        {
            ownerServer.GetComponent<NetworkTransformReliable>().ServerTeleport(new Vector3(1000, 1, 1000), Quaternion.identity);
            otherServer.GetComponent<NetworkTransformReliable>().ServerTeleport(new Vector3(1001.2f, 1, 1000), Quaternion.identity);
            Physics.SyncTransforms();
        }
        private ShotIntent Intent(ulong sequence) => new ShotIntent { AimX = 1, AimZ = 0, LifeId = owner.LifeId,
            Sequence = sequence, Timestamp = NetworkTime.localTime };
        private void Raw(ShotIntent intent, bool foreign = false)
        {
            using (var writer = NetworkWriterPool.Get())
            {
                writer.Write(intent); var command = transport.ShotTemplate.Value; command.payload = writer.ToArraySegment();
                if (foreign) transport.SendFromVirtual(2, command);
                else NetworkClient.Send(command);
            }
        }
        private IEnumerator Capture()
        {
            sink.SubmitAim(0, 0); yield return null;
            Assert.That(transport.ShotTemplate.HasValue, Is.False, "The public sink must reject malformed client aim before sending.");
            var invalid = Intent(1); invalid.AimX = invalid.AimZ = 0;
            typeof(NetworkShotIntentAdapter).GetMethod("CmdSubmitShot", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(sink, new object[] { invalid, null });
            yield return Until(() => transport.ShotTemplate.HasValue && sink.Rejection == ShotRejection.InvalidAim);
        }
        [UnityTest] public IEnumerator ReliableShot_RejectsInvalidAimExpiredWrongLifeAndReplay_WithoutSpendingCooldown()
        {
            yield return Begin(cooldown: 1); yield return Capture();
            var expired = Intent(2); expired.Timestamp -= 1; Raw(expired);
            yield return Until(() => sink.Rejection == ShotRejection.Expired);
            var old = Intent(3); old.LifeId += 100; Raw(old); yield return null; yield return null;
            Assert.That(other.CurrentHealth, Is.EqualTo(10));
            Raw(Intent(3)); yield return Until(() => other.CurrentHealth == 9 && source.Current.DamageDealt == 1);
            Raw(Intent(3)); yield return Until(() => sink.Rejection == ShotRejection.StaleSequence);
            Assert.That(other.CurrentHealth, Is.EqualTo(9)); Assert.That(source.Current.CooldownRemainingSeconds, Is.GreaterThan(0));
            Raw(Intent(4)); yield return Until(() => sink.Rejection == ShotRejection.Cooldown);
            yield return Until(() => hud.Presentation?.Rejection == "Cooldown");
            Assert.That(hud.PresentationText, Does.Contain("Cooldown"));
        }
        [UnityTest] public IEnumerator ForeignConnectionAndRemoteReplicaCannotSubmitForOwner()
        {
            yield return Begin(); yield return Capture();
            Assert.That(other.GetComponent<NetworkShotIntentAdapter>().CanSubmit, Is.False);
            other.GetComponent<NetworkShotIntentAdapter>().SubmitAim(1, 0);
            LogAssert.Expect(LogType.Warning, new Regex("Command .*CmdSubmitShot.*without authority"));
            Raw(Intent(2), foreign: true); yield return null; yield return null;
            Assert.That(other.CurrentHealth, Is.EqualTo(10));
            Raw(Intent(2)); yield return Until(() => other.CurrentHealth == 9);
        }
        [UnityTest] public IEnumerator HeldLmbUsesFreshHorizontalAim_WithoutLegacyMeleeOrRetryCollector()
        {
            yield return Begin(cooldown: .15);
            Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None)); Assert.That(Cursor.visible, Is.True);
            menu.Resume(); Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
            var camera = Track(new GameObject("Shot aim camera")).AddComponent<Camera>();
            camera.pixelRect = new Rect(0, 0, Screen.width, Screen.height);
            camera.transform.position = new Vector3(998, 10, 996);
            camera.transform.LookAt(new Vector3(1001.2f, 1.25f, 1000));
            var muzzle = NetworkClient.localPlayer.transform.Find("ShotMuzzle");
            // First bring the native replica onto the server's pose; the target is sideways from fixed +Z melee.
            yield return Until(() => Vector3.Distance(owner.transform.position, ownerServer.transform.position) < .05);
            var held = NetworkClient.localPlayer.GetComponent<HeldFireInput>();
            held.Configure(NetworkClient.localPlayer.GetComponent<PlayerInputHandler>(), camera, muzzle, sink);
            NetworkClient.localPlayer.GetComponent<NetworkCombatInput>().enabled = true;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(Screen.width * .5f, Screen.height * .5f) });
            InputSystem.Update(); yield return null; yield return null;
            Press(mouse.leftButton); InputSystem.Update();
            yield return Until(() => other.CurrentHealth <= 8 && source.Current.DamageDealt >= 2);
            Assert.That(transport.ShotTemplate.HasValue, Is.True); Assert.That(source.Current.WeaponDamage, Is.EqualTo(1));
            Release(mouse.leftButton); InputSystem.Update(); yield return null; yield return null; var hp = other.CurrentHealth;
            yield return new WaitForSeconds(.2f); Assert.That(other.CurrentHealth, Is.EqualTo(hp));
        }
        [UnityTest] public IEnumerator NearestWallAndRangeBlockShots_WithServerDerivedOrigin()
        {
            yield return Begin();
            var wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube)); wall.layer = 6;
            wall.transform.position = new Vector3(1000.6f, 1, 1000); wall.transform.localScale = new Vector3(.1f, 3, 3);
            Physics.SyncTransforms(); sink.SubmitAim(1, 0); yield return new WaitForSeconds(.12f);
            Assert.That(other.CurrentHealth, Is.EqualTo(10));
            Object.Destroy(wall); yield return null;
            otherServer.GetComponent<NetworkTransformReliable>().ServerTeleport(new Vector3(1004, 1, 1000), Quaternion.identity);
            Physics.SyncTransforms(); sink.SubmitAim(1, 0); yield return new WaitForSeconds(.12f);
            Assert.That(other.CurrentHealth, Is.EqualTo(10));
        }
        [UnityTest] public IEnumerator LiveSnapshotsReplicateBuffHpClockScoresAndVictoryDefeat()
        {
            yield return Begin(target: 1); var context = source.Current.Context;
            Assert.That(source.Current.Health, Is.EqualTo(10)); Assert.That(source.Current.MaxHealth, Is.EqualTo(10));
            var before = source.Current.RemainingSeconds; yield return new WaitForSeconds(.1f);
            Assert.That(source.Current.RemainingSeconds, Is.LessThan(before));
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Network/NetworkLootItem.prefab"); NetworkClient.RegisterPrefab(prefab);
            var item = Track(Object.Instantiate(prefab, ownerServer.transform.position, Quaternion.identity)); NetworkServer.Spawn(item);
            NetworkClient.localPlayer.GetComponent<NetworkPlayer>().RequestPickup(item.GetComponent<NetworkIdentity>().netId);
            yield return Until(() => source.Current.IsWeaponBuffed && source.Current.WeaponDamage == 2);
            for (var n = 0; n < 5; n++)
            {
                var hp = other.CurrentHealth; sink.SubmitAim(1, 0);
                yield return Until(() => other.CurrentHealth < hp); if (n < 4) yield return new WaitForSeconds(.12f);
            }
            var remote = other.GetComponent<NetworkPlayerPresentationAdapter>();
            yield return Until(() => source.Current != null && source.Current.Result == "Victory" && remote.Current?.Result == "Defeat");
            Assert.That(source.Current.Kills, Is.EqualTo(1)); Assert.That(source.Current.DamageDealt, Is.EqualTo(10));
            Assert.That(source.Current.Rank, Is.EqualTo(1)); Assert.That(remote.Current.Deaths, Is.EqualTo(1));
            Assert.That(remote.Current.Health, Is.Zero); Assert.That(source.Current.Phase, Is.EqualTo(MatchLifecyclePhase.Ended));
            Assert.That(source.Current.ShotOutcome, Is.EqualTo(ShotOutcome.None)); Assert.That(source.Current.Rejection, Is.Empty);
            Assert.That(sink.CanSubmit, Is.False); Assert.That(hud.Presentation.Kills, Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator RespawnClearsFeedbackAndContext_RejectsOldLifeAndRestartsSequence()
        {
            yield return Begin(); yield return Capture(); var oldContext = source.Current.Context; var oldLife = owner.LifeId;
            var cleared = false; source.Changed += value => { if (value == null) cleared = true; };
            Assert.That(ownerServer.GetComponent<NetworkHealth>().TakeDamage(10), Is.True);
            yield return Until(() => source.Current?.LifeState == ParticipantLifeState.Respawning);
            Assert.That(source.Current.Rejection, Is.Empty); Assert.That(source.Current.IsAlive, Is.False);
            yield return Until(() => owner.LifeId == oldLife + 1 && sink.CanSubmit);
            Assert.That(cleared, Is.True); Assert.That(source.Current.Context, Is.Not.SameAs(oldContext));
            Assert.That(source.Current.Health, Is.EqualTo(10)); Assert.That(source.Current.CooldownRemainingSeconds, Is.Zero);
            var stale = Intent(900); stale.LifeId = oldLife; Raw(stale); yield return null; yield return null;
            Assert.That(source.Current.Rejection, Is.Empty);
            SideTarget(); sink.SubmitAim(1, 0); yield return Until(() => other.CurrentHealth == 9);
        }
        [UnityTest] public IEnumerator CountdownRejectsRawShot_AndFreshSequenceWorksAfterBarrier()
        {
            yield return Begin(waitForLive: false);
            Assert.That(sink.CanSubmit, Is.False);
            // Invoke the woven client proxy to model a peer bypassing its local permission check.
            typeof(NetworkShotIntentAdapter).GetMethod("CmdSubmitShot", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(sink, new object[] { Intent(1), null });
            yield return Until(() => transport.ShotTemplate.HasValue && sink.Rejection == ShotRejection.NotPlaying);
            Assert.That(other.CurrentHealth, Is.EqualTo(10)); Assert.That(source.Current.DamageDealt, Is.Zero);
            yield return Until(() => sink.CanSubmit);
            Raw(Intent(1)); yield return Until(() => other.CurrentHealth == 9);
        }
        [UnityTest] public IEnumerator SessionReplacementOnSameAvatar_ClearsContextFeedbackAndSequenceAtRepeatedLifeOne()
        {
            yield return Begin(); yield return Capture();
            var context = source.Current.Context; var identity = NetworkClient.localPlayer;
            lifecycle.ResetForLobby();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Network/MatchState.prefab");
            lifecycle.Configure(new GameplayConfiguration(maxHealth: 10, shotDamage: 1, shotRangeMetres: 3,
                shotCooldownSeconds: .1, countdownDurationSeconds: 0, spawnProtectionSeconds: .01), prefab, lifecycle.ArenaFloor);
            lifecycle.BeginSession("replacement-session", new[] { new LobbyMember { ConnectionId = 1, SlotIndex = 0 }, new LobbyMember { ConnectionId = 2, SlotIndex = 1 } });
            yield return Until(() => source.SessionId == "replacement-session" && sink.CanSubmit);
            Assert.That(NetworkClient.localPlayer, Is.SameAs(identity)); Assert.That(owner.LifeId, Is.EqualTo(1));
            Assert.That(source.Current.Context, Is.Not.SameAs(context)); Assert.That(source.Current.Rejection, Is.Empty);
            Assert.That(source.Current.DamageDealt, Is.Zero);
            yield return new WaitForSeconds(.03f); sink.SubmitAim(1, 0);
            yield return Until(() => other.CurrentHealth == 9);
        }
    }

    public sealed class NetworkPresentationLobbyResetTests : LobbyTestFixture
    {
        [UnityTest] public IEnumerator ReturnToLobbyClearsCapturedProviderAndHud_SecondRoundUsesNewContext()
        {
            yield return Host();
            var lifecycle = manager.GetComponent<ServerMatchLifecycleController>();
            lifecycle.Configure(new GameplayConfiguration(maxHealth: 10, shotDamage: 1, countdownDurationSeconds: .1,
                matchDurationSeconds: .5, postMatchDurationSeconds: .2),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Network/MatchState.prefab"));
            Connect(2); yield return Until(() => server.Room.Count == 2);
            ReadyRemote(2, true); view.ReadyButton.onClick.Invoke(); yield return Until(() => view.StartButton.interactable);
            manager.RequestRoomStart(); yield return Until(() => NetworkClient.localPlayer != null);
            transport.SendFromVirtual(2, new ReadyMessage()); transport.SendFromVirtual(2, new AddPlayerMessage());
            var provider = NetworkClient.localPlayer.GetComponent<NetworkPlayerPresentationAdapter>();
            yield return Until(() => provider.Current?.Phase == MatchLifecyclePhase.InProgress);
            var context = provider.Current.Context; var sawClear = false;
            provider.Changed += value => { if (value == null) sawClear = true; };
            yield return Until(() => SceneManager.GetActiveScene().name == "MainMenu" && server.Room.Phase == LobbyPhase.Waiting);
            Assert.That(provider.Current, Is.Null); Assert.That(sawClear, Is.True);
            Assert.That(NetworkClient.localPlayer == null, Is.True);
            view = Object.FindAnyObjectByType<LobbyRoomView>(); ReadyRemote(2, true); view.ReadyButton.onClick.Invoke();
            yield return Until(() => view.StartButton.interactable); manager.RequestRoomStart(); yield return Until(() => NetworkClient.localPlayer != null);
            transport.SendFromVirtual(2, new ReadyMessage()); transport.SendFromVirtual(2, new AddPlayerMessage());
            var next = NetworkClient.localPlayer.GetComponent<NetworkPlayerPresentationAdapter>();
            yield return Until(() => next.Current?.Phase == MatchLifecyclePhase.InProgress);
            Assert.That(next.Current.Context, Is.Not.SameAs(context)); Assert.That(next.Current.Rejection, Is.Empty);
            Assert.That(next.Current.Kills, Is.Zero); Assert.That(next.Current.WeaponDamage, Is.EqualTo(1));
        }
    }
}
#endif
