using System;
using Mirror;
using UnityEngine;

namespace SmallArena.Networking
{
    public enum PlayerMatchResult : byte { Playing, Victory, Defeat }

    /// <summary>Server-owned hit points; clients observe SyncVar hooks only.</summary>
    [DisallowMultipleComponent]
    public sealed class NetworkHealth : NetworkBehaviour
    {
        [SyncVar, SerializeField] private int maxHealth = 100;
        [SyncVar(hook = nameof(HealthChanged)), SerializeField] private int currentHealth = 100;
        [SyncVar(hook = nameof(ResultChanged))] private PlayerMatchResult result;
        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public bool IsDead => currentHealth <= 0;
        public bool CanAct => !IsDead && result == PlayerMatchResult.Playing;
        public PlayerMatchResult Result => result;
        public event Action<int, int> OnHealthChanged;
        public event Action<PlayerMatchResult> OnResultChanged;
        public event Action OnDeath;

        public override void OnStartServer()
        {
            maxHealth = Mathf.Max(1, maxHealth);
            currentHealth = maxHealth;
            result = PlayerMatchResult.Playing;
        }
        public override void OnStartClient()
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnResultChanged?.Invoke(result);
        }
        [Server]
        public bool TakeDamage(int amount)
        {
            if (!isServer || !CanAct || amount <= 0) return false;
            currentHealth = amount >= currentHealth ? 0 : currentHealth - amount;
            if (currentHealth == 0)
            {
                SetResult(PlayerMatchResult.Defeat);
                OnDeath?.Invoke();
                NetworkMatchRules.ReportDefeat();
            }
            return true;
        }
        [Server]
        internal void SetResult(PlayerMatchResult next)
        {
            if (isServer && result == PlayerMatchResult.Playing) result = next;
        }
        private void HealthChanged(int previous, int current) => OnHealthChanged?.Invoke(current, maxHealth);
        private void ResultChanged(PlayerMatchResult previous, PlayerMatchResult current) => OnResultChanged?.Invoke(current);
    }
}
