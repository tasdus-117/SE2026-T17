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
        public string SessionId { get; private set; } = "";

        private void OnEnable()
        {
            manager = GetComponent<ArenaNetworkManager>();
            manager.ServerStarted += Open;
            manager.ServerStopped += Close;
            manager.ServerConnected += Join;
            manager.ServerDisconnected += Leave;
            manager.ServerSceneLoaded += SceneLoaded;
            manager.RoomDirectoryIdentityChanged += Publish;
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
            manager.RoomDirectoryIdentityChanged -= Publish;
            manager.ServerCanSpawnPlayer = null;
            Close();
        }
        private bool CanSpawn() => !manager.RoomEnabled || room.Phase == LobbyPhase.Playing;
        private void Open()
        {
            if (!manager.RoomEnabled) return;
            room.Open();
            SessionId = System.Guid.NewGuid().ToString("N");
            startOperation = new LobbyStartOperation(room, () =>
            {
                var lifecycle = GetComponent<ServerMatchLifecycleController>();
                if (lifecycle != null) lifecycle.BeginSession(SessionId, room.Members);
                manager.LoadRoomArena();
            }, Publish, error => manager.AbortRoomStart(error.Message));
            NetworkServer.RegisterHandler<LobbyReadyRequest>(Ready);
            NetworkServer.RegisterHandler<LobbyStartRequest>(StartMatch);
            NetworkServer.RegisterHandler<LobbyReturnRequest>((connection, _) =>
            {
                foreach (var member in room.Members)
                    if (member.ConnectionId == connection.connectionId && member.IsHost) { ReturnToLobby(); break; }
            });
        }
        private void Close() { startOperation = null; room.Close(); SessionId = ""; }
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
            if (manager.RoomEnabled && (scene == "MainMenu" || scene.EndsWith("/MainMenu.unity"))) { Publish(); return; }
            if (!manager.RoomEnabled || !scene.EndsWith("Arena_MVP.unity") && scene != "Arena_MVP") return;
            startOperation?.Complete();
        }
        public bool ReturnToLobby()
        {
            var lifecycle = GetComponent<ServerMatchLifecycleController>();
            if (!NetworkServer.active || !manager.RoomEnabled || NetworkServer.isLoadingScene
                || lifecycle == null || lifecycle.Session == null || lifecycle.Session.Phase != SmallArena.Domain.MatchLifecyclePhase.Ended
                || !room.ReturnToWaiting()) return false;
            GetComponent<SmallArena.Server.Combat.ServerLootRespawnController>()?.ResetForLobby();
            lifecycle.ResetForLobby();
            GetComponent<ArenaLootSpawner>()?.ResetRound();
            SessionId = System.Guid.NewGuid().ToString("N");
            Publish(); manager.LoadRoomMenu(); return true;
        }
        private void Publish()
        {
            if (!NetworkServer.active) return;
            var members = room.Members;
            manager.UpdateRoomDirectory(room.Phase, members.Length);
            foreach (var connection in NetworkServer.connections.Values)
                if (connection.isAuthenticated && System.Array.Exists(members, member => member.ConnectionId == connection.connectionId))
                    connection.Send(new LobbySnapshot { Phase = room.Phase, LocalConnectionId = connection.connectionId, CanStart = room.CanStart, Members = members,
                        RoomCode = manager.CurrentRoomCode, RoomInstanceId = manager.RoomInstanceId, IsPublic = manager.IsRoomPublic });
        }
    }
}
