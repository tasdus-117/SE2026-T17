using SmallArena.Protocol;
using UnityEngine;

namespace SmallArena.Client
{
    /// <summary>Intent only: intersect a horizontal plane through an explicitly supplied muzzle.</summary>
    public static class MouseAim
    {
        public static bool TryDirection(Ray ray, Vector3 muzzle, out Vector3 direction)
        {
            direction = Vector3.zero;
            if (!Finite(ray.origin) || !Finite(ray.direction) || !Finite(muzzle)
                || Mathf.Abs(ray.direction.y) < .000001f) return false;
            var distance = ((double)muzzle.y - ray.origin.y) / ray.direction.y;
            if (distance <= 0 || double.IsInfinity(distance) || double.IsNaN(distance)) return false;
            var x = (float)(ray.origin.x + distance * ray.direction.x - muzzle.x);
            var z = (float)(ray.origin.z + distance * ray.direction.z - muzzle.z);
            if (!CombatAim.TryNormalizeHorizontal(x, z, out var dx, out var dz)) return false;
            direction = new Vector3(dx, 0, dz);
            return true;
        }
        private static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
