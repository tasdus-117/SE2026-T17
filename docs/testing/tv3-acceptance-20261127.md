# TV3 acceptance handoff — 27-Nov-2026

**Partial/provisional; #26/#27/#28 remain open.** Agent-assisted work for
TV3 @tasdus-117. No human approval, merged candidate cooldown or gameplay rollout.
Baseline after fetch: main/origin main and existing acceptance branch all
`89f2674fa993ff13e30fcd56cca46cec5333494c`; clean at recovery. PR #13/#30
are merged. #39 stays separate. No applicable repository AGENTS.md was found
in the project search. Sources, commands and limits below belong to this date.

## Recovery → delivery

| Task | Existing local evidence | Completed in this session | Still required |
|---|---|---|---|
| #19 | 8-case JSON/helper/logs | Source/archive parity, all diagnostics/exits checked, fixture restoration verified; [report](tv3-scaffold-acceptance.md) | Delivery review before closure |
| PlayMode | Interrupted log, no XML/exit | Preserved old attempt; one new full run and current XML gate | #42/#43 stability/root-cause work belongs TV1/TV2; not resolved by one pass |
| #26 | build-attempt3, window720 log | Content hash and retrospective source parity; real UI Host/Join/Ready/Start, HP/clock, timeout/result/lobby survey | Complete device/visual matrix on agreed same build as #27 |
| #40 | Merged #13 | World axes/jump/revoke/restore source review, current-main native regression | Real held input and TV1/TV2 delayed-packet decision; human approval separate |
| #20/#24 | Merged contracts/oracle | Camera/aim/muzzle/layers/HUD/copy cross-check | Same-build final visible outcomes and owner confirmations |
| #41 | Current #39 head | Separate candidate source review and focused native tests | Real held-fire/lifecycle windows; reviewer/rollout decisions |
| #28 | Prepared novice sheet | [Published preparation sheet](tv3-novice-playtest.md) | Three actual novices after #26 basic survey is complete |
| #25/#27 | TV1 harness/oracle interfaces | Handoff of provisional source/build/gates and explicit limits | TV1 agrees integrated SHA/build and runs final topology; TV2 rules confirmation |

## Native recovery

Original `Run-Native.ps1`/args/PID/log were read. PID13156 was no longer a Unity
process; no Editor/player was running at recovery. Old log ends during the suite,
without completion, XML or exit receipt. **Cause of original termination remains
unknown**; logs alone cannot determine killed process/crash/interrupted runner.
No invented exit code or pass count. No retry-until-green.

After process inventory and successful exclusive-open check, stale copy-project
`Temp/UnityLockfile` was renamed to `UnityLockfile.preserved-20261127`. Reused
the existing runner in `Native-recovery`, changing only output/project paths:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File TestResults/TV3-20261127/Native-recovery/Run-Native.ps1 -Phase PlayMode
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Assert-UnityTestResults.ps1 -XmlPath TestResults/TV3-20261127/Native-recovery/playmode.xml -MinPassedCount 431 -MinProjectPassedCount 429 -MaxVendorSkippedCount 2
```

Unity6000.6.8f1, 15:57:49–16:01:14 UTC, XML duration205.735s. Runner/Unity exit0;
gate exit0: **433 total, 431 pass (429 project +2 vendor), 2 vendor skip,
0 failed/inconclusive**. Skip reasons remain in XML; skips are not passes.
Main source parity rechecked after tests, allowing only CRLF/LF normalization.
This run does not prove #42 ten-run stability or resolve #43 clock race. No
assertion loosened, fixture/protocol/server rule modified or failure masked.
Full EditMode/strict/compiler/LAN/final #27 are not claimed for this handoff.

Raw ignored [XML](../../TestResults/TV3-20261127/Native-recovery/playmode.xml),
[log](../../TestResults/TV3-20261127/Native-recovery/playmode.log), args/PID/exit
and runner live alongside it. Original attempt remains in the parent directory.

## Build identity and real-window survey / #26

**Provisional build**, not an agreed integrated #27 build. Existing production
player: `TestResults/TV3-20261127/Scaffold/Builds/WindowsStandalone/SmallArena.exe`.
Read build-attempt3.log, builder/verifier, build-report.json and build/editor logs:
reported Succeeded, Windows x64 Mono2x, Unity6000.6.8f1, 0errors/15warnings,
113883520bytes, MainMenu0/Bootstrap1/Arena2. That log is historical output,
not a newly observed builder exit receipt. 15 warnings are vendor/build messages.

All 2917 tracked source/archive files currently match baseline Git objects after
line-ending normalization, including config/prefabs/input and the restored fixture.
However, there was **no pre-build manifest**, so this retrospective audit cannot
prove binary-to-source provenance. No old binary certifies a newer source.
203 player-directory files are SHA256-listed in ignored `identity-audit.json`.
Content identity (SHA256 of sorted JSON rows path/sha256/bytes, Python sort_keys):
`319e30a570e2b45d383834a6f7e0e35cd84f211d4d51591e0e48f0585097639c`.
Default M3 H10/D1/C1/R1.8, no harness instrumentation added. #39 absent.

Launched same directory twice, PIDs10256(host720) and21024(client1080), localhost
KCP/UDP7777. Command for each, replacing WxH/log name:

```text
SmallArena.exe -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -logFile "<evidence>/visual-720-new.log"
SmallArena.exe -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -logFile "<evidence>/visual-1080-new.log"
```

Computer Use screenshots include window borders:720 capture1282×752;
1080 capture1920×1080. Thus requested1080 client area was **not independently
measured** (bordered capture is not proof of a1920×1080 render target). Final
resolution certification must measure actual client area. Artifacts remain local
ignored, hash-listed in `artifact-manifest.json`; they are not in a fresh clone.

| Case / expected | Actual | Raw artifact in TestResults/TV3-20261127 |
|---|---|---|
| MainMenu/Multiplayer/lobby readable; four slots and ownership | Observed at both requested sizes, no obvious clipping/overlap | 720-mainmenu.png,720-multiplayer.png,720-lobby.png,1080-mainmenu.png,1080-lobby.png |
| Host/Join → Ready → Start through normal UI | Observed two actual processes, no debug route | 720-start-transition.png; visual logs |
| HP10/10, Damage1 unarmed, clock runs; controls readable | Observed both; no damage taken during survey | 720-match.png,1080-match.png |
| Timeout all-zero tie → host rank1/client rank2, readable result | Client DEFEAT—Rank#2; rows Player1 rank1,Player2 rank2, K/D/damage0. Host result screenshot missed; NOT VERIFIED at720 | 1080-result.png |
| Automatic lobby return keeps slots, resets Ready | Observed host lobby ROOM2/4, both Not Ready | 720-returned-lobby.png |
| Countdown/FIGHT, HP loss, buff, Tab hold, kill feed, respawn, rematch reset | NOT VERIFIED in real windows | Require fresh exact-build captures/device operation |
| Focus/pause/resume, disable-enable, held movement/jump/fire across death/rematch; UI click cannot attack | NOT VERIFIED | Escape injection attempts had no visible pause; not enough to identify game vs injection cause |

Capture helper sometimes returned an occluding window or user-input/minimized
guard. Only inspected foreground captures above support visual observations;
files named720-pause*/1080-escape-attempt do **not** prove pause. Recovery failed
to provide a stable further input session. Current sky API exposes press_key,
not key-down/hold across transitions; do not replace device QA with native tests.
Only the two survey processes were stopped after matching PID/executable/log
command line; cleanup receipt `owned-player-cleanup.json`. No user process closed.

## Next actions and ownership

TV1 @xzviett: agree source/build for #27 with TV3, retain complete pre-build
source/config and player content manifests; run full gates and three rounds
through existing #25 harness. Supply exact same player directory to #26; resolve
#42/#43 with TV2 before final acceptance. New source/build requires affected UI
checks again. No final same-build agreement was present in the read issue threads.

TV3 + a real keyboard/mouse operator: finish every NOT VERIFIED row at actual
1280×720 and1920×1080 client areas, capture expected/actual per lifecycle phase,
then replay affected cases on the agreed #27 player. Investigate Escape with
hardware first; no production bug is asserted from injection failure alone.

TV1/TV2: answer delayed higher-sequence/current-life receipt semantics in #40;
TV2 confirms oracle/copy. Human reviewers submit their own approvals; maintainers
own #29 rollout. See [source review](tv3-source-review-20261127.md).

#28 is BLOCKED by incomplete basic #26 matrix and absence of three real novices.
Prepared sheet is ready; no observations, fabricated participants or unsupported
copy changes. Schedule P1/P2/P3 only after basic survey, record actual assistance
and outcomes, propose at most two small evidence-backed copy/display fixes.
