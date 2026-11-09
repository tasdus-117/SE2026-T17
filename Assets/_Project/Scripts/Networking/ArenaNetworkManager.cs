using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SmallArena.Networking
{
    /// <summary>Server-owned connection, spawn reservation and teardown lifecycle.</summary>
    public sealed class ArenaNetworkManager : NetworkManager
    {
        private readonly Dictionary<NetworkConnectionToClient, Transform> reservedSpawns = new Dictionary<NetworkConnectionToClient, Transform>();
        private readonly Queue<Transform> releasedSpawns = new Queue<Transform>();
        [SerializeField] private bool roomEnabled = false;
        public bool RoomEnabled => roomEnabled;
        public LobbySnapshot CurrentRoom { get; private set; }
        public string ConnectionStatus { get; private set; } = "";
        public event Action RoomChanged;
        public event Action ServerStarted;
        public event Action ServerStopped;
        public event Action<NetworkConnectionToClient> ServerConnected;
        public event Action<NetworkConnectionToClient> ServerDisconnected;
        public event Action<string> ServerSceneLoaded;
        public Func<bool> ServerCanSpawnPlayer { get; set; }

        public override void Awake()
        {
            // A returning menu must adopt the session root instead of replacing its singleton.
            if (roomEnabled && singleton is ArenaNetworkManager existing && existing != null && existing != this
                && (existing.gameObject.scene.name == "DontDestroyOnLoad" || existing.gameObject.scene == gameObject.scene))
            {
                Destroy(gameObject);
                return;
            }
            base.Awake();
        }

        public void HostRoom()
        {
            if (!roomEnabled || NetworkServer.active || NetworkClient.active) return;
            ConfigureRoom();
            ConnectionStatus = "Creating room...";
            StartHost();
            RoomChanged?.Invoke();
        }
        public void JoinRoom(string address)
        {
            if (!roomEnabled || NetworkServer.active || NetworkClient.active) return;
            ConfigureRoom();
            networkAddress = string.IsNullOrWhiteSpace(address) ? "localhost" : address.Trim();
            ConnectionStatus = "Connecting...";
            StartClient();
            RoomChanged?.Invoke();
        }
        private void ConfigureRoom()
        {
            dontDestroyOnLoad = true;
            DontDestroyOnLoad(gameObject);
            maxConnections = 4;
            autoCreatePlayer = false;
            onlineScene = "";
            offlineScene = "Assets/_Project/Scenes/MainMenu.unity";
            CurrentRoom = default;
        }
        public void LeaveRoom()
        {
            ConnectionStatus = "";
            var returnScene = offlineScene;
            if (SceneManager.GetActiveScene().name == "MainMenu") offlineScene = "";
            if (NetworkServer.active && NetworkClient.activeHost) StopHost();
            else if (NetworkClient.active) StopClient();
            offlineScene = returnScene;
        }
        public void LeaveMatch() => MatchSessionNavigation.ReturnToMainMenu();
        internal void ClearRoomClientState()
        {
            CurrentRoom = default;
            ConnectionStatus = "";
            RoomChanged?.Invoke();
        }
        public void RequestReady(bool ready)
        {
            if (roomEnabled && NetworkClient.isConnected) NetworkClient.Send(new LobbyReadyRequest { IsReady = ready });
        }
        public void RequestRoomStart()
        {
            if (roomEnabled && NetworkClient.isConnected) NetworkClient.Send(new LobbyStartRequest());
        }
        // Called only by the server session coordinator after its host/readiness checks.
        public void LoadRoomArena()
        {
            if (!NetworkServer.active || !roomEnabled) return;
            onlineScene = "Assets/_Project/Scenes/Arena_MVP.unity";
            autoCreatePlayer = true;
            ServerChangeScene(onlineScene);
        }
        public override void OnStartServer()
        {
            base.OnStartServer();
            ServerStarted?.Invoke();
        }
        public override void OnStartClient()
        {
            if (roomEnabled)
            {
                NetworkClient.RegisterHandler<LobbySnapshot>(snapshot =>
                {
                    CurrentRoom = snapshot;
                    ConnectionStatus = "";
                    RoomChanged?.Invoke();
                });
                NetworkClient.RegisterHandler<LobbyRejection>(message =>
                {
                    ConnectionStatus = message.Reason;
                    RoomChanged?.Invoke();
                });
            }
            base.OnStartClient();
        }
        public override void OnClientDisconnect()
        {
            if (!string.IsNullOrEmpty(offlineScene) && (SceneManager.GetActiveScene().name == "Arena_MVP" || networkSceneName.EndsWith("Arena_MVP.unity")))
                MatchSessionNavigation.ReturnToMainMenu();
            if (roomEnabled && ConnectionStatus != "")
                ConnectionStatus = "Unable to join. The room may be full, closed, or unavailable.";
            else if (roomEnabled) ConnectionStatus = "Disconnected from room.";
            base.OnClientDisconnect();
        }
        public override void OnStopClient()
        {
            CurrentRoom = default;
            RoomChanged?.Invoke();
            base.OnStopClient();
        }
        public override void OnServerSceneChanged(string sceneName)
        {
            base.OnServerSceneChanged(sceneName);
            ServerSceneLoaded?.Invoke(sceneName);
        }
        public override void OnClientSceneChanged()
        {
            if (roomEnabled) autoCreatePlayer = true;
            base.OnClientSceneChanged();
        }

        public override void Start()
        {
            var launch = ArenaLaunchOptions.Read(System.Environment.GetCommandLineArgs());
            if (launch.Role == ArenaNetworkRole.None) { base.Start(); return; }
            // Explicit client/host roles take precedence over the headless server default.
            networkAddress = launch.Address;
            if (launch.Port != 0 && transport is PortTransport portTransport) portTransport.Port = launch.Port;
            if (launch.Role == ArenaNetworkRole.Server) StartServer();
            else if (launch.Role == ArenaNetworkRole.Client) StartClient();
            else StartHost();
        }

        [ContextMenu("TV1/Start Host (Play Mode)")]
        private void StartInspectorHost() { if (Application.isPlaying && mode == NetworkManagerMode.Offline) StartHost(); }

        [ContextMenu("TV1/Start Client (Play Mode)")]
        private void StartInspectorClient() { if (Application.isPlaying && mode == NetworkManagerMode.Offline) StartClient(); }

        [ContextMenu("TV1/Start Server (Play Mode)")]
        private void StartInspectorServer() { if (Application.isPlaying && mode == NetworkManagerMode.Offline) StartServer(); }

        public override void OnServerConnect(NetworkConnectionToClient conn)
        {
            base.OnServerConnect(conn);
            ServerConnected?.Invoke(conn);
            Debug.Log($"[TV1] Client connected: {conn.connectionId}");
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            // Mirror normally enforces this before dispatching the callback; keep direct calls safe too.
            if (!NetworkServer.active || conn == null || conn.identity != null || reservedSpawns.ContainsKey(conn)) return;
            if (ServerCanSpawnPlayer != null && !ServerCanSpawnPlayer()) return;
            if (playerPrefab == null || playerPrefab.GetComponent<NetworkPlayer>() == null
                || playerPrefab.GetComponent<NetworkIdentity>() == null)
            {
                Debug.LogError("[TV1] Player prefab requires NetworkIdentity and NetworkPlayer.");
                return;
            }

            Transform start = null;
            while (releasedSpawns.Count > 0 && start == null)
            {
                var released = releasedSpawns.Dequeue();
                if (released != null && startPositions.Contains(released) && !reservedSpawns.ContainsValue(released)) start = released;
            }
            // GetStartPosition advances Mirror's round-robin cursor. Skip slots still in use after a reconnect.
            for (var attempt = 0; start == null && attempt < startPositions.Count; attempt++)
            {
                var candidate = GetStartPosition();
                if (candidate != null && !reservedSpawns.ContainsValue(candidate)) { start = candidate; break; }
            }
            if (start == null)
            {
                Debug.LogWarning($"[TV1] No free server spawn for connection {conn.connectionId}.");
                return;
            }

            // Reserve before callbacks can run. Key by connection instance so a delayed old disconnect
            // cannot free the slot of a new connection that happens to reuse its numeric ID.
            reservedSpawns.Add(conn, start);
            GameObject player = null;
            try
            {
                player = Instantiate(playerPrefab, start.position, start.rotation);
                player.name = $"{playerPrefab.name} [connId={conn.connectionId}]";
                player.GetComponent<NetworkPlayer>().SetConnectionId(conn.connectionId);
                if (!NetworkServer.AddPlayerForConnection(conn, player))
                {
                    reservedSpawns.Remove(conn);
                    Destroy(player);
                }
            }
            catch
            {
                reservedSpawns.Remove(conn);
                if (player != null)
                {
                    if (player.GetComponent<NetworkIdentity>().netId != 0) NetworkServer.Destroy(player);
                    else Destroy(player);
                }
                throw;
            }
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            if (conn == null) return;
            reservedSpawns.TryGetValue(conn, out var released);
            try
            {
                NetworkServer.DestroyPlayerForConnection(conn);
                // Mirror's base cleanup is idempotent and clears owned/observer references.
                base.OnServerDisconnect(conn);
            }
            finally
            {
                if (reservedSpawns.Remove(conn) && released != null) releasedSpawns.Enqueue(released);
            }
            Debug.Log($"[TV1] Client disconnected: {conn.connectionId}");
            ServerDisconnected?.Invoke(conn);
        }

        public override void OnStopServer()
        {
            reservedSpawns.Clear();
            releasedSpawns.Clear();
            ServerStopped?.Invoke();
            base.OnStopServer();
        }
    }
}
