using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SmallArena.Domain;

namespace SmallArena.Server.Sessions
{
    /// <summary>Per-key OS lease covers read/check/write across storage instances/processes.
    /// Stage and flush in the same directory, then atomically publish. No Unity APIs on workers.</summary>
    public sealed class AtomicFileDraftStorage : IAtomicDraftStorage
    {
        private readonly string directory;
        public AtomicFileDraftStorage(string directory) => this.directory = Path.GetFullPath(directory ?? throw new ArgumentNullException(nameof(directory)));
        public async Task<IDraftTransaction> OpenAsync(string roomId, CancellationToken cancellationToken)
        {
            var stem = Key(roomId);
            await Task.Run(() => Directory.CreateDirectory(directory), cancellationToken).ConfigureAwait(false);
            var elapsed = Stopwatch.StartNew();
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                FileStream lease;
                try
                {
                    lease = await Task.Run(() => new FileStream(Path.Combine(directory, stem + ".lease"),
                        FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None), cancellationToken).ConfigureAwait(false);
                }
                catch (IOException error) when ((error.HResult & 0xffff) == 32 || (error.HResult & 0xffff) == 33
                    || (error.HResult & 0xffff) == 11)
                {
                    if (elapsed.Elapsed.TotalSeconds >= 5) throw new TimeoutException("Draft lease timed out.", error);
                    await Task.Delay(5, cancellationToken).ConfigureAwait(false);
                    continue;
                }
                try
                {
                    var path = Path.Combine(directory, stem + ".draft");
                    var current = await Task.Run(() => Read(path), cancellationToken).ConfigureAwait(false);
                    if (current != null && !string.Equals(current.RoomId, roomId, StringComparison.Ordinal))
                        throw new InvalidDataException("Draft key does not match its stored payload.");
                    return new Transaction(lease, path, current);
                }
                catch { lease.Dispose(); throw; }
            }
        }
        private static string Key(string key)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(key))).Replace("-", "");
        }
        private static LobbyDraftSnapshot Read(string path)
        {
            if (!File.Exists(path)) return null;
            using (var stream = File.OpenRead(path))
            using (var reader = new BinaryReader(stream, Encoding.UTF8))
            {
                if (stream.Length > 16384 || reader.ReadString() != "VT067-DRAFT") throw new InvalidDataException("Invalid draft header.");
                var schema = reader.ReadInt32(); var key = reader.ReadString(); var revision = reader.ReadInt64();
                var phase = (DraftLobbyPhase)reader.ReadByte(); var count = reader.ReadInt32();
                if (count < 1 || count > 4) throw new InvalidDataException("Invalid draft member count.");
                var members = new LobbyDraftMember[count];
                for (var i = 0; i < count; i++) members[i] = new LobbyDraftMember(reader.ReadString(), reader.ReadInt32(), reader.ReadBoolean(), reader.ReadBoolean());
                var snapshot = new LobbyDraftSnapshot(key, revision, members, phase, schema);
                if (stream.Position != stream.Length || !snapshot.IsValid()) throw new InvalidDataException("Invalid draft payload.");
                return snapshot;
            }
        }
        private static void Write(string path, LobbyDraftSnapshot snapshot, CancellationToken token)
        {
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
                using (var writer = new BinaryWriter(stream, Encoding.UTF8))
                {
                    writer.Write("VT067-DRAFT"); writer.Write(snapshot.Schema); writer.Write(snapshot.RoomId); writer.Write(snapshot.Revision);
                    writer.Write((byte)snapshot.Phase); writer.Write(snapshot.Members.Count);
                    foreach (var member in snapshot.Members)
                    { writer.Write(member.PlayerId); writer.Write(member.Slot); writer.Write(member.IsReady); writer.Write(member.IsHost); }
                    writer.Flush(); stream.Flush(true);
                }
                token.ThrowIfCancellationRequested(); // Point of no return: do not check cancellation after publish.
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        private sealed class Transaction : IDraftTransaction
        {
            private readonly FileStream lease;
            private readonly string path;
            private bool disposed;
            public LobbyDraftSnapshot Current { get; private set; }
            internal Transaction(FileStream lease, string path, LobbyDraftSnapshot current)
            { this.lease = lease; this.path = path; Current = current; }
            public async Task CommitAsync(LobbyDraftSnapshot snapshot, CancellationToken cancellationToken)
            {
                if (disposed) throw new ObjectDisposedException(nameof(Transaction));
                await Task.Run(() => Write(path, snapshot, cancellationToken), cancellationToken).ConfigureAwait(false);
                Current = snapshot;
            }
            public void Dispose() { if (!disposed) { disposed = true; lease.Dispose(); } }
        }
    }
}
