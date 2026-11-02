using System;
using System.Collections.Generic;
using SmallArena.Domain;
using SmallArena.Client;
using SmallArena.Input;
using SmallArena.Player;
using SmallArena.Server.Combat;
using UnityEngine;

namespace SmallArena.Core
{
    /// <summary>Scene readiness and lifecycle coordination; no gameplay simulation.</summary>
    [DisallowMultipleComponent]
    public sealed class GameManager : MonoBehaviour, IMatchStateSource
    {
        [SerializeField] private GameObject playerInstance;
        [SerializeField] private GameObject botInstance;
        [SerializeField] private GameObject pickupInstance;
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private bool autoStart;
        [SerializeField] private MonoBehaviour readinessProvider = null;

        private readonly MatchStateMachine stateMachine = new MatchStateMachine();
        private IMatchReadinessProbe injectedProbe;
        private bool forwardingState;
        private bool starting;
        private bool validating;
        private Health observedBotHealth;

        public MatchState CurrentState => stateMachine.CurrentState;
        public bool IsPlaying => stateMachine.IsPlaying;
        public event Action<MatchState> OnMatchStateChanged;

        public void Configure(GameObject player, GameObject bot, GameObject pickup, Camera camera,
            IMatchReadinessProbe fullReadinessProbe = null, bool startAutomatically = false)
        {
            if (CurrentState != MatchState.Initializing || starting || validating)
                throw new InvalidOperationException("Configure scene dependencies only while idle in Initializing.");
            playerInstance = player;
            botInstance = bot;
            pickupInstance = pickup;
            sceneCamera = camera;
            injectedProbe = fullReadinessProbe;
            autoStart = startAutomatically;
            if (isActiveAndEnabled) BindBotDeath();
        }

        private void Awake() => EnsureEventForwarding();

        private void OnEnable()
        {
            BindBotDeath();
            if (observedBotHealth != null && observedBotHealth.IsDead) ReportBotDefeated();
        }

        private void OnDisable() => UnbindBotDeath();

        private void BindBotDeath()
        {
            UnbindBotDeath();
            observedBotHealth = botInstance != null ? botInstance.GetComponent<Health>() : null;
            if (observedBotHealth != null) observedBotHealth.OnDeath += ReportBotDefeated;
        }

        private void UnbindBotDeath()
        {
            if (observedBotHealth != null) observedBotHealth.OnDeath -= ReportBotDefeated;
            observedBotHealth = null;
        }

        public void ReportBotDefeated()
        {
            if (CurrentState == MatchState.Playing) stateMachine.TriggerVictory();
        }

        private void Start()
        {
            if (CurrentState != MatchState.Initializing) return;
            if (autoStart) { TryStartMatch(); return; }
            var readiness = ValidateSceneReadiness();
            if (!readiness.IsReady) Debug.LogError("Scene validation failed: " + readiness.DiagnosticMessage, this);
        }

        /// <summary>Structural scene validation only; this does not authorize Playing.</summary>
        public MatchReadinessResult ValidateSceneReadiness()
        {
            var missing = new List<string>();
            if (playerInstance == null) missing.Add(nameof(playerInstance));
            if (botInstance == null) missing.Add(nameof(botInstance));
            if (pickupInstance == null) missing.Add(nameof(pickupInstance));
            if (sceneCamera == null) missing.Add(nameof(sceneCamera));
            if (missing.Count > 0) return MatchReadinessResult.Fail("Missing required references: " + string.Join(", ", missing) + ".");
            if (playerInstance == botInstance || playerInstance == pickupInstance || botInstance == pickupInstance)
                return MatchReadinessResult.Fail("Player, Bot and Pickup references must be distinct.");

            var roles = new[] { playerInstance, botInstance, pickupInstance };
            var layers = new[] { "Player", "Bot", "Pickup" };
            for (var index = 0; index < roles.Length; index++)
            {
                var result = ValidateEntity(roles[index], layers[index]);
                if (!result.IsReady) return result;
            }
            if (sceneCamera.gameObject.scene != gameObject.scene || !sceneCamera.isActiveAndEnabled)
                return MatchReadinessResult.Fail("sceneCamera must be active and belong to this scene.");
            return MatchReadinessResult.Success();
        }

        /// <summary>Full startup gate. M1 placeholders cannot pass without a real dependency probe.</summary>
        public MatchReadinessResult ValidateMatchReadiness()
        {
            if (validating) return MatchReadinessResult.Fail("Match readiness validation cannot reenter itself.");
            validating = true;
            try
            {
                var scene = ValidateSceneReadiness();
                if (!scene.IsReady) return scene;
                var probe = injectedProbe ?? readinessProvider as IMatchReadinessProbe;
                if (probe == null)
                    return MatchReadinessResult.Fail("Full match readiness requires a provider validating real Health, player stats, pickup initialization and HUD readiness.");
                if (probe is UnityEngine.Object dependency && dependency == null)
                    return MatchReadinessResult.Fail("The full match readiness provider has been destroyed.");
                var result = probe.ValidateReadiness();
                if (!result.IsReady)
                    return MatchReadinessResult.Fail(string.IsNullOrWhiteSpace(result.DiagnosticMessage)
                        ? "Full match dependencies are not initialized." : result.DiagnosticMessage);
                // The provider may initialize components; references/counts must still be valid afterwards.
                return ValidateSceneReadiness();
            }
            catch (Exception error)
            {
                return MatchReadinessResult.Fail("Full match readiness probe failed: " + error.Message);
            }
            finally { validating = false; }
        }

        public bool TryStartMatch()
        {
            if (starting || validating || CurrentState != MatchState.Initializing) return false;
            starting = true;
            try
            {
                EnsureEventForwarding();
                if (isActiveAndEnabled) BindBotDeath();
                var readiness = ValidateMatchReadiness();
                if (!readiness.IsReady) Debug.LogError("Match startup blocked: " + readiness.DiagnosticMessage, this);
                return stateMachine.StartMatch(readiness);
            }
            finally
            {
                starting = false;
                // Death during a Playing notification is deferred until Domain releases its notification guard.
                if (CurrentState == MatchState.Playing && observedBotHealth != null && observedBotHealth.IsDead)
                    ReportBotDefeated();
            }
        }

        private MatchReadinessResult ValidateEntity(GameObject entity, string role)
        {
            var layer = LayerMask.NameToLayer(role);
            if (layer < 0 || entity.layer != layer)
                return MatchReadinessResult.Fail(role + " reference must use the named " + role + " layer.");
            if (entity.scene != gameObject.scene || !entity.activeInHierarchy || HasRoleAncestor(entity.transform, layer))
                return MatchReadinessResult.Fail(role + " reference must be an active entity root in this scene.");
            var count = 0;
            // Startup-only traversal, scoped to this scene. Same-layer descendants are visuals, not extra actors.
            foreach (var root in gameObject.scene.GetRootGameObjects())
                foreach (var item in root.GetComponentsInChildren<Transform>(true))
                    if (item.gameObject.layer == layer && !HasRoleAncestor(item, layer)) count++;
            return count == 1 ? MatchReadinessResult.Success()
                : MatchReadinessResult.Fail("Expected exactly one " + role + " entity in this scene; found " + count + ".");
        }

        private static bool HasRoleAncestor(Transform item, int layer)
        {
            for (var parent = item.parent; parent != null; parent = parent.parent)
                if (parent.gameObject.layer == layer) return true;
            return false;
        }

        private void EnsureEventForwarding()
        {
            if (forwardingState) return;
            stateMachine.OnStateChanged += ForwardState;
            forwardingState = true;
        }

        private void ForwardState(MatchState state)
        {
            // Stop local actions before publishing terminal state to any observers.
            if (state == MatchState.Victory && playerInstance != null)
            {
                playerInstance.GetComponent<CombatController>()?.EndMatch();
                var movement = playerInstance.GetComponent<PlayerController>();
                var input = playerInstance.GetComponent<PlayerInputHandler>();
                if (movement != null) movement.enabled = false;
                if (input != null) input.enabled = false;
            }
            OnMatchStateChanged?.Invoke(state);
        }

        private void OnDestroy()
        {
            UnbindBotDeath();
            if (forwardingState) stateMachine.OnStateChanged -= ForwardState;
        }
    }
}
