using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SmallArena.Core;
using SmallArena.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SmallArena.Tests.PlayMode
{
    // AC-001/017, TC-001/029 foundation. Stub probes exist only in this test assembly.
    public sealed class MatchInitializationTests
    {
        private readonly List<Scene> scenes = new List<Scene>();
        private Scene scene;
        private GameObject owner;
        private GameObject player;
        private GameObject bot;
        private GameObject pickup;
        private Camera camera;
        private GameManager manager;
        private Probe probe;

        private sealed class Probe : IMatchReadinessProbe
        {
            public MatchReadinessResult Result = MatchReadinessResult.Success();
            public Action OnValidate;
            public int Calls;
            public MatchReadinessResult ValidateReadiness()
            {
                Calls++;
                OnValidate?.Invoke();
                return Result;
            }
        }

        [SetUp]
        public void SetUp()
        {
            scenes.Clear();
            scene = NewScene();
            player = Create("Player", "Player");
            bot = Create("Bot", "Bot");
            pickup = Create("WeaponPickup", "Pickup");
            camera = Create("Match Camera").AddComponent<Camera>();
            owner = Create("_GameManager");
            owner.SetActive(false);
            manager = owner.AddComponent<GameManager>();
            probe = new Probe();
            manager.Configure(player, bot, pickup, camera);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var item in scenes)
                if (item.IsValid() && item.isLoaded) yield return SceneManager.UnloadSceneAsync(item);
        }

        [Test]
        public void InitialState_IsInitializing_WithGameplayBlocked()
        {
            Assert.That(manager.CurrentState, Is.EqualTo(MatchState.Initializing));
            Assert.That(manager.IsPlaying, Is.False);
        }

        [Test]
        public void ThreeDistinctEntityReferencesAndCamera_PassStructuralValidationOnly()
        {
            var result = manager.ValidateSceneReadiness();
            Assert.That(result.IsReady, Is.True);
            Assert.That(result.DiagnosticMessage, Is.Empty);
            Assert.That(manager.CurrentState, Is.EqualTo(MatchState.Initializing));
            Assert.That(manager.ValidateMatchReadiness().IsReady, Is.False);
        }

        [TestCase("playerInstance")]
        [TestCase("botInstance")]
        [TestCase("pickupInstance")]
        [TestCase("sceneCamera")]
        public void MissingReference_ReportsItsName_AndCannotStart(string missing)
        {
            manager.Configure(missing == "playerInstance" ? null : player,
                missing == "botInstance" ? null : bot,
                missing == "pickupInstance" ? null : pickup,
                missing == "sceneCamera" ? null : camera, probe);
            var notifications = new List<MatchState>();
            manager.OnMatchStateChanged += notifications.Add;

            var result = manager.ValidateSceneReadiness();
            Assert.That(result.IsReady, Is.False);
            Assert.That(result.DiagnosticMessage, Does.Contain(missing));
            LogAssert.Expect(LogType.Error, new Regex("Match startup blocked:.*" + missing));
            Assert.That(manager.TryStartMatch(), Is.False);
            Assert.That(manager.CurrentState, Is.EqualTo(MatchState.Initializing));
            Assert.That(notifications, Is.Empty);
            Assert.That(probe.Calls, Is.EqualTo(0));
        }

        [Test]
        public void AllMissingReferences_AreReportedTogether()
        {
            manager.Configure(null, null, null, null);
            var result = manager.ValidateSceneReadiness();
            Assert.That(result.IsReady, Is.False);
            foreach (var name in new[] { "playerInstance", "botInstance", "pickupInstance", "sceneCamera" })
                Assert.That(result.DiagnosticMessage, Does.Contain(name));
        }

        [Test]
        public void AliasedEntityReferences_AreRejected()
        {
            manager.Configure(player, player, pickup, camera, probe);
            Assert.That(manager.ValidateSceneReadiness().DiagnosticMessage, Does.Contain("distinct"));
            Assert.That(manager.ValidateSceneReadiness().IsReady, Is.False);
        }

        [TestCase("Player", true)]
        [TestCase("Player", false)]
        [TestCase("Bot", true)]
        [TestCase("Bot", false)]
        [TestCase("Pickup", true)]
        [TestCase("Pickup", false)]
        public void DuplicateRoleRoot_RejectsEvenInactiveExtras(string role, bool active)
        {
            var duplicate = Create("Duplicate " + role, role);
            duplicate.SetActive(active);
            var result = manager.ValidateSceneReadiness();
            Assert.That(result.IsReady, Is.False);
            Assert.That(result.DiagnosticMessage, Does.Contain("exactly one " + role));
        }

        [Test]
        public void SameLayerVisualDescendants_DoNotCountAsExtraEntities()
        {
            var container = Create("Visual container");
            container.transform.SetParent(player.transform);
            var visual = Create("Player visual", "Player");
            visual.transform.SetParent(container.transform);
            Assert.That(manager.ValidateSceneReadiness().IsReady, Is.True);
        }

        [Test]
        public void ReferencingAVisualInsteadOfEntityRoot_IsRejected()
        {
            var visual = Create("Player visual", "Player");
            visual.transform.SetParent(player.transform);
            manager.Configure(visual, bot, pickup, camera);
            Assert.That(manager.ValidateSceneReadiness().IsReady, Is.False);
        }

        [Test]
        public void WrongEntityLayer_IsRejected()
        {
            player.layer = LayerMask.NameToLayer("Default");
            var result = manager.ValidateSceneReadiness();
            Assert.That(result.IsReady, Is.False);
            Assert.That(result.DiagnosticMessage, Does.Contain("Player layer"));
        }

        [Test]
        public void EntityFromAnotherScene_IsRejected()
        {
            SceneManager.MoveGameObjectToScene(bot, NewScene());
            Assert.That(manager.ValidateSceneReadiness().IsReady, Is.False);
            Assert.That(manager.ValidateSceneReadiness().DiagnosticMessage, Does.Contain("this scene"));
        }

        [Test]
        public void InactiveReferencedEntity_IsRejected()
        {
            pickup.SetActive(false);
            Assert.That(manager.ValidateSceneReadiness().IsReady, Is.False);
        }

        [Test]
        public void DisabledCamera_IsRejected()
        {
            camera.enabled = false;
            Assert.That(manager.ValidateSceneReadiness().IsReady, Is.False);
        }

        [Test]
        public void MissingFullReadinessProvider_KeepsM1PlaceholdersInitializing()
        {
            Assert.That(manager.ValidateSceneReadiness().IsReady, Is.True);
            var result = manager.ValidateMatchReadiness();
            Assert.That(result.IsReady, Is.False);
            Assert.That(result.DiagnosticMessage, Does.Contain("Health").And.Contain("HUD"));
            LogAssert.Expect(LogType.Error, new Regex("Match startup blocked:.*real Health.*HUD"));
            Assert.That(manager.TryStartMatch(), Is.False);
            Assert.That(manager.IsPlaying, Is.False);
        }

        [Test]
        public void UnreadyFullDependencies_PreserveProviderDiagnostic_AndCanRetry()
        {
            probe.Result = MatchReadinessResult.Fail("HUD is not bound");
            manager.Configure(player, bot, pickup, camera, probe);
            LogAssert.Expect(LogType.Error, "Match startup blocked: HUD is not bound");
            Assert.That(manager.TryStartMatch(), Is.False);
            Assert.That(manager.CurrentState, Is.EqualTo(MatchState.Initializing));
            probe.Result = MatchReadinessResult.Success();
            Assert.That(manager.TryStartMatch(), Is.True);
        }

        [Test]
        public void EmptyFailureDiagnostic_GetsAnInformativeFallback()
        {
            probe.Result = default(MatchReadinessResult);
            manager.Configure(player, bot, pickup, camera, probe);
            var result = manager.ValidateMatchReadiness();
            Assert.That(result.IsReady, Is.False);
            Assert.That(result.DiagnosticMessage, Does.Contain("not initialized"));
        }

        [Test]
        public void SuccessfulStart_CommitsPlayingAndForwardsExactlyOneEvent()
        {
            manager.Configure(player, bot, pickup, camera, probe);
            var notifications = new List<MatchState>();
            manager.OnMatchStateChanged += state =>
            {
                Assert.That(manager.CurrentState, Is.EqualTo(state));
                Assert.That(manager.IsPlaying, Is.True);
                notifications.Add(state);
            };
            Assert.That(manager.TryStartMatch(), Is.True);
            Assert.That(manager.TryStartMatch(), Is.False);
            Assert.That(notifications, Is.EqualTo(new[] { MatchState.Playing }));
            Assert.That(probe.Calls, Is.EqualTo(1));
        }

        [Test]
        public void StateNotification_ReentrantStartIsRejected()
        {
            manager.Configure(player, bot, pickup, camera, probe);
            var events = 0;
            manager.OnMatchStateChanged += state => { events++; Assert.That(manager.TryStartMatch(), Is.False); };
            Assert.That(manager.TryStartMatch(), Is.True);
            Assert.That(events, Is.EqualTo(1));
        }

        [Test]
        public void ReadinessQuery_CannotReentrantlyStartTheMatch()
        {
            manager.Configure(player, bot, pickup, camera, probe);
            probe.OnValidate = () => Assert.That(manager.TryStartMatch(), Is.False);
            Assert.That(manager.ValidateMatchReadiness().IsReady, Is.True);
            Assert.That(manager.CurrentState, Is.EqualTo(MatchState.Initializing));
            Assert.That(manager.TryStartMatch(), Is.True);
        }

        [Test]
        public void ThrowingReadinessProbe_FailsClosed_AndReleasesGuard()
        {
            manager.Configure(player, bot, pickup, camera, probe);
            probe.OnValidate = () => throw new InvalidOperationException("HUD initialization failed");
            LogAssert.Expect(LogType.Error, "Match startup blocked: Full match readiness probe failed: HUD initialization failed");
            Assert.That(manager.TryStartMatch(), Is.False);
            Assert.That(manager.CurrentState, Is.EqualTo(MatchState.Initializing));
            probe.OnValidate = null;
            Assert.That(manager.TryStartMatch(), Is.True);
        }

        [Test]
        public void EntityDestroyedDuringReadiness_IsRevalidatedBeforePlaying()
        {
            manager.Configure(player, bot, pickup, camera, probe);
            probe.OnValidate = () => Object.DestroyImmediate(bot);
            LogAssert.Expect(LogType.Error, new Regex("Match startup blocked:.*botInstance"));
            Assert.That(manager.TryStartMatch(), Is.False);
            Assert.That(manager.CurrentState, Is.EqualTo(MatchState.Initializing));
        }

        [Test]
        public void ThrowingStateListener_PreservesCommittedPlaying_WithoutReplay()
        {
            manager.Configure(player, bot, pickup, camera, probe);
            var events = 0;
            manager.OnMatchStateChanged += state => { events++; throw new InvalidOperationException("Listener failed"); };
            Assert.Throws<InvalidOperationException>(() => manager.TryStartMatch());
            Assert.That(manager.CurrentState, Is.EqualTo(MatchState.Playing));
            Assert.That(manager.TryStartMatch(), Is.False);
            Assert.That(events, Is.EqualTo(1));
        }

        [Test]
        public void ConfigureAfterPlaying_IsRejected()
        {
            manager.Configure(player, bot, pickup, camera, probe);
            Assert.That(manager.TryStartMatch(), Is.True);
            Assert.Throws<InvalidOperationException>(() => manager.Configure(player, bot, pickup, camera));
        }

        [UnityTest]
        public IEnumerator AutoStart_ValidDependencies_StartsOnceAfterAwake()
        {
            manager.Configure(player, bot, pickup, camera, probe, true);
            var events = 0;
            manager.OnMatchStateChanged += state => events++;
            owner.SetActive(true);
            Assert.That(manager.CurrentState, Is.EqualTo(MatchState.Initializing));
            yield return null;
            Assert.That(manager.CurrentState, Is.EqualTo(MatchState.Playing));
            Assert.That(events, Is.EqualTo(1));
            Assert.That(probe.Calls, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator AutoStart_WithoutFullProvider_RemainsInitializing()
        {
            manager.Configure(player, bot, pickup, camera, null, true);
            LogAssert.Expect(LogType.Error, new Regex("Match startup blocked:.*real Health.*HUD"));
            owner.SetActive(true);
            yield return null;
            Assert.That(manager.CurrentState, Is.EqualTo(MatchState.Initializing));
        }

        [UnityTest]
        public IEnumerator ManualStartBeforeAwake_DoesNotDoubleSubscribeOrAutoStart()
        {
            manager.Configure(player, bot, pickup, camera, probe, true);
            var events = 0;
            manager.OnMatchStateChanged += state => events++;
            Assert.That(manager.TryStartMatch(), Is.True);
            owner.SetActive(true);
            yield return null;
            Assert.That(events, Is.EqualTo(1));
            Assert.That(probe.Calls, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator AutoStartOff_StillDiagnosesMissingSceneReferences()
        {
            manager.Configure(player, null, pickup, camera);
            LogAssert.Expect(LogType.Error, new Regex("Scene validation failed:.*botInstance"));
            owner.SetActive(true);
            yield return null;
            Assert.That(manager.CurrentState, Is.EqualTo(MatchState.Initializing));
        }

        private Scene NewScene()
        {
            var result = SceneManager.CreateScene("Match initialization " + Guid.NewGuid().ToString("N"));
            scenes.Add(result);
            return result;
        }

        private GameObject Create(string name, string role = null)
        {
            var result = new GameObject(name);
            if (role != null)
            {
                var layer = LayerMask.NameToLayer(role);
                Assert.That(layer, Is.GreaterThan(0), role);
                result.layer = layer;
            }
            SceneManager.MoveGameObjectToScene(result, scene);
            return result;
        }
    }
}
