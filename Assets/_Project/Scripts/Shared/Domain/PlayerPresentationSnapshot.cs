using System;
namespace SmallArena.Domain
{
    /// <summary>NEW local read model; context is an opaque adapter token, never a serialized identity.</summary>
    public sealed class PlayerPresentationSnapshot
    {
        public object Context { get; }
        public ulong Revision { get; }
        public int Health { get; }
        public int MaxHealth { get; }
        public int Score { get; }
        public double RemainingSeconds { get; }
        public string Result { get; }
        public string Rejection { get; }
        public long LifeId { get; }
        public ParticipantLifeState LifeState { get; }
        public MatchLifecyclePhase Phase { get; }
        public int WeaponDamage { get; }
        public int BaseDamage { get; }
        public bool IsWeaponBuffed => WeaponDamage > BaseDamage;
        public bool IsAlive => Health > 0 && LifeState == ParticipantLifeState.Alive;
        public int Kills => Score;
        public int Deaths { get; }
        public long DamageDealt { get; }
        public int Rank { get; }
        public double CooldownRemainingSeconds { get; }
        public double RespawnRemainingSeconds { get; }
        public PlayerPresentationSnapshot(object context, ulong revision, int health, int maxHealth,
            int score, double remainingSeconds, string result, string rejection,
            long lifeId = 0, ParticipantLifeState lifeState = ParticipantLifeState.Alive,
            MatchLifecyclePhase phase = MatchLifecyclePhase.WaitingForPlayers, int weaponDamage = 1,
            int baseDamage = 1, int deaths = 0, long damageDealt = 0, int rank = 0,
            double cooldownRemainingSeconds = 0, double respawnRemainingSeconds = 0)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (maxHealth <= 0 || health < 0 || health > maxHealth || score < 0
                || double.IsNaN(remainingSeconds) || double.IsInfinity(remainingSeconds) || remainingSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(health));
            if (lifeId < 0 || weaponDamage <= 0 || baseDamage <= 0 || deaths < 0 || damageDealt < 0 || rank < 0
                || double.IsNaN(cooldownRemainingSeconds) || double.IsInfinity(cooldownRemainingSeconds) || cooldownRemainingSeconds < 0
                || double.IsNaN(respawnRemainingSeconds) || double.IsInfinity(respawnRemainingSeconds) || respawnRemainingSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(lifeId));
            Context = context; Revision = revision; Health = health; MaxHealth = maxHealth;
            Score = score; RemainingSeconds = remainingSeconds; Result = result ?? ""; Rejection = rejection ?? "";
            LifeId = lifeId; LifeState = lifeState; Phase = phase; WeaponDamage = weaponDamage; BaseDamage = baseDamage;
            Deaths = deaths; DamageDealt = damageDealt; Rank = rank;
            CooldownRemainingSeconds = cooldownRemainingSeconds; RespawnRemainingSeconds = respawnRemainingSeconds;
        }
    }
    public interface IPlayerPresentationSource
    {
        // Null until a complete authoritative baseline exists. New token for each life/binding.
        PlayerPresentationSnapshot Current { get; }
        event Action<PlayerPresentationSnapshot> Changed;
    }
}
