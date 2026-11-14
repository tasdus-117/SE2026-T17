#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
using Mirror;
using NUnit.Framework;
using SmallArena.Client;
using SmallArena.Networking;
using SmallArena.Player;
using SmallArena.Server.Combat;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SmallArena.Tests.PlayMode
{
    public sealed class MeleeReliabilityAndDisplayTests : NetworkGameplayFixture
    {
        private static readonly FieldInfo Deadline = typeof(NetworkCombat).GetField("nextAttackTime", BindingFlags.NonPublic | BindingFlags.Instance);
        private void PlaceTarget(float distance, float angle)
        {
            otherServer.transform.position = ownerServer.transform.position
                + Quaternion.Euler(0, angle, 0) * Vector3.forward * distance;
            Physics.SyncTransforms();
        }

        [UnityTest]
        public IEnumerator ServerCooldown_RejectsRawCommandsUntilOneSecond_AndAcceptsAtOnePointZeroOne()
        {
            yield return StartWorld();
            Arrange(true); AttackVirtual(Vector3.forward);
            yield return Until(() => owner.CurrentHealth == 9);
            var combat = otherServer.GetComponent<NetworkCombat>();
            var deadline = (double)Deadline.GetValue(combat);
            Assert.That(NetworkCombat.Cooldown, Is.EqualTo(1f));
            AttackVirtual(Vector3.forward); AttackVirtual(Vector3.forward);
            yield return new WaitForSecondsRealtime(0.6f);
            AttackVirtual(Vector3.forward);
            for (var i = 0; i < 4; i++) yield return null;
            Assert.That(ownerServer.GetComponent<NetworkHealth>().CurrentHealth, Is.EqualTo(9));
            Assert.That((double)Deadline.GetValue(combat), Is.EqualTo(deadline), "Rejected attacks cannot extend or reset cooldown.");
            yield return Until(() => NetworkTime.localTime >= deadline + 0.01);
            AttackVirtual(Vector3.forward);
            yield return Until(() => owner.CurrentHealth == 8);
        }

        private IEnumerator HitAt(float range, float angle, float height)
        {
            yield return StartWorld();
            PlaceTarget(range, angle);
            otherServer.transform.position += Vector3.up * height;
            Physics.SyncTransforms();
            var body = otherServer.GetComponent<CharacterController>();
            var center = MeleeHitGeometry.Center(ownerServer.transform, MeleeHitGeometry.Offset);
            Assert.That(MeleeHitGeometry.InReach(ownerServer.transform.position, Vector3.forward, body.bounds.center), Is.True,
                $"Reach/cone: owner={ownerServer.transform.position:F4}, target={body.bounds.center:F4}");
            Assert.That(Physics.OverlapSphere(center, MeleeHitGeometry.CandidateRadius(MeleeHitGeometry.Radius), (1 << 7) | (1 << 8), QueryTriggerInteraction.Ignore),
                Has.Member(body), $"Query: center={center:F4}, target={body.bounds}, enabled={body.enabled}");
            Assert.That(MeleeHitGeometry.TouchesSphere(body, center, MeleeHitGeometry.Radius), Is.True);
            Assert.That(Physics.Linecast(center - Vector3.forward * MeleeHitGeometry.Offset.z, body.bounds.center,
                out var blocker, 1 << 6, QueryTriggerInteraction.Ignore), Is.False, $"Blocked by {blocker.collider}");
            AttackOwner(Vector3.forward);
            yield return Until(() => other.CurrentHealth == 9);
            Assert.That(owner.CurrentHealth, Is.EqualTo(10));
        }
        [UnityTest] public IEnumerator OffsetThirtyDegreesAtOnePointTwoMeters_Hits() => HitAt(1.2f, 30, 0);
        [UnityTest] public IEnumerator OffsetSixtyDegreesAtMaxRange_WithHeightDifference_Hits() => HitAt(1.8f, 60, 0.25f);
        [UnityTest] public IEnumerator LeftSideOffset_Hits() => HitAt(1.5f, -60, 0);

        [UnityTest]
        public IEnumerator WideQuery_StillRejectsRearOutsideConeAndBeyondReach()
        {
            yield return StartWorld();
            foreach (var pose in new[] { new Vector2(1.2f, 100), new Vector2(1.2f, 180), new Vector2(2f, 0) })
            {
                PlaceTarget(pose.x, pose.y); AttackOwner(Vector3.forward);
                for (var i = 0; i < 6; i++) yield return null;
                Assert.That(other.CurrentHealth, Is.EqualTo(10));
                yield return new WaitForSecondsRealtime(1.05f);
            }
        }

        [UnityTest]
        public IEnumerator NetworkBotLayer_IsDamagedOnlyByServer_WithoutPlayerLayerRequirement()
        {
            yield return StartWorld();
            PlaceTarget(4, 0);
            var bot = Track(new GameObject("Server bot")); bot.SetActive(false); bot.layer = 8;
            bot.transform.position = ownerServer.transform.position + Quaternion.Euler(0, 30, 0) * Vector3.forward * 1.2f;
            var collider = bot.AddComponent<CapsuleCollider>(); collider.height = 2; collider.radius = 0.5f; collider.center = Vector3.up;
            bot.AddComponent<NetworkIdentity>().serverOnly = true;
            var health = bot.AddComponent<NetworkHealth>(); bot.SetActive(true); NetworkServer.Spawn(bot);
            Physics.SyncTransforms(); AttackOwner(Vector3.forward);
            yield return Until(() => health.CurrentHealth == 9);
            Assert.That(health.isServer, Is.True);
        }

        [UnityTest]
        public IEnumerator OfflineBotPrefab_ThirtyDegreeOffset_ReceivesOneDamage()
        {
            var player = Track(OfflinePlayerFixture.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/Characters/Player.prefab"), new Vector3(1000, 1, 1000), Quaternion.identity));
            player.GetComponent<PlayerController>().enabled = false;
            var bot = Track(Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/Characters/Bot.prefab"), player.transform.position
                + Quaternion.Euler(0, 30, 0) * Vector3.forward * 1.2f, Quaternion.identity));
            Physics.SyncTransforms();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.J)); InputSystem.Update(); yield return null;
            Assert.That(bot.GetComponent<Health>().CurrentHealth, Is.EqualTo(9));
            Assert.That(player.GetComponent<CombatController>().CanPunch, Is.False);
        }

        [UnityTest]
        public IEnumerator F11_RequestsHalfNativeWindow_ThenNativeBorderless_WithoutChangingCursor()
        {
            var display = Object.FindAnyObjectByType<ScreenDisplayController>();
            if (display == null) display = Track(new GameObject("Resolution test")).AddComponent<ScreenDisplayController>();
            if (!display.LastRequestedFullscreen) display.ToggleFullscreen();
            var visible = Cursor.visible; var locked = Cursor.lockState;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F11)); yield return null;
            Assert.That(display.LastRequestedMode, Is.EqualTo(FullScreenMode.Windowed));
            Assert.That(display.LastRequestedResolution, Is.EqualTo(new Vector2Int(
                Mathf.Max(1, Display.main.systemWidth / 2), Mathf.Max(1, Display.main.systemHeight / 2))));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F11)); yield return null;
            Assert.That(display.LastRequestedMode, Is.EqualTo(FullScreenMode.FullScreenWindow));
            Assert.That(display.LastRequestedResolution, Is.EqualTo(new Vector2Int(
                Mathf.Max(1, Display.main.systemWidth), Mathf.Max(1, Display.main.systemHeight))));
            Assert.That(Cursor.visible, Is.EqualTo(visible)); Assert.That(Cursor.lockState, Is.EqualTo(locked));
        }
    }
}
#endif
