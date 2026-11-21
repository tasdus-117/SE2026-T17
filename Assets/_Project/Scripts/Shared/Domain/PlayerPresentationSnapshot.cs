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
        public PlayerPresentationSnapshot(object context, ulong revision, int health, int maxHealth,
            int score, double remainingSeconds, string result, string rejection)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (maxHealth <= 0 || health < 0 || health > maxHealth || score < 0
                || double.IsNaN(remainingSeconds) || double.IsInfinity(remainingSeconds) || remainingSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(health));
            Context = context; Revision = revision; Health = health; MaxHealth = maxHealth;
            Score = score; RemainingSeconds = remainingSeconds; Result = result ?? ""; Rejection = rejection ?? "";
        }
    }
    public interface IPlayerPresentationSource
    {
        // Null until a complete authoritative baseline exists. New token for each life/binding.
        PlayerPresentationSnapshot Current { get; }
        event Action<PlayerPresentationSnapshot> Changed;
    }
}
