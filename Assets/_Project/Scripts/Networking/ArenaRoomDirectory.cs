using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using SmallArena.Domain;
using UnityEngine;

namespace SmallArena.Networking
{
    public sealed partial class ArenaNetworkManager : IRoomBrowserPort
    {
        public string CurrentRoomCode { get; private set; } = "";
        public string RoomInstanceId { get; private set; } = "";
        public bool IsRoomPublic { get; private set; } = true;
        public ushort DiscoveryPort { get; set; } = LanRoomDirectoryService.DefaultDiscoveryPort;
        public event Action RoomsDiscovered;
        public event Action RoomDirectoryIdentityChanged;
        private IRoomDirectoryService roomDirectory;
        private string assignedRoomCode, hostedRoomName = "Small Arena", expectedDirectoryRoom, expectedDirectoryCode;
        private readonly HashSet<NetworkConnectionToClient> pendingRoomAdmissions = new HashSet<NetworkConnectionToClient>();
        private RoomDirectoryPhase directoryPhase;
        private int directoryPlayers;
        private string browserFingerprint = "";
        private double nextBrowserPoll;
        private int joinAttempt;

        // The manager owns/disposes an injected adapter just as it owns the default LAN adapter.
        public void SetRoomDirectoryService(IRoomDirectoryService service)
        {
            if (NetworkServer.active || NetworkClient.active) throw new InvalidOperationException("Configure directory before starting a session.");
            roomDirectory?.Dispose(); roomDirectory = service ?? throw new ArgumentNullException(nameof(service));
        }
        private IRoomDirectoryService Directory()
        {
            if (roomDirectory == null)
            {
                try { roomDirectory = new LanRoomDirectoryService(DiscoveryPort); }
                catch (System.Net.Sockets.SocketException) { ConnectionStatus = "Room directory unavailable."; }
            }
            return roomDirectory;
        }
        public void StartHostedRoom(string roomName = null, bool isPublic = true)
        {
            if (!roomEnabled || NetworkServer.active || NetworkClient.active) return;
            var name = roomName ?? "Small Arena";
            _ = new RoomListingInfo("AAAAAA", name, "Host", 0);
            if (Directory()?.IsAvailable != true) { RoomChanged?.Invoke(); return; }
            CancelRoomCodeJoin();
            hostedRoomName = name; IsRoomPublic = isPublic;
            ConfigureRoom(); ConnectionStatus = "Creating room...";
            try
            {
                StartHost();
                if (roomDirectory?.IsAvailable != true)
                { StopHost(); ConnectionStatus = "Room directory unavailable."; }
            }
            catch (InvalidOperationException)
            {
                if (NetworkServer.active) StopHost();
                ConnectionStatus = "Unable to register room.";
            }
            RoomChanged?.Invoke();
        }
        public bool TryJoinRoomByCode(string roomCode, out RoomJoinFailureReason failureReason)
        {
            failureReason = RoomJoinFailureReason.DirectoryUnavailable;
            if (!roomEnabled || NetworkClient.active) return false;
            if (!RoomCodeGenerator.IsValid(RoomCodeGenerator.Normalize(roomCode)))
            { failureReason = RoomJoinFailureReason.InvalidCodeFormat; return false; }
            var result = Directory()?.ResolveRoomCode(roomCode);
            failureReason = result?.FailureReason ?? RoomJoinFailureReason.DirectoryUnavailable;
            if (result?.Success != true) return false;
            if (!(transport is PortTransport port)) { failureReason = RoomJoinFailureReason.DirectoryUnavailable; return false; }
            ConfigureRoom(); expectedDirectoryRoom = result.Endpoint.RoomInstanceId; expectedDirectoryCode = result.Endpoint.RoomCode;
            networkAddress = result.Endpoint.Host; port.Port = result.Endpoint.Port;
            ConnectionStatus = "Connecting..."; StartClient(); RoomChanged?.Invoke();
            return true;
        }
        public IReadOnlyList<RoomListingInfo> GetDiscoveredPublicRooms()
            => Directory()?.GetPublicRooms() ?? Array.Empty<RoomListingInfo>();
        public void JoinRoomByCode(string roomCode, Action<RoomJoinFailureReason> completed)
        {
            if (completed == null) throw new ArgumentNullException(nameof(completed));
            var attempt = ++joinAttempt;
            StartCoroutine(ResolveAndJoin(roomCode, completed, attempt));
        }
        public void CancelRoomCodeJoin() => joinAttempt++;
        private System.Collections.IEnumerator ResolveAndJoin(string code, Action<RoomJoinFailureReason> completed, int attempt)
        {
            var deadline = Time.realtimeSinceStartupAsDouble + 2;
            while (attempt == joinAttempt)
            {
                if (TryJoinRoomByCode(code, out var failure)) { completed(RoomJoinFailureReason.None); yield break; }
                if (failure != RoomJoinFailureReason.RoomNotFound || Time.realtimeSinceStartupAsDouble >= deadline)
                { completed(failure); yield break; }
                Directory()?.Refresh(); yield return null;
            }
        }
        public void RefreshDiscoveredRooms() { Directory()?.Refresh(); RoomsDiscovered?.Invoke(); }
        private void BeginDirectoryHosting()
        {
            var directory = Directory();
            if (directory?.IsAvailable != true) return;
            RoomInstanceId = Guid.NewGuid().ToString("N");
            CurrentRoomCode = assignedRoomCode ?? RoomCodeGenerator.Generate(code => directory.ResolveRoomCode(code).FailureReason != RoomJoinFailureReason.RoomNotFound);
            if (assignedRoomCode != null && directory.ResolveRoomCode(assignedRoomCode).FailureReason != RoomJoinFailureReason.RoomNotFound)
                CurrentRoomCode = RoomCodeGenerator.Generate(code => directory.ResolveRoomCode(code).FailureReason != RoomJoinFailureReason.RoomNotFound);
            directoryPhase = RoomDirectoryPhase.Lobby; directoryPlayers = 0;
            PublishDirectoryRoom();
        }
        public void UpdateRoomDirectory(LobbyPhase phase, int admittedPlayers)
        {
            if (!NetworkServer.active || !roomEnabled) return;
            directoryPlayers = admittedPlayers;
            directoryPhase = phase == LobbyPhase.Waiting ? RoomDirectoryPhase.Lobby : RoomDirectoryPhase.InMatch;
            PublishDirectoryRoom();
        }
        private void PublishDirectoryRoom()
        {
            if (string.IsNullOrEmpty(CurrentRoomCode) || roomDirectory?.IsAvailable != true) return;
            var port = transport is PortTransport configured ? configured.Port : (ushort)7777;
            roomDirectory.PublishRoom(new RoomListingInfo(CurrentRoomCode, hostedRoomName, "Host", directoryPlayers,
                directoryPhase, IsRoomPublic, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
                new RoomResolvedEndpoint("127.0.0.1", port, CurrentRoomCode, RoomInstanceId));
        }
        private bool AcceptDirectorySnapshot(LobbySnapshot snapshot)
        {
            if (expectedDirectoryRoom != null && expectedDirectoryRoom != snapshot.RoomInstanceId)
            {
                rejectionReason = "Room no longer available.";
                ConnectionStatus = rejectionReason;
                RoomRejected?.Invoke(rejectionReason);
                StopClient(); return false;
            }
            CurrentRoomCode = snapshot.RoomCode ?? ""; IsRoomPublic = snapshot.IsPublic;
            return true;
        }
        private void StopDirectoryHosting()
        {
            if (RoomInstanceId != "") roomDirectory?.RemoveRoom(RoomInstanceId);
            CurrentRoomCode = RoomInstanceId = "";
            roomDirectory?.Dispose(); roomDirectory = null;
        }
        private void ReceiveRoomAdmission(NetworkConnectionToClient connection, LobbyAdmissionRequest request)
        {
            if (!connection.isAuthenticated || rejectedConnections.Contains(connection) || !pendingRoomAdmissions.Remove(connection)) return;
            if (request.Version != LobbyAdmissionRequest.CurrentVersion
                || ((!string.IsNullOrEmpty(request.RoomCode) || !string.IsNullOrEmpty(request.RoomInstanceId))
                    && (request.RoomCode != CurrentRoomCode || request.RoomInstanceId != RoomInstanceId)))
            { RejectRoomConnection(connection, "Room no longer available or incompatible client."); return; }
            ServerConnected?.Invoke(connection);
            Debug.Log($"[TV1] Room admission: {connection.connectionId}");
        }
        private System.Collections.IEnumerator ExpireRoomAdmission(NetworkConnectionToClient connection)
        {
            yield return new WaitForSecondsRealtime(5);
            if (pendingRoomAdmissions.Remove(connection) && NetworkServer.active
                && NetworkServer.connections.TryGetValue(connection.connectionId, out var current) && ReferenceEquals(current, connection)) connection.Disconnect();
        }
        public override void Update()
        {
            base.Update();
            if (roomDirectory == null) return;
            roomDirectory.Tick();
            if (NetworkServer.active && roomDirectory is LanRoomDirectoryService lan && lan.HostedClaimLost)
            {
                roomDirectory.RemoveRoom(RoomInstanceId);
                CurrentRoomCode = RoomCodeGenerator.Generate(code => roomDirectory.ResolveRoomCode(code).FailureReason != RoomJoinFailureReason.RoomNotFound);
                PublishDirectoryRoom();
                // Lobby snapshots must carry the new code even if roster/readiness did not change.
                RoomDirectoryIdentityChanged?.Invoke();
            }
            if (Time.realtimeSinceStartupAsDouble >= nextBrowserPoll)
            { nextBrowserPoll = Time.realtimeSinceStartupAsDouble + 5; roomDirectory.Refresh(); }
            var rooms = roomDirectory.GetPublicRooms();
            var fingerprint = string.Join("|", rooms.Select(r => r.RoomCode + ":" + r.CurrentPlayers + ":" + r.Phase + ":" + r.RoomName + ":" + r.HostDisplayName));
            if (fingerprint != browserFingerprint)
            { browserFingerprint = fingerprint; RoomsDiscovered?.Invoke(); }
        }
        public override void OnDestroy()
        {
            CancelRoomCodeJoin();
            roomDirectory?.Dispose(); roomDirectory = null;
            base.OnDestroy();
        }
    }
}
