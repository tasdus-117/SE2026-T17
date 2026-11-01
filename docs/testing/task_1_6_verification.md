# Task 1.6 - Scene lifecycle coordination and startup validation

Date: 2026-11-02 (Asia/Saigon). Starting commit: `675f40c`.
Changes remain uncommitted for Lead Engineer / PM review.

**Status:** implementation, saved-scene serialization checks, full-source Roslyn
compilation and pure domain tests pass. Native PlayMode execution is blocked by
Unity license error 198; it is not marked Passed.

## Readiness interpretation and M1 invariant

The task requires both structural success when entity references are present and
no transition to Playing before real Health/HUD dependencies exist. These are
separate validation stages:

| API | Contract |
| --- | --- |
| ValidateSceneReadiness | Three distinct active entity roots, one per named role in the manager's scene, plus an active same-scene Camera; Success means structural readiness only |
| ValidateMatchReadiness | Structural readiness plus an explicitly supplied IMatchReadinessProbe validating real Health, player stats, pickup initialization and bound initial HUD |
| TryStartMatch | Evaluates the full gate, logs failures, then passes the result to Domain.MatchStateMachine.StartMatch |

The production `Arena_MVP` has no full-readiness provider and `autoStart: 0`.
It therefore stays **Initializing**, even though its references are valid. There
is no serialized "pretend ready" flag or production success stub. Enabling
auto-start without a provider still fails closed. Positive transition tests use
substitutes located exclusively in the test assembly; they do not represent
Health/HUD implementation or full TC-001 acceptance.

This preserves the task's requested structural-positive test and its stronger
M1 guardrail. Camera validation is included because the explicit dependency
guardrail lists Camera alongside Player, Bot and Pickup.

## Delivered changes

- Added `Scripts/Core/GameManager.cs` in SmallArena.Runtime with Inspector fields
  and a Configure method, a private per-instance Domain state machine, read-only
  CurrentState/IsPlaying and a local OnMatchStateChanged event.
- Added the small `Scripts/Core/IMatchReadinessProbe.cs` contract for future real
  dependencies; no Health, HUD, stats or collection component is scaffolded.
- Added pure Domain `MatchReadinessResult.Success()` / `Fail(message)` factories
  and four NUnit cases; constructor/default behavior remains unchanged.
- Added `Tests/PlayMode/MatchInitializationTests.cs` with 34 native cases.
- Added one arena layout case checking the actual scene coordinator and M1 gate.
- Wired `_GameManager` in `Scenes/Arena_MVP.unity` to Player, Bot, WeaponPickup
  scene GameObjects and the Camera component. Prefab GameObject references use
  proper stripped-object descriptors, not Transform or prefab-asset references.
- Added matching metadata for the three new C# files and preserved the scene GUID.
- Extended the saved-layout verifier to validate coordinator ownership, unique
  component, source script, all four bindings and the M1 gate configuration.
- Updated setup, architecture and traceability documentation.

All Assets paths above are relative to `Assets/_Project/`. Entity prefabs,
geometry, materials, input adapter, packages/asmdefs and physics/render settings
were not changed by this task. The pre-existing manifest `testables` edit and
ProjectSettings template scene edit were preserved, not staged or reverted.

## Coordinator responsibilities and failure behavior

Awake subscribes to the state machine once. Start either attempts full startup
when autoStart is enabled or validates scene structure without starting. Missing
scene references are diagnosed even with autoStart off. Explicit start calls
before Awake are supported without duplicate forwarding; destruction unsubscribes.

Validation rejects null/destroyed references, aliases, wrong layers, inactive
referenced entities, cross-scene references, non-root visual references and
duplicate role roots (including inactive extras). Visual descendants beneath a
same-role ancestor are treated as part of that entity. The traversal is scoped
to the manager's scene and occurs only during validation, not in an Update loop.

A missing, unready or throwing full-readiness provider cannot start the match.
Empty failure diagnostics receive a useful fallback. Structure is rechecked after
the provider returns to prevent stale readiness if an entity was destroyed during
validation. Reentrant validation/start/configuration is guarded; repeated starts
after Playing are rejected without re-running the provider or firing an event.

State commits and event notification semantics remain owned by the Domain state
machine. Subscriber exceptions propagate with the committed state preserved.
GameManager has no public state setter, static Instance, movement simulation,
physics queries, damage calculation, pickup logic or global object searches.

Real readiness integration must later validate actual bot HP 10, initialized
player stats and pickup state, and a bound HUD that has read initial state. A
full-readiness provider must not merely echo structural success. Action consumers
must respect IsPlaying; no consumers are introduced or wired in this step.

## Executed verification

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-DomainTests.ps1 -MutationChecks
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Compile-UnityAssemblies.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Verify-ArenaLayout.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Verify-Scaffold.ps1
```

| Check | Actual result |
| --- | --- |
| Standalone domain NUnit execution | **34 passed, 0 failed/skipped/inconclusive**, including Success/Fail factory cases |
| Existing targeted domain mutations | **13/13 detected** in isolated copied sources; production code never mutated |
| Domain engine boundary | Compiled references remain System.Runtime and System.Threading only |
| SmallArena.Domain compilation | 4 actual source files compile |
| SmallArena.Runtime compilation | 4 actual source files compile, including GameManager, readiness contract and input adapter |
| SmallArena.EditMode.Tests compilation | 2 actual source files compile |
| SmallArena.PlayMode.Tests compilation | 5 actual source files compile, including all four fixtures |
| Compiler diagnostics | Zero warnings/errors with C# 9 and warnings as errors |
| Saved scene/layout checks | **631 assertions passed** across 11 Unity YAML files, including exact prefab GameObject/Camera bindings and M1 gate fields |
| Metadata/scaffold | Four asmdefs and **72 matching asset/folder metadata pairs**, unique GUIDs and existing requirement mappings |
| Native initialization test attempt | **Blocked**: Unity exited **198**, `No valid Unity Editor license found. Please activate your license.` |

Roslyn uses the installed Unity 6000.6.7f1's SDK 8.0.318, .NET 8.0.21 references,
engine/editor modules and existing cached package DLLs. Full-source compilation
and saved YAML validation do not prove successful Unity import or native scene
execution. The native attempt exited before a result XML was produced.

## Initialization assertion catalogue

All **34 MatchInitializationTests cases compile but have not executed natively**:

| Assertion group | Cases |
| --- | ---: |
| Initializing and IsPlaying false on construction | 1 |
| Valid entities/Camera yield structural success while full readiness stays false | 1 |
| Each missing entity/Camera names its diagnostic, blocks startup and emits no state event | 4 |
| All missing references reported together | 1 |
| Aliased role references rejected | 1 |
| Duplicate role roots, active and inactive | 6 |
| Same-layer visual descendants do not become extra entities | 1 |
| Visual child cannot replace its entity-root reference | 1 |
| Wrong layer, foreign scene, inactive referenced entity and disabled Camera | 4 |
| Missing full provider blocks placeholders from Playing | 1 |
| Provider failure preserves diagnostic and permits later successful retry | 1 |
| Empty failure diagnostic gets a fallback | 1 |
| Successful start forwards committed Playing once; repeat is rejected | 1 |
| Event callback cannot reentrantly start | 1 |
| Readiness query cannot reentrantly start | 1 |
| Throwing readiness provider fails closed and releases guard | 1 |
| Entity destroyed during readiness is revalidated | 1 |
| Throwing state listener preserves committed Playing and prevents replay | 1 |
| Configuration after Playing is rejected | 1 |
| Configured auto-start, blocked auto-start, manual-before-Awake subscription and auto-start-off diagnostics | 4 |

The existing ArenaLayoutTests fixture adds one native case checking the actual
serialized `_GameManager`: structural readiness true, full readiness false and
state Initializing. That fixture now has 23 cases, also compile-only here.

## Evidence and acceptance limits

- `TestResults/Domain/results.xml`, `results.log`, and `mutation-results.json`:
  actual pure NUnit execution and detecting mutation case names.
- `TestResults/Compilation/results.json`, response files and compiler logs:
  actual source/reference lists for all four assemblies.
- `TestResults/ArenaLayout/results.json`: parsed scene/reference/gate assertions.
- `TestResults/MatchInitialization/Unity-playmode.log`: failed native attempt.
- No native NUnit result XML exists; no PlayMode case is claimed Passed.

After license activation and successful import, execute
`SmallArena.Tests.PlayMode.MatchInitializationTests` and
`SmallArena.Tests.PlayMode.ArenaLayoutTests`. Complete real dependency integration
before approving Playing in the production arena. TC-001/028/029 and AC-017's
actual combat/pickup cancellation remain incomplete; readiness foundation is not
full gameplay acceptance. Generated diagnostics and binaries stay ignored by Git.

## Working tree status

```text
 M Assets/_Project/Scenes/Arena_MVP.unity
 M Assets/_Project/Scripts/Core/Domain/MatchReadinessResult.cs
 M Assets/_Project/Tests/EditMode/MatchStateTests.cs
 M Assets/_Project/Tests/PlayMode/ArenaLayoutTests.cs
 M Packages/manifest.json
 M ProjectSettings/ProjectSettings.asset
 M docs/architecture/assembly_boundaries.md
 M docs/setup.md
 M docs/testing/traceability_matrix.md
 M tools/ArenaLayoutVerifier.cs
?? Assets/_Project/Scripts/Core/GameManager.cs
?? Assets/_Project/Scripts/Core/GameManager.cs.meta
?? Assets/_Project/Scripts/Core/IMatchReadinessProbe.cs
?? Assets/_Project/Scripts/Core/IMatchReadinessProbe.cs.meta
?? Assets/_Project/Tests/PlayMode/MatchInitializationTests.cs
?? Assets/_Project/Tests/PlayMode/MatchInitializationTests.cs.meta
?? docs/testing/task_1_6_verification.md
```
