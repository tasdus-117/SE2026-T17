using System;
using System.Collections.Generic;
using System.Linq;

namespace SmallArena.Domain
{
    public enum DraftLobbyPhase : byte { Waiting, Loading, Playing }

    public sealed class LobbyDraftMember
    {
        public string PlayerId { get; }
        public int Slot { get; }
        public bool IsReady { get; }
        public bool IsHost { get; }
        public LobbyDraftMember(string playerId, int slot, bool isReady, bool isHost)
        { PlayerId = playerId; Slot = slot; IsReady = isReady; IsHost = isHost; }
    }

    /// <summary>Immutable archival DTO; never restores live connection authority/readiness.</summary>
    public sealed class LobbyDraftSnapshot
    {
        public const int CurrentSchema = 1;
        public string RoomId { get; }
        public long Revision { get; }
        public int Schema { get; }
        public DraftLobbyPhase Phase { get; }
        public IReadOnlyList<LobbyDraftMember> Members { get; }
        public LobbyDraftSnapshot(string roomId, long revision, IEnumerable<LobbyDraftMember> members,
            DraftLobbyPhase phase = DraftLobbyPhase.Waiting, int schema = CurrentSchema)
        {
            RoomId = roomId; Revision = revision; Phase = phase; Schema = schema;
            Members = members == null ? null : Array.AsReadOnly(members.ToArray());
        }
        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(RoomId) || RoomId.Length > 64 || Revision <= 0 || Schema != CurrentSchema
                || !Enum.IsDefined(typeof(DraftLobbyPhase), Phase) || Members == null || Members.Count < 1 || Members.Count > 4) return false;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var slots = new HashSet<int>();
            var hosts = 0;
            foreach (var member in Members)
            {
                if (member == null || string.IsNullOrWhiteSpace(member.PlayerId) || member.PlayerId.Length > 64
                    || member.Slot < 0 || member.Slot > 3 || !ids.Add(member.PlayerId) || !slots.Add(member.Slot)) return false;
                if (member.IsHost && ++hosts > 1) return false;
            }
            return true;
        }
    }
}
