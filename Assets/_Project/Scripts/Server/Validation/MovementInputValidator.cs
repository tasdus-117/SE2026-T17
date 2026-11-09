using System;
using UnityEngine;

namespace SmallArena.Server.Validation
{
    public static class MovementInputValidator
    {
        public static bool IsNewer(uint sequence, uint previous) => unchecked((int)(sequence - previous)) > 0;

        public static bool TryClamp(float x, float z, out Vector2 move)
        {
            move = Vector2.zero;
            if (float.IsNaN(x) || float.IsInfinity(x) || float.IsNaN(z) || float.IsInfinity(z)) return false;
            // Double precision prevents float.MaxValue payloads overflowing normalization to infinity.
            var length = Math.Sqrt((double)x * x + (double)z * z);
            move = length > 1d ? new Vector2((float)(x / length), (float)(z / length)) : new Vector2(x, z);
            move = Vector2.ClampMagnitude(move, 1f);
            return true;
        }
    }
}
