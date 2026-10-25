using System;
using Unity.Netcode;
using UnityEngine;

namespace SmallArena.Combat
{
    /// <summary>
    /// Authoritative health system - all damage calculations happen on server
    /// </summary>
    public class HealthSystem : NetworkBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float respawnDelay = 3f;
        [SerializeField] private bool canRespawn = true;
        
        // Network variables
        private NetworkVariable<float> currentHealth = new NetworkVariable<float>();
        private NetworkVariable<bool> isDead = new NetworkVariable<bool>();
        
        // Events
        public event Action<float, float> OnHealthChanged; // current, max
        public event Action<ulong> OnDeath; // killer client ID
        public event Action OnRespawn;
        public event Action<float, ulong> OnDamageTaken; // damage amount, attacker ID
        
        // Stats
        private int killCount;
        private int deathCount;
        
        public float CurrentHealth => currentHealth.Value;
        public float MaxHealth => maxHealth;
        public bool IsDead => isDead.Value;
        public int KillCount => killCount;
        public int DeathCount => deathCount;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (IsServer)
            {
                currentHealth.Value = maxHealth;
                isDead.Value = false;
            }
            
            currentHealth.OnValueChanged += HandleHealthChanged;
            isDead.OnValueChanged += HandleDeathStateChanged;
        }
        
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            
            currentHealth.OnValueChanged -= HandleHealthChanged;
            isDead.OnValueChanged -= HandleDeathStateChanged;
        }
        
        /// <summary>
        /// Apply damage (only on server)
        /// </summary>
        public void TakeDamage(float damage, ulong attackerId)
        {
            if (!IsServer)
            {
                Debug.LogWarning("TakeDamage called on client - damage must be applied on server");
                return;
            }
            
            if (isDead.Value)
            {
                return; // Already dead
            }
            
            // Validate damage amount (anti-cheat)
            if (damage < 0 || damage > 100)
            {
                Debug.LogWarning($"Invalid damage amount: {damage} from attacker {attackerId}");
                return;
            }
            
            currentHealth.Value = Mathf.Max(0, currentHealth.Value - damage);
            
            // Notify clients about damage
            NotifyDamageTakenClientRpc(damage, attackerId);
            
            Debug.Log($"Player {OwnerClientId} took {damage} damage from {attackerId}. Health: {currentHealth.Value}/{maxHealth}");
            
            if (currentHealth.Value <= 0)
            {
                Die(attackerId);
            }
        }
        
        /// <summary>
        /// Heal the player
        /// </summary>
        public void Heal(float amount)
        {
            if (!IsServer) return;
            
            if (isDead.Value) return;
            
            currentHealth.Value = Mathf.Min(maxHealth, currentHealth.Value + amount);
        }
        
        private void Die(ulong killerId)
        {
            if (!IsServer) return;
            
            isDead.Value = true;
            deathCount++;
            
            Debug.Log($"Player {OwnerClientId} killed by {killerId}");
            
            // Update killer's kill count
            if (NetworkManager.ConnectedClients.TryGetValue(killerId, out var killerClient))
            {
                var killerHealth = killerClient.PlayerObject?.GetComponent<HealthSystem>();
                if (killerHealth != null)
                {
                    killerHealth.AddKill();
                }
            }
            
            // Notify clients
            NotifyDeathClientRpc(killerId);
            
            // Schedule respawn
            if (canRespawn)
            {
                Invoke(nameof(Respawn), respawnDelay);
            }
        }
        
        private void AddKill()
        {
            if (!IsServer) return;
            killCount++;
        }
        
        private void Respawn()
        {
            if (!IsServer) return;
            
            currentHealth.Value = maxHealth;
            isDead.Value = false;
            
            // Find spawn point
            Vector3 spawnPosition = FindSpawnPoint();
            transform.position = spawnPosition;
            
            // Reset velocity if has rigidbody
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            NotifyRespawnClientRpc(spawnPosition);
            
            Debug.Log($"Player {OwnerClientId} respawned at {spawnPosition}");
        }
        
        private Vector3 FindSpawnPoint()
        {
            // Find spawn points in scene
            var spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
            
            if (spawnPoints.Length > 0)
            {
                // Choose random spawn point
                int index = UnityEngine.Random.Range(0, spawnPoints.Length);
                return spawnPoints[index].transform.position;
            }
            
            // Default spawn if no spawn points found
            return new Vector3(
                UnityEngine.Random.Range(-10f, 10f),
                2f,
                UnityEngine.Random.Range(-10f, 10f)
            );
        }
        
        [ClientRpc]
        private void NotifyDamageTakenClientRpc(float damage, ulong attackerId)
        {
            OnDamageTaken?.Invoke(damage, attackerId);
        }
        
        [ClientRpc]
        private void NotifyDeathClientRpc(ulong killerId)
        {
            OnDeath?.Invoke(killerId);
            
            // Disable controls if this is the local player
            if (IsOwner)
            {
                var controller = GetComponent<Player.PlayerController>();
                if (controller != null)
                {
                    controller.enabled = false;
                }
            }
        }
        
        [ClientRpc]
        private void NotifyRespawnClientRpc(Vector3 spawnPosition)
        {
            transform.position = spawnPosition;
            OnRespawn?.Invoke();
            
            // Re-enable controls if this is the local player
            if (IsOwner)
            {
                var controller = GetComponent<Player.PlayerController>();
                if (controller != null)
                {
                    controller.enabled = true;
                }
            }
        }
        
        private void HandleHealthChanged(float previousValue, float newValue)
        {
            OnHealthChanged?.Invoke(newValue, maxHealth);
        }
        
        private void HandleDeathStateChanged(bool previousValue, bool newValue)
        {
            if (newValue)
            {
                // Player died
                OnDeath?.Invoke(0); // Called again to ensure UI updates
            }
        }
        
        /// <summary>
        /// Force respawn (admin/debug)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ForceRespawnServerRpc()
        {
            if (!IsServer) return;
            Respawn();
        }
        
        /// <summary>
        /// Reset stats
        /// </summary>
        public void ResetStats()
        {
            if (!IsServer) return;
            
            killCount = 0;
            deathCount = 0;
        }
        
        /// <summary>
        /// Get K/D ratio
        /// </summary>
        public float GetKDRatio()
        {
            if (deathCount == 0) return killCount;
            return (float)killCount / deathCount;
        }
    }
}
