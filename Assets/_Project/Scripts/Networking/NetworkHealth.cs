using System;
using Mirror;
using UnityEngine;
using SmallArena.Domain;

namespace SmallArena.Networking
{
    public enum PlayerMatchResult : byte { Playing, Victory, Defeat, Draw }

    /// <summary>Server-owned hit points; clients observe SyncVar hooks only.</summary>
    [DisallowMultipleComponent]
    public sealed class NetworkHealth : NetworkBehaviour, IHealthSource
    {
        [SyncVar, SerializeField] private int maxHealth = 10;
        [SyncVar(hook = nameof(HealthChanged)), SerializeField] private int currentHealth = 10;
        [SyncVar(hook = nameof(ResultChanged))] private PlayerMatchResult result;
        [SyncVar(hook = nameof(SessionChanged))] private string sessionId = "";
        [SyncVar] private string participantId = "";
        [SyncVar(hook = nameof(LifeIdChanged))] private long lifeId;
        [SyncVar] private int slotIndex;
        [SyncVar] private int remainingLives;
        [SyncVar(hook = nameof(LifeChanged))] private ParticipantLifeState lifeState;
        [SyncVar(hook = nameof(PhaseChanged))] private MatchLifecyclePhase matchPhase;
        [SyncVar] private double respawnAt;
        [SyncVar] private int baseDamage = 1;
        [SyncVar] private double attackCooldown = 1;
        [SyncVar] private double shotRange = 1.8;
        private Func<bool> runtimeCanAct;
        private Func<int, NetworkHealth, bool> runtimeDamage;
        private Func<bool> runtimeAttack;
        private GameplayConfiguration runtimeConfiguration;
        public double MovementSpeed => runtimeConfiguration == null ? 5 : Math.Min(5, runtimeConfiguration.MaxHorizontalSpeedMetresPerSecond);
        public double InputTimeoutSeconds => runtimeConfiguration?.ValidInputTimeoutSeconds ?? .25;
        public bool IsLifecycleBound => !string.IsNullOrEmpty(sessionId);
        public string SessionId => sessionId;
        public string ParticipantId => participantId;
        public long LifeId => lifeId;
        public int SlotIndex => slotIndex;
        public int RemainingLives => remainingLives;
        public ParticipantLifeState LifeState => lifeState;
        public MatchLifecyclePhase MatchPhase => matchPhase;
        public int BaseDamage => IsLifecycleBound ? baseDamage : 1;
        public double AttackCooldownSeconds => IsLifecycleBound ? attackCooldown : NetworkCombat.Cooldown;
        public double ShotRangeMetres => shotRange;
        public double RespawnRemainingSeconds => lifeState == ParticipantLifeState.Respawning ? Math.Max(0, respawnAt - NetworkTime.time) : 0;
        public event Action OnLifecycleChanged;
        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public bool IsDead => currentHealth <= 0;
        public bool CanAct => !IsDead && result == PlayerMatchResult.Playing && (!IsLifecycleBound
            || (isServer ? runtimeCanAct != null && runtimeCanAct() : matchPhase == MatchLifecyclePhase.InProgress && lifeState == ParticipantLifeState.Alive));
        public PlayerMatchResult Result => result;
        public event Action<int, int> OnHealthChanged;
        public event Action<PlayerMatchResult> OnResultChanged;
        public event Action OnDeath;

        public override void OnStartServer()
        {
            maxHealth = Mathf.Max(1, maxHealth);
            currentHealth = maxHealth;
            result = PlayerMatchResult.Playing;
        }
        public override void OnStartClient()
        {
            KeepReplicaCollisionDisabled();
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnResultChanged?.Invoke(result);
        }
        [Server]
        public bool TakeDamage(int amount)
            => ApplyDamage(amount, null);
        [Server] public bool TakeDamageFrom(NetworkHealth attacker, int amount)
        {
            if (attacker == null || !attacker.isServer || !attacker.CanAct) return false;
            return ApplyDamage(amount, attacker);
        }
        private bool ApplyDamage(int amount, NetworkHealth attacker)
        {
            if (!isServer || !CanAct || amount <= 0) return false;
            if (IsLifecycleBound)
            {
                if (attacker != null && (!attacker.IsLifecycleBound || attacker.SessionId != sessionId)) return false;
                return runtimeDamage != null && runtimeDamage(amount, attacker);
            }
            currentHealth = amount >= currentHealth ? 0 : currentHealth - amount;
            if (currentHealth == 0)
            {
                SetResult(PlayerMatchResult.Defeat);
                OnDeath?.Invoke();
                NetworkMatchRules.ReportDefeat();
            }
            return true;
        }
        [Server] public void BindRuntime(Func<bool> canAct, Func<int, NetworkHealth, bool> damage, Func<bool> attack)
        {
            if (!isServer) return;
            runtimeCanAct = canAct; runtimeDamage = damage; runtimeAttack = attack;
        }
        [Server] public bool TryBeginRuntimeAttack() => isServer && (!IsLifecycleBound || runtimeAttack != null && runtimeAttack());
        [Server] public void PublishRuntime(MatchParticipantRuntime participant, GameplayConfiguration configuration, MatchLifecyclePhase phase,
            PlayerMatchResult outcome = PlayerMatchResult.Playing)
        {
            if (!isServer) return;
            runtimeConfiguration = configuration;
            var changed = sessionId != participant.SessionId || lifeId != participant.LifeId
                || lifeState != participant.LifeState || matchPhase != phase;
            var died = currentHealth > 0 && participant.Character.Health == 0;
            sessionId = participant.SessionId; participantId = participant.ParticipantId; lifeId = participant.LifeId;
            slotIndex = participant.SlotIndex; remainingLives = participant.RemainingLives;
            maxHealth = configuration.MaxHealth; currentHealth = participant.Character.Health;
            baseDamage = configuration.ShotDamage; attackCooldown = configuration.ShotCooldownSeconds;
            shotRange = configuration.ShotRangeMetres;
            respawnAt = participant.Character.RespawnAtSeconds ?? 0;
            lifeState = participant.LifeState; matchPhase = phase; result = outcome;
            if (died) OnDeath?.Invoke();
            if (changed) OnLifecycleChanged?.Invoke();
        }
        private void LifeIdChanged(long previous, long current) => OnLifecycleChanged?.Invoke();
        private void SessionChanged(string previous, string current) => OnLifecycleChanged?.Invoke();
        private void LifeChanged(ParticipantLifeState previous, ParticipantLifeState current)
        { KeepReplicaCollisionDisabled(); OnLifecycleChanged?.Invoke(); }
        private void KeepReplicaCollisionDisabled()
        {
            if (!isServer && TryGetComponent<CharacterController>(out var character)) character.enabled = false;
        }
        private void PhaseChanged(MatchLifecyclePhase previous, MatchLifecyclePhase current) => OnLifecycleChanged?.Invoke();
        [Server]
        internal void SetResult(PlayerMatchResult next)
        {
            if (isServer && result == PlayerMatchResult.Playing) result = next;
        }
        private void HealthChanged(int previous, int current) => OnHealthChanged?.Invoke(current, maxHealth);
        private void ResultChanged(PlayerMatchResult previous, PlayerMatchResult current) => OnResultChanged?.Invoke(current);
    }
}
