using System;

namespace SmallArena.Server.Sessions
{
    /// <summary>One Start per room session. Called on the server thread, not a worker.</summary>
    public sealed class LobbyStartOperation
    {
        private readonly LobbyRoom room;
        private readonly Action beginLoad;
        private readonly Action changed;
        private readonly Action<Exception> failed;

        public LobbyStartOperation(LobbyRoom room, Action beginLoad, Action changed, Action<Exception> failed)
        {
            this.room = room ?? throw new ArgumentNullException(nameof(room));
            this.beginLoad = beginLoad ?? throw new ArgumentNullException(nameof(beginLoad));
            this.changed = changed ?? throw new ArgumentNullException(nameof(changed));
            this.failed = failed ?? throw new ArgumentNullException(nameof(failed));
        }

        public bool TryStart(int requester)
        {
            // Commit Loading before publishing or calling external code; reentrant retry is rejected.
            if (!room.TryStart(requester)) return false;
            changed();
            try { beginLoad(); }
            catch (Exception error)
            {
                if (room.FailLoading()) { changed(); failed(error); }
            }
            return true;
        }

        public bool Complete()
        {
            if (!room.FinishLoading()) return false;
            changed();
            return true;
        }
    }
}
