using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace SmallArena.Networking
{
    /// <summary>Server-owned connection, spawn reservation and teardown lifecycle.</summary>
    public sealed class ArenaNetworkManager : NetworkManager
    {
        private readonly Dictionary<int, Transform> reservedSpawns = new Dictionary<int, Transform>();

        public override void OnServerConnect(NetworkConnectionToClient conn)
        {
            base.OnServerConnect(conn);
            Debug.Log($"[TV1] Client connected: {conn.connectionId}");
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            // Mirror normally enforces this before dispatching the callback; keep direct calls safe too.
            if (!NetworkServer.active || conn == null || conn.identity != null) return;
            if (playerPrefab == null || playerPrefab.GetComponent<NetworkPlayer>() == null
                || playerPrefab.GetComponent<NetworkIdentity>() == null)
            {
                Debug.LogError("[TV1] Player prefab requires NetworkIdentity and NetworkPlayer.");
                return;
            }

            Transform start = null;
            // GetStartPosition advances Mirror's round-robin cursor. Skip slots still in use after a reconnect.
            for (var attempt = 0; attempt < startPositions.Count; attempt++)
            {
                var candidate = GetStartPosition();
                if (candidate != null && !reservedSpawns.ContainsValue(candidate)) { start = candidate; break; }
            }
            if (start == null)
            {
                Debug.LogWarning($"[TV1] No free server spawn for connection {conn.connectionId}.");
                return;
            }

            var player = Instantiate(playerPrefab, start.position, start.rotation);
            player.name = $"{playerPrefab.name} [connId={conn.connectionId}]";
            player.GetComponent<NetworkPlayer>().SetConnectionId(conn.connectionId);
            if (NetworkServer.AddPlayerForConnection(conn, player)) reservedSpawns[conn.connectionId] = start;
            else Destroy(player);
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            if (conn == null) return;
            reservedSpawns.Remove(conn.connectionId);
            NetworkServer.DestroyPlayerForConnection(conn);
            // Mirror's base cleanup is idempotent and also clears observer/connection references.
            base.OnServerDisconnect(conn);
            Debug.Log($"[TV1] Client disconnected: {conn.connectionId}");
        }

        public override void OnStopServer()
        {
            reservedSpawns.Clear();
            base.OnStopServer();
        }
    }
}
