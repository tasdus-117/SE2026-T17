# Milestone 1 verification and traceability audit

Date: 2026-11-02 (Asia/Saigon). Checkpoint: **M1 Structural Baseline**.
Local annotated tag: `m1-structural-baseline`. No remote publication is performed.

## Decision and evidence boundary

The saved structural baseline, pure domain suite and independent compilation
checks pass. Full M1 acceptance remains open: licensed Unity import, native tests,
clean-checkout verification and a Windows smoke build have no passing evidence.
The original M1 acceptance checkbox in `srs.md` remains unchecked. This tag
records a reviewable checkpoint and does not waive those gates.

TC-001, TC-004, TC-028 and TC-029 are each **Planned / Partially Verified
(M1 Structural Baseline)**. No whole SRS gameplay test is marked Passed.
Health, combat, movement, pickup transactions and HUD remain later milestones.
No networking, lobby, additional bots or gameplay implementation was added here.

The [traceability matrix](traceability_matrix.md) covers all 17 FRs, seven NFRs
and 37 catalogue test IDs. NFR-004 is Verified for the M1 assembly/ownership
boundary, and NFR-007 for the M1 test infrastructure; their full acceptance
criteria remain deferred. See the [debt register](../debt/technical_debt_register.md).

## Baseline and audit identity

| Component | Observed baseline / source |
| --- | --- |
| Unity Editor | 6000.6.7f1, revision b22767f04f04; `ProjectSettings/ProjectVersion.txt` and locally installed Editor |
| URP | 17.6.0; pinned manifest/lockfile, saved pipeline and renderer assets |
| Input System | 1.20.0; pinned manifest/lockfile, package exposed through `testables` for InputTestFixture |
| Unity Test Framework | 1.8.0 |
| uGUI | 2.6.0 |
| NUnit | Editor-bundled `com.unity.ext.nunit` 2.1.0 package; actual NUnit builder/runner used by the standalone host |
| Compiler / runtime | Editor-bundled .NET SDK 8.0.318, runtime/reference pack 8.0.21, C# 9; warnings treated as errors |
| Script host | Windows PowerShell; exact version retained in `verification-summary.json` |

The audit began at `bed9a8f032371fff5b3ad2d35e6b6be855ee129c` on
`feature/system-skeleton`. The final audited tree is the commit targeted by
`m1-structural-baseline`; resolve its full hash with:

```powershell
git rev-parse 'm1-structural-baseline^{commit}'
git show --stat m1-structural-baseline
```

| Earlier checkpoint | Commit | Contribution |
| --- | --- | --- |
| Baseline reset/specs | 8b47fe0 | Legacy reset and normalized specifications |
| Task 1.1 | edad0e7 | Unity scaffold and four assemblies |
| Task 1.2 | e8bb3cc | Pure domain rules and targeted mutation suite |
| Task 1.3 | 7b205c68e814ce43d91b435cb1207c12bd3e703c | Named layers, physics matrix and render settings |
| Task 1.4 | 1d39a7b5249f6e6366b59150a3143d7d1beb780f | Bounded scene and placeholder prefabs |
| Task 1.5 | 675f40c | Input action asset and intent adapter |
| Task 1.6 | bed9a8f032371fff5b3ad2d35e6b6be855ee129c | Lifecycle coordinator and full-readiness gate |

Four previously pending M1 corrections were reviewed and included with this
checkpoint: four readiness-factory NUnit cases in `MatchStateTests.cs`, the actual
scene/coordinator gate case in `ArenaLayoutTests.cs`, the Input System `testables`
manifest entry, and the Arena_MVP template-default scene path. They explain why
the audited tree differs from the starting commit. No production C# was changed
by Task 1.7.

## Structural inventory and verified invariants

Paths below are relative to the repository root. Geometry and reference checks
read saved files; they do not simulate Unity physics.

| Asset / boundary | Verified saved baseline |
| --- | --- |
| `Assets/_Project/Scenes/Arena_MVP.unity` | One 16m x 16m floor, four 3m perimeter walls, closed corners, playable inner perimeter x/z = +/-8; Directional Light and Camera |
| `Assets/_Project/Prefabs/Player.prefab` | Exactly one scene instance; Player layer 7, capsule and CharacterController, no Rigidbody; spawn (0, 1, -4) |
| `Assets/_Project/Prefabs/Bot.prefab` | Exactly one scene instance; Bot layer 8, static capsule collider, no AI or Health; spawn (0, 1, 4) |
| `Assets/_Project/Prefabs/WeaponPickup.prefab` | Exactly one scene instance; Pickup layer 9, trigger collider, kinematic Rigidbody with gravity off; spawn (-3, 0.3, 0) |
| Arena geometry | Environment layer 6; spawns supported by floor, separated and inside perimeter |
| `_GameManager` | One coordinator; serialized references resolve to the three scene prefab roots and Camera; no static singleton or per-frame global lookup |
| Startup | `ValidateSceneReadiness` checks structure; `TryStartMatch` requires real Health/stats/pickup/HUD readiness through IMatchReadinessProbe. Scene provider is unassigned and autoStart is off, so production remains Initializing |
| Input | Gameplay map, WASD/Space/J/E, normalized/clamped movement and fresh-press consumption adapter; adapter remains unattached to Player; native behavior unexecuted |
| Rendering | Saved Graphics/Quality references resolve to URP settings assets, including `Assets/_Project/Settings/PC_RPAsset.asset`; Linear color space |
| Asset hygiene | 72 asset/folder metadata pairs, unique GUIDs, no orphan metadata; Force Text and Visible Meta Files configured |

The physics matrix is symmetric. Player interacts with Default, Environment and
Bot; Player-Pickup is enabled to permit triggers. Pickup-Environment and Pickup-Bot
are disabled. The pickup collider is a trigger, so enabling Player-Pickup does not
make it a solid obstacle. Runtime contact/trigger behavior still needs native tests.

Exactly four project asmdefs exist:

| Assembly | Explicit references / isolation |
| --- | --- |
| SmallArena.Domain | None; noEngineReferences true, automatic plugin references excluded; compiled domain references only System.Runtime and System.Threading |
| SmallArena.Runtime | SmallArena.Domain, Unity.InputSystem |
| SmallArena.EditMode.Tests | Domain, Runtime, UnityEngine.TestRunner, UnityEditor.TestRunner; Editor-only, test/NUnit references |
| SmallArena.PlayMode.Tests | Domain, Runtime, UnityEngine.TestRunner, UnityEditor.TestRunner, Unity.InputSystem, Unity.InputSystem.TestFramework; test/NUnit references |

The negative Domain probe fails with CS0246 when using UnityEngine without an
engine reference; the same probe compiles with an explicit engine reference as a
positive control. All four actual assemblies compile independently with zero
diagnostics. This checks the compiler boundary, not a licensed Unity import.

## Executed checks and historical count reconciliation

| Check | Outcome | Versioned evidence |
| --- | --- | --- |
| Pure domain NUnit | **34/34 Passed**, 168 NUnit assertions; zero failed, skipped or inconclusive | [domain-results.xml](evidence/M1/domain-results.xml) |
| Targeted mutation checks | **13/13 killed** by failing tests; each mutant compiled in isolation | [mutation-results.json](evidence/M1/mutation-results.json) |
| Task 1.3 configuration reproduction | **2,301 assertions** on isolated archive of 7b205c6 | [configuration-task1.3.json](evidence/M1/configuration-task1.3.json) |
| Current configuration | **2,333 assertions**, including 1,024 matrix entries and 1,024 symmetry checks; 32 YAML files and one JSON parsed | [configuration-current.json](evidence/M1/configuration-current.json) |
| Task 1.4 layout reproduction | **584 assertions** on isolated archive of 1d39a7b; 11 YAML files parsed | [layout-task1.4.json](evidence/M1/layout-task1.4.json) |
| Current layout / scene wiring | **631 assertions**, including coordinator references/readiness configuration; 11 YAML files parsed | [layout-current.json](evidence/M1/layout-current.json) |
| Input asset / adapter scope | **39 saved-asset/scope assertions** | [input-current.json](evidence/M1/input-current.json) |
| Actual assembly compilation | Four assemblies; source counts Domain 4, Runtime 4, EditMode 2, PlayMode 5; zero diagnostics | [compilation-current.json](evidence/M1/compilation-current.json) |
| Scaffold / Domain engine boundary | Four asmdefs, 72 metadata pairs, 24 requirement mappings; negative and positive engine probes passed | [boundary-and-scaffold.json](evidence/M1/boundary-and-scaffold.json) |

The requested 2,301 and 584 are historical snapshots, reproduced during this
audit using their original committed verifiers. Current metadata/wiring checks
increase those totals to 2,333 and 631. The 631 scene-wiring total is the current
layout verifier's combined geometry/reference total, not 631 additional tests.
These counts overlap and must not be added into an independent-test total.

Domain coverage includes Initializing, failed/default readiness, retry, successful
start, event ordering/counts, reentrancy, listener exceptions, isolated instances,
terminal Victory and readiness factories. Mutation evidence lists each mutation,
its failing cases and detected invariant. Killing these 13 selected mutants is
not a claim of exhaustive mutation coverage.

## Native execution and deferred acceptance

| Native fixture | Declared cases | Status |
| --- | --- | --- |
| ArenaConfigurationTests | 25 | Compiled; native execution blocked |
| ArenaLayoutTests | 23 | Compiled; native execution blocked |
| PlayerInputHandlerTests | 34 | Compiled; native execution blocked |
| MatchInitializationTests | 34 | Compiled; native execution blocked |
| Total | 116 | No native passing cases claimed |

Latest observed batchmode attempt was Task 1.6, at
2026-11-01T19:52:22Z (2026-11-02 local). It reports:

```text
No valid Unity Editor license found. Please activate your license.
Application will exit with return code 198
```

The [retained excerpt](evidence/M1/native-license-excerpt.txt) records its source
log path, timestamp and raw SHA256. Task 1.7 did not repeat that native launch;
the blocker is carried forward explicitly. Original logs stay ignored locally;
the versioned excerpt omits unrelated machine/session identifiers.

| Gate | Evidence now | Required completion |
| --- | --- | --- |
| TC-001 | Planned / Partially Verified (M1 Structural Baseline) | Real Health/stats/pickup/HUD and actual Playing integration, M3-M6 |
| TC-004 | Planned / Partially Verified (M1 Structural Baseline) | Native walking/jumping against walls/corners, M2 |
| TC-028 | Planned / Partially Verified (M1 Structural Baseline) | Full match and terminal victory playthrough, M6/M7 |
| TC-029 | Planned / Partially Verified (M1 Structural Baseline) | Execute missing-reference/native initialization tests and integrate real readiness, M3-M6 |
| Original M1 Editor/build gate | Open, TD-002/TD-003 | Licensed clean checkout, Package Manager import, Unity compilation, native suites and Windows smoke build |
| NFR-004 / NFR-007 | Verified only for M1 architecture/infrastructure | Complete gameplay design and invariant suites; native execution/full review |
| Remaining NFRs | Planned | Measured performance, latency, soak, usability and Windows/offline evidence in M7 |

TD-001 tracks the older proposed 6000.3 Editor documentation versus the actual
6000.6.7f1 baseline. TD-002 tracks license activation/native verification; TD-003
tracks dependent import/package/build reproducibility evidence. Every item has
ID, evidence, impact, repayment trigger and owner. Deferred M2-M7 features are
planned scope and are not mislabeled as incurred technical debt.

## Reproduction and retained evidence

Run from the repository root with the installed Editor at the scripts' default
path. Each command exits nonzero on failure and writes generated artifacts only
to ignored directories:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-DomainTests.ps1 -MutationChecks
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Verify-ArenaConfiguration.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Verify-ArenaLayout.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Verify-Input.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Verify-Scaffold.ps1 -CompileProbes
```

For historical counts, extract `git archive 7b205c6` and `git archive 1d39a7b`
into separate ignored temporary directories, then run the respective configuration
and layout verifier from each archive. Do not replace the current working tree.

[verification-summary.json](evidence/M1/verification-summary.json) records tool
versions, source commit IDs, counters, native status and SHA256 source fingerprints.
Text fingerprints normalize CRLF to LF and use UTF-8 without BOM to remain stable
across Git checkout line endings. The tagged commit preserves the verifiers,
sources, metadata and documentation together; the evidence directory retains
text/XML/JSON summaries without binaries or package caches.

After license activation, execute all four native fixtures through Unity's Test
Runner, retain XML/logs against the exact commit and versions, then complete the
clean-import/build gate. Their compilation alone cannot close deferred tests.

## Git baseline and review

The checkpoint commit includes this report, traceability/debt updates, setup
guidance, retained evidence and the four reviewed pending corrections listed
above. It adds no gameplay implementation. The local annotated tag targets that
commit. Review the exact documentation diff with:

```powershell
git diff 'm1-structural-baseline^' m1-structural-baseline -- docs
git status -s
git ls-files -- Library Temp Obj Build Builds Logs UserSettings TestResults
```

The final status and tracked-generated-files checks must both produce no output.
Ignored local caches and diagnostics are excluded from the milestone; they are
not staged or published. No temporary audit files are included in the commit.
