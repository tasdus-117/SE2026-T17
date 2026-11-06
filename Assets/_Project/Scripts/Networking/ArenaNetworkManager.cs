using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace SmallArena.Networking
{
    /// <summary>Server-owned connection, spawn reservation and teardown lifecycle.</summary>
    public sealed class ArenaNetworkManager : NetworkManager
    {
        private readonly Dictionary<NetworkConnectionToClient, Transform> reservedSpawns = new Dictionary<NetworkConnectionToClient, Transform>();
        private readonly Queue<Transform> releasedSpawns = new Queue<Transform>();

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
            Debug.Log($"[TV1] Client connected: {conn.connectionId}");
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            // Mirror normally enforces this before dispatching the callback; keep direct calls safe too.
            if (!NetworkServer.active || conn == null || conn.identity != null || reservedSpawns.ContainsKey(conn)) return;
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
        }

        public override void OnStopServer()
        {
            reservedSpawns.Clear();
            releasedSpawns.Clear();
            base.OnStopServer();
        }
    }
}
