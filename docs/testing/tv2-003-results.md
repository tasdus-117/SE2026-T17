# TV2-003 — movement intent revocation

## Integrated candidate — 22-Nov-2026

This section supersedes the 17-Nov dependency/status claims below; earlier evidence
is retained as history. Updated **existing PR #13**, preserving its history by
merging main `995e6a47931637fdcb1cd9a90a47a5c377f8f46a` into `db133b0`.
Worktree: `.tv2-completion-003` (isolated detached checkout of the assigned branch;
published to `feature/TV2-003-server-movement-input`). Original worktrees/settings
are preserved. Only the motor conflict was present; resolved using the M3 motor
with life rejection, configured speed/timeout, current CanAct and ClearRuntimeInput.
No legacy motor replacement or wire change.

ClearIntent now clears direction, freshness timestamp, hasInput and pending jump
on component disable/server stop and observed disabled CharacterController or
control loss at ingress/fixed step. ClearRuntimeInput delegates to it and retains
M3's vertical reset. Temporary revocation retains sequence/jump watermarks;
OnStartServer remains the new-avatar reset. Lifecycle publication already invokes
ClearRuntimeInput on death/revoke and before respawn teleport, including transitions
between observations. No new session store, config provider or clock adapter.

### FR-202 / AC-202 acceptance and evidence

| Criterion | Status / actual evidence | Owner / next action |
| --- | --- | --- |
| Disable/enable before another input/step cannot replay held movement/jump | Verified: NetworkMovementTests.DisableEnable_CannotReactivateHeldDirectionOrJump; synchronous Unity OnDisable, old input still fresh, replay rejected and fresh sequence accepted | TV2 regression; TV3 restore UI #26 |
| Real runtime revoke/restore between ingress/physics | Verified: NetworkMatchLifecycleTests.RuntimeDeathRespawnBetweenObservations_ClearsFreshIntentAndRetainsWatermarks drives NetworkHealth.TakeDamage → production lifecycle Damage/Publish, then production Update at trusted respawn deadline without yielding. Old input is within .25s; life increments, CharacterController disables/enables, tick unchanged until explicit step; freshness/jump cleared and no movement resumes | TV1 review; #27 integrated run |
| Duplicate/out-of-order/old-life and jump watermarks | Verified in new runtime regression (100/99/current sequence with old LifeId rejected; 101/current LifeId accepted without repeating jump7), existing actual Mirror Command ordering/wrap/forged-owner tests and five-death runtime test | TV1 confirm continuity; temporary revocation retains counters, new avatar resets |
| Configured speed/timeout including zero and inclusive boundary | Verified: ConfiguredMovement_UsesZeroSpeedAndInclusiveCustomTimeout, actual M3 binding + zero speed; trusted runtime publication 2m/s/.125s and .124/.125/.126 boundary steps. Existing .249/.25/.251 test retained. Server-owned speed min(5, config); no client delta/speed | TV1 review |
| Real lifecycle gates/protection/ClearRuntimeInput | Verified: countdown freeze/live replication; death/respawn/old-life/deadline/disconnect/rematch suites; new callback regression checks protection allows movement but rejects fire and ClearRuntimeInput clears freshness while retaining counters | TV1/TV3 human confirmation pending |
| Grounded jump and authority/replication | Verified regressions: JumpCounter_ProducesGroundedJump_AndDropsAirborneOrRepeatedEdges; actual Command flood/owner/observer/forged target tests; world-XZ unchanged | TV3 review axes/jump; #26 real windows |
| Retained-session reconnect/rebind | Outside current melee scope under #29. Domain connection methods do not establish token/proof or restore protocol; not implemented or certified here | TV1 + team approve separate scope |
| Four-player three rounds + visual/LAN | Pending #25/#27 and #26 on same integrated SHA. In-process fixtures do not certify LAN/visual QA | TV1 + TV3 |

### Fresh verification

Pinned Unity **6000.6.8f1 (237b6593792b)**. Base + final source patch tested before
merge commit. Local ignored artifacts: `TestResults/Completion/` in this worktree.

| Run | Passed/discovered | Exit | Failed/skipped/inconclusive |
| --- | --- | --- | --- |
| Focused movement + M3 lifecycle | 55/55 (38 + 17) | 0 | 0/0/0 |
| Project PlayMode | 414/414 | 0 | 0/0/0 |
| Project EditMode | 241/241 | 0 | 0/0/0 |
| Independent compiler | Seven project assemblies + Mirror compiled | 0 | Project warnings treated as errors |

XML case/fixture discovery and fresh logs inspected. Project namespace filters
exclude vendor suites. Verify.ps1 records exact arguments, times, head/status and
C#/asmdef source manifests; summary JSON records exit/discovery/pass/fail/skip and
XML hash. Source parity is checked against final commit before publication.
Initial sandbox attempt exited198 without XML/discovery (no accessible license);
licensed retry passed. No security or license settings changed.

Reproduce via pinned Editor `-batchmode -nographics -projectPath <worktree>
-runTests -testPlatform <PlayMode|EditMode> -testFilter <SmallArena.Tests.PlayMode|
SmallArena.Tests.EditMode> -testResults <fresh.xml> -logFile <fresh.log>`; no -quit.
Focused filter joins NetworkMovementTests and NetworkMatchLifecycleTests with `;`.
Compiler: `powershell -NoProfile -ExecutionPolicy Bypass -File
tools/Compile-UnityAssemblies.ps1 -EditorRoot <6000.6.8f1/Editor>`.

KCP/build results, final exact head, CI and Codex review are recorded in the PR and
local handoff after publication. Human reviews remain distinct and pending; no
merge/issue closure. TV1 @xzviett reviews binding/clock/revocation/counters; TV3
@tasdus-117 reviews world axes/grounded jump/input restore/HUD. #26 restore cases
and #27 integrated acceptance must use the eventual same merged SHA.

## Historical candidate — 17-Nov-2026

Candidate base: `9ee7ea715b6fc5d07156411aac543a481585cee8` (origin/main fetched 2026-11-17).
PR #9 merge `d939568aae110668df5f0114541e4498593b6108` is an ancestor.
Status: partial FR-202 implementation; draft pending the integration decisions below.

## Scope and production path

`PlayerInputHandler` reads local-owner axes. `PlayerController` forwards them to
`NetworkMovement.SubmitInput`; networked avatars do not execute client physics.
The unreliable Mirror Command requires authority and carries only axes, uint
sequence and jump counter. `ServerPlayerMovement.ReceiveInput` rejects old serials
and nonfinite vectors; `MovementInputValidator` preserves analog magnitude and
normalizes oversized finite vectors without overflow. Only accepted input refreshes
server-local time. FixedUpdate advances one CharacterController step with trusted
fixed delta, 5 m/s horizontal cap and gravity. Mirror replicates server transforms.

The change clears direction, pending jump and freshness when the server observes
legacy CanAct loss, a disabled CharacterController, component disable or server stop.
Temporary revocation retains input/jump replay watermarks. A fresh sequence is
required after restoration; an already consumed jump counter cannot jump again.
OnStartServer resets counters for a newly started avatar input session. This is
not a retained-session reconnect or respawn policy. A revoke/restore transition
entirely between server ingress/fixed-step observations still needs lifecycle callbacks.

Existing world-XZ axes, fixed camera yaw zero, grounded jump (1.2 m), airborne edge
consumption and inclusive freshness `age <= 0.25 seconds` remain unchanged.
Timeout removes horizontal intent and pending jump while gravity continues.
Offline movement, combat, wire fields, scenes, prefabs and presentation are unchanged.

## FR-202 acceptance and remaining prerequisites

| Prerequisite / criterion | Current evidence and remaining work | Owner |
| --- | --- | --- |
| TV2-002 data/config available | Merged; immutable GameplayConfiguration, OnlineMatch and OnlineCharacterState exist with configuration validation and state tests | Satisfied as data prerequisite |
| Actor inferred from connection; ownership | Mirror requiresAuthority routes each avatar Command; existing ownership, forged-target and replication tests exercise this path | Existing TV1 path |
| Finite/analog/bounded speed; no client speed/delta/pose authority | Server validator and motor, invalid freshness, sequence/wrap, burst and forged-transform tests | Implemented legacy path |
| No stale intent after observed control loss | ClearIntent at ingress/fixed step/disable/stop, retain watermarks; restoration needs new input | Implemented legacy path |
| Authoritative match config consumed by motor | No production GameplayConfiguration instance/provider exists. Motor still uses constants. Supply match-owned config, including zero-speed policy, before configured-speed acceptance can be verified | TV1 + TV2 |
| Current participant/life/connection mapping | No production consumer of OnlineCharacterState or OnlineMatch exists. ServerLobbyController uses LobbyRoom connection IDs and phase; ArenaNetworkManager destroys avatars on disconnect. This does not establish stable participant identity or retained life/session | TV1 |
| Waiting/Finished/Aborted/Dead/Pending/disconnected gates; protection allows movement | Domain CanReceiveControl expresses these rules, but ingress/motor cannot obtain a current snapshot. Supply current-state lookup, clock domain, lifecycle ordering and revocation notifications. Test both ingress and simulation against all states after integration | TV1 + TV2 |
| Session/life counter reset boundaries | Only temporary legacy revocation and new-avatar OnStartServer are supported. Define retained-session reconnect, new-life input/jump continuity, old-connection revocation and replay boundaries before wiring them | TV1 + TV2 |
| Camera and jump contract | Current world-XZ/fixed-yaw camera and grounded jump preserved; no new recorded TV1/TV3 confirmation found. Confirm axes conversion/expected vectors and jump policy | TV3 + TV1 |
| Exact timeout boundary | Existing inclusive 0.25-second boundary retained and tested; formal TV1/TV2 comparator confirmation remains open in local decisions | TV1 + TV2 |
| Separate server and at least two OS clients | In-process Mirror transport/proxy tests do not establish this. No movement-specific multi-process acceptance run performed | TV1 integration harness |

No guessed config/state/session adapter was introduced. Domain-only test success
does not prove movement integration. TV2-004 remains pending; its intended baseline
must first establish identity/life/clock/order/replay and camera interfaces above.

## Fresh verification

Unity `6000.6.8f1 (237b6593792b)`, actual candidate worktree, 2026-11-17:

| Run | Exit | Discovered / passed | Failed / skipped / inconclusive |
| --- | --- | --- | --- |
| Focused NetworkMovementTests final | 0 | 38 / 38 | 0 / 0 / 0 |
| Full project PlayMode namespace, 23 fixtures | 0 | 330 / 330 | 0 / 0 / 0 |
| Full project EditMode namespace, 3 fixtures | 0 | 142 / 142 | 0 / 0 / 0 |

PlayMode includes NetworkMovementTests 38, MultiClientIntegrationTests 7,
NetworkLifecycleTests 8, ArenaFallNetworkTests 2 and all current project arena,
lobby, input, camera, offline and combat regressions. EditMode includes
GameplayConfigurationTests 62, OnlineStateTests 45, MatchStateTests 35.
Package/vendor suites were excluded by the project namespace filter.

The first sandboxed attempt exited 198 before discovery (license unavailable).
The licensed retry discovered 38: 37 passed, 1 failed because the new restoration
fixture started a real jump before asserting nonpositive vertical velocity. The
fixture was corrected to begin grounded without jumping; focused and full suites
then passed on identical final source. Both unsuccessful runs remain in evidence.
No production changes followed the successful runs. Source patch sections match
across focused/full runs; Git blob hashes identify tested source for commit parity.

Raw XML/logs, source manifests and exact commands are local evidence under
`.tv2-003-current/TestResults/TV2-003-20261117/`; excluded from Git. Each run has
`<label>-run.json`, `<label>-tested.patch`, `<label>-summary.json`, `<label>.xml`
when discovered, and `Unity-<label>.log`. Labels are `Movement`, `Movement-retry`,
`Movement-final`, `PlayMode`, `EditMode`. Historical counts are not current results.
Unity-generated settings are preserved locally and excluded from the commit.

Reproduce from the worktree, using a new evidence directory for each run:

```powershell
$editor = 'C:/Program Files/Unity/Hub/Editor/6000.6.8f1/Editor/Unity.exe'
& $editor -batchmode -nographics -projectPath (Get-Location).Path -runTests -testPlatform PlayMode -testFilter SmallArena.Tests.PlayMode.NetworkMovementTests -testResults '<evidence>/Movement.xml' -logFile '<evidence>/Unity-Movement.log'
& $editor -batchmode -nographics -projectPath (Get-Location).Path -runTests -testPlatform PlayMode -testFilter SmallArena.Tests.PlayMode -testResults '<evidence>/PlayMode.xml' -logFile '<evidence>/Unity-PlayMode.log'
& $editor -batchmode -nographics -projectPath (Get-Location).Path -runTests -testPlatform EditMode -testFilter SmallArena.Tests.EditMode -testResults '<evidence>/EditMode.xml' -logFile '<evidence>/Unity-EditMode.log'
```

Separate OS server/two-client movement, retained-session reconnect and performance
were not run. No authoritative config/state runtime integration is claimed.
No new team or user code-review sign-off is inferred; maintainer review is pending.

Recovery patch SHA256:
`05561F74DD02429C94DA8D9F970E72E572B2057F47DCED27C23E4CE25077F529`.
The original dirty worktree is preserved at `.tv2-003-worktree`, detached at
`a702d1719b196cb0cc6f9fca8cc77dbd203edc24`, including generated settings and old evidence.
The candidate is isolated at `.tv2-003-current` on
`feature/TV2-003-server-movement-input`; no published history was rewritten.
