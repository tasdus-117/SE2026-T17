using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace SmallArena.Domain
{
    public sealed class InMemoryDraftStorage : IAtomicDraftStorage
    {
        private sealed class Entry
        {
            internal readonly SemaphoreSlim Gate = new SemaphoreSlim(1, 1);
            internal LobbyDraftSnapshot Value;
        }
        private readonly ConcurrentDictionary<string, Entry> entries = new ConcurrentDictionary<string, Entry>(StringComparer.Ordinal);
        public async Task<IDraftTransaction> OpenAsync(string roomId, CancellationToken cancellationToken)
        {
            var entry = entries.GetOrAdd(roomId, _ => new Entry());
            await entry.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            return new Transaction(entry);
        }
        private sealed class Transaction : IDraftTransaction
        {
            private readonly Entry entry;
            private bool disposed;
            internal Transaction(Entry entry) => this.entry = entry;
            public LobbyDraftSnapshot Current => entry.Value;
            public Task CommitAsync(LobbyDraftSnapshot snapshot, CancellationToken cancellationToken)
            {
                if (disposed) throw new ObjectDisposedException(nameof(Transaction));
                cancellationToken.ThrowIfCancellationRequested();
                entry.Value = snapshot;
                return Task.CompletedTask;
            }
            public void Dispose() { if (!disposed) { disposed = true; entry.Gate.Release(); } }
        }
    }
}
