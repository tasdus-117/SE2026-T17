#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using Mirror;
using NUnit.Framework;
using SmallArena.Domain;
using SmallArena.Networking;
using SmallArena.Server.Sessions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SmallArena.Tests.PlayMode
{
    public sealed class NetworkRaycastOcclusionAndHudTests : NetworkGameplayFixture
    {
        private NetworkShotIntentAdapter shots;
        private NetworkPlayerPresentationAdapter source;
        private ServerMatchLifecycleController lifecycle;
        private IEnumerator Begin(double range = 3, double cooldown = .1, int damage = 1)
        {
            yield return StartWorld();
            var floor = Track(GameObject.CreatePrimitive(PrimitiveType.Cube)); floor.layer = 6;
            floor.transform.position = new Vector3(1000, -.5f, 1000); floor.transform.localScale = new Vector3(20, 1, 20);
            var markers = NetworkManager.startPositions.Where(t => t.name == "Combat start").ToArray();
            markers[0].position = new Vector3(997, 1, 997); markers[1].position = new Vector3(1003, 1, 1003);
            var state = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Network/MatchState.prefab"); NetworkClient.RegisterPrefab(state);
            lifecycle = manager.gameObject.AddComponent<ServerMatchLifecycleController>();
            lifecycle.Configure(new GameplayConfiguration(maxHealth: 10, shotDamage: damage, shotRangeMetres: range, shotCooldownSeconds: cooldown,
                countdownDurationSeconds: .05, spawnProtectionSeconds: .01, respawnDelaySeconds: .2), state, floor.GetComponent<BoxCollider>());
            lifecycle.BeginSession("occlusion", new[] { new LobbyMember { ConnectionId = 1, SlotIndex = 0 }, new LobbyMember { ConnectionId = 2, SlotIndex = 1 } });
            shots = NetworkClient.localPlayer.GetComponent<NetworkShotIntentAdapter>();
            source = NetworkClient.localPlayer.GetComponent<NetworkPlayerPresentationAdapter>();
            yield return Until(() => shots.CanSubmit && other.CanAct);
            ownerServer.GetComponent<NetworkTransformReliable>().ServerTeleport(new Vector3(1000, 1, 1000), Quaternion.identity);
            otherServer.GetComponent<NetworkTransformReliable>().ServerTeleport(new Vector3(1001.2f, 1, 1000), Quaternion.identity);
            Physics.SyncTransforms();
        }
        private GameObject Cover(float x)
        {
            var wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube)); wall.layer = 6;
            wall.transform.position = new Vector3(x, 1, 1000); wall.transform.localScale = new Vector3(.1f, 3, 3);
            Physics.SyncTransforms(); return wall;
        }
        [UnityTest] public IEnumerator CoverBlocksOverMirror_ThenClearRayDamagesAndReportsHitOnlyToShooter()
        {
            yield return Begin(); var cover = Cover(1000.6f); shots.SubmitAim(1, 0);
            yield return Until(() => source.Current?.ShotOutcome == ShotOutcome.Blocked);
            Assert.That(other.CurrentHealth, Is.EqualTo(10)); Assert.That(otherServer.GetComponent<NetworkHealth>().CurrentHealth, Is.EqualTo(10));
            Assert.That(source.Current.DamageDealt, Is.Zero); Assert.That(hud.PresentationText, Is.EqualTo("Shot blocked by cover"));
            Object.Destroy(cover); yield return null; yield return new WaitForSeconds(.12f); Physics.SyncTransforms();
            shots.SubmitAim(1, 0); yield return Until(() => other.CurrentHealth == 9 && source.Current?.ShotOutcome == ShotOutcome.Hit);
            Assert.That(source.Current.DamageDealt, Is.EqualTo(1)); Assert.That(hud.PresentationText, Is.EqualTo("Hit"));
            Assert.That(other.GetComponent<NetworkShotIntentAdapter>().Outcome, Is.EqualTo(ShotOutcome.None));
            Assert.That(hud.PresentationText, Does.Not.Contain("HP ")); Assert.That(hud.PresentationText, Does.Not.Contain("InProgress"));
            var label = hud.GetComponentsInChildren<TextMeshProUGUI>(true).Single(t => t.name == "PlayerPresentation");
            Assert.That(label.rectTransform.anchorMin, Is.EqualTo(Vector2.zero)); Assert.That(label.rectTransform.anchorMax, Is.EqualTo(Vector2.zero));
            Assert.That(label.rectTransform.anchoredPosition.y, Is.GreaterThan(0)); Assert.That(label.raycastTarget, Is.False);
        }
        [UnityTest] public IEnumerator TargetBeforeCoverRegistersHit_OnlyNearestTargetDamaged()
        {
            yield return Begin(); Cover(1002); shots.SubmitAim(1, 0);
            yield return Until(() => other.CurrentHealth == 9 && source.Current?.ShotOutcome == ShotOutcome.Hit);
            Assert.That(owner.CurrentHealth, Is.EqualTo(10));
        }
        [UnityTest] public IEnumerator OriginInsideCoverIsBlocked_EvenWhenRaycastCannotSeeContainingCollider()
        {
            yield return Begin(); Cover(1000); shots.SubmitAim(1, 0);
            yield return Until(() => source.Current?.ShotOutcome == ShotOutcome.Blocked);
            Assert.That(other.CurrentHealth, Is.EqualTo(10));
        }
        [UnityTest] public IEnumerator ConfiguredRangeAndCooldownApplyToMisses_AndFeedbackExpires()
        {
            yield return Begin(range: .5, cooldown: 1); shots.SubmitAim(1, 0);
            yield return Until(() => source.Current?.ShotOutcome == ShotOutcome.Missed);
            shots.SubmitAim(1, 0); yield return Until(() => source.Current?.Rejection == "Cooldown");
            Assert.That(other.CurrentHealth, Is.EqualTo(10));
            yield return new WaitForSeconds(1.6f);
            Assert.That(source.Current.ShotOutcome, Is.EqualTo(ShotOutcome.None)); Assert.That(source.Current.Rejection, Is.Empty);
            Assert.That(hud.PresentationText, Is.Empty);
        }
        [UnityTest] public IEnumerator ConfiguredBaseDamageAndRespawnClearFeedbackAndPreserveDataBinding()
        {
            yield return Begin(damage: 2); shots.SubmitAim(1, 0);
            yield return Until(() => other.CurrentHealth == 8 && source.Current?.ShotOutcome == ShotOutcome.Hit);
            Assert.That(source.Current.DamageDealt, Is.EqualTo(2)); var oldLife = owner.LifeId;
            ownerServer.GetComponent<NetworkHealth>().TakeDamage(10);
            yield return Until(() => source.Current?.LifeState == ParticipantLifeState.Respawning);
            Assert.That(source.Current.ShotOutcome, Is.EqualTo(ShotOutcome.None)); Assert.That(hud.PresentationText, Is.Empty);
            yield return Until(() => owner.LifeId > oldLife && shots.CanSubmit);
            Assert.That(source.Current.ShotOutcome, Is.EqualTo(ShotOutcome.None)); Assert.That(source.Current.Health, Is.EqualTo(10));
            lifecycle.ResetForLobby(); yield return Until(() => source.Current == null && hud.Presentation == null);
            Assert.That(shots.Outcome, Is.EqualTo(ShotOutcome.None)); Assert.That(hud.PresentationText, Is.Empty);
        }
    }

    public sealed class NetworkRaycastArenaLayoutTests : LobbyTestFixture
    {
        [UnityTest] public IEnumerator SavedArenaGeneratedCoverBlocksAuthoritativeShotAfterRealLobbyTransition()
        {
            yield return Host();
            var lifecycle = manager.GetComponent<ServerMatchLifecycleController>();
            lifecycle.Configure(new GameplayConfiguration(maxHealth: 10, shotDamage: 1, shotRangeMetres: 10,
                shotCooldownSeconds: .1, countdownDurationSeconds: .1, spawnProtectionSeconds: .01),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Network/MatchState.prefab"));
            Connect(2); yield return Until(() => server.Room.Count == 2);
            ReadyRemote(2, true); view.ReadyButton.onClick.Invoke(); yield return Until(() => view.StartButton.interactable);
            manager.RequestRoomStart(); yield return Until(() => NetworkClient.localPlayer != null);
            transport.SendFromVirtual(2, new ReadyMessage()); transport.SendFromVirtual(2, new AddPlayerMessage());
            var shots = NetworkClient.localPlayer.GetComponent<NetworkShotIntentAdapter>();
            var source = NetworkClient.localPlayer.GetComponent<NetworkPlayerPresentationAdapter>();
            yield return Until(() => shots.CanSubmit && NetworkServer.connections[2].identity != null);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Arena_MVP"));
            var arena = Object.FindAnyObjectByType<ArenaObstacles>();
            Assert.That(arena.Layout.Count, Is.GreaterThan(0));
            var bounds = arena.Layout[0].Bounds;
            var target = NetworkServer.connections[2].identity.GetComponent<NetworkHealth>();
            foreach (var actor in new[] { NetworkClient.localPlayer, target.netIdentity })
                actor.GetComponent<SmallArena.Server.Simulation.ServerPlayerMovement>().enabled = false;
            // Pass through the centre of the generated shape, including spheres/capsules.
            var y = bounds.center.y - NetworkCombat.ShotMuzzleOffset.y;
            NetworkClient.localPlayer.GetComponent<NetworkTransformReliable>().ServerTeleport(
                new Vector3(bounds.min.x - .7f, y, bounds.center.z), Quaternion.identity);
            target.GetComponent<NetworkTransformReliable>().ServerTeleport(
                new Vector3(bounds.max.x + .7f, y, bounds.center.z), Quaternion.identity);
            Physics.SyncTransforms(); shots.SubmitAim(1, 0);
            yield return Until(() => source.Current?.ShotOutcome == ShotOutcome.Blocked);
            Assert.That(target.CurrentHealth, Is.EqualTo(10)); Assert.That(source.Current.DamageDealt, Is.Zero);
            Assert.That(Object.FindAnyObjectByType<SmallArena.Client.UI.NetworkHealthHUD>().PresentationText,
                Is.EqualTo("Shot blocked by cover"));
        }
    }
}
#endif
