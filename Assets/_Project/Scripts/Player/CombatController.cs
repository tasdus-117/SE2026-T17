using System.Collections.Generic;
using System;
using SmallArena.Domain;
using SmallArena.Input;
using SmallArena.Pickups;
using SmallArena.Server.Combat;
using UnityEngine;

namespace SmallArena.Player
{
    /// <summary>Local intent composition for melee and weapon interaction.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputHandler))]
    public sealed class CombatController : MonoBehaviour, IDamageSource
    {
        [SerializeField] private PlayerInputHandler inputHandler;
        [SerializeField] private float punchCooldown = 0.5f;
        [SerializeField] private int punchDamage = 1;
        [SerializeField] private float hitSphereRadius = 0.6f;
        [SerializeField] private Vector3 hitSphereOffset = new Vector3(0f, 1f, 0.8f);
        [SerializeField] private LayerMask targetLayerMask = 1 << 8;

        private float lastPunchTime = -999f;
        private bool matchEnded;
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
            if (inputHandler == null) inputHandler = GetComponent<PlayerInputHandler>();
        }

        private void Update()
        {
            // Collection is independent of punch cooldown. A same-frame E + J uses the buff.
            var interactPressed = inputHandler.ConsumeInteractPressed();
            if (interactPressed && CanInteract) TryCollectNearbyPickup();

            // Always consume: cooldown presses are dropped rather than queued.
            if (!inputHandler.ConsumePunchPressed() || !CanPunch) return;

            lastPunchTime = Time.time;
            var center = transform.position + transform.rotation * hitSphereOffset;
            var hits = Physics.OverlapSphere(center, hitSphereRadius, targetLayerMask, QueryTriggerInteraction.Ignore);
            damagedTargets.Clear();
            foreach (var hit in hits)
            {
                if (matchEnded) break;
                if (hit.transform.IsChildOf(transform)) continue;
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
