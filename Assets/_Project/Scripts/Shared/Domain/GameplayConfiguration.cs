using System;

namespace SmallArena.Domain
{
    /// <summary>
    /// Immutable server-owned tuning snapshot. A match retains this instance; tuning changes
    /// apply to new matches. HP/damage are integral HP, distances metres, speed metres/second,
    /// all durations seconds. Zero speed/distance is permitted; other values must be positive.
    /// These zero boundaries are local validation policy where rules-v0.1 is silent.
    /// </summary>
    public sealed class GameplayConfiguration
    {
        public const string SupportedRevision = "rules-v0.1";
        // Fixed scoring rule for TV2-009; this type does not execute scoring.
        public const int LethalOpponentHitScore = 1;

        public GameplayConfiguration(string revision = SupportedRevision, int maxHealth = 100,
            int shotDamage = 25, double shotCooldownSeconds = 0.5, double shotRangeMetres = 30,
            double maxHorizontalSpeedMetresPerSecond = 5, double validInputTimeoutSeconds = 0.25,
            double matchDurationSeconds = 180, double respawnDelaySeconds = 3,
            double minimumSafeSpawnDistanceMetres = 5, double spawnRetryIntervalSeconds = 0.5,
            double spawnProtectionSeconds = 1, double reconnectGraceSeconds = 15)
        {
            if (!string.Equals(revision, SupportedRevision, StringComparison.Ordinal))
                throw new ArgumentException("Unsupported gameplay revision.", nameof(revision));
            if (maxHealth <= 0) throw new ArgumentOutOfRangeException(nameof(maxHealth));
            if (shotDamage <= 0) throw new ArgumentOutOfRangeException(nameof(shotDamage));
            RequireValue(shotCooldownSeconds, nameof(shotCooldownSeconds));
            RequireValue(shotRangeMetres, nameof(shotRangeMetres));
            RequireValue(maxHorizontalSpeedMetresPerSecond, nameof(maxHorizontalSpeedMetresPerSecond), true);
            RequireValue(validInputTimeoutSeconds, nameof(validInputTimeoutSeconds));
            RequireValue(matchDurationSeconds, nameof(matchDurationSeconds));
            RequireValue(respawnDelaySeconds, nameof(respawnDelaySeconds));
            RequireValue(minimumSafeSpawnDistanceMetres, nameof(minimumSafeSpawnDistanceMetres), true);
            RequireValue(spawnRetryIntervalSeconds, nameof(spawnRetryIntervalSeconds));
            RequireValue(spawnProtectionSeconds, nameof(spawnProtectionSeconds));
            RequireValue(reconnectGraceSeconds, nameof(reconnectGraceSeconds));
            Revision = revision;
            MaxHealth = maxHealth;
            ShotDamage = shotDamage;
            ShotCooldownSeconds = shotCooldownSeconds;
            ShotRangeMetres = shotRangeMetres;
            MaxHorizontalSpeedMetresPerSecond = maxHorizontalSpeedMetresPerSecond;
            ValidInputTimeoutSeconds = validInputTimeoutSeconds;
            MatchDurationSeconds = matchDurationSeconds;
            RespawnDelaySeconds = respawnDelaySeconds;
            MinimumSafeSpawnDistanceMetres = minimumSafeSpawnDistanceMetres;
            SpawnRetryIntervalSeconds = spawnRetryIntervalSeconds;
            SpawnProtectionSeconds = spawnProtectionSeconds;
            ReconnectGraceSeconds = reconnectGraceSeconds;
        }

        public string Revision { get; }
        public int MaxHealth { get; }
        public int ShotDamage { get; }
        public double ShotCooldownSeconds { get; }
        public double ShotRangeMetres { get; }
        public double MaxHorizontalSpeedMetresPerSecond { get; }
        public double ValidInputTimeoutSeconds { get; }
        public double MatchDurationSeconds { get; }
        public double RespawnDelaySeconds { get; }
        public double MinimumSafeSpawnDistanceMetres { get; }
        public double SpawnRetryIntervalSeconds { get; }
        public double SpawnProtectionSeconds { get; }
        public double ReconnectGraceSeconds { get; }

        private static void RequireValue(double value, string name, bool allowZero = false)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || (allowZero ? value < 0 : value <= 0))
                throw new ArgumentOutOfRangeException(name);
        }
    }
}
