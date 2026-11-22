# TV2-004 verification and delivery

## Reconciliation candidate — 22-Nov-2026

Documentation-only follow-up to merged [#14](https://github.com/tasdus-117/SE2026-T17/pull/14),
owned by [#20](https://github.com/tasdus-117/SE2026-T17/issues/20), coordinated under
[#29](https://github.com/tasdus-117/SE2026-T17/issues/29). Source/runtime reference
is main `995e6a47931637fdcb1cd9a90a47a5c377f8f46a`. Branch:
`feature/TV2-004-runtime-contract-reconciliation`, worktree `.tv2-completion-004`.
Exact final documentation SHA is the PR head; no code/config/prefab changes.

The new [CT/FR matrix](../protocol/tv2-004-contracts.md#current-reconciliation--22-nov-2026)
supersedes the old missing-M3-provider claims below. Inspected production ingress,
M3 binding/config/clock/state/lifecycle, melee aim-before-cooldown ordering, Domain
HP/scoring/protection, replica/HUD consumers and prefab wiring. M3 has production
OnlineMatch/OnlineCharacterState consumers. M3 added LifeId to all three gameplay
Commands; the old claim of compatibility with pre-M3 wire is historical.

No new combat code fix is needed: existing real Command aim rejection and M3
runtime cooldown tests cover the path. Remaining motor revocation work stays in
PR #13. Its new focused verification is **55/55**, exit0, no fail/skip/inconclusive
(38 movement + 17 lifecycle), on `995e6a4` integrated with PR13 source changes,
not this docs-only branch. Do not treat that result as a clean-main/full-suite or
four-player certificate. Final PR13 verification is in its acceptance report.

Historical evidence remains attributed: #14 head `24bdf3e`, focused34, PlayMode336,
EditMode142 (19-Nov); M3 report baselines and TV3 report counts remain dated in
[M3.1](m3.1-match-lifecycle.md), [M3.2](m3.2-scoreboard-and-win-loop.md),
[TV3](tv3-input-aim-hud.md), [audit](audit-f1-f2-f3.md). Counts are not added or
relabelled as tests at this candidate. Documentation validation checks relative
links, source/test symbols, CT-201–205/FR-201/202/204/208 mapping, source equality
to runtime SHA and diff whitespace. Raw check output is local TestResults/Completion.

### Acceptance disposition

| Criterion | Disposition and next action | Owner |
| --- | --- | --- |
| Live movement/attack/result/state/lifecycle matrix with source and tests | Verified by source inspection; exact-head human confirmation pending | TV2; TV1/TV3 review |
| Ownership/match/life/config/clock/order/revocation/counter policy | Live M3 mapped; PR13 integration and exact-revision review pending | TV1 @xzviett |
| Camera/world axes, grounded jump, invalidation and live HUD | Live source/tests mapped; review and real-window evidence pending | TV3 @tasdus-117, #26 |
| Authoritative melee aim/cooldown/HP/score and replica/HUD | Existing production path and focused regressions identified, no wire/gameplay change | TV1/TV2 |
| Original ranged muzzle/layers/held-fire/snapshot/rejection decisions | Out of current melee scope; interfaces alone do not close original acceptance | Team + TV1/TV2/TV3 |
| Retained-session reconnect | Out of current scope; needs approved proof/rebind/baseline protocol | TV1 + team |
| Four-player three-round KCP integration and visual QA | Pending #25/#27 and #26 on the same integrated SHA | TV1 + TV3 |
| Original TV2-004 acceptance/signatures | **Open**; no revised criteria or exact-head human approval inferred | TV1/TV2/TV3 |

Review requests must name final head: TV1 reviews authority/life/time/order/revoke/
counters; TV3 reviews axes/jump/invalidation/HUD. No actual responses received as
of preparation. #21's technical notes contain placeholder commit hashes; they
cannot certify this revision. CI/Codex review are separate from human sign-off.

Post-publication checks: CI run44994703276 on initial docs head19d179a passed both
Unity and Git diff jobs. Codex reviewed that head and correctly identified that
kill feed uses retained SyncList state, not an elimination RPC; CT-203 was corrected
after reading NetworkMatchController.PublishElimination/KillFeedHudView.Update.
Subsequent final-head CI/review is recorded in the PR, not inferred from this run.

Fresh saved-config verifier attempt with pinned EditorRoot exited1 at collision
pair7/7: tools/ArenaConfigurationVerifier.cs still omits Player from Player's
allowed partners. Actual DynamicsManager and ArenaConfigurationTests require the
enabled pair. This baseline tool mismatch is recorded, not counted as a passing
check, and no gameplay/config/tool code is changed by docs-only #20. Tool-owner
follow-up under #29 is needed; artifacts remain TestResults/ArenaConfiguration.

## Historical delivery record — 19/20-Nov-2026

19-Nov-2026. Generated summary by Codex, not a raw transcript or human approval.
Status: independently implemented/verified; **DRAFT, full integration and actual
TV1/TV2/TV3 reviews pending**. See the [contract and acceptance matrix](../protocol/tv2-004-contracts.md).

## Candidate and scope

Worktree: `C:/Long/3D_arena_game/SE2026-T17-TV2-004`.
Branch: `feature/TV2-004-input-shot-presentation-contracts`, PR base main.
Exact execution base: `9ee7ea715b6fc5d07156411aac543a481585cee8` (fresh origin/main).
PR9 merged at `d939568aae110668df5f0114541e4498593b6108`, ancestor of base.
PR13 remains open/draft at `db133b0e8b99e3dc2ba065b6964ac818d12c1e80`;
no stack, merge or modification of its sources/ownership.

Added engine-independent `CombatAim` horizontal validation for the logical shot
contract and a compatibility validator used by the real legacy melee entry.
Existing Vector3 Command identity, serialization, defaults, finite/unit/+Z
tolerances, cooldown, hit geometry and server HP mutation are preserved.
The logical horizontal helper has no runtime raycast/camera consumer yet.
New tests cover huge/tiny/invalid horizontal aim and legacy tolerances, plus real
rejected owner Commands followed by a valid hit without a cooldown wait, observing
server HP, native replica and overhead/local HUD. Existing forged ownership,
proxy mutation, movement, lifecycle and UI regressions remain relevant.

The protocol packet preserves CT meanings, includes one concrete contextual
example per CT and maps FR-201/202/204/208 plus every TV2-004 acceptance criterion.
All added Unity assets have .meta files. No wire changes, assembly changes,
settings, scenes, credentials, logs, binaries or personal context are committed.

## Fresh Unity results

Pinned/observed Unity: **6000.6.8f1 (237b6593792b)**. Actual worktree, hidden Editor.
Tests executed on base plus final pre-commit source changes. The 98-file source
manifest includes all project C# and asmdefs plus new files. All successful runs
and final working source were compared; committed-source-parity.json later
compares every tested Git blob with the commit. No code changes after tests.

| Suite | Passed / discovered | Exit | Failed / skipped / inconclusive |
| --- | --- | --- | --- |
| Focused | 34 / 34 | 0 | 0 / 0 / 0 |
| PlayMode | 336 / 336 | 0 | 0 / 0 / 0 |
| EditMode | 142 / 142 | 0 | 0 / 0 / 0 |

Fresh XML fixtures were inspected, including CombatAimTests20 and the new
NetworkedCombatAndHUDTests regression. EditMode includes config/state/offline
fixtures; PlayMode project namespace excludes vendor/package fixtures.
Focused UTC: 2026-11-19T15:51:17.2923568Z to 2026-11-19T15:54:39.2416681Z.
PlayMode UTC: 2026-11-19T15:58:45.4293861Z to 2026-11-19T16:00:49.2292368Z.
EditMode UTC: 2026-11-19T16:00:53.8491005Z to 2026-11-19T16:01:07.7332387Z.

Command pattern (exact arguments/timestamps/exit codes in each run JSON):

```powershell
$editor = 'C:/Program Files/Unity/Hub/Editor/6000.6.8f1/Editor/Unity.exe'
# Start-Process -WindowStyle Hidden; wait for exit and inspect fresh XML.
& $editor -batchmode -nographics -projectPath <worktree> -runTests -testPlatform <PlayMode|EditMode> -testFilter <filter> -testResults <fresh.xml> -logFile <fresh.log>
```

Focused filter: `SmallArena.Tests.PlayMode.CombatAimTests;SmallArena.Tests.PlayMode.NetworkedCombatAndHUDTests`.
Full filters: `SmallArena.Tests.PlayMode`, `SmallArena.Tests.EditMode`.
Runner: `TestResults/TV2-004-20261119/Verify.ps1` (local ignored evidence).
XML guards require exit0, nonzero discovery, no failed/skipped/inconclusive;
fixture counts are preserved in summaries. Git working/staged/complete PR
`diff --check` and local Markdown link/CT/FR mapping/metadata checks passed.

Initial import/compilation is part of the focused run; no previous task's counts
are treated as candidate verification. PowerShell's first focused-run metadata
expanded a decorated version string; normalized metadata preserves actual values
and raw metadata is retained locally. No product source or test behavior changed
to address this evidence-format issue.

## Evidence and limits

Raw XML/logs, commands, runner, summaries, source manifests and parity:
`TestResults/TV2-004-20261119/` in the worktree, mirrored into
`C:/Long/3D_arena_game/docs/AI_EVIDENCE/TV2/tests/TV2-004-20261119/`.
Handoff: `AI_EVIDENCE/TV2/reports/TV2-004-handoff.md` in those local docs.
Unity-generated settings remain locally preserved and excluded from this PR.

These tests establish current engine-independent math/Domain rules and an
in-process serialized Mirror Command/replica/HUD path. They do not establish
dedicated server+two separate OS clients, KCP impairment, retained-session
reconnect, framing rejection of surplus bytes, raycast geometry, current-life
wire rejection, revisioned snapshot reconciliation, performance or human review.
Those are NOT RUN or blocked by the missing interfaces below. No transport
guarantee changed, so this validation refactor requires no new transport claim.

## Remaining acceptance / owners

- TV1: provide authoritative connection→session/participant/match/life/generation
  binding, current config/state lookup, common clock, lifecycle-before-ingress
  ordering, notification even for revoke/restore between observations, replay
  reset boundaries and reliable reconnect baseline barrier. Existing connId and
  destroy-on-disconnect manager cannot supply them. TECH-001 dedicated toolchain,
  transport compatibility and budgets remain separate decisions.
- TV2 + TV1: integrate the provider in ingress/simulation, preserve PR13's
  watermarks/clearing after its review, confirm exact timeout comparator. TV2
  assigned tasks005/006/007/008/009 implement cooldown/raycast/effect/score/spawn
  execution; this PR does not implement them under contract scope.
- TV3 + TV1/TV2: approve camera-to-world, mouse-plane/muzzle/layers/inside-collider
  policy, held-LMB fresh intent collection (legacy melee is press-edge), grounded
  jump, authoritative state/reject/lifecycle payload consumers and reconciliation.
- TV1/TV2/TV3: review this concrete contract revision. No actual review, reviewer
  signature or approval was received during this work. Owner task authorization
  and the 08-Nov gameplay decision do not constitute these reviews.

PR must remain draft and TV2-004 must not be marked DONE until those affected
criteria, required checks and actual three-member review are evidenced. CI and
final PR metadata are recorded in the local handoff/final GitHub audit after push;
no maintainer merge is performed.
