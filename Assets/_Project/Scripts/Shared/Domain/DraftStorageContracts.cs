using System;
using System.Threading;
using System.Threading.Tasks;

namespace SmallArena.Domain
{
    public enum DraftSaveResult { Saved, RejectedInvalid, RejectedStale }

    /// <summary>Exclusive lease per key, including across services sharing the storage.
    /// Commit failure/cancellation must leave the previous value intact. No callbacks into the service.</summary>
    public interface IAtomicDraftStorage
    {
        Task<IDraftTransaction> OpenAsync(string roomId, CancellationToken cancellationToken);
    }
    public interface IDraftTransaction : IDisposable
    {
        LobbyDraftSnapshot Current { get; }
        Task CommitAsync(LobbyDraftSnapshot snapshot, CancellationToken cancellationToken);
    }
}
