using System;
using System.Threading;
using System.Threading.Tasks;

namespace SmallArena.Domain
{
    public sealed class AutoSaveDraftService
    {
        private readonly IAtomicDraftStorage storage;
        public AutoSaveDraftService(IAtomicDraftStorage storage) => this.storage = storage ?? throw new ArgumentNullException(nameof(storage));

        public async Task<DraftSaveResult> SaveAsync(LobbyDraftSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            if (snapshot == null || !snapshot.IsValid()) return DraftSaveResult.RejectedInvalid;
            using (var transaction = await storage.OpenAsync(snapshot.RoomId, cancellationToken).ConfigureAwait(false))
            {
                if (transaction.Current != null && snapshot.Revision <= transaction.Current.Revision)
                    return DraftSaveResult.RejectedStale;
                await transaction.CommitAsync(snapshot, cancellationToken).ConfigureAwait(false);
                return DraftSaveResult.Saved;
            }
        }
        public async Task<LobbyDraftSnapshot> ReadAsync(string roomId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(roomId)) throw new ArgumentException("A room key is required.", nameof(roomId));
            using (var transaction = await storage.OpenAsync(roomId, cancellationToken).ConfigureAwait(false)) return transaction.Current;
        }
    }
}
