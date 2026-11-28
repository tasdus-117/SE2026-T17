using System.Collections.Generic;
using SmallArena.Protocol;
using SmallArena.Domain;
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
        public bool CanReceiveWeapon => isServer && health != null && health.CanAct && attackDamage == health.BaseDamage;
        public event Action<int> OnDamageChanged;
        private static readonly Vector3 Offset = MeleeHitGeometry.Offset;
        private const float Radius = MeleeHitGeometry.Radius;
        [SyncVar] private double nextAttackTime;
        public double CooldownRemaining => Math.Max(0, nextAttackTime - NetworkTime.time);
        public static readonly Vector3 ShotMuzzleOffset = new Vector3(0, .25f, 0);
        private NetworkHealth health;
        private readonly HashSet<NetworkHealth> damaged = new HashSet<NetworkHealth>();
        private void Awake() => health = GetComponent<NetworkHealth>();
        public override void OnStartServer() { nextAttackTime = 0; attackDamage = UnarmedDamage; }
        public override void OnStartClient() => OnDamageChanged?.Invoke(attackDamage);
        [Server]
        internal void GrantWeapon() { if (CanReceiveWeapon) attackDamage = health.BaseDamage + 1; }
        [Server] public void ResetForLife()
        {
            if (!isServer) return;
            attackDamage = health.BaseDamage;
            if (!health.IsLifecycleBound) nextAttackTime = 0;
        }
        // Bound combat only mirrors the deadline committed by the match runtime.
        [Server] public void PublishRuntimeDeadline(double deadline) { if (isServer) nextAttackTime = deadline; }
        private bool TryBeginAttack()
        {
            if (health.IsLifecycleBound) return health.TryBeginRuntimeAttack();
            var now = NetworkTime.localTime;
            if (now < nextAttackTime) return false;
            nextAttackTime = now + health.AttackCooldownSeconds;
            return true;
        }
        private void DamageChanged(int previous, int current) => OnDamageChanged?.Invoke(current);

        [Server] internal ShotRejection PerformShot(float aimX, float aimZ, out ShotOutcome outcome)
        {
            outcome = ShotOutcome.Rejected;
            if (!isServer || !health.IsLifecycleBound || !health.CanAct) return ShotRejection.NotPlaying;
            if (!CombatAim.TryNormalizeHorizontal(aimX, aimZ, out var dx, out var dz)) return ShotRejection.InvalidAim;
            if (!TryBeginAttack()) return ShotRejection.Cooldown;
            var origin = transform.position + ShotMuzzleOffset;
            // Unity rays do not report a collider containing their origin.
            foreach (var overlap in Physics.OverlapSphere(origin, .02f, 1 << 6, QueryTriggerInteraction.Ignore))
                if (!overlap.transform.IsChildOf(transform)) { outcome = ShotOutcome.Blocked; return ShotRejection.None; }
            var hits = Physics.RaycastAll(origin, new Vector3(dx, 0, dz), (float)Math.Min(float.MaxValue, health.ShotRangeMetres),
                (1 << 6) | (1 << 7) | (1 << 8), QueryTriggerInteraction.Ignore);
            double targetDistance = double.PositiveInfinity, blockerDistance = double.PositiveInfinity;
            NetworkHealth nearestTarget = null;
            foreach (var hit in hits)
            {
                if (hit.transform.IsChildOf(transform)) continue;
                var target = hit.collider.GetComponentInParent<NetworkHealth>();
                if (target != null && target != health && target.isServer && target.IsLifecycleBound
                    && target.SessionId == health.SessionId && target.LifeId > 0 && target.CanAct)
                {
                    if (hit.distance < targetDistance) { targetDistance = hit.distance; nearestTarget = target; }
                }
                else blockerDistance = Math.Min(blockerDistance, hit.distance);
            }
            outcome = ShotHitRules.Resolve(targetDistance, blockerDistance, health.ShotRangeMetres);
            if (outcome == ShotOutcome.Hit && !nearestTarget.TakeDamageFrom(health, attackDamage)) outcome = ShotOutcome.Missed;
            return ShotRejection.None;
        }

        [Server]
        internal void PerformAttack(Vector3 aimDirection)
        {
            // Bound matches use the sealed session roster and its spawn deadline, not transport-wide counts.
            if (!health.IsLifecycleBound)
                foreach (var connection in NetworkServer.connections.Values)
                    if (connection.isAuthenticated && connection.identity == null) return;
            // This arena keeps world +Z facing. Requests cannot extend range or rotate the hitbox.
            if (!isServer || !health.CanAct
                || !CombatAim.IsLegacyMeleeDirection(aimDirection.x, aimDirection.y, aimDirection.z)
                || !TryBeginAttack()) return;
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
                target.TakeDamageFrom(health, attackDamage);
            }
        }
    }
}
