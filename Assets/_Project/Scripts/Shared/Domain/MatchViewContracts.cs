using System;

namespace SmallArena.Domain
{
    // Read-only contracts keep presentation independent of Runtime and Server implementations.
    public interface IMatchStateSource
    {
        MatchState CurrentState { get; }
        event Action<MatchState> OnMatchStateChanged;
    }

    public interface IRoomBrowserPort
    {
        string CurrentRoomCode { get; }
        bool IsRoomPublic { get; }
        event Action RoomsDiscovered;
        void StartHostedRoom(string roomName = null, bool isPublic = true);
        bool TryJoinRoomByCode(string roomCode, out RoomJoinFailureReason failureReason);
        void JoinRoomByCode(string roomCode, Action<RoomJoinFailureReason> completed);
        void CancelRoomCodeJoin();
        System.Collections.Generic.IReadOnlyList<RoomListingInfo> GetDiscoveredPublicRooms();
        void RefreshDiscoveredRooms();
    }

    public interface IHealthSource
    {
        int CurrentHealth { get; }
        int MaxHealth { get; }
        event Action<int, int> OnHealthChanged;
    }

    public interface IDamageSource
    {
        int CurrentDamage { get; }
        event Action<int> OnDamageChanged;
    }

    public interface IMatchEndParticipant
    {
        void EndMatch();
    }

    public interface IMatchGameplayParticipant
    {
        void SetGameplayAllowed(bool allowed);
    }

    public interface IMatchHudReadiness
    {
        bool IsReady { get; }
        bool IsBoundTo(IMatchStateSource manager, IHealthSource health, IDamageSource combat);
    }
}
