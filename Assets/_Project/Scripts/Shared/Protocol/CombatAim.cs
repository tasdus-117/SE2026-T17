using System;

namespace SmallArena.Protocol
{
    /// <summary>Engine-independent aim validation. This type is not a new wire message.</summary>
    public static class CombatAim
    {
        /// <summary>
        /// Logical raycast contract: finite nonzero world-XZ intent becomes a unit direction.
        /// The caller derives the muzzle from authoritative pose; Y is implicitly zero.
        /// Not connected to the legacy melee Command or a camera adapter.
        /// </summary>
        public static bool TryNormalizeHorizontal(float x, float z, out float directionX, out float directionZ)
        {
            directionX = directionZ = 0;
            if (!Finite(x) || !Finite(z)) return false;
            var length = Math.Sqrt((double)x * x + (double)z * z);
            if (length == 0) return false;
            directionX = (float)(x / length);
            directionZ = (float)(z / length);
            return true;
        }

        /// <summary>
        /// Existing melee wire compatibility: finite vector, squared length [0.99,1.01],
        /// dot with world +Z at least 0.99. Do not substitute horizontal normalization:
        /// malformed legacy vectors must be rejected without consuming cooldown.
        /// </summary>
        public static bool IsLegacyMeleeDirection(float x, float y, float z)
        {
            if (!Finite(x) || !Finite(y) || !Finite(z)) return false;
            var squaredLength = x * x + y * y + z * z;
            return squaredLength >= 0.99f && squaredLength <= 1.01f && z >= 0.99f;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
