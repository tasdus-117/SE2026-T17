# Task 1.1 verification record

Date: 2026-11-02, Asia/Saigon. Baseline commit: `8b47fe0`.
Scaffold changes are uncommitted for Lead Engineer / PM review.

## Executed checks

| Check | Outcome | Evidence / limits |
| --- | --- | --- |
| Legacy reset and spec import | Passed | Baseline commit records 29 backend deletions plus `.gitignore`, `srs.md`, and `system_engineering.md`. |
| Exactly four project asmdefs | Passed | `tools/Verify-Scaffold.ps1` checks names, exact reference lists and isolation flags. |
| Required folder skeleton | Passed | Includes both requested Domain directories; only `Scripts/Core/Domain` owns domain code. README assets preserve empty leaves across checkout. |
| Asset metadata | Passed statically | 50 Assets file/folder metadata pairs; unique valid GUIDs; no orphan sidecars. Existing template GUIDs retained. New metadata is generated, not confirmed by Editor import yet. |
| Serialization and version control | Passed statically | Force Text (`m_SerializationMode: 2`); Visible Meta Files. |
| URP and bootstrap references | Passed statically | Default PC pipeline and quality overrides resolve to existing asset GUIDs; bootstrap scene path exists and is configured in build settings. |
| Package manifest/lock consistency | Passed statically | Five direct dependencies; 25 packages in the retained closure. No multiplayer packages. Package Manager resolve is not yet verified. |
| Requirements traceability | Passed | All FR-001–017 and NFR-001–007 mapped to planned SRS TC IDs. No SRS gameplay case marked Passed. |
| Generated files excluded | Passed | Git index contains no generated cache/build directories; ignore rules checked separately. |
| Four assembly source compilations | Passed independently | Bundled Roslyn compiler, C# 9, warnings treated as errors; compiles each `AssemblyInfo.cs` with the framework and applicable configured references. This is not Unity compilation. |
| Domain engine-access negative probe | Passed independently | Without engine references, `UnityEngine.Vector3` produces expected CS0246. Adding `UnityEngine.CoreModule.dll` makes the same source compile (positive control). |
| Unity Editor import/compilation | **Blocked** | Editor logs `No valid Unity Editor license found. Please activate your license.` and exits with code 198. No Editor compilation pass claimed. |
| Windows build / EditMode / PlayMode execution | Not executed | Requires successful licensed import; gameplay/tests are outside this scaffold step. |

Independent compiler inputs: Editor 6000.6.7f1's SDK 8.0.318 / reference pack
8.0.21; engine CoreModule; Input System DLL from the template-seeded local cache;
template TestRunner DLLs; bundled NUnit DLL. Seeded template DLLs are used only as
compiler reference inputs and do not establish that this project's packages or
assemblies have been imported by Unity.

Reproduction: `powershell -NoProfile -ExecutionPolicy Bypass -File
tools/Verify-Scaffold.ps1 -CompileProbes` from the repository root.
Local diagnostics and temporary probes are under ignored
`Logs/ScaffoldVerification/`. The failed Editor attempt is in ignored
`Logs/Unity-create.log`. Generated sources never enter Assets.

## Outstanding acceptance gate

Activate a valid Editor license in Hub, run a fresh import with the pinned Editor,
verify the package lockfile and generated metadata, and retain the successful
Editor compilation log before declaring Task 1.1 fully accepted. Do not waive this
gate based on the independent compiler checks. Gameplay test statuses stay Planned.
