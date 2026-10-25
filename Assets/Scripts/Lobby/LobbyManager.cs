using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SmallArena.Lobby
{
    /// <summary>
    /// Manages lobby system, player readiness, and match starting
    /// </summary>
    public class LobbyManager : NetworkBehaviour
    {
        public static LobbyManager Instance { get; private set; }
        
        [SerializeField] private int maxPlayers = 4;
        [SerializeField] private string arenaSceneName = "ArenaScene";
        
        // Network variables
        private NetworkList<LobbyPlayerData> lobbyPlayers;
        
        // Events
        public event Action<LobbyPlayerData> OnPlayerJoinedLobby;
        public event Action<ulong> OnPlayerLeftLobby;
        public event Action<ulong, bool> OnPlayerReadyChanged;
        public event Action OnMatchStarting;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            lobbyPlayers = new NetworkList<LobbyPlayerData>();
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (IsServer)
            {
                Network.GameNetworkManager.Instance.OnPlayerConnected += HandlePlayerConnected;
                Network.GameNetworkManager.Instance.OnPlayerDisconnected += HandlePlayerDisconnected;
            }
            
            lobbyPlayers.OnListChanged += HandleLobbyPlayersChanged;
        }
        
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            
            if (IsServer)
            {
                Network.GameNetworkManager.Instance.OnPlayerConnected -= HandlePlayerConnected;
                Network.GameNetworkManager.Instance.OnPlayerDisconnected -= HandlePlayerDisconnected;
            }
            
            lobbyPlayers.OnListChanged -= HandleLobbyPlayersChanged;
        }
        
        private void HandlePlayerConnected(ulong clientId)
        {
            if (!IsServer) return;
            
            if (lobbyPlayers.Count >= maxPlayers)
            {
                Debug.LogWarning($"Lobby is full. Disconnecting client {clientId}");
                NetworkManager.Singleton.DisconnectClient(clientId);
                return;
            }
            
            var playerData = new LobbyPlayerData
            {
                ClientId = clientId,
                PlayerName = $"Player {clientId}",
                IsReady = false
            };
            
            lobbyPlayers.Add(playerData);
            Debug.Log($"Player {clientId} joined lobby. Total players: {lobbyPlayers.Count}");
        }
        
        private void HandlePlayerDisconnected(ulong clientId)
        {
            if (!IsServer) return;
            
            for (int i = 0; i < lobbyPlayers.Count; i++)
            {
                if (lobbyPlayers[i].ClientId == clientId)
                {
                    lobbyPlayers.RemoveAt(i);
                    Debug.Log($"Player {clientId} left lobby. Total players: {lobbyPlayers.Count}");
                    break;
                }
            }
        }
        
        private void HandleLobbyPlayersChanged(NetworkListEvent<LobbyPlayerData> changeEvent)
        {
            switch (changeEvent.Type)
            {
                case NetworkListEvent<LobbyPlayerData>.EventType.Add:
                    OnPlayerJoinedLobby?.Invoke(changeEvent.Value);
                    break;
                    
                case NetworkListEvent<LobbyPlayerData>.EventType.Remove:
                    OnPlayerLeftLobby?.Invoke(changeEvent.Value.ClientId);
                    break;
                    
                case NetworkListEvent<LobbyPlayerData>.EventType.Value:
                    OnPlayerReadyChanged?.Invoke(changeEvent.Value.ClientId, changeEvent.Value.IsReady);
                    break;
            }
        }
        
        /// <summary>
        /// Set player ready status (called by clients)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SetPlayerReadyServerRpc(ulong clientId, bool isReady)
        {
            for (int i = 0; i < lobbyPlayers.Count; i++)
            {
                if (lobbyPlayers[i].ClientId == clientId)
                {
                    var playerData = lobbyPlayers[i];
                    playerData.IsReady = isReady;
                    lobbyPlayers[i] = playerData;
                    
                    Debug.Log($"Player {clientId} ready status: {isReady}");
                    
                    // Check if all players are ready
                    CheckAllPlayersReady();
                    break;
                }
            }
        }
        
        /// <summary>
        /// Update player name
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SetPlayerNameServerRpc(ulong clientId, string playerName)
        {
            for (int i = 0; i < lobbyPlayers.Count; i++)
            {
                if (lobbyPlayers[i].ClientId == clientId)
                {
                    var playerData = lobbyPlayers[i];
                    playerData.PlayerName = playerName;
                    lobbyPlayers[i] = playerData;
                    
                    Debug.Log($"Player {clientId} name set to: {playerName}");
                    break;
                }
            }
        }
        
        private void CheckAllPlayersReady()
        {
            if (!IsServer) return;
            
            // Need at least 2 players
            if (lobbyPlayers.Count < 2)
            {
                return;
            }
            
            // Check if all players are ready
            bool allReady = true;
            foreach (var player in lobbyPlayers)
            {
                if (!player.IsReady)
                {
                    allReady = false;
                    break;
                }
            }
            
            if (allReady)
            {
                Debug.Log("All players ready! Starting match...");
                StartMatch();
            }
        }
        
        /// <summary>
        /// Force start match (host only)
        /// </summary>
        public void ForceStartMatch()
        {
            if (!IsServer)
            {
                Debug.LogWarning("Only server can force start match");
                return;
            }
            
            if (lobbyPlayers.Count < 2)
            {
                Debug.LogWarning("Need at least 2 players to start match");
                return;
            }
            
            StartMatch();
        }
        
        private void StartMatch()
        {
            OnMatchStarting?.Invoke();
            NotifyMatchStartingClientRpc();
            
            // Load arena scene
            NetworkManager.Singleton.SceneManager.LoadScene(arenaSceneName, LoadSceneMode.Single);
        }
        
        [ClientRpc]
        private void NotifyMatchStartingClientRpc()
        {
            Debug.Log("Match is starting!");
            OnMatchStarting?.Invoke();
        }
        
        /// <summary>
        /// Get all players in lobby
        /// </summary>
        public List<LobbyPlayerData> GetLobbyPlayers()
        {
            var players = new List<LobbyPlayerData>();
            foreach (var player in lobbyPlayers)
            {
                players.Add(player);
            }
            return players;
        }
        
        /// <summary>
        /// Get player data by client ID
        /// </summary>
        public LobbyPlayerData? GetPlayerData(ulong clientId)
        {
            foreach (var player in lobbyPlayers)
            {
                if (player.ClientId == clientId)
                {
                    return player;
                }
            }
            return null;
        }
        
        /// <summary>
        /// Get current player count
        /// </summary>
        public int GetPlayerCount()
        {
            return lobbyPlayers.Count;
        }
        
        /// <summary>
        /// Check if lobby is full
        /// </summary>
        public bool IsLobbyFull()
        {
            return lobbyPlayers.Count >= maxPlayers;
        }
    }
    
    /// <summary>
    /// Data structure for lobby player information
    /// </summary>
    [Serializable]
    public struct LobbyPlayerData : INetworkSerializable, IEquatable<LobbyPlayerData>
    {
        public ulong ClientId;
        public FixedString64Bytes PlayerName;
        public bool IsReady;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref PlayerName);
            serializer.SerializeValue(ref IsReady);
        }
        
        public bool Equals(LobbyPlayerData other)
        {
            return ClientId == other.ClientId;
        }
    }
}
