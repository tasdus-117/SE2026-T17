using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using SmallArena.Domain;

namespace SmallArena.Tests.EditMode.VT067
{
    [TestFixture]
    public sealed class AutoSaveDraftConcurrencyTests
    {
        private static LobbyDraftSnapshot Draft(long revision, string value = "player", string room = "room") =>
            new LobbyDraftSnapshot(room, revision, new[] { new LobbyDraftMember(value, 0, false, true) });
        private static AutoSaveDraftService Service() => new AutoSaveDraftService(new InMemoryDraftStorage());
        private static TaskCompletionSource<bool> Signal() => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        [Test] public async Task TC01_NormalSave_PublicReadReturnsCompleteDraft()
        {
            var service = Service();
            Assert.That(await service.SaveAsync(Draft(1, "initial")), Is.EqualTo(DraftSaveResult.Saved));
            var saved = await service.ReadAsync("room");
            Assert.That(saved.Revision, Is.EqualTo(1)); Assert.That(saved.Members.Single().PlayerId, Is.EqualTo("initial"));
            Assert.That(saved.IsValid(), Is.True);
        }
        [Test] public async Task TC02_NewerRevision_ReplacesPayload_AndAllowsGaps()
        {
            var service = Service(); await service.SaveAsync(Draft(1, "old"));
            Assert.That(await service.SaveAsync(Draft(100, "new")), Is.EqualTo(DraftSaveResult.Saved));
            var saved = await service.ReadAsync("room");
            Assert.That(saved.Revision, Is.EqualTo(100)); Assert.That(saved.Members[0].PlayerId, Is.EqualTo("new"));
        }
        [Test] public async Task TC03_StaleSave_CannotOverwriteNewerObservableState()
        {
            var service = Service(); await service.SaveAsync(Draft(9, "new"));
            Assert.That(await service.SaveAsync(Draft(2, "old")), Is.EqualTo(DraftSaveResult.RejectedStale));
            var saved = await service.ReadAsync("room");
            Assert.That(saved.Revision, Is.EqualTo(9)); Assert.That(saved.Members[0].PlayerId, Is.EqualTo("new"));
        }
        [Test] public async Task TC04_SlowOldTask_CompletesAfterFastNewTask_WithoutRegression()
        {
            var service = Service(); var started = Signal(); var releaseOld = Signal();
            var old = Task.Run(async () => { started.SetResult(true); await releaseOld.Task; return await service.SaveAsync(Draft(1, "slow")); });
            await started.Task;
            try { Assert.That(await service.SaveAsync(Draft(2, "fast")), Is.EqualTo(DraftSaveResult.Saved)); }
            finally { releaseOld.TrySetResult(true); }
            Assert.That(await old, Is.EqualTo(DraftSaveResult.RejectedStale));
            Assert.That((await service.ReadAsync("room")).Members[0].PlayerId, Is.EqualTo("fast"));
        }
        [Test] public async Task TC05_Contention_InterleavedTasks_EndAtHighestValidRevision()
        {
            var service = Service(); var start = Signal();
            var tasks = Enumerable.Range(0, 40).Select(i => Task.Run(async () =>
            { await start.Task; var revision = i * 17 % 40 + 1; await service.SaveAsync(Draft(revision, "r" + revision)); })).ToArray();
            start.SetResult(true); await Task.WhenAll(tasks);
            var saved = await service.ReadAsync("room");
            Assert.That(saved.Revision, Is.EqualTo(40)); Assert.That(saved.Members[0].PlayerId, Is.EqualTo("r40"));
        }
        [Test] public async Task TC06_ConcurrentSameRevision_ExactlyOneCommit_StableWinnerOnRetry()
        {
            var service = Service(); var start = Signal();
            var tasks = Enumerable.Range(0, 40).Select(i => Task.Run(async () =>
            { await start.Task; return await service.SaveAsync(Draft(7, "candidate-" + i)); })).ToArray();
            start.SetResult(true); var outcomes = await Task.WhenAll(tasks);
            Assert.That(outcomes.Count(result => result == DraftSaveResult.Saved), Is.EqualTo(1));
            var winner = (await service.ReadAsync("room")).Members[0].PlayerId;
            Assert.That(await service.SaveAsync(Draft(7, "retry")), Is.EqualTo(DraftSaveResult.RejectedStale));
            Assert.That((await service.ReadAsync("room")).Members[0].PlayerId, Is.EqualTo(winner));
        }
        [Test] public async Task TC07_NullDraft_IsRejected_AndExistingStateSurvives()
        {
            var service = Service(); await service.SaveAsync(Draft(10));
            Assert.That(await service.SaveAsync(null), Is.EqualTo(DraftSaveResult.RejectedInvalid));
            Assert.That((await service.ReadAsync("room")).Revision, Is.EqualTo(10));
        }
        [TestCase(0L)] [TestCase(-1L)] [TestCase(long.MinValue)]
        public async Task TC08_NonPositiveRevision_IsRejected(long revision)
        {
            var service = Service(); await service.SaveAsync(Draft(10));
            Assert.That(await service.SaveAsync(Draft(revision)), Is.EqualTo(DraftSaveResult.RejectedInvalid));
            Assert.That((await service.ReadAsync("room")).Revision, Is.EqualTo(10));
        }
        [TestCase("room-empty")] [TestCase("room-null")] [TestCase("player-empty")]
        [TestCase("members-null")] [TestCase("members-empty")] [TestCase("schema")]
        [TestCase("duplicate-slot")] [TestCase("duplicate-id")] [TestCase("phase")]
        public async Task TC09_CorruptPayload_IsRejected_WithoutPartialState(string kind)
        {
            var service = Service(); await service.SaveAsync(Draft(10, "retained"));
            var members = new[] { new LobbyDraftMember(kind == "player-empty" ? " " : "p", 0, false, true) };
            if (kind == "members-empty") members = Array.Empty<LobbyDraftMember>();
            if (kind == "members-null") members = null;
            if (kind == "duplicate-slot") members = new[] { new LobbyDraftMember("a", 0, false, true), new LobbyDraftMember("b", 0, true, false) };
            if (kind == "duplicate-id") members = new[] { new LobbyDraftMember("p", 0, false, true), new LobbyDraftMember("p", 1, true, false) };
            var draft = new LobbyDraftSnapshot(kind == "room-empty" ? " " : kind == "room-null" ? null : "room", 11,
                members, kind == "phase" ? (DraftLobbyPhase)99 : DraftLobbyPhase.Waiting, kind == "schema" ? 99 : 1);
            Assert.That(await service.SaveAsync(draft), Is.EqualTo(DraftSaveResult.RejectedInvalid));
            var saved = await service.ReadAsync("room");
            Assert.That(saved.Revision, Is.EqualTo(10)); Assert.That(saved.Members[0].PlayerId, Is.EqualTo("retained"));
        }
        [Test] public async Task TC10_StagedIoFailure_RetainsCommittedDraft_AndAllowsRetry()
        {
            var storage = new FailingStorage(); var service = new AutoSaveDraftService(storage);
            await service.SaveAsync(Draft(8, "durable")); storage.FailNextCommit = true;
            Assert.ThrowsAsync<IOException>(async () => await service.SaveAsync(Draft(9, "torn")));
            var saved = await service.ReadAsync("room");
            Assert.That(saved.Revision, Is.EqualTo(8)); Assert.That(saved.Members[0].PlayerId, Is.EqualTo("durable"));
            Assert.That(await service.SaveAsync(Draft(9, "retried")), Is.EqualTo(DraftSaveResult.Saved));
        }
        [Test] public async Task TC11_Contract_IsImmutable_AndRoomsAreIsolated()
        {
            var service = Service(); var source = new[] { new LobbyDraftMember("original", 0, false, true) };
            var draft = new LobbyDraftSnapshot("room", 1, source); await service.SaveAsync(draft);
            source[0] = new LobbyDraftMember("changed", 0, true, true);
            await service.SaveAsync(Draft(99, "other", "Room"));
            Assert.That((await service.ReadAsync("room")).Members[0].PlayerId, Is.EqualTo("original"));
            Assert.That((await service.ReadAsync("Room")).Revision, Is.EqualTo(99));
        }
        [Test] public async Task TC12_Load40ConcurrentTasks_P95AtMost1500ms_AndFinalIntegrity()
        {
            var output = TestContext.Out;
            // Timing starts at a common asynchronous release, includes scheduling/lease/storage wait.
            var service = Service(); var start = Signal(); long released = 0;
            var tasks = Enumerable.Range(0, 40).Select(i => Task.Run(async () =>
            {
                await start.Task;
                var revision = i * 17 % 40 + 1;
                await service.SaveAsync(Draft(revision, "load-" + revision));
                return (Stopwatch.GetTimestamp() - Interlocked.Read(ref released)) * 1000d / Stopwatch.Frequency;
            })).ToArray();
            Interlocked.Exchange(ref released, Stopwatch.GetTimestamp()); start.SetResult(true);
            var samples = await Task.WhenAll(tasks); Array.Sort(samples);
            double Percentile(double p) => samples[(int)Math.Ceiling(p * samples.Length) - 1];
            output.WriteLine(FormattableString.Invariant($"VT067_LOAD users=40 backend=in-memory p50_ms={Percentile(.50):F6} p95_ms={Percentile(.95):F6} p99_ms={Percentile(.99):F6} max_ms={samples[39]:F6} limit_ms=1500 cores={Environment.ProcessorCount}"));
            Assert.That(Percentile(.95), Is.LessThanOrEqualTo(1500));
            var saved = await service.ReadAsync("room");
            Assert.That(saved.Revision, Is.EqualTo(40)); Assert.That(saved.Members[0].PlayerId, Is.EqualTo("load-40"));
        }
        [Test] public async Task TC13_CancelledSave_DoesNotPublish_AndLeaseIsReusable()
        {
            var service = Service(); await service.SaveAsync(Draft(1));
            using (var cancel = new CancellationTokenSource())
            {
                cancel.Cancel(); Assert.CatchAsync<OperationCanceledException>(async () => await service.SaveAsync(Draft(2), cancel.Token));
            }
            Assert.That((await service.ReadAsync("room")).Revision, Is.EqualTo(1));
            Assert.That(await service.SaveAsync(Draft(2)), Is.EqualTo(DraftSaveResult.Saved));
        }
        [Test] public async Task TC14_MultipleServicesSharingStorage_StillRejectStaleAndOverflowBoundary()
        {
            var storage = new InMemoryDraftStorage(); var a = new AutoSaveDraftService(storage); var b = new AutoSaveDraftService(storage);
            await a.SaveAsync(Draft(long.MaxValue, "max"));
            Assert.That(await b.SaveAsync(Draft(long.MaxValue - 1)), Is.EqualTo(DraftSaveResult.RejectedStale));
            Assert.That((await a.ReadAsync("room")).Members[0].PlayerId, Is.EqualTo("max"));
        }

        /// <summary>Fault model stages serialization then fails before the storage's atomic publish.</summary>
        private sealed class FailingStorage : IAtomicDraftStorage
        {
            private readonly InMemoryDraftStorage inner = new InMemoryDraftStorage();
            public bool FailNextCommit;
            public async Task<IDraftTransaction> OpenAsync(string roomId, CancellationToken token) =>
                new Transaction(await inner.OpenAsync(roomId, token), this);
            private sealed class Transaction : IDraftTransaction
            {
                private readonly IDraftTransaction inner; private readonly FailingStorage owner;
                internal Transaction(IDraftTransaction inner, FailingStorage owner) { this.inner = inner; this.owner = owner; }
                public LobbyDraftSnapshot Current => inner.Current;
                public Task CommitAsync(LobbyDraftSnapshot snapshot, CancellationToken token)
                {
                    var staged = snapshot.Members.Select(member => member.PlayerId).ToArray();
                    if (owner.FailNextCommit && staged.Length > 0) { owner.FailNextCommit = false; throw new IOException("Simulated serialization/storage failure before publish."); }
                    return inner.CommitAsync(snapshot, token);
                }
                public void Dispose() => inner.Dispose();
            }
        }
    }
}
