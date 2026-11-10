using System.Collections.Generic;
using System.Linq;
using SmallArena.Networking;

namespace SmallArena.Server.Sessions
{
    /// <summary>Server-owned membership/readiness rules. Connection IDs come from Mirror, never client payloads.</summary>
    public sealed class LobbyRoom
    {
        public const int Capacity = LobbyLimits.Capacity;
        private readonly Dictionary<int, LobbyMember> members = new Dictionary<int, LobbyMember>();
        public LobbyPhase Phase { get; private set; } = LobbyPhase.Offline;
        public int Count => members.Count;
        public bool CanStart => Phase == LobbyPhase.Waiting && Count >= 2 && AllReady;
        public bool AllReady => Count > 0 && members.Values.All(member => member.IsReady);
        public LobbyMember[] Members => members.Values.OrderBy(member => member.SlotIndex).ToArray();

        public void Open() { members.Clear(); Phase = LobbyPhase.Waiting; }
        public void Close() { members.Clear(); Phase = LobbyPhase.Offline; }
        public bool TryJoin(int id, bool isHost)
        {
            if (Phase != LobbyPhase.Waiting || members.ContainsKey(id) || Count >= Capacity) return false;
            var slot = Enumerable.Range(0, Capacity).First(index => !members.Values.Any(member => member.SlotIndex == index));
            members.Add(id, new LobbyMember { ConnectionId = id, SlotIndex = slot, IsHost = isHost });
            return true;
        }
        public bool Remove(int id) => members.Remove(id);
        public bool SetReady(int id, bool ready)
        {
            if (Phase != LobbyPhase.Waiting || !members.TryGetValue(id, out var member) || member.IsReady == ready) return false;
            member.IsReady = ready;
            members[id] = member;
            return true;
        }
        public bool TryStart(int requester)
        {
            if (!CanStart || !members.TryGetValue(requester, out var member) || !member.IsHost) return false;
            Phase = LobbyPhase.Loading;
            return true;
        }
        public void FinishLoading() { if (Phase == LobbyPhase.Loading) Phase = LobbyPhase.Playing; }
    }
}
