using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace SmallArena.Networking
{
    /// <summary>Trusted melee simulation. No input devices, UI, client pose, or client damage values.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(NetworkHealth))]
    public sealed class NetworkCombat : NetworkBehaviour
    {
        public const float Cooldown = 0.5f;
        public const int UnarmedDamage = 10;
        private static readonly Vector3 Offset = new Vector3(0, 1, 0.8f);
        private const float Radius = 0.6f;
        private double nextAttackTime;
        private NetworkHealth health;
        private readonly HashSet<NetworkHealth> damaged = new HashSet<NetworkHealth>();
        private void Awake() => health = GetComponent<NetworkHealth>();
        public override void OnStartServer() => nextAttackTime = double.NegativeInfinity;

        [Server]
        internal void PerformAttack(Vector3 aimDirection)
        {
            // Scene loads finish at different times. Do not resolve a match while a member
            // is still waiting for its authoritative avatar to spawn.
            foreach (var connection in NetworkServer.connections.Values)
                if (connection.isAuthenticated && connection.identity == null) return;
            // This arena keeps world +Z facing. Requests cannot extend range or rotate the hitbox.
            if (!isServer || !health.CanAct || !Finite(aimDirection.x) || !Finite(aimDirection.y) || !Finite(aimDirection.z)
                || aimDirection.sqrMagnitude < 0.99f || aimDirection.sqrMagnitude > 1.01f
                || Vector3.Dot(aimDirection, Vector3.forward) < 0.99f || NetworkTime.localTime < nextAttackTime) return;
            nextAttackTime = NetworkTime.localTime + Cooldown;
            var center = transform.position + Offset;
            var hits = Physics.OverlapSphere(center, Radius, 1 << 7, QueryTriggerInteraction.Ignore);
            damaged.Clear();
            foreach (var hit in hits)
            {
                if (!health.CanAct) break;
                if (hit == null) continue;
                var target = hit.GetComponentInParent<NetworkHealth>();
                if (target == null || target == health || !target.isServer || !target.CanAct || !damaged.Add(target)) continue;
                if (Physics.Linecast(transform.position + Vector3.up, hit.bounds.center, 1 << 6, QueryTriggerInteraction.Ignore)) continue;
                target.TakeDamage(UnarmedDamage);
            }
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
