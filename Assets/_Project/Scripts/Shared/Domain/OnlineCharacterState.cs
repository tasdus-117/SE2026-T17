using System;

namespace SmallArena.Domain
{
    public enum CharacterLifeState { Alive, Dead, RespawnPending }
    public enum CharacterConnectionState { Connected, Disconnected }

    /// <summary>
    /// Immutable server snapshot, not a wire contract. ParticipantId is an opaque stable local
    /// identity; LifeId is positive and increases per respawn within this participant/match.
    /// Callers must keep the current snapshot and compare life references; no network dedup here.
    /// Score/deaths and the accepted-shot cooldown are supplied by their owning tasks and retained
    /// by lifecycle operations. Health reduction accepts an already resolved server effect, not a
    /// shot request. This model neither awards kills/deaths nor performs physics/spawn execution.
    /// </summary>
    public sealed class OnlineCharacterState
    {
        public OnlineCharacterState(OnlineMatch match, string participantId, long lifeId,
            CharacterLifeState lifeState, CharacterConnectionState connectionState, int health,
            int score, int deaths, double nextShotAtSeconds, double protectionUntilSeconds,
            double? respawnAtSeconds = null)
        {
            Match = match ?? throw new ArgumentNullException(nameof(match));
            if (string.IsNullOrWhiteSpace(participantId)) throw new ArgumentException("Participant identity required.", nameof(participantId));
            if (lifeId <= 0) throw new ArgumentOutOfRangeException(nameof(lifeId));
            if (!Enum.IsDefined(typeof(CharacterLifeState), lifeState)) throw new ArgumentOutOfRangeException(nameof(lifeState));
            if (!Enum.IsDefined(typeof(CharacterConnectionState), connectionState)) throw new ArgumentOutOfRangeException(nameof(connectionState));
            if (health < 0 || health > match.Configuration.MaxHealth || (lifeState == CharacterLifeState.Alive) != (health > 0))
                throw new ArgumentOutOfRangeException(nameof(health), "Alive requires positive HP; dead/pending require zero HP.");
            if (score < 0) throw new ArgumentOutOfRangeException(nameof(score));
            if (deaths < 0) throw new ArgumentOutOfRangeException(nameof(deaths));
            OnlineMatch.RequireTime(nextShotAtSeconds);
            OnlineMatch.RequireTime(protectionUntilSeconds);
            if ((lifeState == CharacterLifeState.Alive) == respawnAtSeconds.HasValue)
                throw new ArgumentException("Only dead/pending states require a respawn deadline.", nameof(respawnAtSeconds));
            if (respawnAtSeconds.HasValue) OnlineMatch.RequireTime(respawnAtSeconds.Value);
            ParticipantId = participantId;
            LifeId = lifeId;
            LifeState = lifeState;
            ConnectionState = connectionState;
            Health = health;
            Score = score;
            Deaths = deaths;
            NextShotAtSeconds = nextShotAtSeconds;
            ProtectionUntilSeconds = protectionUntilSeconds;
            RespawnAtSeconds = respawnAtSeconds;
        }

        /// <summary>Initial roster spawn: full HP, zero ledger/cooldown, same protection as respawn.</summary>
        public static OnlineCharacterState CreateInitial(OnlineMatch match, string participantId, double serverTimeSeconds)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));
            OnlineMatch.RequireTime(serverTimeSeconds);
            if (match.State != OnlineMatchState.Waiting) throw new InvalidOperationException("Initialize the locked roster before Playing.");
            return new OnlineCharacterState(match, participantId, 1, CharacterLifeState.Alive,
                CharacterConnectionState.Connected, match.Configuration.MaxHealth, 0, 0, 0,
                OnlineMatch.AddDuration(serverTimeSeconds, match.Configuration.SpawnProtectionSeconds));
        }

        public OnlineMatch Match { get; }
        public string ParticipantId { get; }
        public long LifeId { get; }
        public CharacterLifeState LifeState { get; }
        public CharacterConnectionState ConnectionState { get; }
        public int Health { get; }
        public int Score { get; }
        public int Deaths { get; }
        public double NextShotAtSeconds { get; }
        public double ProtectionUntilSeconds { get; }
        public double? RespawnAtSeconds { get; }

        public bool IsCurrentLife(string participantId, long lifeId) => ParticipantId == participantId && LifeId == lifeId;

        public bool CanReceiveControl(double serverTimeSeconds) => Match.IsGameplayActive(serverTimeSeconds)
            && LifeState == CharacterLifeState.Alive && ConnectionState == CharacterConnectionState.Connected;

        // Protection is [spawnTime, ProtectionUntilSeconds); both deadlines are inclusive for eligibility.
        public bool CanFire(double serverTimeSeconds) => CanReceiveControl(serverTimeSeconds)
            && serverTimeSeconds >= ProtectionUntilSeconds && serverTimeSeconds >= NextShotAtSeconds;

        public bool CanReceiveDamage(double serverTimeSeconds) => Match.IsGameplayActive(serverTimeSeconds)
            && LifeState == CharacterLifeState.Alive && serverTimeSeconds >= ProtectionUntilSeconds;

        /// <summary>TV1 has already checked the session/proof. Reconnect changes no life data or clocks.</summary>
        public bool TrySetConnection(CharacterConnectionState connectionState, out OnlineCharacterState next)
        {
            next = this;
            if (!Enum.IsDefined(typeof(CharacterConnectionState), connectionState)) throw new ArgumentOutOfRangeException(nameof(connectionState));
            if (Match.IsTerminal || connectionState == ConnectionState) return false;
            next = Copy(LifeId, LifeState, connectionState, Health, ProtectionUntilSeconds, RespawnAtSeconds);
            return true;
        }

        /// <summary>HP can only decrease. Zero commits Dead; effects/credit and death counters belong to TV2-007/009.</summary>
        public bool TryReduceHealth(long expectedLifeId, int remainingHealth, double serverTimeSeconds, out OnlineCharacterState next)
        {
            next = this;
            if (remainingHealth < 0 || remainingHealth > Match.Configuration.MaxHealth) throw new ArgumentOutOfRangeException(nameof(remainingHealth));
            if (expectedLifeId != LifeId || !CanReceiveDamage(serverTimeSeconds) || remainingHealth >= Health) return false;
            var dead = remainingHealth == 0;
            double? respawn = dead ? OnlineMatch.AddDuration(serverTimeSeconds, Match.Configuration.RespawnDelaySeconds) : (double?)null;
            next = Copy(LifeId, dead ? CharacterLifeState.Dead : CharacterLifeState.Alive, ConnectionState,
                remainingHealth, ProtectionUntilSeconds, respawn);
            return true;
        }

        /// <summary>At now >= deathTime + delay, offline players may also become pending.</summary>
        public bool TryMarkRespawnPending(double serverTimeSeconds, out OnlineCharacterState next)
        {
            next = this;
            if (!Match.IsGameplayActive(serverTimeSeconds) || LifeState != CharacterLifeState.Dead || serverTimeSeconds < RespawnAtSeconds.Value) return false;
            next = Copy(LifeId, CharacterLifeState.RespawnPending, ConnectionState, 0, ProtectionUntilSeconds, RespawnAtSeconds);
            return true;
        }

        /// <summary>The caller validates/reserves geometry and clears held input before using the new life.</summary>
        public bool TryRespawn(long expectedLifeId, double serverTimeSeconds, bool spawnConditionsSatisfied, out OnlineCharacterState next)
        {
            next = this;
            if (!Match.IsGameplayActive(serverTimeSeconds) || expectedLifeId != LifeId || LifeId == long.MaxValue
                || LifeState != CharacterLifeState.RespawnPending || ConnectionState != CharacterConnectionState.Connected
                || !spawnConditionsSatisfied || serverTimeSeconds < RespawnAtSeconds.Value) return false;
            var protection = OnlineMatch.AddDuration(serverTimeSeconds, Match.Configuration.SpawnProtectionSeconds);
            next = Copy(LifeId + 1, CharacterLifeState.Alive, ConnectionState, Match.Configuration.MaxHealth, protection, null);
            return true;
        }

        private OnlineCharacterState Copy(long lifeId, CharacterLifeState life, CharacterConnectionState connection,
            int health, double protection, double? respawn) => new OnlineCharacterState(Match, ParticipantId, lifeId,
                life, connection, health, Score, Deaths, NextShotAtSeconds, protection, respawn);
    }
}
