#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using SmallArena.Client;
using SmallArena.Pickups;
using SmallArena.Player;
using SmallArena.Server.Combat;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SmallArena.Tests.PlayMode
{
    // AC-012/013: fresh-E collection, single-use state and subsequent two-HP punches.
    public sealed class WeaponPickupTests : InputTestFixture
    {
        private Keyboard keyboard;
        private GameObject player;
        private GameObject pickupObject;
        private GameObject bot;
        private CombatController combat;
        private WeaponPickup pickup;
        private Health botHealth;
        private float previousCaptureDeltaTime;
        private bool previousQueriesHitTriggers;

        public override void Setup()
        {
            base.Setup();
            previousCaptureDeltaTime = Time.captureDeltaTime;
            previousQueriesHitTriggers = Physics.queriesHitTriggers;
            Time.captureDeltaTime = 1f / 60f;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            keyboard = InputSystem.AddDevice<Keyboard>();
            player = Spawn("Player", new Vector3(1000f, 1f, 1000f));
            player.GetComponent<PlayerController>().enabled = false;
            combat = player.GetComponent<CombatController>();
            pickupObject = Spawn("WeaponPickup", player.transform.position + Vector3.right);
            pickup = pickupObject.GetComponent<WeaponPickup>();
            bot = Spawn("Bot", player.transform.position + Vector3.forward * 1.2f);
            botHealth = bot.GetComponent<Health>();
            Assert.That(combat.CurrentDamage, Is.EqualTo(1));
            Physics.SyncTransforms();
        }

        public override void TearDown()
        {
            try
            {
                if (player != null) Object.DestroyImmediate(player);
                if (pickupObject != null) Object.DestroyImmediate(pickupObject);
                if (bot != null) Object.DestroyImmediate(bot);
            }
            finally
            {
                Time.captureDeltaTime = previousCaptureDeltaTime;
                Physics.queriesHitTriggers = previousQueriesHitTriggers;
                base.TearDown();
            }
        }

        [UnityTest]
        public IEnumerator InRangeFreshE_CollectsAndBuffs_WithoutAutomaticProximityCollection()
        {
            yield return null;
            Assert.That(pickup.IsCollected, Is.False);
            Assert.That(combat.CurrentDamage, Is.EqualTo(1));
            SetKeys(Key.E);
            yield return null;
            AssertCollected();
        }

        [UnityTest]
        public IEnumerator OutOfRangeE_IsDropped_HoldingWhileEnteringRangeDoesNotCollect()
        {
            pickupObject.transform.position = player.transform.position + Vector3.right * 3f;
            Physics.SyncTransforms();
            SetKeys(Key.E);
            yield return null;
            Assert.That(pickup.IsCollected, Is.False);
            Assert.That(combat.CurrentDamage, Is.EqualTo(1));
            pickupObject.transform.position = player.transform.position + Vector3.right;
            Physics.SyncTransforms();
            for (var frame = 0; frame < 5; frame++) yield return null;
            Assert.That(pickup.IsCollected, Is.False, "Holding an out-of-range press must not queue collection.");
            SetKeys();
            yield return null;
            SetKeys(Key.E);
            yield return null;
            AssertCollected();
        }

        [TestCase(0f, true)]
        [TestCase(1.5f, true)]
        [TestCase(1.51f, false)]
        public void TransformDistanceBoundary_IsInclusiveAndIndependentOfColliderOverlap(float distance, bool expected)
        {
            pickupObject.transform.position = player.transform.position + Vector3.right * distance;
            Physics.SyncTransforms();
            Assert.That(pickup.InteractionRadius, Is.EqualTo(1.5f));
            Assert.That(pickup.CanCollect(player.transform.position), Is.EqualTo(expected));
            Assert.That(pickup.TryCollect(combat, player.transform.position), Is.EqualTo(expected));
            Assert.That(combat.CurrentDamage, Is.EqualTo(expected ? 2 : 1));
        }

        [UnityTest]
        public IEnumerator ColliderOverlapBeyondRadius_DoesNotBypassTransformDistance()
        {
            // The 0.6m-wide pickup collider overlaps the query sphere at 1.6m.
            pickupObject.transform.position = player.transform.position + Vector3.right * 1.6f;
            Physics.SyncTransforms();
            Assert.That(Physics.OverlapSphere(player.transform.position, 1.5f, 1 << 9, QueryTriggerInteraction.Collide),
                Does.Contain(pickupObject.GetComponent<Collider>()));
            SetKeys(Key.E);
            yield return null;
            Assert.That(pickup.IsCollected, Is.False);
            Assert.That(combat.CurrentDamage, Is.EqualTo(1));
        }

        [Test]
        public void VerticalDistanceCountsTowardCollectionRange()
        {
            pickupObject.transform.position = player.transform.position + Vector3.up * 1.6f;
            Assert.That(pickup.CanCollect(player.transform.position), Is.False);
            Assert.That(pickup.TryCollect(combat, player.transform.position), Is.False);
        }

        [Test]
        public void NullCombat_IsRejectedWithoutChangingPickupOrPublishingCollection()
        {
            var notifications = 0;
            pickup.OnCollected += _ => notifications++;
            Assert.That(pickup.TryCollect(null, player.transform.position), Is.False);
            Assert.That(pickup.IsCollected, Is.False);
            Assert.That(pickupObject.GetComponent<Collider>().enabled, Is.True);
            Assert.That(pickupObject.GetComponent<Renderer>().enabled, Is.True);
            Assert.That(notifications, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Collection_CommitsBeforeEvent_RejectsReentryAndLaterE_AndPersistsAcrossReenable()
        {
            var notifications = 0;
            pickup.OnCollected += collected =>
            {
                notifications++;
                Assert.That(collected, Is.SameAs(pickup));
                AssertCollected();
                Assert.That(collected.TryCollect(combat, player.transform.position), Is.False);
            };
            Assert.That(pickup.TryCollect(combat, player.transform.position), Is.True);
            Assert.That(pickup.TryCollect(combat, player.transform.position), Is.False);
            Assert.That(pickup.CanCollect(player.transform.position), Is.False);
            pickupObject.SetActive(false);
            pickupObject.SetActive(true);
            player.SetActive(false);
            player.SetActive(true);
            for (var attempt = 0; attempt < 3; attempt++)
            {
                SetKeys(Key.E);
                yield return null;
                SetKeys();
                yield return null;
            }
            AssertCollected();
            Assert.That(notifications, Is.EqualTo(1));
            Assert.That(pickupObject.activeSelf, Is.True, "Collection disables visuals and collision without destroying the observable state.");
        }

        [UnityTest]
        public IEnumerator Collection_PermitsTriggerQueriesEvenWhenGlobalTriggerQueriesAreDisabled()
        {
            Physics.queriesHitTriggers = false;
            SetKeys(Key.E);
            yield return null;
            AssertCollected();
        }

        [UnityTest]
        public IEnumerator OnlyPickupLayer_IsConsideredForEInteraction()
        {
            foreach (var wrongLayer in new[] { 6, 7 })
            {
                pickupObject.layer = wrongLayer;
                Physics.SyncTransforms();
                SetKeys(Key.E);
                yield return null;
                Assert.That(pickup.IsCollected, Is.False);
                Assert.That(combat.CurrentDamage, Is.EqualTo(1));
                SetKeys();
                yield return null;
            }
            pickupObject.layer = 9;
            Physics.SyncTransforms();
            SetKeys(Key.E);
            yield return null;
            AssertCollected();
        }

        [UnityTest]
        public IEnumerator BuffedPunch_ReducesFreshBotFromTenToEight()
        {
            SetKeys(Key.E);
            yield return null;
            AssertCollected();
            Assert.That(botHealth.CurrentHealth, Is.EqualTo(10));
            SetKeys(Key.J);
            yield return null;
            Assert.That(botHealth.CurrentHealth, Is.EqualTo(8));
        }

        [UnityTest]
        public IEnumerator PickupDuringPunchCooldown_BuffsWithoutResettingBotHealthOrCooldown()
        {
            SetKeys(Key.J);
            yield return null;
            Assert.That(botHealth.CurrentHealth, Is.EqualTo(9));
            Assert.That(combat.CanPunch, Is.False);
            SetKeys(Key.E);
            yield return null;
            AssertCollected();
            Assert.That(botHealth.CurrentHealth, Is.EqualTo(9));
            Assert.That(combat.CanPunch, Is.False);
            for (var frame = 0; frame < 32; frame++) yield return null;
            SetKeys(Key.J);
            yield return null;
            Assert.That(botHealth.CurrentHealth, Is.EqualTo(7));
        }

        [UnityTest]
        public IEnumerator SameFrameEAndJ_UsesBuffForThatPunch()
        {
            SetKeys(Key.E, Key.J);
            yield return null;
            AssertCollected();
            Assert.That(botHealth.CurrentHealth, Is.EqualTo(8));
        }

        [UnityTest]
        public IEnumerator ArenaPickup_InheritsReferencesAndLayer_AndCollectsAtFloorHeight()
        {
            yield return SceneManager.LoadSceneAsync("Arena_MVP", LoadSceneMode.Additive);
            var arena = SceneManager.GetSceneByName("Arena_MVP");
            yield return null; // Let offline startup complete before teleporting the integration-test player.
            try
            {
                var roots = arena.GetRootGameObjects();
                var arenaPlayer = roots.Single(item => item.name == "Player");
                var arenaPickupObject = roots.Single(item => item.name == "WeaponPickup");
                var arenaPickup = arenaPickupObject.GetComponent<WeaponPickup>();
                var arenaCombat = arenaPlayer.GetComponent<CombatController>();
                Assert.That(arenaPickupObject.layer, Is.EqualTo(9));
                Assert.That(arenaPickup.enabled, Is.True);
                Assert.That(arenaPickup.IsCollected, Is.False);
                var serialized = new SerializedObject(arenaPickup);
                Assert.That(serialized.FindProperty("pickupCollider").objectReferenceValue, Is.SameAs(arenaPickupObject.GetComponent<Collider>()));
                Assert.That(serialized.FindProperty("pickupRenderer").objectReferenceValue, Is.SameAs(arenaPickupObject.GetComponent<Renderer>()));
                arenaPlayer.GetComponent<PlayerController>().enabled = false;
                var character = arenaPlayer.GetComponent<CharacterController>();
                character.enabled = false;
                arenaPlayer.transform.position = arenaPickupObject.transform.position + new Vector3(0f, 0.7f, -1f);
                character.enabled = true;
                Physics.SyncTransforms();
                SetKeys(Key.E);
                yield return null;
                Assert.That(arenaPickup.IsCollected, Is.True);
                Assert.That(arenaCombat.CurrentDamage, Is.EqualTo(2));
                Assert.That(arenaPickupObject.GetComponent<Collider>().enabled, Is.False);
                Assert.That(arenaPickupObject.GetComponent<Renderer>().enabled, Is.False);
            }
            finally { SceneManager.UnloadSceneAsync(arena); }
        }

        private void AssertCollected()
        {
            Assert.That(pickup.IsCollected, Is.True);
            Assert.That(combat.CurrentDamage, Is.EqualTo(2));
            Assert.That(pickupObject.GetComponent<Collider>().enabled, Is.False);
            Assert.That(pickupObject.GetComponent<Renderer>().enabled, Is.False);
        }

        private static GameObject Spawn(string name, Vector3 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Prefabs/{(name == "WeaponPickup" ? "Combat" : "Characters")}/{name}.prefab");
            return name == "Player" ? OfflinePlayerFixture.Instantiate(prefab, position, Quaternion.identity)
                : Object.Instantiate(prefab, position, Quaternion.identity);
        }

        private void SetKeys(params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
            InputSystem.Update();
        }
    }
}
#endif
