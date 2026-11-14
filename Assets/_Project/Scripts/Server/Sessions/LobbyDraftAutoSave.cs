using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using SmallArena.Domain;

namespace SmallArena.Server.Sessions
{
    /// <summary>Called by the lobby main thread; only immutable DTOs cross the asynchronous boundary.</summary>
    public sealed class LobbyDraftAutoSave
    {
        private readonly AutoSaveDraftService service;
        private long revision;
        public string RoomId { get; } = Guid.NewGuid().ToString("N");
        public Task<DraftSaveResult> LastSave { get; private set; } = Task.FromResult(DraftSaveResult.RejectedInvalid);
        public LobbyDraftAutoSave(IAtomicDraftStorage storage) => service = new AutoSaveDraftService(storage);
        public Task<DraftSaveResult> CaptureAsync(LobbyRoom room)
        {
            if (room.Count == 0) return LastSave;
            var members = room.Members.Select(member => new LobbyDraftMember(
                member.ConnectionId.ToString(CultureInfo.InvariantCulture), member.SlotIndex, member.IsReady, member.IsHost)).ToArray();
            var phase = (DraftLobbyPhase)((int)room.Phase - 1); // Waiting/Loading/Playing map to 0/1/2.
            var snapshot = new LobbyDraftSnapshot(RoomId, checked(++revision), members, phase);
            return LastSave = service.SaveAsync(snapshot);
        }
        public Task<LobbyDraftSnapshot> ReadAsync() => service.ReadAsync(RoomId);
    }
}
