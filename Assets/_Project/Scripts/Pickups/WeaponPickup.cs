using System;
using SmallArena.Player;
using UnityEngine;

namespace SmallArena.Pickups
{
    /// <summary>One-time, range-checked local collection; no UI or match-state responsibilities.</summary>
    [DisallowMultipleComponent]
    public sealed class WeaponPickup : MonoBehaviour
    {
        [SerializeField] private float interactionRadius = 1.5f;
        [SerializeField] private int buffedDamage = 2;
        [SerializeField] private Collider pickupCollider;
        [SerializeField] private Renderer pickupRenderer;

        private bool isCollected = false;

        public bool IsCollected => isCollected;
        public float InteractionRadius => interactionRadius;
        public event Action<WeaponPickup> OnCollected;

        private void Reset() => BindComponents();

        private void Awake() => BindComponents();

        private void BindComponents()
        {
            if (pickupCollider == null) pickupCollider = GetComponent<Collider>();
            if (pickupRenderer == null) pickupRenderer = GetComponent<Renderer>();
        }

        public bool CanCollect(Vector3 playerPosition) => !isCollected
            && Vector3.Distance(transform.position, playerPosition) <= interactionRadius;

        public bool TryCollect(CombatController combatController, Vector3 playerPosition)
        {
            if (!CanCollect(playerPosition) || combatController == null) return false;

            // Commit before publishing so observers cannot collect the same pickup twice.
            isCollected = true;
            combatController.SetDamage(buffedDamage);
            if (pickupCollider != null) pickupCollider.enabled = false;
            if (pickupRenderer != null) pickupRenderer.enabled = false;
            OnCollected?.Invoke(this);
            return true;
        }
    }
}
