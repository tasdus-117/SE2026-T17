using Mirror;

namespace SmallArena.Networking
{
    public enum LobbyPhase : byte { Offline, Waiting, Loading, Playing }

    public struct LobbyMember
    {
        public int ConnectionId;
        public bool IsReady;
        public bool IsHost;
    }

    public struct LobbySnapshot : NetworkMessage
    {
        public LobbyPhase Phase;
        public int LocalConnectionId;
        public LobbyMember[] Members;
    }

    public struct LobbyReadyRequest : NetworkMessage { public bool IsReady; }
    public struct LobbyStartRequest : NetworkMessage { }
    public struct LobbyRejection : NetworkMessage { public string Reason; }
}
