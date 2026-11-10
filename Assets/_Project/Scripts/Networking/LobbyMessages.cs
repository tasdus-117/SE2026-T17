using Mirror;

namespace SmallArena.Networking
{
    public enum LobbyPhase : byte { Offline, Waiting, Loading, Playing }

    public struct LobbyMember
    {
        public int ConnectionId;
        public int SlotIndex;
        public bool IsReady;
        public bool IsHost;
    }

    public struct LobbySnapshot : NetworkMessage
    {
        public LobbyPhase Phase;
        public int LocalConnectionId;
        public bool CanStart;
        public LobbyMember[] Members;
    }

    public struct LobbyReadyRequest : NetworkMessage { public bool IsReady; }
    public struct LobbyStartRequest : NetworkMessage { }
    public static class LobbyLimits
    {
        public const int Capacity = 4;
        public const string FullReason = "Room is full (4/4)";
    }
    public struct LobbyRejectionAcknowledged : NetworkMessage { }
    public struct LobbyRejection : NetworkMessage { public string Reason; }
}
