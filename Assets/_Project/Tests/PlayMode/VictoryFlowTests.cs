#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SmallArena.Client;
using SmallArena.Client.Presentation;
using SmallArena.Client.UI;
using SmallArena.Core;
using SmallArena.Domain;
using SmallArena.Input;
using SmallArena.Pickups;
using SmallArena.Player;
using SmallArena.Server.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SmallArena.Tests.PlayMode
{
    // AC-001/011 and TC-017/018/029: real offline startup, observable defeat and terminal match behavior.
    public sealed class VictoryFlowTests : InputTestFixture
    {
        private Scene arena;
        private Keyboard keyboard;
        private GameObject player;
        private GameManager manager;
        private Health health;
        private CombatController combat;
        private WeaponPickup pickup;
        private MatchUI ui;
        private TextMeshProUGUI hpText;
        private TextMeshProUGUI damageText;
        private GameObject banner;
        private float previousCaptureDeltaTime;

        public override void Setup()
        {
            base.Setup();
            previousCaptureDeltaTime = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            keyboard = InputSystem.AddDevice<Keyboard>();
        }

        private IEnumerator LoadArena()
        {
            yield return SceneManager.LoadSceneAsync("Arena_MVP", LoadSceneMode.Additive);
            arena = SceneManager.GetSceneByName("Arena_MVP");
            yield return null;
            var roots = arena.GetRootGameObjects();
            player = roots.Single(item => item.name == "Player");
            manager = roots.Single(item => item.name == "_GameManager").GetComponent<GameManager>();
            health = roots.Single(item => item.name == "Bot").GetComponent<Health>();
            combat = player.GetComponent<CombatController>();
            pickup = roots.Single(item => item.name == "WeaponPickup").GetComponent<WeaponPickup>();
            ui = roots.Single(item => item.name == "MatchCanvas").GetComponent<MatchUI>();
            var texts = ui.GetComponentsInChildren<TextMeshProUGUI>(true);
            hpText = texts.Single(item => item.name == "BotHpText");
            damageText = texts.Single(item => item.name == "DamageText");
            banner = texts.Single(item => item.name == "VictoryText").transform.parent.gameObject;
        }

        [UnityTearDown]
        public IEnumerator UnloadArena()
        {
            if (arena.IsValid() && arena.isLoaded) yield return SceneManager.UnloadSceneAsync(arena);
        }

        public override void TearDown()
        {
            Time.captureDeltaTime = previousCaptureDeltaTime;
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator Startup_EntersPlaying_WithInitialHudAndHiddenVictory()
        {
            yield return LoadArena();
            Assert.That(manager.CurrentState, Is.EqualTo(MatchState.Playing));
            Assert.That(ui.IsReady && ui.IsBoundTo(manager, health, combat), Is.True);
            Assert.That(hpText.text, Is.EqualTo("Bot HP: 10 / 10"));
            Assert.That(damageText.text, Is.EqualTo("Damage: 1 HP (Unarmed)"));
            Assert.That(banner.activeSelf, Is.False);
            Assert.That(hpText.font, Is.Not.Null);
            Assert.That(ui.GetComponent<Canvas>().renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            var scaler = ui.GetComponent<CanvasScaler>();
            Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1920f, 1080f)));
            Assert.That(ui.GetComponent<Canvas>().scaleFactor, Is.GreaterThan(0f));
            Assert.That(ui.GetComponent<RectTransform>().rect.width, Is.GreaterThan(0f));
            Assert.That(ui.transform.lossyScale.x, Is.GreaterThan(0f));
        }

        [UnityTest]
        public IEnumerator TenFreshPunches_ReachVictoryOnce_AndShowBannerImmediately()
        {
            yield return LoadArena();
            MovePlayerTo(health.transform.position - Vector3.forward * 1.2f);
            var victories = 0;
            var deaths = 0;
            manager.OnMatchStateChanged += state => { if (state == MatchState.Victory) victories++; };
            health.OnDeath += () => deaths++;
            for (var punch = 1; punch <= 10; punch++)
            {
                SetKeys(Key.J);
                yield return null;
                Assert.That(health.CurrentHealth, Is.EqualTo(10 - punch));
                Assert.That(hpText.text, Is.EqualTo($"Bot HP: {10 - punch} / 10"));
                SetKeys();
                for (var frame = 0; frame < 64; frame++) yield return null;
            }
            Assert.That(manager.CurrentState, Is.EqualTo(MatchState.Victory));
            Assert.That(banner.activeInHierarchy, Is.True);
            Assert.That(banner.GetComponentInChildren<TextMeshProUGUI>().text, Is.EqualTo("VICTORY!"));
            manager.ReportBotDefeated();
            manager.ReportBotDefeated();
            Assert.That(victories, Is.EqualTo(1));
            Assert.That(deaths, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator LethalHealthEvent_UpdatesHudAndVictorySynchronously()
        {
            yield return LoadArena();
            Assert.That(health.TakeDamage(10), Is.True);
            Assert.That(hpText.text, Is.EqualTo("Bot HP: 0 / 10"));
            Assert.That(manager.CurrentState, Is.EqualTo(MatchState.Victory));
            Assert.That(banner.activeInHierarchy, Is.True);
        }

        [UnityTest]
        public IEnumerator Pickup_UpdatesDamageHud_WithoutResettingExistingBotHp()
        {
            yield return LoadArena();
            health.TakeDamage(3);
            Assert.That(hpText.text, Is.EqualTo("Bot HP: 7 / 10"));
            MovePlayerTo(pickup.transform.position + new Vector3(0f, 0.7f, -1f));
            SetKeys(Key.E);
            yield return null;
            Assert.That(pickup.IsCollected, Is.True);
            Assert.That(damageText.text, Is.EqualTo("Damage: 2 HP (Buffed)"));
            Assert.That(hpText.text, Is.EqualTo("Bot HP: 7 / 10"));
        }

        [UnityTest]
        public IEnumerator Victory_RejectsDamagePunchAndPickup_FreezesMovement_AndKeepsCameraAndHudActive()
        {
            yield return LoadArena();
            health.TakeDamage(10);
            var position = player.transform.position;
            var changed = 0;
            health.OnHealthChanged += (_, __) => changed++;
            Assert.That(health.TakeDamage(1), Is.False);
            Assert.That(combat.CanPunch || combat.CanInteract, Is.False);
            Assert.That(pickup.TryCollect(combat, pickup.transform.position), Is.False);
            combat.SetDamage(99);
            SetKeys(Key.W, Key.Space, Key.J, Key.E);
            for (var frame = 0; frame < 70; frame++) yield return null;
            Assert.That(player.transform.position, Is.EqualTo(position));
            Assert.That(player.GetComponent<PlayerInputHandler>().enabled, Is.False);
            Assert.That(player.GetComponent<PlayerController>().enabled, Is.False);
            Assert.That(combat.CurrentDamage, Is.EqualTo(1));
            Assert.That(pickup.IsCollected, Is.False);
            Assert.That(health.CurrentHealth, Is.Zero);
            Assert.That(changed, Is.Zero);
            Assert.That(ui.isActiveAndEnabled && banner.activeInHierarchy, Is.True);
            Assert.That(arena.GetRootGameObjects().Single(item => item.name == "Main Camera")
                .GetComponent<ThirdPersonCameraController>().enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator UiDisableReenableAndDestroy_UnsubscribesAndResynchronizesWithoutLeaking()
        {
            yield return LoadArena();
            var stateSubscriptions = Subscribers(manager, "OnMatchStateChanged");
            var healthSubscriptions = Subscribers(health, "OnHealthChanged");
            var damageSubscriptions = Subscribers(combat, "OnDamageChanged");
            for (var cycle = 0; cycle < 3; cycle++)
            {
                ui.enabled = false;
                Assert.That(Subscribers(manager, "OnMatchStateChanged"), Is.EqualTo(stateSubscriptions - 1));
                Assert.That(Subscribers(health, "OnHealthChanged"), Is.EqualTo(healthSubscriptions - 1));
                Assert.That(Subscribers(combat, "OnDamageChanged"), Is.EqualTo(damageSubscriptions - 1));
                ui.enabled = true;
                Assert.That(Subscribers(manager, "OnMatchStateChanged"), Is.EqualTo(stateSubscriptions));
                Assert.That(Subscribers(health, "OnHealthChanged"), Is.EqualTo(healthSubscriptions));
                Assert.That(Subscribers(combat, "OnDamageChanged"), Is.EqualTo(damageSubscriptions));
            }
            ui.enabled = false;
            health.TakeDamage(1);
            combat.SetDamage(2);
            Assert.That(hpText.text, Is.EqualTo("Bot HP: 10 / 10"));
            Assert.That(damageText.text, Is.EqualTo("Damage: 1 HP (Unarmed)"));
            ui.enabled = true;
            Assert.That(hpText.text, Is.EqualTo("Bot HP: 9 / 10"));
            Assert.That(damageText.text, Is.EqualTo("Damage: 2 HP (Buffed)"));
            Object.DestroyImmediate(ui);
            Assert.That(Subscribers(manager, "OnMatchStateChanged"), Is.EqualTo(stateSubscriptions - 1));
            Assert.That(Subscribers(health, "OnHealthChanged"), Is.EqualTo(healthSubscriptions - 1));
            Assert.That(Subscribers(combat, "OnDamageChanged"), Is.EqualTo(damageSubscriptions - 1));
            health.TakeDamage(9);
            Assert.That(manager.CurrentState, Is.EqualTo(MatchState.Victory));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator DisabledGameManager_UnsubscribesDeath_AndCatchesUpOnReenable()
        {
            yield return LoadArena();
            var count = Subscribers(health, "OnDeath");
            manager.enabled = false;
            Assert.That(Subscribers(health, "OnDeath"), Is.EqualTo(count - 1));
            health.TakeDamage(10);
            Assert.That(manager.CurrentState, Is.EqualTo(MatchState.Playing));
            manager.enabled = true;
            Assert.That(manager.CurrentState, Is.EqualTo(MatchState.Victory));
            Assert.That(Subscribers(health, "OnDeath"), Is.EqualTo(count));
            Assert.That(banner.activeInHierarchy, Is.True);
        }

        [UnityTest]
        public IEnumerator RealReadinessProvider_RejectsDisabledHudWithoutChangingState()
        {
            yield return LoadArena();
            ui.enabled = false;
            var readiness = manager.GetComponent<OfflineMatchReadinessProbe>().ValidateReadiness();
            Assert.That(readiness.IsReady, Is.False);
            Assert.That(readiness.DiagnosticMessage, Does.Contain("HUD"));
            Assert.That(manager.CurrentState, Is.EqualTo(MatchState.Playing));
        }

        private void MovePlayerTo(Vector3 position)
        {
            player.GetComponent<PlayerController>().enabled = false;
            var character = player.GetComponent<CharacterController>();
            character.enabled = false;
            player.transform.position = position;
            character.enabled = true;
            Physics.SyncTransforms();
        }

        private static int Subscribers(object source, string eventName) =>
            (source.GetType().GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(source) as Delegate)?.GetInvocationList().Length ?? 0;

        private void SetKeys(params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
            InputSystem.Update();
        }
    }
}
#endif
