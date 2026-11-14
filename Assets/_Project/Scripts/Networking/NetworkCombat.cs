using System.Collections.Generic;
using Mirror;
using UnityEngine;
using System;

namespace SmallArena.Networking
{
    /// <summary>Trusted melee simulation. No input devices, UI, client pose, or client damage values.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(NetworkHealth))]
    public sealed class NetworkCombat : NetworkBehaviour
    {
        public const float Cooldown = MeleeHitGeometry.Cooldown;
        public const int UnarmedDamage = 1;
        [SyncVar(hook = nameof(DamageChanged))] private int attackDamage = UnarmedDamage;
        public int CurrentDamage => attackDamage;
        public bool CanReceiveWeapon => isServer && health != null && health.CanAct && attackDamage == UnarmedDamage;
        public event Action<int> OnDamageChanged;
        private static readonly Vector3 Offset = MeleeHitGeometry.Offset;
        private const float Radius = MeleeHitGeometry.Radius;
        private double nextAttackTime;
        private NetworkHealth health;
        private readonly HashSet<NetworkHealth> damaged = new HashSet<NetworkHealth>();
        private void Awake() => health = GetComponent<NetworkHealth>();
        public override void OnStartServer() { nextAttackTime = double.NegativeInfinity; attackDamage = UnarmedDamage; }
        public override void OnStartClient() => OnDamageChanged?.Invoke(attackDamage);
        [Server]
        internal void GrantWeapon() { if (CanReceiveWeapon) attackDamage = UnarmedDamage + 1; }
        private void DamageChanged(int previous, int current) => OnDamageChanged?.Invoke(current);

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
            var center = MeleeHitGeometry.Center(transform, Offset);
            var hits = Physics.OverlapSphere(center, MeleeHitGeometry.CandidateRadius(Radius), (1 << 7) | (1 << 8), QueryTriggerInteraction.Ignore);
            damaged.Clear();
            foreach (var hit in hits)
            {
                if (!health.CanAct) break;
                if (hit == null || !MeleeHitGeometry.InReach(transform.position, Vector3.forward, hit.bounds.center)
                    || !MeleeHitGeometry.TouchesSphere(hit, center, Radius)) continue;
                var target = hit.GetComponentInParent<NetworkHealth>();
                if (target == null || target == health || !target.isServer || !target.CanAct || !damaged.Add(target)) continue;
                if (Physics.Linecast(center - Vector3.forward * Offset.z, hit.bounds.center, 1 << 6, QueryTriggerInteraction.Ignore)) continue;
                target.TakeDamage(attackDamage);
            }
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
