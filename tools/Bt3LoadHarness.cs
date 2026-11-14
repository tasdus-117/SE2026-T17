using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using SmallArena.Networking;
using SmallArena.Server.Sessions;

// Dependency substitution only: the real LobbyMessages.cs is compiled, without a Mirror runtime.
namespace Mirror { public interface NetworkMessage { } }

public static class Bt3LoadHarness
{
    public static async Task<int> Main(string[] args)
    {
        var output = args[0]; int rounds = args.Length > 1 ? int.Parse(args[1]) : 20;
        Directory.CreateDirectory(output);
        var all = new List<object>();
        var raw = new List<string> { "round,identity,requests,boundary_attempts,fake_loader_side_effects,success_transitions,failure_transitions,failure_callbacks,expected_failure,latency_ms,terminal,final_room" };
        int errors = 0;
        for (int round = 1; round <= rounds; round++)
        {
            var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var dispatchGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            int submitted = 0;
            int inFlight = 0, peak = 0;
            var tasks = Enumerable.Range(0, 100).Select(async id =>
            {
                // Every operation owns a different room/session. No Unity API runs on workers.
                var room = new LobbyRoom(); room.Open(); room.TryJoin(0, true); room.TryJoin(1, false); room.SetReady(0, true); room.SetReady(1, true);
                var terminal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                int boundary = 0, loads = 0, success = 0, failed = 0, failureCallbacks = 0, requests = 0;
                bool expectedFailure = id % 10 == 0;
                var operation = new LobbyStartOperation(room,
                    () => { boundary++; if (expectedFailure) throw new InvalidOperationException("fake startup failure"); loads++; },
                    () =>
                    {
                        if (room.Phase == LobbyPhase.Playing) { success++; terminal.TrySetResult(true); }
                        if (room.Phase == LobbyPhase.Failed) { failed++; terminal.TrySetResult(false); }
                    }, _ => failureCallbacks++);
                await gate.Task;
                var watch = Stopwatch.StartNew(); // submit, including caller-side dispatch wait
                int outstanding = Interlocked.Increment(ref inFlight);
                int old;
                do { old = Volatile.Read(ref peak); if (old >= outstanding) break; }
                while (Interlocked.CompareExchange(ref peak, outstanding, old) != old);
                // All 100 callers submit before the harness dispatches any operation.
                // Stopwatch includes this queue/barrier wait, not just the fake dependency.
                if (Interlocked.Increment(ref submitted) == 100) dispatchGate.SetResult(true);
                await dispatchGate.Task;
                requests++; operation.TryStart(0);
                requests++; operation.TryStart(0); // same identity, tight retry
                if (!expectedFailure)
                {
                    // A fake async dependency: explicit 12..24ms delay, not arena IO or network.
                    await Task.Delay(12 + id % 13);
                    operation.Complete(); operation.Complete(); // repeated callback
                }
                await terminal.Task.WaitAsync(TimeSpan.FromSeconds(3));
                watch.Stop(); // caller actually observed terminal, not merely accepted/queued
                requests++; operation.TryStart(0); // retry after terminal, same identity
                Interlocked.Decrement(ref inFlight);
                var state = room.Phase.ToString();
                bool valid = boundary == 1 && loads == (expectedFailure ? 0 : 1) && success == (expectedFailure ? 0 : 1)
                    && failed == (expectedFailure ? 1 : 0) && failureCallbacks == (expectedFailure ? 1 : 0);
                room.Close();
                return new { id, requests, boundary, loads, success, failed, failureCallbacks, expectedFailure,
                    latencyMs = watch.Elapsed.TotalMilliseconds, state, finalRoom = room.Phase.ToString(), valid };
            }).ToArray();
            gate.SetResult(true); // all 100 tasks have reached the same asynchronous barrier
            var results = await Task.WhenAll(tasks);
            foreach (var r in results)
                raw.Add(string.Join(",", round, $"r{round}-session{r.id}", r.requests, r.boundary, r.loads, r.success, r.failed,
                    r.failureCallbacks, r.expectedFailure, r.latencyMs.ToString("F6", System.Globalization.CultureInfo.InvariantCulture), r.state, r.finalRoom));
            var sorted = results.Select(r => r.latencyMs).OrderBy(x => x).ToArray();
            var successOnly = results.Where(r => !r.expectedFailure).Select(r => r.latencyMs).OrderBy(x => x).ToArray();
            int bad = results.Count(r => !r.valid); errors += bad;
            all.Add(new { round, barrierParticipants = 100, completed = results.Length, requests = results.Sum(r => r.requests),
                expectedFailures = results.Count(r => r.expectedFailure), unexpectedErrors = bad, timeouts = 0,
                duplicateBoundary = results.Sum(r => Math.Max(0, r.boundary - 1)), duplicateFakeLoads = results.Sum(r => Math.Max(0, r.loads - 1)),
                duplicateTerminal = results.Sum(r => Math.Max(0, r.success + r.failed - 1)), peakSubmittedInFlight = peak,
                p95Ms = sorted[(int)Math.Ceiling(0.95 * sorted.Length) - 1], p95SuccessOnlyMs = successOnly[(int)Math.Ceiling(0.95 * successOnly.Length) - 1],
                targetMs = 3000, meetsTarget = sorted[94] <= 3000 });
        }
        File.WriteAllLines(Path.Combine(output, "raw.csv"), raw);
        var report = new { runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            os = System.Runtime.InteropServices.RuntimeInformation.OSDescription, logicalProcessors = Environment.ProcessorCount,
            rounds, operationsPerRound = 100, productionSources = new[] { "LobbyRoom.cs", "LobbyStartOperation.cs", "LobbyMessages.cs" },
            dependencies = "Fake Mirror.NetworkMessage marker only; fake load increment + Task.Delay(12..24ms), 10% injected pre-side-effect failures. No Unity, transport, serialization, renderer, disk arena load or real clients.",
            scope = "Independent production room/start-operation logic, serial calls within each session; release barrier plus a dispatch barrier ensures 100 submitted operations in flight. Stopwatch includes harness dispatch queue wait. Does not establish real multiplayer/arena p95 or thread safety for shared room access.",
            results = all };
        File.WriteAllText(Path.Combine(output, "summary.json"), JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Rounds={rounds}, operations={rounds * 100}, requests={rounds * 300}, unexpected errors={errors}; raw.csv and summary.json saved.");
        return errors == 0 ? 0 : 1;
    }
}
