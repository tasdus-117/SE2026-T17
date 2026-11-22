using System;

namespace SmallArena.Domain
{
    public enum ShotRejection : byte { None, WrongOwner, WrongLife, NotPlaying, StaleSequence, InvalidTime, Expired, Cooldown, InvalidAim }

    /// <summary>Per-life replay/age gate. Ownership, phase, aim and effects remain server responsibilities.</summary>
    public sealed class ShotRequestGate
    {
        public const double MaximumAgeSeconds = .25;
        private long life;
        private ulong lastSequence;
        public void Reset() { life = 0; lastSequence = 0; }
        public ShotRejection Process(long currentLife, long requestedLife, ulong sequence, double timestamp, double now)
        {
            if (currentLife <= 0 || requestedLife != currentLife) return ShotRejection.WrongLife;
            if (life != currentLife) { life = currentLife; lastSequence = 0; }
            if (sequence <= lastSequence) return ShotRejection.StaleSequence;
            // A rejected request is processed once too; retrying cannot make it fresh later.
            lastSequence = sequence;
            if (!Finite(now) || now < 0 || !Finite(timestamp) || timestamp < 0) return ShotRejection.InvalidTime;
            return now - Math.Min(now, timestamp) > MaximumAgeSeconds ? ShotRejection.Expired : ShotRejection.None;
        }
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
