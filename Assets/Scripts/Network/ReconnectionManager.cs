using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SmallArena.Network
{
    /// <summary>
    /// Handles player reconnection and state restoration
    /// </summary>
    public class ReconnectionManager : NetworkBehaviour
    {
        public static ReconnectionManager Instance { get; private set; }
        
        [SerializeField] private float reconnectTimeout = 30f;
        [SerializeField] private int maxReconnectAttempts = 3;
        
        // Disconnected player data storage
        private Dictionary<ulong, DisconnectedPlayerData> disconnectedPlayers = new Dictionary<ulong, DisconnectedPlayerData>();
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (IsServer)
            {
                GameNetworkManager.Instance.OnPlayerDisconnected += HandlePlayerDisconnected;
                GameNetworkManager.Instance.OnPlayerConnected += HandlePlayerReconnected;
            }
        }
        
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            
            if (IsServer)
            {
                GameNetworkManager.Instance.OnPlayerDisconnected -= HandlePlayerDisconnected;
                GameNetworkManager.Instance.OnPlayerConnected -= HandlePlayerReconnected;
            }
        }
        
        private void HandlePlayerDisconnected(ulong clientId)
        {
            if (!IsServer) return;
            
            // Store player state for potential reconnection
            var playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
            
            if (playerObject != null)
            {
                var playerData = new DisconnectedPlayerData
                {
                    ClientId = clientId,
                    DisconnectTime = Time.time,
                    Position = playerObject.transform.position,
                    Rotation = playerObject.transform.rotation
                };
                
                // Store health state
                var healthSystem = playerObject.GetComponent<Combat.HealthSystem>();
                if (healthSystem != null)
                {
                    playerData.Health = healthSystem.CurrentHealth;
                    playerData.KillCount = healthSystem.KillCount;
                    playerData.DeathCount = healthSystem.DeathCount;
                }
                
                disconnectedPlayers[clientId] = playerData;
                
                Debug.Log($"Stored state for disconnected player {clientId}");
                
                // Start timeout coroutine
                StartCoroutine(ReconnectTimeoutCoroutine(clientId));
            }
        }
        
        private void HandlePlayerReconnected(ulong clientId)
        {
            if (!IsServer) return;
            
            // Check if this is a reconnecting player
            if (disconnectedPlayers.TryGetValue(clientId, out var playerData))
            {
                Debug.Log($"Player {clientId} reconnecting - restoring state");
                
                // Wait a frame for player object to spawn
                StartCoroutine(RestorePlayerStateCoroutine(clientId, playerData));
            }
        }
        
        private IEnumerator RestorePlayerStateCoroutine(ulong clientId, DisconnectedPlayerData playerData)
        {
            // Wait for player object to be ready
            yield return new WaitForSeconds(0.5f);
            
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                var playerObject = client.PlayerObject;
                
                if (playerObject != null)
                {
                    // Restore position
                    playerObject.transform.position = playerData.Position;
                    playerObject.transform.rotation = playerData.Rotation;
                    
                    // Restore health
                    var healthSystem = playerObject.GetComponent<Combat.HealthSystem>();
                    if (healthSystem != null)
                    {
                        healthSystem.Heal(playerData.Health);
                        // Note: Kill/death counts would need to be stored differently in a real implementation
                    }
                    
                    // Notify player they reconnected
                    NotifyReconnectionClientRpc(clientId);
                    
                    Debug.Log($"Restored state for player {clientId}");
                }
            }
            
            // Remove from disconnected list
            disconnectedPlayers.Remove(clientId);
        }
        
        private IEnumerator ReconnectTimeoutCoroutine(ulong clientId)
        {
            yield return new WaitForSeconds(reconnectTimeout);
            
            // If player hasn't reconnected, clean up their data
            if (disconnectedPlayers.ContainsKey(clientId))
            {
                Debug.Log($"Reconnect timeout for player {clientId} - cleaning up");
                disconnectedPlayers.Remove(clientId);
            }
        }
        
        [ClientRpc]
        private void NotifyReconnectionClientRpc(ulong clientId)
        {
            if (NetworkManager.Singleton.LocalClientId == clientId)
            {
                Debug.Log("Successfully reconnected and state restored!");
                // Could show UI notification here
            }
        }
        
        /// <summary>
        /// Attempt to reconnect to server
        /// </summary>
        public void AttemptReconnect()
        {
            if (IsServer) return;
            
            StartCoroutine(ReconnectCoroutine());
        }
        
        private IEnumerator ReconnectCoroutine()
        {
            int attempts = 0;
            
            while (attempts < maxReconnectAttempts)
            {
                attempts++;
                Debug.Log($"Reconnection attempt {attempts}/{maxReconnectAttempts}");
                
                // Try to reconnect
                bool success = GameNetworkManager.Instance.StartClient();
                
                if (success)
                {
                    Debug.Log("Reconnection successful!");
                    yield break;
                }
                
                // Wait before next attempt
                yield return new WaitForSeconds(2f);
            }
            
            Debug.LogError("Failed to reconnect after maximum attempts");
            // Could show UI message to user
        }
        
        /// <summary>
        /// Check if a player is in reconnection grace period
        /// </summary>
        public bool IsPlayerReconnecting(ulong clientId)
        {
            return disconnectedPlayers.ContainsKey(clientId);
        }
        
        /// <summary>
        /// Get stored player data
        /// </summary>
        public DisconnectedPlayerData GetDisconnectedPlayerData(ulong clientId)
        {
            return disconnectedPlayers.TryGetValue(clientId, out var data) ? data : null;
        }
    }
    
    /// <summary>
    /// Data structure for storing disconnected player state
    /// </summary>
    [System.Serializable]
    public class DisconnectedPlayerData
    {
        public ulong ClientId;
        public float DisconnectTime;
        public Vector3 Position;
        public Quaternion Rotation;
        public float Health;
        public int KillCount;
        public int DeathCount;
    }
}
