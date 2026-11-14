// In-process query measurement only. Each worker owns a realistic four-member room.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmallArena.Server.Sessions;

internal static class LobbySearchLoad
{
    private static LobbyRoom Fixture()
    {
        var room = new LobbyRoom(); room.Open();
        foreach (var id in new[] { 9, 0, 7, 2 }) { room.TryJoin(id, id == 0); if (id != 0) room.SetReady(id, true); }
        return room;
    }

    public static int Main(string[] args)
    {
        const int users = 200, runs = 3;
        for (var i = 0; i < 1000; i++) Fixture().SearchMembers(0);
        var samples = new List<double>();
        var errors = 0; var timeouts = 0;
        var csv = new List<string> { "run,worker,seconds,success,error" };
        var summaries = new List<string>();
        for (var run = 0; run < runs; run++)
        {
            using var ready = new CountdownEvent(users);
            using var release = new ManualResetEventSlim(false);
            long releasedAt = 0;
            var active = 0; var peak = 0;
            var tasks = Enumerable.Range(0, users).Select(worker => Task.Factory.StartNew(() =>
            {
                var room = Fixture(); // Excluded setup; no shared room access across workers.
                ready.Signal();
                release.Wait();
                var now = Interlocked.Increment(ref active);
                int previous;
                do { previous = Volatile.Read(ref peak); if (previous >= now) break; }
                while (Interlocked.CompareExchange(ref peak, now, previous) != previous);
                var ok = false; var error = "";
                try
                {
                    int[] expected;
                    int[] actual;
                    switch (worker % 4)
                    {
                        case 0: actual = room.SearchMembers(0, pageSize: 2).Select(m => m.ConnectionId).ToArray(); expected = new[] { 2, 7 }; break;
                        case 1: actual = room.SearchMembers(0, page: 1, pageSize: 2).Select(m => m.ConnectionId).ToArray(); expected = new[] { 9, 0 }; break;
                        case 2: actual = room.SearchMembers(0, ready: true, host: false).Select(m => m.ConnectionId).ToArray(); expected = new[] { 2, 7, 9 }; break;
                        default: actual = room.SearchMembers(0, ready: false, host: true, pageSize: 1).Select(m => m.ConnectionId).ToArray(); expected = new[] { 0 }; break;
                    }
                    ok = actual.SequenceEqual(expected);
                    if (!ok) error = "oracle mismatch";
                }
                catch (Exception e) { error = e.GetType().Name; }
                finally { Interlocked.Decrement(ref active); }
                var elapsed = (Stopwatch.GetTimestamp() - Volatile.Read(ref releasedAt)) / (double)Stopwatch.Frequency;
                return (worker, elapsed, ok, error);
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();
            if (!ready.Wait(TimeSpan.FromSeconds(30))) throw new TimeoutException("200 workers did not reach gate; no load result.");
            Volatile.Write(ref releasedAt, Stopwatch.GetTimestamp());
            release.Set();
            if (!Task.WaitAll(tasks, TimeSpan.FromSeconds(30)))
            {
                File.WriteAllLines(Path.Combine(args[0], "latency.csv"), csv);
                File.WriteAllText(Path.Combine(args[0], "load-summary.txt"),
                    $"INCOMPLETE run={run}; completed={tasks.Count(t => t.IsCompleted)}; deadlineSeconds=30; pass=False");
                // LongRunning workers may still be running; never label this population successful.
                Environment.Exit(1);
            }
            foreach (var task in tasks)
            {
                var value = task.Result;
                if (!value.ok) errors++;
                if (value.elapsed > 5) timeouts++;
                samples.Add(value.elapsed);
                csv.Add($"{run},{value.worker},{value.elapsed.ToString("R", CultureInfo.InvariantCulture)},{value.ok},{value.error}");
            }
            var sorted = tasks.Select(t => t.Result.elapsed).OrderBy(x => x).ToArray();
            summaries.Add($"run={run}; requested={users}; atGate={users}; pendingAtRelease={users}; peakActiveQueries={peak}; p95Seconds={sorted[(int)Math.Ceiling(.95 * users) - 1].ToString("R", CultureInfo.InvariantCulture)}");
        }
        samples.Sort();
        var p95 = samples[(int)Math.Ceiling(.95 * samples.Count) - 1];
        summaries.Add($"samples={samples.Count}; success={samples.Count-errors}; errors={errors}; over5Seconds={timeouts}; nearestRankP95Seconds={p95.ToString("R", CultureInfo.InvariantCulture)}; pass={errors == 0 && timeouts == 0 && p95 <= 5}");
        summaries.Add("Boundary: release timestamp through query plus ID oracle (includes scheduling delay). Warmup=1000; 3 populations x 200 LongRunning tasks. Gate/completion deadlines=30s. over5Seconds is a latency-budget violation, not cancellation. No sockets, clients, UI, mutation concurrency or room-cap increase. Data: 4 members/room, 3 equal-ready ties; four request mixes equally represented, no random seed.");
        File.WriteAllLines(Path.Combine(args[0], "latency.csv"), csv);
        File.WriteAllLines(Path.Combine(args[0], "load-summary.txt"), summaries);
        foreach (var line in summaries) Console.WriteLine(line);
        return errors == 0 && timeouts == 0 && p95 <= 5 ? 0 : 1;
    }
}
