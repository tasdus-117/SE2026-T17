#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using SmallArena.Client;
using SmallArena.Input;
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
    // AC-008/009/010/011: local press-edge melee and observable, terminal health.
    public sealed class CombatTests : InputTestFixture
    {
        private Keyboard keyboard;
        private GameObject player;
        private GameObject bot;
        private GameObject extra;
        private CombatController combat;
        private Health health;
        private float previousCaptureDeltaTime;

        public override void Setup()
        {
            base.Setup();
            previousCaptureDeltaTime = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            keyboard = InputSystem.AddDevice<Keyboard>();
            player = OfflinePlayerFixture.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Player.prefab"),
                new Vector3(1000f, 1f, 1000f), Quaternion.identity);
            player.GetComponent<PlayerController>().enabled = false;
            combat = player.GetComponent<CombatController>();
            bot = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Bot.prefab"),
                player.transform.position + Vector3.forward * 1.2f, Quaternion.identity);
            health = bot.GetComponent<Health>();
            Assert.That(combat, Is.Not.Null);
            Assert.That(health.CurrentHealth, Is.EqualTo(10));
            Physics.SyncTransforms();
        }

        public override void TearDown()
        {
            try
            {
                if (player != null) Object.DestroyImmediate(player);
                if (bot != null) Object.DestroyImmediate(bot);
                if (extra != null) Object.DestroyImmediate(extra);
            }
            finally
            {
                Time.captureDeltaTime = previousCaptureDeltaTime;
                base.TearDown();
            }
        }

        [TestCase(10)]
        [TestCase(int.MaxValue)]
        public void LethalOrOverkillDamage_ClampsAndNotifiesDeathExactlyOnce(int damage)
        {
            var changes = 0;
            var deaths = 0;
            health.OnHealthChanged += (current, maximum) =>
            {
                changes++;
                Assert.That(current, Is.EqualTo(0));
                Assert.That(maximum, Is.EqualTo(10));
                Assert.That(health.TakeDamage(1), Is.False, "Reentrant damage after lethal HP commit must be rejected.");
            };
            health.OnDeath += () => deaths++;
            Assert.That(health.TakeDamage(damage), Is.True);
            Assert.That(health.IsDead, Is.True);
            Assert.That(health.CurrentHealth, Is.Zero);
            Assert.That(health.TakeDamage(1), Is.False);
            Assert.That(health.TakeDamage(int.MaxValue), Is.False);
            Assert.That(changes, Is.EqualTo(1));
            Assert.That(deaths, Is.EqualTo(1));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(int.MinValue)]
        public void NonPositiveDamage_IsRejectedWithoutEvents(int damage)
        {
            var changes = 0;
            var deaths = 0;
            health.OnHealthChanged += (_, __) => changes++;
            health.OnDeath += () => deaths++;
            Assert.That(health.TakeDamage(damage), Is.False);
            Assert.That(health.CurrentHealth, Is.EqualTo(10));
            Assert.That(changes + deaths, Is.Zero);
        }

        [TestCase(5, 99, 5, 5)]
        [TestCase(-1, 20, 1, 1)]
        [TestCase(5, -20, 5, 0)]
        public void SerializedHealth_IsClampedOnAwake(int maximum, int current, int expectedMax, int expectedCurrent)
        {
            extra = new GameObject("Serialized health under test");
            extra.SetActive(false);
            var configured = extra.AddComponent<Health>();
            JsonUtility.FromJsonOverwrite($"{{\"maxHealth\":{maximum},\"currentHealth\":{current}}}", configured);
            extra.SetActive(true);
            Assert.That(configured.MaxHealth, Is.EqualTo(expectedMax));
            Assert.That(configured.CurrentHealth, Is.EqualTo(expectedCurrent));
        }

        [Test]
        public void HealthObserverException_DoesNotSuppressDeathOrAllowMoreDamage()
        {
            var deaths = 0;
            health.OnHealthChanged += (_, __) => throw new InvalidOperationException("Observer failed");
            health.OnDeath += () => deaths++;
            Assert.Throws<InvalidOperationException>(() => health.TakeDamage(10));
            Assert.That(health.CurrentHealth, Is.Zero);
            Assert.That(deaths, Is.EqualTo(1));
            Assert.That(health.TakeDamage(1), Is.False);
        }

        [Test]
        public void DisableAndReenable_DoesNotResetHealth()
        {
            health.TakeDamage(3);
            bot.SetActive(false);
            bot.SetActive(true);
            Assert.That(health.CurrentHealth, Is.EqualTo(7));
        }

        [UnityTest]
        public IEnumerator FreshPunch_DealsOneDamage_HeldJDoesNotRepeat()
        {
            SetKeys(Key.J);
            yield return null;
            Assert.That(health.CurrentHealth, Is.EqualTo(9));
            for (var frame = 0; frame < 40; frame++) yield return null;
            Assert.That(combat.CanPunch, Is.True);
            Assert.That(health.CurrentHealth, Is.EqualTo(9), "Holding J past the cooldown must not punch again.");
            SetKeys();
            yield return null;
            SetKeys(Key.J);
            yield return null;
            Assert.That(health.CurrentHealth, Is.EqualTo(8));
        }

        [UnityTest]
        public IEnumerator CooldownPressWithinPointOneSeconds_IsDroppedAndNotQueued()
        {
            SetKeys(Key.J);
            yield return null;
            var firstHitTime = Time.time;
            Assert.That(health.CurrentHealth, Is.EqualTo(9));
            SetKeys();
            yield return null;
            SetKeys(Key.J);
            yield return null;
            Assert.That(Time.time - firstHitTime, Is.LessThan(0.1f));
            Assert.That(health.CurrentHealth, Is.EqualTo(9));
            Assert.That(combat.CanPunch, Is.False);
            SetKeys();
            for (var frame = 0; frame < 40; frame++) yield return null;
            Assert.That(combat.CanPunch, Is.True);
            Assert.That(health.CurrentHealth, Is.EqualTo(9), "Rejected input must not run when the cooldown expires.");
            SetKeys(Key.J);
            yield return null;
            Assert.That(health.CurrentHealth, Is.EqualTo(8));
        }

        [UnityTest]
        public IEnumerator OutOfRangePunch_MissesButStillConsumesCooldown()
        {
            bot.transform.position = player.transform.position + Vector3.forward * 3f;
            Physics.SyncTransforms();
            SetKeys(Key.J);
            yield return null;
            Assert.That(health.CurrentHealth, Is.EqualTo(10));
            Assert.That(combat.CanPunch, Is.False);
        }

        [UnityTest]
        public IEnumerator BotBehindPlayer_IsNotHit()
        {
            bot.transform.position = player.transform.position - Vector3.forward * 1.2f;
            Physics.SyncTransforms();
            SetKeys(Key.J);
            yield return null;
            Assert.That(health.CurrentHealth, Is.EqualTo(10));
        }

        [UnityTest]
        public IEnumerator Punch_IgnoresPlayerAndEnvironmentHealth()
        {
            var self = player.AddComponent<Health>();
            extra = GameObject.CreatePrimitive(PrimitiveType.Cube);
            extra.layer = LayerMask.NameToLayer("Environment");
            extra.transform.position = player.transform.position + new Vector3(0f, 1f, 0.8f);
            var environment = extra.AddComponent<Health>();
            Physics.SyncTransforms();
            SetKeys(Key.J);
            yield return null;
            Assert.That(self.CurrentHealth, Is.EqualTo(10));
            Assert.That(environment.CurrentHealth, Is.EqualTo(10));
            Assert.That(health.CurrentHealth, Is.EqualTo(9));
        }

        [UnityTest]
        public IEnumerator CompoundBotColliders_ReceiveOnlyOneDamagePerPunch()
        {
            var child = new GameObject("Bot child collider");
            child.transform.SetParent(bot.transform, false);
            child.transform.localPosition = Vector3.up * 0.6f;
            child.layer = LayerMask.NameToLayer("Bot");
            child.AddComponent<SphereCollider>().radius = 0.5f;
            Physics.SyncTransforms();
            SetKeys(Key.J);
            yield return null;
            Assert.That(health.CurrentHealth, Is.EqualTo(9));
        }

        [UnityTest]
        public IEnumerator SetDamage_ChangesSubsequentPunchWithoutResettingHealth()
        {
            health.TakeDamage(3);
            combat.SetDamage(2);
            Assert.That(combat.CurrentDamage, Is.EqualTo(2));
            Assert.That(health.CurrentHealth, Is.EqualTo(7));
            SetKeys(Key.J);
            yield return null;
            Assert.That(health.CurrentHealth, Is.EqualTo(5));
        }

        [UnityTest]
        public IEnumerator TenFreshPunches_DefeatBot_AndFurtherPunchCannotDamageIt()
        {
            var deaths = 0;
            health.OnDeath += () => deaths++;
            for (var punch = 1; punch <= 11; punch++)
            {
                SetKeys(Key.J);
                yield return null;
                Assert.That(health.CurrentHealth, Is.EqualTo(Mathf.Max(0, 10 - punch)));
                SetKeys();
                for (var frame = 0; frame < 32; frame++) yield return null;
            }
            Assert.That(deaths, Is.EqualTo(1));
            Assert.That(health.IsDead, Is.True);
        }

        [UnityTest]
        public IEnumerator ArenaInstances_InheritCombatAndHealthWiring()
        {
            yield return SceneManager.LoadSceneAsync("Arena_MVP", LoadSceneMode.Additive);
            var arena = SceneManager.GetSceneByName("Arena_MVP");
            var roots = arena.GetRootGameObjects();
            try
            {
                var arenaPlayer = roots.Single(item => item.name == "Player");
                var arenaBot = roots.Single(item => item.name == "Bot");
                var arenaCombat = arenaPlayer.GetComponent<CombatController>();
                var arenaHealth = arenaBot.GetComponent<Health>();
                Assert.That(arenaCombat.enabled && arenaHealth.enabled, Is.True);
                Assert.That(arenaBot.layer, Is.EqualTo(8));
                Assert.That(arenaHealth.CurrentHealth, Is.EqualTo(10));
                Assert.That(arenaHealth.MaxHealth, Is.EqualTo(10));
                var serialized = new SerializedObject(arenaCombat);
                Assert.That(serialized.FindProperty("inputHandler").objectReferenceValue, Is.SameAs(arenaPlayer.GetComponent<PlayerInputHandler>()));
                Assert.That(serialized.FindProperty("targetLayerMask").intValue, Is.EqualTo(1 << 8));
            }
            finally { SceneManager.UnloadSceneAsync(arena); }
        }

        private void SetKeys(params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
            InputSystem.Update();
        }
    }
}
#endif
