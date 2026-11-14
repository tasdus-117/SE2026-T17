#if UNITY_EDITOR
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using SmallArena.Domain;
using SmallArena.Server.Sessions;
using UnityEngine;
using UnityEngine.TestTools;

namespace SmallArena.Tests.PlayMode
{
    public sealed class AutoSaveDraftFileTests
    {
        private string directory;
        private string root;
        private static LobbyDraftSnapshot Draft(long revision, string value = "p") => new LobbyDraftSnapshot(
            "file-room", revision, new[] { new LobbyDraftMember(value, 0, false, true) });
        private AutoSaveDraftService Service() => new AutoSaveDraftService(new AtomicFileDraftStorage(directory));
        private IEnumerator Run(Func<Task> action)
        {
            root = Path.GetFullPath(Path.Combine(Application.dataPath, "../TestResults/VT067/FileIntegration"));
            directory = Path.Combine(root, Guid.NewGuid().ToString("N"));
            var task = action(); var deadline = Time.realtimeSinceStartup + 20;
            while (!task.IsCompleted && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(task.IsCompleted, Is.True, "File integration timed out.");
            task.GetAwaiter().GetResult();
        }
        [TearDown] public void Cleanup()
        {
            if (directory == null || !Directory.Exists(directory)) return;
            var path = Path.GetFullPath(directory);
            Assert.That(path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), Is.True);
            Directory.Delete(path, true);
        }
        [UnityTest] public IEnumerator TC16_FileReopenAndIndependentInstances_PreserveHighestRevision() => Run(async () =>
        {
            var first = Service(); await first.SaveAsync(Draft(8, "new")).ConfigureAwait(false);
            var reopened = Service();
            Assert.That(await reopened.SaveAsync(Draft(7, "stale")).ConfigureAwait(false), Is.EqualTo(DraftSaveResult.RejectedStale));
            var loaded = await reopened.ReadAsync("file-room").ConfigureAwait(false);
            Assert.That(loaded.Revision, Is.EqualTo(8)); Assert.That(loaded.Members[0].PlayerId, Is.EqualTo("new"));
        });
        [UnityTest] public IEnumerator TC17_FileFailureAtPublish_RollsBackBytesAndRemovesStagingFile() => Run(async () =>
        {
            var service = Service(); await service.SaveAsync(Draft(4, "stable")).ConfigureAwait(false);
            var path = Directory.GetFiles(directory, "*.draft").Single(); var before = File.ReadAllBytes(path);
            using (var reader = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Exception failure = null;
                try { await service.SaveAsync(Draft(5, "broken")).ConfigureAwait(false); }
                catch (IOException error) { failure = error; }
                Assert.That(failure, Is.Not.Null, "Windows sharing denies replacing an open non-delete-shared file.");
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(before));
                Assert.That(Directory.GetFiles(directory, "*.tmp"), Is.Empty);
            }
            Assert.That((await Service().ReadAsync("file-room").ConfigureAwait(false)).Revision, Is.EqualTo(4));
            Assert.That(await service.SaveAsync(Draft(5, "retry")).ConfigureAwait(false), Is.EqualTo(DraftSaveResult.Saved));
        });
        [UnityTest] public IEnumerator TC18_DurableLoad40AcrossIndependentBackends_P95AtMost1500ms()
        {
            var output = TestContext.Out;
            return Run(async () =>
            {
                await Service().SaveAsync(Draft(1)).ConfigureAwait(false);
                var start = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                long released = 0;
                var tasks = Enumerable.Range(0, 40).Select(i => Task.Run(async () =>
                {
                    await start.Task.ConfigureAwait(false);
                    await Service().SaveAsync(Draft(i * 17 % 40 + 2)).ConfigureAwait(false);
                    return (Stopwatch.GetTimestamp() - System.Threading.Interlocked.Read(ref released)) * 1000d / Stopwatch.Frequency;
                })).ToArray();
                System.Threading.Interlocked.Exchange(ref released, Stopwatch.GetTimestamp()); start.SetResult(true);
                var samples = await Task.WhenAll(tasks).ConfigureAwait(false); Array.Sort(samples);
                output.WriteLine(FormattableString.Invariant($"VT067_LOAD users=40 backend=atomic-file p50_ms={samples[19]:F6} p95_ms={samples[37]:F6} p99_ms={samples[39]:F6} max_ms={samples[39]:F6} limit_ms=1500 cores={Environment.ProcessorCount}"));
                Assert.That(samples[37], Is.LessThanOrEqualTo(1500));
                Assert.That((await Service().ReadAsync("file-room").ConfigureAwait(false)).Revision, Is.EqualTo(41));
            });
        }
        [UnityTest] public IEnumerator TC19_CorruptedFile_FailsClosed_WithoutOverwritingEvidence() => Run(async () =>
        {
            var service = Service(); await service.SaveAsync(Draft(4)).ConfigureAwait(false);
            var path = Directory.GetFiles(directory, "*.draft").Single(); File.AppendAllText(path, "corrupt");
            var before = File.ReadAllBytes(path); Exception failure = null;
            try { await Service().SaveAsync(Draft(5)).ConfigureAwait(false); }
            catch (InvalidDataException error) { failure = error; }
            Assert.That(failure, Is.Not.Null); Assert.That(File.ReadAllBytes(path), Is.EqualTo(before));
        });
    }
}
#endif
