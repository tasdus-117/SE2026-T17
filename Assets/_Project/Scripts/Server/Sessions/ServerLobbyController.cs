using Mirror;
using System;
using System.IO;
using System.Threading.Tasks;
using SmallArena.Domain;
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
        public LobbyRoom Room => room;
        private IAtomicDraftStorage draftStorage;
        public LobbyDraftAutoSave Drafts { get; private set; }
        public void ConfigureDraftStorage(IAtomicDraftStorage storage)
        {
            if (NetworkServer.active) throw new InvalidOperationException("Configure draft storage before starting the server.");
            draftStorage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

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
            // Resolve the Unity path on the main thread. File storage itself uses only System APIs.
            Drafts = new LobbyDraftAutoSave(draftStorage ?? new AtomicFileDraftStorage(
                Path.Combine(Application.persistentDataPath, "VT067", "Drafts")));
            NetworkServer.RegisterHandler<LobbyReadyRequest>(Ready);
            NetworkServer.RegisterHandler<LobbyStartRequest>(StartMatch);
        }
        private void Close() => room.Close();
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
            if (!room.TryStart(connection.connectionId)) return;
            Publish();
            manager.LoadRoomArena();
        }
        private void SceneLoaded(string scene)
        {
            if (!manager.RoomEnabled || !scene.EndsWith("Arena_MVP.unity") && scene != "Arena_MVP") return;
            room.FinishLoading();
            Publish();
        }
        private void Publish()
        {
            if (!NetworkServer.active) return;
            var members = room.Members;
            if (Drafts != null) ObserveSave(Drafts.CaptureAsync(room));
            foreach (var connection in NetworkServer.connections.Values)
                if (connection.isAuthenticated)
                    connection.Send(new LobbySnapshot { Phase = room.Phase, LocalConnectionId = connection.connectionId, CanStart = room.CanStart, Members = members });
        }
        private async void ObserveSave(Task<DraftSaveResult> save)
        {
            try { await save; }
            catch (Exception error)
            {
                // Capture the Unity synchronization context; persistence never logs from a worker.
                if (this != null) Debug.LogWarning("[VT067] Draft save failed; previous commit retained: " + error.GetType().Name);
            }
        }
    }
}
