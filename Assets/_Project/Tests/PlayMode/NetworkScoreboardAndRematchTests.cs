#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using Mirror;
using NUnit.Framework;
using SmallArena.Client.UI;
using SmallArena.Domain;
using SmallArena.Networking;
using SmallArena.Server.Sessions;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SmallArena.Tests.PlayMode
{
    public sealed class NetworkScoreboardAndRematchTests : NetworkGameplayFixture
    {
        private ServerMatchLifecycleController lifecycle;
        private NetworkMatchController proxy;
        private NetworkHealth ServerOwner => ownerServer.GetComponent<NetworkHealth>();
        private NetworkHealth ServerOther => otherServer.GetComponent<NetworkHealth>();
        private IEnumerator Begin(int target = 0)
        {
            yield return StartWorld();
            var floor = Track(GameObject.CreatePrimitive(PrimitiveType.Cube)); floor.layer = 6;
            floor.transform.position = new Vector3(1000, -.5f, 1000); floor.transform.localScale = new Vector3(20, 1, 20);
            var markers = NetworkManager.startPositions.Where(t => t.name == "Combat start").ToArray();
            markers[0].position = new Vector3(997, 1, 997); markers[1].position = new Vector3(1003, 1, 1003);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Network/MatchState.prefab"); NetworkClient.RegisterPrefab(prefab);
            lifecycle = manager.gameObject.AddComponent<ServerMatchLifecycleController>();
            lifecycle.Configure(new GameplayConfiguration(maxHealth: 10, shotDamage: 1, shotCooldownSeconds: .04,
                countdownDurationSeconds: .2, matchDurationSeconds: 60, respawnDelaySeconds: .25,
                spawnRetryIntervalSeconds: .01, minimumSafeSpawnDistanceMetres: 1, spawnProtectionSeconds: .01, targetKills: target), prefab, floor.GetComponent<BoxCollider>());
            lifecycle.BeginSession("score-session", new[] { new LobbyMember { ConnectionId = 1, SlotIndex = 0 }, new LobbyMember { ConnectionId = 2, SlotIndex = 1 } });
            yield return Until(() => owner.CanAct && other.CanAct && owner.IsLifecycleBound && other.IsLifecycleBound);
            yield return Until(() => hud.GetComponent<MatchLifecycleHudView>().State != null);
            proxy = hud.GetComponent<MatchLifecycleHudView>().State;
            Assert.That(proxy, Is.Not.SameAs(lifecycle.ReplicatedState));
        }
        private static void Place(NetworkHealth health, Vector3 position)
        {
            var body = health.GetComponent<CharacterController>(); body.enabled = false;
            health.GetComponent<ArenaNetworkTransform>().ServerTeleport(position, Quaternion.identity);
            body.enabled = true; Physics.SyncTransforms();
        }
        private IEnumerator KillOther()
        {
            yield return Until(() => ServerOther.CanAct && other.CanAct);
            Place(ServerOwner, new Vector3(1000, 1, 1000)); Place(ServerOther, new Vector3(1000, 1, 1001.1f));
            for (var expected = 9; expected >= 0; expected--)
            {
                yield return Until(() => lifecycle.Session.ByConnection(1).Character.CanFire(NetworkTime.localTime)
                    && lifecycle.Session.ByConnection(2).Character.CanReceiveDamage(NetworkTime.localTime));
                AttackOwner(Vector3.forward);
                yield return Until(() => other.CurrentHealth == expected);
            }
        }
        [UnityTest] public IEnumerator TenActualMeleeHits_ReplicateLedgerAndKillFeedToNativeClient()
        {
            yield return Begin(); yield return KillOther();
            yield return Until(() => proxy.Scores.Any(r => r.Kills == 1) && proxy.Feed.Count == 1);
            var killer = proxy.Scores.Single(r => r.ParticipantId == owner.ParticipantId);
            var victim = proxy.Scores.Single(r => r.ParticipantId == other.ParticipantId);
            Assert.That(killer.Kills, Is.EqualTo(1)); Assert.That(killer.DamageDealt, Is.EqualTo(10)); Assert.That(victim.Deaths, Is.EqualTo(1));
            Assert.That(lifecycle.Session.ByConnection(1).DamageDealt, Is.EqualTo(10));
            yield return Until(() => hud.GetComponent<KillFeedHudView>().FeedText.Contains("Player 1 eliminated Player 2"));
        }
        [UnityTest] public IEnumerator TargetTwoKills_ReplicatesWinnerAndResult_AndLocksInput()
        {
            yield return Begin(2); yield return KillOther(); yield return Until(() => other.LifeId == 2); yield return KillOther();
            var result = hud.GetComponent<MatchResultHudView>();
            yield return Until(() => proxy.Phase == MatchLifecyclePhase.Ended && result.IsVisible);
            Assert.That(proxy.WinnerParticipantId, Is.EqualTo(owner.ParticipantId)); Assert.That(proxy.Scores[0].Rank, Is.EqualTo(1));
            Assert.That(proxy.Scores[0].Kills, Is.EqualTo(2)); Assert.That(proxy.Scores[0].DamageDealt, Is.EqualTo(20));
            Assert.That(owner.Result, Is.EqualTo(PlayerMatchResult.Victory)); Assert.That(other.Result, Is.EqualTo(PlayerMatchResult.Defeat));
            Assert.That(result.HeaderText, Is.EqualTo("VICTORY")); Assert.That(result.StandingsText, Does.Contain("20"));
            Assert.That(result.CountdownText, Does.Contain("Returning to lobby")); Assert.That(menu.CompetitiveResultVisible, Is.True);
            Assert.That(ServerOwner.CanAct, Is.False); Assert.That(ServerOther.TakeDamageFrom(ServerOwner, 1), Is.False);
            var before = ownerServer.transform.position;
            NetworkClient.localPlayer.GetComponent<NetworkMovement>().SubmitInput(Vector2.one, true); AttackOwner(Vector3.forward);
            yield return new WaitForFixedUpdate(); Assert.That(ownerServer.transform.position, Is.EqualTo(before));
            hud.gameObject.SetActive(false); yield return null; hud.gameObject.SetActive(true); yield return null;
            Assert.That(menu.CompetitiveResultVisible, Is.True, "Re-enabling HUD restores its menu/input gate.");
        }
        [UnityTest] public IEnumerator TabIsHeldOverlay_AndPauseHidesItWithoutChangingScore()
        {
            yield return Begin(); var view = hud.GetComponent<ScoreboardHudView>();
            Press(keyboard.tabKey); InputSystem.Update(); yield return null; Assert.That(view.IsVisible, Is.True);
            Assert.That(view.RowsText, Does.Contain("Player 1")); Assert.That(view.RowsText, Does.Contain("Alive"));
            Press(keyboard.escapeKey); InputSystem.Update(); yield return null;
            Release(keyboard.escapeKey); InputSystem.Update(); yield return null;
            Assert.That(menu.IsPaused, Is.True); Assert.That(view.IsVisible, Is.False);
            menu.Resume(); yield return null; Assert.That(view.IsVisible, Is.True);
            Release(keyboard.tabKey); InputSystem.Update(); yield return null; Assert.That(view.IsVisible, Is.False);
        }
        [UnityTest] public IEnumerator VoidDeathHasNoKiller_AndFeedExpiresAfterFourSeconds()
        {
            yield return Begin(); Place(ServerOwner, new Vector3(1000, -6, 1000));
            yield return Until(() => proxy.Feed.Count == 1 && proxy.Scores.Any(r => r.Deaths == 1));
            Assert.That(proxy.Feed[0].AttackerId, Is.Empty); Assert.That(proxy.Scores.Sum(r => r.Kills), Is.Zero);
            yield return Until(() => hud.GetComponent<KillFeedHudView>().FeedText.Contains("fell into the void"));
            yield return Until(() => NetworkTime.time > proxy.Feed[0].ServerTime + 4.05);
            yield return null; Assert.That(hud.GetComponent<KillFeedHudView>().FeedText, Is.Empty);
        }
        [UnityTest] public IEnumerator ClientAndForeignUnboundAttackerCannotMutateOfficialStats()
        {
            yield return Begin(); Assert.That(other.TakeDamageFrom(owner, 10), Is.False);
            var fake = Track(new GameObject("Unbound server attacker")); fake.SetActive(false);
            fake.AddComponent<NetworkIdentity>().serverOnly = true; var attacker = fake.AddComponent<NetworkHealth>();
            fake.SetActive(true); NetworkServer.Spawn(fake);
            Assert.That(ServerOther.TakeDamageFrom(attacker, 10), Is.False);
            Assert.That(ServerOther.CurrentHealth, Is.EqualTo(10)); Assert.That(lifecycle.Session.ByConnection(1).DamageDealt, Is.Zero);
        }
    }

    public sealed class NetworkRematchRoomTests : LobbyTestFixture
    {
        private ServerMatchLifecycleController lifecycle;
        private IEnumerator Begin(double duration = .8, double post = .5)
        {
            yield return Host(); lifecycle = manager.GetComponent<ServerMatchLifecycleController>();
            lifecycle.Configure(new GameplayConfiguration(maxHealth: 10, shotDamage: 1, shotCooldownSeconds: .04,
                countdownDurationSeconds: .1, matchDurationSeconds: duration, postMatchDurationSeconds: post, spawnProtectionSeconds: .01),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Network/MatchState.prefab"));
            Connect(2); yield return Until(() => server.Room.Count == 2); ReadyRemote(2, true); view.ReadyButton.onClick.Invoke();
            yield return Until(() => view.StartButton.interactable); manager.RequestRoomStart();
            yield return Until(() => NetworkClient.localPlayer != null);
            transport.SendFromVirtual(2, new ReadyMessage()); transport.SendFromVirtual(2, new AddPlayerMessage());
            yield return Until(() => lifecycle.Session.Phase == MatchLifecyclePhase.InProgress);
        }
        [UnityTest] public IEnumerator AutomaticReturnKeepsConnectionsAndSlots_ThenStartsFreshSecondMatch()
        {
            yield return Begin(); var old = lifecycle.Session; var root = manager; var id = old.SessionId;
            var connection = NetworkServer.connections[2]; var slots = server.Room.Members.Select(m => m.SlotIndex).ToArray();
            var host = NetworkClient.localPlayer.GetComponent<NetworkHealth>();
            Assert.That(connection.identity.GetComponent<NetworkHealth>().TakeDamageFrom(host, 10), Is.True);
            Assert.That(old.ByParticipant(host.ParticipantId).Kills, Is.EqualTo(1));
            Assert.That(old.ByParticipant(host.ParticipantId).DamageDealt, Is.EqualTo(10));
            Assert.That(old.ByConnection(2).Deaths, Is.EqualTo(1));
            yield return Until(() => SceneManager.GetActiveScene().name == "MainMenu" && server.Room.Phase == LobbyPhase.Waiting);
            yield return Until(() => Object.FindAnyObjectByType<LobbyRoomView>() != null && NetworkClient.ready);
            view = Object.FindAnyObjectByType<LobbyRoomView>();
            Assert.That(NetworkManager.singleton, Is.SameAs(root)); Assert.That(NetworkServer.connections[2], Is.SameAs(connection));
            Assert.That(server.Room.Members.Select(m => m.SlotIndex), Is.EqualTo(slots)); Assert.That(server.Room.Members.All(m => !m.IsReady), Is.True);
            Assert.That(view.RoomVisible, Is.True); Assert.That(view.StartButton.interactable, Is.False);
            yield return Until(() => transport.SnapshotTo(2).Phase == LobbyPhase.Waiting);
            Assert.That(transport.SnapshotTo(2).Members.All(m => !m.IsReady), Is.True, "Guest receives the reset roster through Mirror.");
            Assert.That(transport.VisibleTo(2).Count, Is.Zero, "Guest has no old round identities.");
            Assert.That(old.ActiveBindingCount, Is.Zero); Assert.That(NetworkClient.localPlayer == null, Is.True); Assert.That(lifecycle.Session, Is.Null);
            yield return Until(() => NetworkClient.spawned.Count == 0);
            Assert.That(NetworkServer.spawned.Count, Is.Zero); Assert.That(server.SessionId, Is.Not.EqualTo(id));
            ReadyRemote(2, true); view.ReadyButton.onClick.Invoke(); yield return Until(() => view.StartButton.interactable);
            manager.RequestRoomStart(); yield return Until(() => NetworkClient.localPlayer != null);
            transport.SendFromVirtual(2, new ReadyMessage()); transport.SendFromVirtual(2, new AddPlayerMessage());
            yield return Until(() => lifecycle.Session != null && lifecycle.Session.Phase == MatchLifecyclePhase.InProgress);
            Assert.That(lifecycle.Session.SessionId, Is.Not.EqualTo(id)); Assert.That(lifecycle.Session.Participants.All(p => p.Kills == 0 && p.Deaths == 0 && p.DamageDealt == 0), Is.True);
            Assert.That(lifecycle.ReplicatedState.Feed.Count, Is.Zero); Assert.That(lifecycle.ReplicatedState.WinnerParticipantId, Is.Empty);
            yield return Until(() => manager.GetComponent<SmallArena.Server.Combat.ServerLootRespawnController>().MapItem != null);
        }
        [UnityTest] public IEnumerator OnlyHostCanReturnEarly_AndOnlyAfterMatchEnded()
        {
            yield return Begin(.8, 10); manager.RequestReturnToLobby(); transport.SendFromVirtual(2, new LobbyReturnRequest());
            yield return null; Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Arena_MVP"));
            yield return Until(() => lifecycle.Session.Phase == MatchLifecyclePhase.Ended);
            transport.SendFromVirtual(2, new LobbyReturnRequest()); yield return null; yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Arena_MVP"));
            var result = Object.FindAnyObjectByType<MatchResultHudView>(); yield return Until(() => result.IsVisible);
            result.ReturnButton.onClick.Invoke();
            yield return Until(() => SceneManager.GetActiveScene().name == "MainMenu" && server.Room.Phase == LobbyPhase.Waiting);
            Assert.That(NetworkServer.connections.Count, Is.EqualTo(2));
        }
    }
}
#endif
