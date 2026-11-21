# TV2-004 verification and delivery

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
