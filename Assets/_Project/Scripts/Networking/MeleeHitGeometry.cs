using UnityEngine;

namespace SmallArena.Networking
{
    /// <summary>Shared geometry for server simulation and the offline rules adapter; no client hit resolution.</summary>
    public static class MeleeHitGeometry
    {
        public const float Cooldown = 1f;
        public const float Radius = 1.2f;
        public const float Reach = 1.8f;
        public const float HalfAngle = 70f;
        public static readonly Vector3 Offset = new Vector3(0f, 0.25f, 0.5f);

        // CharacterController query shapes can shrink around their skin/contact boundary.
        // Gather conservatively, then test the intended sphere against the authoritative capsule.
        public static float CandidateRadius(float radius) => radius + 0.5f;

        public static bool TouchesSphere(Collider target, Vector3 center, float radius)
        {
            if (target is CharacterController body)
            {
                var scale = body.transform.lossyScale;
                var bodyRadius = body.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
                var halfSegment = Mathf.Max(0, body.height * Mathf.Abs(scale.y) * 0.5f - bodyRadius);
                var bodyCenter = body.transform.TransformPoint(body.center);
                var axis = body.transform.up;
                var closest = bodyCenter + axis * Mathf.Clamp(Vector3.Dot(center - bodyCenter, axis), -halfSegment, halfSegment);
                var combined = radius + bodyRadius;
                return (center - closest).sqrMagnitude <= combined * combined;
            }
            if (target is MeshCollider mesh && !mesh.convex) return false;
            return (target.ClosestPoint(center) - center).sqrMagnitude <= radius * radius;
        }

        public static Vector3 Center(Transform actor, Vector3 offset)
        {
            // Player roots are at capsule centre, not at the feet. Follow the actual controller
            // centre so the query stays at chest height instead of grazing the head cap.
            var body = actor.GetComponent<CharacterController>();
            var center = body != null ? actor.TransformPoint(body.center) : actor.position;
            return center + actor.rotation * offset;
        }

        public static bool InReach(Vector3 origin, Vector3 forward, Vector3 targetCenter)
        {
            var delta = targetCenter - origin;
            delta.y = 0;
            if (delta.sqrMagnitude > (Reach + 0.001f) * (Reach + 0.001f)) return false;
            return delta.sqrMagnitude < 0.000001f
                || Vector3.Dot(forward, delta.normalized) >= Mathf.Cos(HalfAngle * Mathf.Deg2Rad);
        }
    }
}
