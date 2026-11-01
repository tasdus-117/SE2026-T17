# Small Arena project setup

## Pinned baseline

Task 1.1 explicitly authorizes the installed Unity 6 patch, superseding the earlier
6000.3 proposal for this scaffold. Use **6000.6.7f1**, revision **b22767f04f04**.
Do not open with another patch and silently accept an upgrade.

Source template: installed `com.unity.template.urp-blank-17.2.6.tgz`. Its bundled
Universal 3D project data supplies rendering assets and project settings. The
template unpacked, but the Editor then exited with license error 198 before a
successful import/compilation. This is not a verified Editor-created baseline yet.

| Direct package | Version |
| --- | --- |
| Universal Render Pipeline | 17.6.0 |
| Input System | 1.20.0 |
| Unity Test Framework | 1.8.0 |
| uGUI | 2.6.0 |
| 3D Physics module | 1.0.0 |

`Packages/manifest.json` pins these dependencies. `packages-lock.json` retains the
installed template's selected transitive versions, pruned to the retained closure
and with dependency depths recalculated. It has not yet been confirmed by a
successful Package Manager resolve. First import may require Internet access for
registry dependencies; offline gameplay has no network dependency. No networking,
multiplayer, Unity Version Control, IDE integration or Cinemachine package is added
by this task. Cinemachine selection belongs to later camera setup.

## Open and verify

1. In Unity Hub, sign in and activate a valid Editor license. Install the pinned
   Editor and required Windows build support on each development workstation.
2. Add this repository root as an existing project and open it with 6000.6.7f1.
3. Let package resolution and import finish. Confirm zero compilation errors and
   record any lockfile changes; do not delete the lockfile to hide a mismatch.
4. Verify Force Text serialization and Visible Meta Files (already configured).
5. Open `Assets/_Project/Scenes/Arena_MVP.unity`, the enabled startup scene from
   Task 1.4. It contains the bounded placeholder arena and three unscripted prefab
   instances, a fixed overview camera, directional light, global volume and the
   Task 1.6 `_GameManager` with explicit entity/Camera references. Auto-start is
   off and full readiness is unassigned, so M1 remains Initializing.
   `Bootstrap.unity` remains as the original template reference and is not enabled
   in build settings. Graphics/quality settings retain the verified URP assets.
6. Run the scaffold checks from PowerShell at the repository root:

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File tools/Verify-Scaffold.ps1
   powershell -NoProfile -ExecutionPolicy Bypass -File tools/Verify-Scaffold.ps1 -CompileProbes
   ```

   The second command uses the installed Editor's bundled Roslyn compiler and
   template test DLLs. It is an independent source/reference check, not a Unity
   import, assembly graph validation, PlayMode execution or Windows build.

7. After license activation, run the actual Editor compilation check:

   ```powershell
   & 'C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath "$PWD" -logFile "$PWD\Logs\Unity-verify.log"
   ```

   Inspect the process exit code and full log after Unity exits. A normal return
   from the PowerShell launch alone does not establish Editor success.

No gameplay tests are implemented in Task 1.1. The four `AssemblyInfo.cs` files
contain identity attributes only, ensuring each assembly has a source file without
adding game behavior. Future EditMode/PlayMode cases remain Planned.

The pure domain suite has 34 NUnit cases after Task 1.6's readiness factories.
Run them without an Editor license or package downloads using the bundled
compiler, runtime and NUnit:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-DomainTests.ps1 -MutationChecks
```

This invokes NUnit's own test builder and execution runner, saves NUnit XML, and
tests isolated compiled mutations. See `testing/task_1_2_verification.md` for
scope and evidence. It does not replace licensed Unity import verification.

Task 1.4's saved-file geometry/reference checks and compile-only PlayMode checks:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Verify-ArenaLayout.ps1
```

After license activation, run `SmallArena.Tests.PlayMode.ArenaLayoutTests` in
Unity's PlayMode Test Runner. These 23 cases load and unload `Arena_MVP` for
isolation, including the Task 1.6 coordinator gate check. They verify the
placeholder stage and must be reviewed when later tasks
intentionally add gameplay components. See `testing/task_1_4_verification.md`.

## Input adapter verification

Task 1.5 input verification:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Verify-Input.ps1
```

This checks saved action configuration and compiles actual sources in all four
asmdefs, including Runtime and both test assemblies. It uses the installed
Editor's framework/modules and template-seeded package DLLs; it does not prove
Package Manager import. Compilation responses and logs are retained under ignored
`TestResults/Input/Assemblies`. `Compile-UnityAssemblies.ps1` is also used by the
scaffold compilation option and layout verifier to keep source/reference coverage
consistent as scripts are added.

After license activation, run `SmallArena.Tests.PlayMode.PlayerInputHandlerTests`
in Unity's PlayMode Test Runner. These tests use `InputTestFixture`, the actual
action JSON under `Assets/_Project/Input`, and simulated keyboard state. The
existing Input System package is in manifest `testables` as required for its test
framework. Do not add another input package or change its version for these tests.
Standalone player-test builds are outside this fixture's current file-loading
setup. See `testing/task_1_5_verification.md` for execution status and limits.

The adapter remains unattached to the placeholder Player. At later composition,
assign its InputActionAsset explicitly before enabling; missing/invalid action
configuration logs a diagnostic and leaves the component disabled. The global
project-wide action asset is not used.

## Scene startup verification

Run `SmallArena.Tests.PlayMode.MatchInitializationTests` after a successful
licensed Editor import. Its 34 cases exercise structural readiness, failure
diagnostics, full-readiness gating, events, reentrancy and auto-start. The extra
arena layout case checks the actual serialized coordinator bindings.

`ValidateSceneReadiness()` may return true for M1's three placeholders and Camera;
it does not enter Playing. `TryStartMatch()` uses `ValidateMatchReadiness()` and
requires a real `IMatchReadinessProbe`. Keep `_GameManager.readinessProvider`
unassigned and autoStart off until those dependencies exist. Do not introduce a
temporary success provider in Runtime. Failed manual start attempts log the full
readiness diagnostic. See `testing/task_1_6_verification.md`.

## Git and asset ownership

- The baseline commit is `8b47fe0` (`chore: establish clean project baseline and import specs`).
- Commit source assets with matching `.meta`, Packages and ProjectSettings.
- Metadata for moved template assets preserves original GUIDs. New scaffold
  metadata was generated with unique GUIDs outside the unlicensed Editor and
  must be confirmed during the first successful import.
- README assets preserve otherwise empty folders in a clean checkout.
- Unity does not require `.meta` for repository documentation, tools, Packages
  manifests or ProjectSettings files. Metadata checks apply to Assets.
- Library, Temp, Obj, Build, Builds, Logs, UserSettings and IDE output are ignored.
- Give scene/pipeline edits one owner at a time. Review scaffold changes before
  integration; no additional implementation commit was created by this task.

See `architecture/assembly_boundaries.md`, `testing/traceability_matrix.md`,
`testing/task_1_1_verification.md`, and `debt/technical_debt_register.md`.
