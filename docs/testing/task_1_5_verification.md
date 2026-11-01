# Task 1.5 - Input asset and intent adapter verification

Date: 2026-11-02 (Asia/Saigon). Starting commit: `1d39a7b`.
Implementation remains uncommitted for Lead Engineer / PM review.

**Review status:** asset checks and full-source Roslyn compilation pass. Native
PlayMode input behavior is unverified because Unity exits with license error 198.

## Delivered files and responsibilities

| File | Responsibility |
| --- | --- |
| `Assets/_Project/Input/SmallArena.inputactions` | One keyboard-only Gameplay map, four actions, eight bindings including composite parts |
| `Assets/_Project/Scripts/Input/PlayerInputHandler.cs` | SmallArena.Runtime adapter: input callbacks to clamped movement and consumable fresh-press intent |
| `Assets/_Project/Tests/PlayMode/PlayerInputHandlerTests.cs` | 34 simulated-keyboard cases using InputTestFixture and the real project action JSON |
| Matching `.meta` files | InputActionImporter metadata with wrapper generation off; unique C# metadata |
| `tools/Verify-Input.ps1` | Saved JSON/importer/scope assertions and compilation entry point |
| `tools/Compile-UnityAssemblies.ps1` | Compiles all actual sources in the four asmdefs with dependency ownership and explicit references |

The PlayMode asmdef adds `Unity.InputSystem` and `Unity.InputSystem.TestFramework`.
`Packages/manifest.json` adds only `testables: ["com.unity.inputsystem"]`, as
required for that existing package's InputTestFixture. Dependency versions and
`packages-lock.json` are unchanged. The four-asmdef boundary remains intact.
Scaffold/layout verification commands now delegate to the shared compiler instead
of compiling only assembly identity attributes or using incomplete fixture refs.
Input folder descriptions, setup, architecture and traceability are updated.

The Player prefab and all scene/prefab assets remain unchanged and unscripted.
No physics, movement/gravity, Health, combat, collection, camera or match logic is
implemented or attached. No networking or extra package dependency is introduced.

An existing uncommitted `ProjectSettings/ProjectSettings.asset` change was present
before this task: `templateDefaultScene` already points to `Arena_MVP.unity`.
That user change was preserved; it is not a Task 1.5 modification.

## Input configuration and lifecycle contract

| Action | Type | Binding / configuration |
| --- | --- | --- |
| Move | Value / Vector2 | WASD 2DVector, DigitalNormalized (`mode=0`); initial-state check enabled |
| Jump | Button | `<Keyboard>/space`; `Press(behavior=0)`; initial-state check disabled |
| Punch | Button | `<Keyboard>/j`; `Press(behavior=0)`; initial-state check disabled |
| Interact | Button | `<Keyboard>/e`; `Press(behavior=0)`; initial-state check disabled |

The adapter owns a clone of the Inspector-assigned action asset. An optional
`Configure(asset)` seam accepts explicit composition while inactive, before first
successful initialization; runtime reconfiguration is rejected. Missing or invalid
configuration logs a diagnostic and disables the component without enabling a
partial action map. The shared source is never enabled or disabled by the adapter.

OnEnable subscribes callbacks and enables the owned Gameplay map. Move performed
and canceled callbacks update `MoveInput` through `Vector2.ClampMagnitude(...,1)`.
Button performed callbacks latch independent boolean intents; each Consume method
returns and clears its bit. Held keys do not generate automatic repeats.

OnDisable removes callbacks, disables Gameplay, clears movement and all pending
bits. Button initial-state checks are explicitly disabled in the clone, so held
buttons are not replayed on enable/re-enable. A held direction can resume through
the Move Value action's next initial-state check. OnDestroy disables and destroys
the owned asset. Other owners and the shared source remain independent.

Press flags coalesce until consumed; they are not a queue of attempts. Future
controllers must drain input each gameplay update and discard ineligible requests
without buffering. Grounding, cooldown, range, match readiness and terminal action
gating remain downstream responsibilities. MoveInput is a 2D intent; camera/world
mapping is deferred to the motor.

## Executed verification

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Verify-Input.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Verify-Scaffold.ps1 -CompileProbes
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Verify-ArenaLayout.ps1
```

| Check | Actual result |
| --- | --- |
| Saved asset and adapter scope | **39 assertions passed**: action types/controls, normalized composite, all keyboard paths, press-only/initial-state settings, binding groups, unique IDs, importer/no wrapper, New Input System, testables and adapter scope |
| SmallArena.Domain | **4 actual source files compile**, framework references only |
| SmallArena.Runtime | **2 actual source files compile**, including PlayerInputHandler |
| SmallArena.EditMode.Tests | **2 actual source files compile**, including MatchStateTests |
| SmallArena.PlayMode.Tests | **4 actual source files compile**, including all three fixtures and assembly identity |
| Compiler diagnostics | **Zero errors/warnings**, C# 9 with warnings treated as errors |
| Domain boundary controls | Engine-access negative probe rejected; same probe with engine reference compiled |
| Metadata/scaffold | Passed: 69 asset/folder metadata pairs, unique GUIDs, four asmdefs and 24 requirement mappings |
| Arena structure regression | **587 saved-layout assertions passed**; prefabs/scenes remain unchanged |
| Whitespace | `git diff --check` passed |
| Native input test execution | **Blocked**: fresh Unity batch run exited **198**, `No valid Unity Editor license found. Please activate your license.` |

The compiler reads each asmdef and assigns each C# file to its nearest parent
definition. Nested Domain sources are excluded from Runtime. It compiles in
Domain -> Runtime -> EditMode.Tests -> PlayMode.Tests order, using the actual
references and NUnit precompiled reference. Response files avoid command-line
length limits and retain reproducible source/reference lists.

Environment: Unity 6000.6.7f1's bundled SDK 8.0.318, .NET 8.0.21 reference/runtime
assemblies, Unity engine/editor modules and existing template-seeded Input System
and TestRunner DLLs. These cached DLLs are compiler inputs, not evidence of a
successful Package Manager restore/import for this project.

## PlayMode assertion catalogue

All **34 cases compile but have not executed**. Tests load the actual action JSON,
use an isolated InputTestFixture, add a simulated Keyboard and process state
events. They do not replace the adapter with fake production logic.

| Assertion group | Cases |
| --- | ---: |
| W/A/S/D cardinal unit vectors | 4 |
| W+S / A+D cancel previously active movement | 2 |
| All four diagonal combinations normalized to magnitude <=1 | 4 |
| All four directions cancel | 1 |
| Release clears movement | 1 |
| Adapter clamp independently handles an unnormalized composite | 1 |
| Jump/Punch/Interact consume true exactly once | 3 |
| Holding each button across updates and unrelated keyboard events does not repeat | 3 |
| Release/repress produces one new intent per button | 3 |
| Button flags are independent | 1 |
| Component/GameObject disable clears movement, pending flags and enabled actions | 2 |
| Disabled component receives no new input | 1 |
| Held-key re-enable restores movement without replaying buttons | 1 |
| Repeated enable/disable retains exactly four owned actions | 1 |
| Shared source remains disabled | 1 |
| Disabling one adapter leaves another owner's actions working | 1 |
| Removing keyboard clears movement | 1 |
| Reconfiguration after initialization rejected | 1 |
| Missing asset fails closed with diagnostic | 1 |
| Missing Gameplay map fails without partially enabled actions | 1 |

## Evidence and acceptance limits

- `TestResults/Input/results.json`: saved configuration assertions and declared
  case count; no native behavioral result is inferred.
- `TestResults/Input/Assemblies/results.json`: exact source/reference paths for
  all four compilation units.
- `TestResults/Input/Assemblies/*.rsp` and `*.compile.log`: compiler inputs and
  successful no-diagnostics records.
- `TestResults/Input/Unity-playmode.log`: license failure from the attempted run.
- No `Unity-playmode-results.xml` was produced; **zero native tests are claimed
  Passed**. Generated binaries/results stay ignored by Git.

After license activation and successful package import, run
`SmallArena.Tests.PlayMode.PlayerInputHandlerTests` in the Editor PlayMode runner.
The fixture's file-loading setup targets Editor PlayMode; standalone player-test
builds are outside this step. Full TC-002/003/006/010/017/018 and TC-028 remain
Planned until native execution and the corresponding movement, combat, pickup
and match integrations exist. The license blocker is not waived.

Test-framework setup follows the installed Input System documentation,
`Documentation~/set-up-test-assemblies.md`, including manifest testables and both
Input System assembly references. See also the official
[testing manual](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/manual/Testing.html).

## Working tree status

```text
 M Assets/_Project/Input/README.md
 M Assets/_Project/Scripts/Input/README.md
 M Assets/_Project/Tests/PlayMode/SmallArena.PlayMode.Tests.asmdef
 M Packages/manifest.json
 M ProjectSettings/ProjectSettings.asset
 M docs/architecture/assembly_boundaries.md
 M docs/setup.md
 M docs/testing/traceability_matrix.md
 M tools/Verify-ArenaLayout.ps1
 M tools/Verify-Scaffold.ps1
?? Assets/_Project/Input/SmallArena.inputactions
?? Assets/_Project/Input/SmallArena.inputactions.meta
?? Assets/_Project/Scripts/Input/PlayerInputHandler.cs
?? Assets/_Project/Scripts/Input/PlayerInputHandler.cs.meta
?? Assets/_Project/Tests/PlayMode/PlayerInputHandlerTests.cs
?? Assets/_Project/Tests/PlayMode/PlayerInputHandlerTests.cs.meta
?? docs/testing/task_1_5_verification.md
?? tools/Compile-UnityAssemblies.ps1
?? tools/Verify-Input.ps1
```
