using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace SmallArena.Network
{
    /// <summary>
    /// Main network manager for the game
    /// Handles server/client initialization and connection management
    /// </summary>
    public class GameNetworkManager : MonoBehaviour
    {
        public static GameNetworkManager Instance { get; private set; }
        
        [SerializeField] private NetworkConfig networkConfig;
        [SerializeField] private NetworkManager networkManager;
        
        public NetworkConfig Config => networkConfig;
        public bool IsServer => networkManager != null && networkManager.IsServer;
        public bool IsClient => networkManager != null && networkManager.IsClient;
        public bool IsHost => networkManager != null && networkManager.IsHost;
        
        // Events
        public event Action OnServerStarted;
        public event Action OnClientConnected;
        public event Action OnClientDisconnected;
        public event Action<ulong> OnPlayerConnected;
        public event Action<ulong> OnPlayerDisconnected;
        
        private Dictionary<ulong, PlayerConnectionData> connectedPlayers = new Dictionary<ulong, PlayerConnectionData>();
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (networkManager == null)
            {
                networkManager = GetComponent<NetworkManager>();
            }
            
            InitializeNetworkCallbacks();
        }
        
        private void InitializeNetworkCallbacks()
        {
            if (networkManager != null)
            {
                networkManager.OnServerStarted += HandleServerStarted;
                networkManager.OnClientConnectedCallback += HandleClientConnected;
                networkManager.OnClientDisconnectCallback += HandleClientDisconnected;
            }
        }
        
        /// <summary>
        /// Start the game as a server
        /// </summary>
        public bool StartServer()
        {
            if (networkManager == null)
            {
                Debug.LogError("NetworkManager is null!");
                return false;
            }
            
            ConfigureTransport();
            
            bool success = networkManager.StartServer();
            if (success)
            {
                Debug.Log($"Server started on port {networkConfig.serverPort}");
            }
            else
            {
                Debug.LogError("Failed to start server");
            }
            
            return success;
        }
        
        /// <summary>
        /// Start the game as a host (server + client)
        /// </summary>
        public bool StartHost()
        {
            if (networkManager == null)
            {
                Debug.LogError("NetworkManager is null!");
                return false;
            }
            
            ConfigureTransport();
            
            bool success = networkManager.StartHost();
            if (success)
            {
                Debug.Log($"Host started on port {networkConfig.serverPort}");
            }
            else
            {
                Debug.LogError("Failed to start host");
            }
            
            return success;
        }
        
        /// <summary>
        /// Start the game as a client
        /// </summary>
        public bool StartClient()
        {
            if (networkManager == null)
            {
                Debug.LogError("NetworkManager is null!");
                return false;
            }
            
            ConfigureTransport();
            
            bool success = networkManager.StartClient();
            if (success)
            {
                Debug.Log($"Client connecting to {networkConfig.serverIP}:{networkConfig.serverPort}");
            }
            else
            {
                Debug.LogError("Failed to start client");
            }
            
            return success;
        }
        
        /// <summary>
        /// Shutdown the network connection
        /// </summary>
        public void Shutdown()
        {
            if (networkManager != null && networkManager.IsListening)
            {
                networkManager.Shutdown();
                connectedPlayers.Clear();
                Debug.Log("Network shutdown");
            }
        }
        
        private void ConfigureTransport()
        {
            if (networkManager.NetworkConfig.NetworkTransport is UnityTransport transport)
            {
                transport.ConnectionData.Address = networkConfig.serverIP;
                transport.ConnectionData.Port = networkConfig.serverPort;
                transport.ConnectionData.ServerListenAddress = "0.0.0.0";
                
                Debug.Log($"Transport configured: {networkConfig.serverIP}:{networkConfig.serverPort}");
            }
        }
        
        private void HandleServerStarted()
        {
            Debug.Log("Server started successfully");
            OnServerStarted?.Invoke();
        }
        
        private void HandleClientConnected(ulong clientId)
        {
            Debug.Log($"Client connected: {clientId}");
            
            if (!connectedPlayers.ContainsKey(clientId))
            {
                connectedPlayers[clientId] = new PlayerConnectionData
                {
                    ClientId = clientId,
                    ConnectedTime = Time.time
                };
            }
            
            if (networkManager.IsServer && clientId != networkManager.LocalClientId)
            {
                OnPlayerConnected?.Invoke(clientId);
            }
            else if (clientId == networkManager.LocalClientId)
            {
                OnClientConnected?.Invoke();
            }
        }
        
        private void HandleClientDisconnected(ulong clientId)
        {
            Debug.Log($"Client disconnected: {clientId}");
            
            if (connectedPlayers.ContainsKey(clientId))
            {
                connectedPlayers.Remove(clientId);
            }
            
            if (networkManager.IsServer && clientId != networkManager.LocalClientId)
            {
                OnPlayerDisconnected?.Invoke(clientId);
            }
            else if (clientId == networkManager.LocalClientId)
            {
                OnClientDisconnected?.Invoke();
            }
        }
        
        /// <summary>
        /// Get connection data for a specific client
        /// </summary>
        public PlayerConnectionData GetPlayerConnectionData(ulong clientId)
        {
            return connectedPlayers.TryGetValue(clientId, out var data) ? data : null;
        }
        
        /// <summary>
        /// Get all connected players
        /// </summary>
        public Dictionary<ulong, PlayerConnectionData> GetAllConnectedPlayers()
        {
            return new Dictionary<ulong, PlayerConnectionData>(connectedPlayers);
        }
        
        private void OnDestroy()
        {
            if (networkManager != null)
            {
                networkManager.OnServerStarted -= HandleServerStarted;
                networkManager.OnClientConnectedCallback -= HandleClientConnected;
                networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
            }
        }
    }
    
    /// <summary>
    /// Data structure for player connection information
    /// </summary>
    [Serializable]
    public class PlayerConnectionData
    {
        public ulong ClientId;
        public string PlayerName;
        public float ConnectedTime;
        public int Ping;
        public bool IsReconnecting;
    }
}
