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
   instances, a fixed overview camera, directional light and global volume.
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

Task 1.2 adds the pure match-state rules and 30 NUnit cases. Run them without an
Editor license or package downloads using the bundled compiler, runtime and NUnit:

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
Unity's PlayMode Test Runner. These 22 cases load and unload `Arena_MVP` for
isolation. They verify the placeholder stage and must be reviewed when later tasks
intentionally add gameplay components. See `testing/task_1_4_verification.md`.

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
