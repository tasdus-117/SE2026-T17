# TV2-005 / FR-203 verification and handoff

24-Nov-2026 (Asia/Bangkok). Generated summary by Codex, not human acceptance.
Baseline: `74a6c56f8e9baeddfa2ef11273b2bf5f21cf9837` (PR #38).
Branch: `feature/TV2-005-authoritative-gun-rules`; worktree: `.tv2-005`.

## Approved rules and rollout

The local source `C:/Long/3D_arena_game/docs/reference/TV2-001/docs/DECISIONS.md`,
section "TV2-001 — kết quả chốt luật 2026-11-08", records GD-01–08 accepted
through the TV2 owner: H=100, D=25, C=.5 s, R=30 m; infinite ammo, no reload.
It supplies no separate TV1/TV3 signatures. GD-09 technical decisions and
actual integration review remain separate. No later approval to change the
acceptance profile was found in the reviewed team records/issues/PR discussions.

[Issue #29](https://github.com/tasdus-117/SE2026-T17/issues/29) retains melee
acceptance. Production `ServerMatchLifecycleController.BeginSession` still
creates H=10/D=1/C=1/R=1.8 unless its existing `Configure` seam supplies an
immutable configuration before session start. Player prefab HP=10 does not
override bound runtime HP. Tests use this seam and the production player prefab;
no new selectable mode or default change. M3 pickup still adds one damage;
one-gun tests use base damage without pickup. No new modifier/ammo/reload policy.

## Authoritative path and prerequisites

| Contract / owner | Runtime symbols and callers | Evidence |
| --- | --- | --- |
| CT-202 ingress / TV1 | `HeldFireInput` → `IShotIntentSink` → `NetworkShotIntentAdapter.CmdSubmitShot` → `ShotRequestGate.Process` | Authenticated owner Command and separate replica fixture; reliable wire, ownership, age, sequence, session/life gates retained |
| Clock/config/life / TV1 | `Configure`, `ServerNow`, `BeginAttack` → `MatchSessionRuntime.TryAttack` → `OnlineCharacterState.TryBeginAttack` | One monotonic server-time sample evaluates eligibility and commits t+C from immutable configuration |
| CT-203 damage / TV1+TV2 | `NetworkCombat.PerformShot` → server raycast/`ShotHitRules.Resolve` → `NetworkHealth.TakeDamageFrom` → runtime `TryDamage` | Nearest hit point from server muzzle, inclusive R, cover wins ties; single damage invocation |
| CT-204 deadline / TV1+TV2 | runtime `NextShotAtSeconds` → `Publish` → `NetworkHealth.PublishRuntime` → `NetworkCombat.PublishRuntimeDeadline` | Exact committed deadline mirrored into existing SyncVar; no second bound gameplay cooldown gate/sample |
| CT-203/204/205 consumers / TV3 | `TargetFeedback`, `NetworkPlayerPresentationAdapter`, `PlayerPresentationBinding`, `NetworkHealthHUD`, lifecycle invalidation | Existing shooter-only feedback, configured held-fire cadence, replica/HUD tests |
| Movement / TV2 | PR #13 motor revocation | Movement-only dependency; shot path independent; no copying/stacking unmerged code |

Bound melee and ranged requests share the runtime deadline. Accepted hits,
misses and blocked shots consume cooldown once. Rejected aim/ingress/cooldown
does not change gameplay deadline, HP or score. Publication/life reset retain
the runtime deadline even with respawn shorter than cooldown. Unbound combat
retains its legacy gate. Client timestamps only affect freshness.

## GitHub baseline audit

Read metadata, discussions, submitted reviews and final PR-head Actions on
23-Nov-2026 UTC. Raw responses: ignored `TestResults/TV2-005/github-baseline.json`.

| PR | State / head | Final PR-head CI run |
| --- | --- | --- |
| #9 | merged / `3da84b9` | No PR-triggered run returned; success not inferred |
| #13 | open draft / `861866f` | 44996303915 success |
| #14 | merged / `24bdf3e` | 44569986165 success |
| #22 | merged / `42badfb` | 44927295017 success |
| #30 | open draft / `4e5e9cc` | 44997984259 success |
| #31 | merged / `c30801d` | 45036160027 success |
| #37 | merged / `c4404c5` | 45098610516 success |
| #38 | merged / `0a7a6c6` | 45144190187 success |

No APPROVED human review submission returned for these eight PRs. #31 has a
TV1 COMMENTED Codex request, not sign-off. Codex completed #31/#37 without
findings. #38's recorded completed review was for `8601420`, not final `0a7a6c6`.
#30's final Codex receipt covers `4e5e9cc` after correcting kill-feed wording.
Issues #24/#27 remain open without comments; #29's only comment reports prior
TV2 delivery. PR #30 owns broader contract reconciliation; updates here are
scoped to FR-203 and do not replace that draft.

## Verification

Verified source commit: `0a646f6be11b2dd57bf1000a0bf41bdb4b2608bf`.
Pinned Unity: 6000.6.8f1 (237b6593792b). Commands/logs/XML/source manifests and
failures remain under ignored `TestResults/TV2-005`. Successful focused/full
native runs used the identical source tree committed above.

| Verification | Discovered / passed / failed / skipped / inconclusive | Exit |
| --- | --- | --- |
| Focused native shot/presentation | 12 / 12 / 0 / 0 / 0 | 0 |
| Native EditMode | 288 / 288 / 0 / 0 / 0 | 0 |
| Native PlayMode (all) | 420 / 418 / 0 / 2 / 0 | 0 |
| Native PlayMode project subset | 416 / 416 / 0 / 0 / 0 | XML policy pass |
| Pure C# Domain runner | 288 / 288 / 0 / 0 / 0 | 0 |
| Strict compilation | Seven project assemblies + Mirror; project warnings are errors | 0 |
| XML validator regression | 55 / 55 | 0 |
| Four-process harness regression | 56 / 56 | 0 |
| Scaffold/GUID, input, saved arena configuration/layout | All pass; 1,530 metadata pairs, 4,631 layout assertions | 0 |

The two vendor skips have reasons and pass the current allowlist/budget; neither
is counted as a project pass. The first manual XML invocation incorrectly used
418 as the project floor (the total includes two vendor passes); it correctly
failed. Rerunning with project floor416/total418 passes without XML/source edits.
The skill's optional Python checker could not run: neither `python` nor `py`
finds an installed interpreter. The repository scaffold/GUID verifier passed.

| FR-203 criterion | Evidence / outcome |
| --- | --- |
| Before/exactly/after .5 s | Independent Domain cases10.499999/10.5/10.500001; controlled monotonic-time runtime integration checks; pass |
| Custom C / invalid config | .75 s runtime publication and ingress freshness tests; zero/negative/NaN/infinite config rejection; pass |
| Client time cannot shorten C | Future timestamp passes ingress freshness but actual owner Command is rejected for cooldown; deadline/HP/score unchanged; pass |
| D=25, once, configured HP | Real owner Command, production prefab, separate replica and HUD: 100→75→50, damage25→50; duplicate rejected; pass |
| Inclusive R=30 m | Domain29.999999/30/30.000001; real collider surfaces29.875/30/30.125 from server muzzle; pass |
| Config consumed, not hardcoded | Integration D=17/C=.75; existing close-range3m and M3 profile fixtures; pass |
| Hit/miss/blocked consume once | Controlled runtime deadlines and cover fixtures; pass |
| Invalid input consumes none | Invalid aim followed immediately by a valid request; replay/stale/wrong-life/phase/foreign-owner existing ingress fixtures; pass |
| Protection / lifecycle / feedback | Respawned protected target loses zero HP, shared melee deadline; existing life/session reset, shooter feedback, held-fire and HUD regressions; pass |
| Ammo/buff | No ammo counter/reload path introduced; infinite/no reload follows GD-02. Existing M3 pickup +1 and offline regressions retained; no one-gun pickup policy added |
| Production rollout | Pending TV1/TV3 decision under #29; defaults remain M3 |

Focused command: `powershell -NoProfile -ExecutionPolicy Bypass -File
TestResults/TV2-005/Run.ps1 -Phase Focused`; full runs use `-Phase EditMode`
and `-Phase PlayMode`. The ignored script records actual Unity arguments,
timestamps, exits and source manifests; native tests omit `-quit`. XML policy:
`tools/Assert-UnityTestResults.ps1 -XmlPath TestResults/TV2-005/PlayMode.xml
-MinPassedCount 418 -MinProjectPassedCount 416` (equivalent288/288 for EditMode).
Repository compile/scaffold/input/config/layout scripts were executed directly.

Initial sandbox native attempt: licensing exit198 before discovery. First
licensed import exposed fixture CS1061 (`NetworkIdentity.AddComponent`), fixed
to `gameObject.AddComponent`. Early strict compiler lacked imported
Mirror.Components; post-import retry compiled all seven project assemblies.
These initial failed attempts are not passing test runs.

Source parity: all173 tracked project C#/asmdef/prefab/scene files match the
successful focused/EditMode/PlayMode source manifests, working tree and source
commit. An initial parity script mishandled a PowerShell JSON array; corrected
the bookkeeping loop without product changes. Four Unity-generated settings
files remain unstaged and excluded; no generated settings/vendor files enter
this PR. `git diff --check` passes. No new asset requires new metadata.

Windows x64 Mono instrumented build succeeded from source commit0a646f6,
Unity6000.6.8f1, zero errors/15 warnings, 113,899,683 bytes. Existing
`tools/Build-FourPlayerHarness.ps1 -Commit 0a646f6be11b2dd57bf1000a0bf41bdb4b2608bf`
exports a separate validation project under ignored TestResults. Binary SHA256:
`8060790b33cf3ca520e289bf961094e2ed5473c7fad5d6148d8489e96055d27b`;
full content/tool hashes are in its `four-player-build.json`. It retains the M3
default, not an unapproved ranged rollout. The existing three-round localhost
KCP invocation uses `tools/Invoke-FourPlayerHarness.ps1 -ExpectedCommit
0a646f6be11b2dd57bf1000a0bf41bdb4b2608bf -Rounds 3`; process-run outcomes are
reported separately in the final PR/local handoff and ignored
`TestResults/FourPlayerHarness/Runs`. This build and native owner/replica tests
do not alone establish a four-process result or project-wide #27 acceptance.

Instructions applied: Unity `using-unity` index, its multiplayer and CLI guides,
local `unity-csharp-scripting` and `references/agent-checks.md`; the pinned
project's batch-mode/test/compiler/metadata workflows. Existing Mirror source
and repository protocol/tests supply authority/wire behavior; no NGO/UGS stack
was added. No applicable AGENTS/AGENT file was present in the original code
checkout or parent directories checked. External local docs have their own
`docs/AGENTS.md`, read and followed for evidence/record preservation. No separate
Mirror/testing/PR-review skill was installed in the inspected local catalog.

## Remaining acceptance

TV1 @xzviett and TV3 @tasdus-117 own integration review and one-gun rollout
confirmation; #29's default profile remains intact. GD-09 topology/budgets,
retained reconnect, #24 official oracle, #26 visual QA and #27 integrated
acceptance remain with their existing owners/tasks. Localhost automation does
not certify LAN, impairment or human visual QA. Draft delivery remains required.
Revert this scoped change to restore prior duplicate deadline calculation;
there is no wire/config migration. No merge or dependency closure performed.
