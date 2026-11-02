using System.Collections.Generic;
using SmallArena.Input;
using SmallArena.Server.Combat;
using UnityEngine;

namespace SmallArena.Player
{
    /// <summary>Local melee composition: fresh input, cooldown and one hit per target per punch.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputHandler))]
    public sealed class CombatController : MonoBehaviour
    {
        [SerializeField] private PlayerInputHandler inputHandler;
        [SerializeField] private float punchCooldown = 0.5f;
        [SerializeField] private int punchDamage = 1;
        [SerializeField] private float hitSphereRadius = 0.6f;
        [SerializeField] private Vector3 hitSphereOffset = new Vector3(0f, 1f, 0.8f);
        [SerializeField] private LayerMask targetLayerMask = 1 << 8;

        private float lastPunchTime = -999f;
        private readonly HashSet<Health> damagedTargets = new HashSet<Health>();

        public int CurrentDamage => punchDamage;
        public bool CanPunch => Time.time >= lastPunchTime + punchCooldown;

        public void SetDamage(int newDamage) => punchDamage = newDamage;

        private void Reset() => BindInput();

        private void Awake() => BindInput();

        private void BindInput()
        {
            if (inputHandler == null) inputHandler = GetComponent<PlayerInputHandler>();
        }

        private void Update()
        {
            // Always consume: cooldown presses are dropped rather than queued.
            if (!inputHandler.ConsumePunchPressed() || !CanPunch) return;

            lastPunchTime = Time.time;
            var center = transform.position + transform.rotation * hitSphereOffset;
            var hits = Physics.OverlapSphere(center, hitSphereRadius, targetLayerMask, QueryTriggerInteraction.Ignore);
            damagedTargets.Clear();
            foreach (var hit in hits)
            {
                if (hit.transform.IsChildOf(transform)) continue;
                var health = hit.GetComponentInParent<Health>();
                if (health != null && damagedTargets.Add(health)) health.TakeDamage(punchDamage);
            }
        }
    }
}
