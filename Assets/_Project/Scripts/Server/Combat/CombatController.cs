using System.Collections.Generic;
using System;
using Mirror;
using SmallArena.Networking;
using SmallArena.Domain;
using SmallArena.Protocol;
using SmallArena.Pickups;
using SmallArena.Server.Combat;
using UnityEngine;

namespace SmallArena.Player
{
    /// <summary>Local intent composition for melee and weapon interaction.</summary>
    [DisallowMultipleComponent]
    public sealed class CombatController : MonoBehaviour, IDamageSource, IMatchEndParticipant
    {
        [SerializeField] private MonoBehaviour inputHandler = null;
        [SerializeField] private float punchCooldown = MeleeHitGeometry.Cooldown;
        [SerializeField] private int punchDamage = 1;
        [SerializeField] private float hitSphereRadius = MeleeHitGeometry.Radius;
        [SerializeField] private Vector3 hitSphereOffset = MeleeHitGeometry.Offset;
        [SerializeField] private LayerMask targetLayerMask = 1 << 8;

        private float lastPunchTime = -999f;
        private bool matchEnded;
        private ICombatInputSource inputSource;
        private NetworkIdentity netIdentity;
        private readonly HashSet<Health> damagedTargets = new HashSet<Health>();

        public int CurrentDamage => punchDamage;
        public bool CanInteract => !matchEnded;
        public bool CanPunch => !matchEnded && Time.time >= lastPunchTime + punchCooldown;
        public event Action<int> OnDamageChanged;

        public void SetDamage(int newDamage)
        {
            if (matchEnded || punchDamage == newDamage) return;
            punchDamage = newDamage;
            OnDamageChanged?.Invoke(punchDamage);
        }

        public void EndMatch() => matchEnded = true;

        private void Reset() => BindInput();

        private void Awake() => BindInput();

        private void BindInput()
        {
            netIdentity = GetComponent<NetworkIdentity>();
            if (inputHandler == null)
                foreach (var component in GetComponents<MonoBehaviour>())
                    if (component is ICombatInputSource) { inputHandler = component; break; }
            inputSource = inputHandler as ICombatInputSource;
        }

        private void Update()
        {
            // Network actors use the owner Command pipeline, never the offline damage/pickup path.
            if (netIdentity != null) return;
            if (inputHandler == null || inputSource == null) return;
            // Collection is independent of punch cooldown. A same-frame E + J uses the buff.
            var interactPressed = inputSource.ConsumeInteractPressed();
            if (interactPressed && CanInteract) TryCollectNearbyPickup();

            // Always consume: cooldown presses are dropped rather than queued.
            if (!inputSource.ConsumePunchPressed() || !CanPunch) return;

            lastPunchTime = Time.time;
            var center = MeleeHitGeometry.Center(transform, hitSphereOffset);
            var hits = Physics.OverlapSphere(center, MeleeHitGeometry.CandidateRadius(hitSphereRadius), targetLayerMask, QueryTriggerInteraction.Ignore);
            damagedTargets.Clear();
            foreach (var hit in hits)
            {
                if (matchEnded) break;
                if (hit.transform.IsChildOf(transform)
                    || !MeleeHitGeometry.InReach(transform.position, transform.forward, hit.bounds.center)
                    || !MeleeHitGeometry.TouchesSphere(hit, center, hitSphereRadius)) continue;
                var health = hit.GetComponentInParent<Health>();
                if (health != null && damagedTargets.Add(health)) health.TakeDamage(punchDamage);
            }
        }

        private void TryCollectNearbyPickup()
        {
            var nearby = Physics.OverlapSphere(transform.position, 1.5f, LayerMask.GetMask("Pickup"),
                QueryTriggerInteraction.Collide);
            foreach (var collider in nearby)
            {
                var pickup = collider.GetComponentInParent<WeaponPickup>();
                if (pickup != null && pickup.TryCollect(this, transform.position)) break;
            }
        }
    }
}
