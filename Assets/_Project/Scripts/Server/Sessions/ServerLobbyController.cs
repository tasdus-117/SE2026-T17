using Mirror;
using SmallArena.Networking;
using UnityEngine;

namespace SmallArena.Server.Sessions
{
    /// <summary>Server session coordinator. UI and network bridges cannot mutate room rules.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ArenaNetworkManager))]
    public sealed class ServerLobbyController : MonoBehaviour
    {
        private ArenaNetworkManager manager;
        private readonly LobbyRoom room = new LobbyRoom();
        private LobbyStartOperation startOperation;
        public LobbyRoom Room => room;

        private void OnEnable()
        {
            manager = GetComponent<ArenaNetworkManager>();
            manager.ServerStarted += Open;
            manager.ServerStopped += Close;
            manager.ServerConnected += Join;
            manager.ServerDisconnected += Leave;
            manager.ServerSceneLoaded += SceneLoaded;
            manager.ServerCanSpawnPlayer = CanSpawn;
        }
        private void OnDisable()
        {
            if (manager == null) return;
            manager.ServerStarted -= Open;
            manager.ServerStopped -= Close;
            manager.ServerConnected -= Join;
            manager.ServerDisconnected -= Leave;
            manager.ServerSceneLoaded -= SceneLoaded;
            manager.ServerCanSpawnPlayer = null;
            Close();
        }
        private bool CanSpawn() => !manager.RoomEnabled || room.Phase == LobbyPhase.Playing;
        private void Open()
        {
            if (!manager.RoomEnabled) return;
            room.Open();
            startOperation = new LobbyStartOperation(room, manager.LoadRoomArena, Publish, error => manager.AbortRoomStart(error.Message));
            NetworkServer.RegisterHandler<LobbyReadyRequest>(Ready);
            NetworkServer.RegisterHandler<LobbyStartRequest>(StartMatch);
        }
        private void Close() { startOperation = null; room.Close(); }
        private void Join(NetworkConnectionToClient connection)
        {
            if (!manager.RoomEnabled) return;
            if (!room.TryJoin(connection.connectionId, connection == NetworkServer.localConnection))
            {
                manager.RejectRoomConnection(connection, room.Count >= LobbyRoom.Capacity
                    ? LobbyLimits.FullReason : "The match has already started.");
                return;
            }
            Publish();
        }
        private void Leave(NetworkConnectionToClient connection)
        {
            if (room.Remove(connection.connectionId)) Publish();
        }
        private void Ready(NetworkConnectionToClient connection, LobbyReadyRequest request)
        {
            if (room.SetReady(connection.connectionId, request.IsReady)) Publish();
        }
        private void StartMatch(NetworkConnectionToClient connection, LobbyStartRequest request)
        {
            startOperation?.TryStart(connection.connectionId);
        }
        private void SceneLoaded(string scene)
        {
            if (!manager.RoomEnabled || !scene.EndsWith("Arena_MVP.unity") && scene != "Arena_MVP") return;
            startOperation?.Complete();
        }
        private void Publish()
        {
            if (!NetworkServer.active) return;
            var members = room.Members;
            foreach (var connection in NetworkServer.connections.Values)
                if (connection.isAuthenticated)
                    connection.Send(new LobbySnapshot { Phase = room.Phase, LocalConnectionId = connection.connectionId, CanStart = room.CanStart, Members = members });
        }
    }
}
