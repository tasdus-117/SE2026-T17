using System;
using System.Collections.Generic;
using System.Linq;
using SmallArena.Networking;

namespace SmallArena.Server.Sessions
{
    /// <summary>Server-owned membership/readiness rules. Connection IDs come from Mirror, never client payloads.</summary>
    public sealed class LobbyRoom
    {
        public const int Capacity = 4;
        private readonly Dictionary<int, LobbyMember> members = new Dictionary<int, LobbyMember>();
        public LobbyPhase Phase { get; private set; } = LobbyPhase.Offline;
        public int Count => members.Count;
        public bool AllReady => Count > 0 && members.Values.All(member => member.IsReady);
        public LobbyMember[] Members => members.Values.OrderBy(member => member.ConnectionId).ToArray();

        /// <summary>
        /// Single-threaded server query. Bind requester to a trusted connection.
        /// Ordering is stable only while membership/readiness remains unchanged.
        /// </summary>
        public LobbyMember[] SearchMembers(int requester, bool? ready = null,
            bool? host = null, int page = 0, int pageSize = Capacity)
        {
            if (Phase != LobbyPhase.Waiting || !members.ContainsKey(requester))
                throw new UnauthorizedAccessException("Only waiting room members may search.");
            if (page < 0) throw new ArgumentOutOfRangeException(nameof(page));
            if (pageSize < 1 || pageSize > Capacity) throw new ArgumentOutOfRangeException(nameof(pageSize));

            var offset = (long)page * pageSize;
            if (offset >= Count) return Array.Empty<LobbyMember>();
            return members.Values
                .Where(member => (!ready.HasValue || member.IsReady == ready.Value)
                    && (!host.HasValue || member.IsHost == host.Value))
                .OrderByDescending(member => member.IsReady)
                .ThenBy(member => member.ConnectionId)
                .Skip((int)offset)
                .Take(pageSize)
                .ToArray();
        }

        public void Open() { members.Clear(); Phase = LobbyPhase.Waiting; }
        public void Close() { members.Clear(); Phase = LobbyPhase.Offline; }
        public bool TryJoin(int id, bool isHost)
        {
            if (Phase != LobbyPhase.Waiting || members.ContainsKey(id) || Count >= Capacity) return false;
            members.Add(id, new LobbyMember { ConnectionId = id, IsHost = isHost });
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
            if (Phase != LobbyPhase.Waiting || !AllReady || !members.TryGetValue(requester, out var member) || !member.IsHost) return false;
            Phase = LobbyPhase.Loading;
            return true;
        }
        public void FinishLoading() { if (Phase == LobbyPhase.Loading) Phase = LobbyPhase.Playing; }
    }
}
