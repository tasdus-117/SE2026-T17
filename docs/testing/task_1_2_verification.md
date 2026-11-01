# Task 1.2 - Pure domain match-state verification

Date: 2026-11-02 (Asia/Saigon). Starting commit: `edad0e7`.
Implementation is uncommitted for Lead Engineer / PM review.

## Implementation and diff summary

- Added `MatchState.cs`, immutable `MatchReadinessResult.cs`, and
  `MatchStateMachine.cs` strictly under `Assets/_Project/Scripts/Core/Domain/`.
- Added `MatchStateTests.cs` under `Assets/_Project/Tests/EditMode/` and matching
  Unity metadata for all four C# files.
- Added `tools/NUnitDomainRunner.cs` and `tools/Run-DomainTests.ps1` for standalone
  NUnit execution and isolated mutation verification. Neither tool enters Assets
  or creates a new Unity assembly/package dependency.
- Updated domain assembly identity commentary, architecture/setup documentation
  and requirement traceability; added this report.
- No changes to asmdefs, Runtime/PlayMode source, packages, scenes, rendering,
  physics, input or gameplay integration. No additional technical debt recorded.

## State and notification contract

Initial state is Initializing. A ready result permits Playing once; failed or
default readiness does not change state or emit an event. Only Playing permits
Victory. Victory is terminal; rejected requests return false and emit nothing.
`CurrentState` has a private setter; `IsPlaying` reflects the committed state.

Readiness is a readonly struct with explicit `IsReady` and `DiagnosticMessage`.
Default readiness is false; default/null messages read as empty. Validation is
the caller's responsibility; the domain never inspects scene objects.

State commits before notification. While an event is being delivered, all nested
transition requests are rejected without queuing. This prevents a Playing
listener from causing Victory before subsequent listeners observe Playing.
Requests can be retried after notification completes. Subscriber exceptions
propagate according to normal multicast-event behavior: state remains committed,
later listeners in that invocation are not called, and the notification guard is
released in `finally`. The machine is single-threaded, not a concurrency API.

## Execution method and results

Command, from the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-DomainTests.ps1 -MutationChecks
```

Uses Unity 6000.6.7f1's bundled Roslyn (SDK 8.0.318), .NET 8.0.21 runtime/reference
assemblies, and its installed `com.unity.ext.nunit` 2.1.0 framework package.
NUnit's actual `NUnitTestAssemblyRunner` discovers and executes the fixture;
assertions are not simulated with a custom reflection test loop. No NuGet
downloads or Unity license are required. Compilation uses C# 9 with warnings
treated as errors.

**Baseline: 30 passed, 0 failed, 0 skipped, 0 inconclusive.**

| Executed coverage | Cases | Result |
| --- | ---: | --- |
| Initializing and IsPlaying false on construction | 1 | Passed |
| No public state setter | 1 | Passed |
| Readiness flag/message preservation, null/empty/diagnostic partitions and getter-only properties | 6 | Passed |
| Default readiness rejects startup | 1 | Passed |
| Failed readiness rejects with no notification | 3 | Passed |
| Successful start commits Playing before one notification | 1 | Passed |
| Failed initialization can retry successfully | 1 | Passed |
| Start rejected in Playing and Victory, with either readiness flag | 4 | Passed |
| Victory rejected during Initializing | 1 | Passed |
| Valid victory commits before one notification | 1 | Passed |
| Repeated victory and restart rejected (1 and 10 repetitions) | 2 | Passed |
| Lifecycle notification ordering and operation without listeners | 2 | Passed |
| Unsubscription prevents further notifications | 1 | Passed |
| Machine state and subscriptions are isolated per instance | 1 | Passed |
| Reentrant start/victory rejected; listener observations consistent | 2 | Passed |
| Throwing Playing/Victory listeners preserve committed state; guard recovers | 2 | Passed |

The domain is compiled from all actual Domain C# files with framework references
only. Inspection of the compiled assembly reports **System.Runtime** and
**System.Threading** (the C# event implementation). There are no Unity, NUnit,
Runtime or third-party references in the domain. NUnit is referenced only by the
test assembly and standalone host.

## Mutation evidence

Each mutation changes a copied source file under ignored `TestResults/Domain/`.
Production files are never mutated. Each mutated domain and the unchanged fixture
must compile successfully, then execute in a separate process with its own host
and binaries. The host checks the loaded domain path. A mutation is counted as
detected only when NUnit reports actual failing cases with all 30 cases discovered;
compiler failures and runner failures do not count. Old XML is removed before a
run so stale results cannot satisfy the check.

**13/13 targeted mutations detected; 0 survived.** This is coverage of the named
mutations, not a claim that every possible mutation or defect has been excluded.

| ID | Deliberate defect | Failing NUnit cases |
| --- | --- | ---: |
| M01 | Invert readiness guard | 21 |
| M02 | Invert Initializing start guard | 17 |
| M03 | Invert Playing victory guard | 14 |
| M04 | Victory transitions to Playing | 11 |
| M05 | Suppress notification | 11 |
| M06 | Allow victory during Playing notification | 1 |
| M07 | Notify before committing state | 6 |
| M08 | Never release notification guard | 13 |
| M09 | Notify twice | 10 |
| M10 | Invert IsPlaying | 15 |
| M11 | Expose a public state setter | 1 |
| M12 | Remove null/default diagnostic normalization | 3 |
| M13 | Release notification guard only when listeners succeed | 1 |

In particular, M06 is detected by
`PlayingNotification_RejectsReentrantTransitions_AndKeepsListenersConsistent`,
M11 by `CurrentState_HasNoPublicSetter`, and M13 by
`ThrowingPlayingListener_PropagatesWithoutRollback_AndReleasesNotificationGuard`.

## Evidence and practical limits

- `TestResults/Domain/results.xml`: per-case NUnit results.
- `TestResults/Domain/results.log`: names and baseline pass counts.
- `TestResults/Domain/mutation-results.json`: mutation descriptions and exact
  detecting test names.
- `TestResults/Domain/Mutations/M01` through `M13`: copied sources, compiled
  binaries, NUnit XML and logs for each mutant.
- `tools/Verify-Scaffold.ps1`: static scaffold validation passes with 54 matching
  asset/folder metadata pairs, four asmdefs and 24 requirement mappings.
- Generated evidence is ignored by Git; the runner reproduces it locally.

This establishes domain portions of AC-001/TC-029, AC-015/TC-024 and
AC-017/TC-026. It does not establish actual Health defeat, scene initialization,
UI, input gating or pending-hit cancellation. Full SRS scenario statuses remain
Planned. Unity Test Runner/import was not used; Task 1.1's license blocker remains
separate from this successful standalone NUnit execution.
