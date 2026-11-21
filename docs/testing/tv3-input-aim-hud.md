# TV3 verification and delivery

## Current verification after resolving main conflicts

Integration baseline: main `7723ea9b9ff125242215a273208bb758bd4fc807`
(merged #21), merged into the TV3 branch. The original delivery results below
are retained as history; the following results verify the resolved source.

| Current run | Discovered / passed / failed / skipped / inconclusive | Exit |
| --- | --- | --- |
| Focused input, TV3, combat/HUD and network match lifecycle | 83 / 83 / 0 / 0 / 0 | 0 |
| Project PlayMode | 386 / 386 / 0 / 0 / 0 | 0 |
| Project EditMode | 236 / 236 / 0 / 0 / 0 | 0 |

Unity remains 6000.6.8f1. Commands use the same runner arguments as the original
delivery, with ignored `TestResults/Run-Tv3Main.ps1` writing to
`TestResults/TV3Main/`. Full filters: `SmallArena.Tests.PlayMode` and
`SmallArena.Tests.EditMode`. XML/logs, focused.xml/log, lifecycle-red.xml/log
and tested-source.json remain local/ignored. Source manifest covers 124 C#/asmdef
files, unchanged between focused/full runs and final review.

New regression checks:
- A bound lifecycle that cannot act blocks CanCollectGameplayInput.
- Lifecycle notification clears pending buttons/movement and invalidates held-fire.
- A real owner Command with invalid aim does not consume the runtime attack
  cooldown; an immediate valid Command still damages the target.

Red/green evidence: the two lifecycle assertions failed before the gate/notification
bridge and passed after it. An initial fixture setup incorrectly added NetworkHealth
after NetworkIdentity.Awake, causing a NullReferenceException; corrected the test
to construct components while inactive so Mirror initializes them together. The
initial focused run was 81/82, followed by corrected assertion failures 0/2 and
the final focused83/83. No original assertion was weakened or test skipped.

PlayMode includes NetworkMatchLifecycle15, NetworkMatchLobbyIntegration2,
NetworkScoreboardAndRematch5, NetworkedCombatAndHUD14, PlayerInputHandler39,
and the retained TV3 fixtures. This verifies in-process serialized Mirror gameplay
plus unit/fake boundary tests. Standalone build/smoke and visual QA were not rerun
for this conflict-resolution update; the older standalone results below belong
to the earlier baseline. No claim of new raycast/reconnect end-to-end validation.

The PR targets main, so the normal synchronize push is eligible for CI. Exact new
head/merge SHA, run link and current status are recorded in PR #22 metadata after
push. Old #14 CI does not verify this merge.

## Original delivery evidence (historical)

21 November 2026. Draft integration; automated inspection is not human review.
Contract, baseline and provider ownership: [TV3 integration](../architecture/tv3-input-aim-hud.md).
Base #14: `24bdf3eddc2975a7a8646f343ca62af0311badec`; main inspected at
`9ee7ea715b6fc5d07156411aac543a481585cee8`. No #13/#21 source imported.
TV3 source commit: `c8aa12f00041909ee10352e1fddb01ef9cde602a` (aim/cadence first slice `6331cfd`).

## Actual environment and commands

Managed isolated worktree:
`C:/Users/tasdus_117/.codex/worktrees/53d6/SE2026-T17`.
Unity **6000.6.8f1 (237b6593792b)** from ProjectVersion. Mirror/package/assembly versions unchanged.
All Editor/player processes launched hidden. No test result is inferred from CI.

Run from that worktree:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Run-Tv31Unity.ps1 -Phase PlayMode -TestFilter SmallArena.Tests.PlayMode
powershell -ExecutionPolicy Bypass -File tools/Run-Tv31Unity.ps1 -Phase EditMode -TestFilter SmallArena.Tests.EditMode
powershell -ExecutionPolicy Bypass -File tools/Run-Tv31Unity.ps1 -Phase Build
powershell -ExecutionPolicy Bypass -File tools/Run-Tv31StandaloneSmoke.ps1
```

Runner uses `-batchmode -nographics -projectPath <worktree> -runTests
-testPlatform <phase> -testFilter <filter> -testResults <xml> -logFile <log>`.
Build installs existing temporary harness sources and removes them afterward.
They and generated settings are excluded from delivery.

## Observed results

| Run | Discovered / passed / failed / skipped / inconclusive | Outcome |
| --- | --- | --- |
| Baseline input/camera/combat-HUD | 53 / 53 / 0 / 0 / 0 | exit0 |
| Final focused TV3 + input | 52 / 52 / 0 / 0 / 0 | exit0 |
| Project PlayMode, final source | 354 / 354 / 0 / 0 / 0 | exit0 |
| Project EditMode | 142 / 142 / 0 / 0 / 0 | exit0 |
| Standalone build | Not an NUnit suite | Success, exit0 |
| KCP host + client, separate OS processes | Both success, error arrays empty | smoke exit0 |
| Deliberately inverted cadence release gate | 8 / 6 / 2 / 0 / 0 | Mutation detected; restored before final verification |

Project filters exclude vendor/package fixtures, so skipped0 is a project claim.
PlayMode includes movement23, camera5, player input37, lifecycle8, combat/HUD14,
in-game menu/teardown10, multi-client7, match initialization35, victory8,
plus arena/lobby/loot/offline regressions. New standalone TV3 fixtures are
intent8, health binding3 and presentation4. Input37 includes the real Input System
held-fire collector exercised with a test-only sink.

Tests cover world axes/analog preservation and grounded/airborne jump via retained
movement/controller suites; aim (3,4)→(.6,.8), zero/parallel/behind/NaN/Infinity;
held fire initial release, cadence boundary, lag without backlog, invalid cadence,
clock rollback, enable/disable, menu/focus/timeScale pause, observed authority loss,
new sink, old sink invalidation, fresh mouse aim and destroyed muzzle.
Presentation covers initial state, duplicate subscriptions/revisions, source and
life changes, clearing before callbacks, captured late callbacks and disposal.
The HP view test uses the real Slider/TMP view with an IHealthSource fake; no fake
is installed in production.

## Failed runs retained and diagnosis

- Initial aim/cadence contract stubs: intent8, passed5/failed3; expected missing
  normalization and release/cadence behavior. Green8 after implementation.
- Input/health lifecycle: discovered39, passed34/failed5; missing focus cleanup,
  menu collection gate, captured old health callbacks and unbind text cleanup.
  Green63 across input/health/combat/menu after implementation.
- Initial presentation binding: failed3/3; initial snapshot/lifecycle behavior
  absent. Initial held-fire adapter: failed1/1 (no requests), then green.
- Clear-order regression: presentation4, passed3/failed1; State still exposed old
  life during Changed(null). Fixed clearing order and guarded reentrant rebinding.
- First broad run: discovered353, passed352/failed1,
  `NetworkLifecycleTests.ArenaMarkers_RegisterFourStarts_WhileOfflinePlayerRemainsIdentityFree`.
  InputTestFixture restores Input System in TearDown before additive scene cleanup;
  action-map.enabled is not initialization readiness. Split CanCollectGameplayInput
  from the established IsInputReady contract. Original lifecycle assertions were
  not changed. Focus/menu tests assert the new collection gate and retain initialized
  readiness; input/buttons remain cleared. Focused lifecycle/input/startup then80/80.
- Review mutation intentionally flips only the release condition; two intent tests
  detect it. Original source was restored and compared with the tested manifest.

## Evidence, parity and limits

Ignored evidence in `TestResults/TV31/`: baseline.xml/log, tv3-focused.xml/log,
intent-red.xml, lifecycle-red.xml, presentation-red.xml, adapter-red.xml,
clear-order-red.xml, full-first.xml/log, cadence-mutation.xml/log,
final-playmode.xml/log, editmode.xml/log, build.log, tested-source.json.
Source manifest covers **107** project C#/asmdef files. Check each SHA256 against
the current source and Git blobs before delivery; the deliberately mutated file was restored before the final project run;
all 107 hashes match and the code was committed without further source changes.

Standalone evidence:
`StandaloneSmoke/8c34cedcee3040fe90b092b780dfba71/`.
Actual KCP localhost port61753, MainMenu Host/Join, Ready/Start, matching obstacle
descriptors/colliders, server-gravity client fall, authoritative HP0 and replicated
Defeat/Victory result headers. This is the existing arena/melee presentation path.
It does not exercise a raycast sink, dedicated server + two clients, retained-session
reconnect, latency/loss, new scoreboard/timer or rejection transport.

No camera/layout/prefab design changed. Runs are headless: **visual appearance by eye
has not been verified**. HP text/slider/result headers are checked as runtime values;
this is not visual QA. Human review and graphical validation remain required.

CI workflow `.github/workflows/unity-ci.yml` accepts pull_request base main only.
The stacked base is the #14 feature branch, so CI for the TV3 head is not triggered;
#14's green CI is not evidence for TV3. Do not change the pipeline merely to obtain
a green badge. Re-run CI on the actual future head/merge commit when retargeted.

Automated review checked correctness/lifecycle, simplicity, seven-assembly dependency
direction, client/server trust boundary, allocation/cadence and test sensitivity.
No network message, gameplay authority, dependencies or server store added.
Draft acceptance gaps remain the provider table in the integration document;
no human signature or ready-to-merge claim is made.
