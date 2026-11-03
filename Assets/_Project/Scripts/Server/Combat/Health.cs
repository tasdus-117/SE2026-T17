using System;
using SmallArena.Domain;
using UnityEngine;

namespace SmallArena.Server.Combat
{
    /// <summary>Entity hit points and terminal death notification; independent of input and combat detection.</summary>
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour, IHealthSource
    {
        [SerializeField] private int maxHealth = 10;
        [SerializeField] private int currentHealth = 10;

        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public bool IsDead => currentHealth <= 0;

        public event Action<int, int> OnHealthChanged;
        public event Action OnDeath;

        private void Awake() => ClampHealth();

        private void OnValidate() => ClampHealth();

        private void ClampHealth()
        {
            maxHealth = Mathf.Max(1, maxHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        }

        public bool TakeDamage(int amount)
        {
            if (IsDead || amount <= 0) return false;

            currentHealth = Mathf.Max(0, currentHealth - amount);
            var died = currentHealth == 0;
            try { OnHealthChanged?.Invoke(currentHealth, maxHealth); }
            finally
            {
                // A failing health observer must not suppress the terminal notification.
                if (died) OnDeath?.Invoke();
            }
            return true;
        }
    }
}
