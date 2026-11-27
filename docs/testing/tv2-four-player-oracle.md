# TV2 four-player rules oracle / #24 — 25-Nov-2026

Prepared for TV1 @xzviett and TV3 @tasdus-117 by TV2 Long/23001901.
Candidate source baseline: `74a6c56f8e9baeddfa2ef11273b2bf5f21cf9837`.
**Delivered oracle for review; no agreed final integrated build or human sign-off.**
Use M3 H10/D1/C1/R1.8, pickup D2, 5kills or180s, unlimited lives, countdown3,
respawn3/protection1, safe distance1.5, loot30, postmatch10. Approved08-Nov gun
profile H100/D25/C.5/R30 is a separate Configure-seam test profile; do not use
its values as the localhost M3 oracle. No retained reconnect in this matrix.

## Independent expected values

Expected numbers below are literal arithmetic and sorted explicit rows, never
computed by MatchSessionRuntime/ComputeFinalStandings/production hit functions.
Source locators establish observed behavior, not the expected-value calculator:
[lifecycle/config](../../Assets/_Project/Scripts/Server/Sessions/ServerMatchLifecycleController.cs),
[session](../../Assets/_Project/Scripts/Shared/Domain/MatchSessionRuntime.cs),
[health](../../Assets/_Project/Scripts/Networking/NetworkHealth.cs),
[loot](../../Assets/_Project/Scripts/Server/Combat/ServerLootRespawnController.cs),
[replicas/feed](../../Assets/_Project/Scripts/Networking/NetworkMatchController.cs),
[lobby](../../Assets/_Project/Scripts/Server/Sessions/ServerLobbyController.cs).

| ID / precondition and action | Expected server | Expected each client / measurable assertion |
|---|---|---|
| O01 Waiting, sealed4, one not spawned | No gameplay; wait within10s grace, no clock consumed | Four slots; loading/Waiting; no movement/attack/pickup effect |
| O02 all4 spawned; wait3s countdown | Countdown then InProgress; no input mutation before FIGHT; match clock starts180 at actual transition | All peers agree phase; countdown positive then FIGHT; no early HP/score changes |
| O03 one missing at10s grace; fewer than2 eligible before FIGHT | Missing participant disconnected; abort without winner if fewer than2 | No winner/result credit; Lobby recovery. With >=2 spawned, continue countdown |
| O04 InProgress Alive, held move(1,1) for1s obstacle-free | Horizontal displacement <=5m; world XZ; no client speed/delta truth; age exactly.25 fresh, later stale | All poses converge; no continuing held direction/jump after timeout. Fresh current-life input works |
| O05 same sequence, older sequence, invalid float, wrong LifeId; then fresh | Reject without freshness extension/watermark rollback; fresh newer/current-life accepted | No old-life movement/attack; no stuck control after valid input |
| O06 victim HP10, attacker D1; 3 accepted attacks >=1s apart outside protection | HP9,8,7; attacker DamageDealt1,2,3, kills0; victim deaths0 | HP/damage bars and Tab agree with those exact integers |
| O07 victim HP3, attacker D2; two accepted attacks | HP1 then0; actual damage credited2 then1 (total3); kill+1/death+1 once; duplicate lethal callback no extra credit | Single death per life, one feed entry; never HP-1 or DamageDealt4 |
| O08 reject cooldown/invalid aim/wrong owner/life/phase/protection | No HP, score or gameplay deadline mutation; accepted miss/cover consumes C once | Rejection shown only shooter as applicable; no false hit or health prediction. Next valid permitted request still works |
| O09 unprotected HP7 falls belowY=-5 after opponent damaged earlier | HP0, death+1, no last-damager kill/damage credit for environmental remainder | Environment feed (empty attacker), one death, no opponent kill increment |
| O10 death at t0, safe spawn available | No respawn beforet0+3; first eligible update at/after creates LifeId+1, HP10, baseD1, retained K/D/actual damage/cooldown; clear movement/jump/vertical; teleport before life publication | All peers see one new life and grounded pose, no old-life hit/move; HUD buff clears, respawn countdown disappears |
| O11 all markers unsafe at deadline | Remain Respawning, retry no earlier than.5s; never force unsafe spawn; match-end cancels | Pending/respawn view; no fake full-HP actor or early movement. Record geometry/pose if delayed |
| O12 fresh respawn at ts; damage/fire ts+.999 then ts+1 | Protection blocks fire/damage beforets+1; boundary permits if other gates/C met; movement allowed during protection | Fresh movement works; held input from prior life does not replay; no HP loss in protection |
| O13 pickup E, carrier dies/disconnects at tloss | One map item consumed, D2; loss resets carrier buff. No replacement beforetloss+30; safe candidate first eligible update >=30, retry if placement blocked; at most one item | Item disappearance/reappearance converges; no auto pickup or duplicate buff; all clients D/HP agree |
| O14 A kills5 before180s | Ended once, A winner rank1, gameplay frozen; no late damage/respawn | Clock/result stable; VICTORY only A; Tab retains final exact rows |
| O15 timeout at180s; rows A(slot0)K2/D1/dmg21; B(slot1)K2/D1/dmg20; C(slot2)K2/D2/dmg30; D(slot3)K1/D0/dmg40 | Rank A,B,C,D. Exact all0 tie ranks slot0,1,2,3, slot0 winner in **M3**, not Draw | Same ranks/winner on4; do not apply original rules-v0.1 Draw policy to M3. Deadline processed before attack/respawn at/after180 |
| O16 Ended wait10s or Host Return | Lobby, connections/slots retained, Ready=false, old avatars/MatchState/feed destroyed | No stale result/feed; ready again; next session new identity, HP10/D1/K0/D0/damage0/feed0 |
| O17 repeat O02/O06/O10/O13/O14/O16 for3rounds | Independent sessions, no accumulated score/item leases/timers | Four processes remain; all3 final standings agree; no stale Ready/input/life context |
| O18 client3 Leave, then Host Leave in partial fourth round | No retained resume; removed client identity; remaining clocks advance. Host teardown stops network | Client MainMenu; then all MainMenu, no orphan identities/room members, no process/callback leak |
| O19 disable/re-enable, pause/resume, focus loss, death/rematch while holding WASD/Space/LMB | #13 final integrated motor clears intent/freshness/jump/vertical but retains sequence/jump watermark until new avatar server start | At1280x720 and1920x1080: no replay until fresh permitted input; UI clicks do not attack; Tab/cursor/resume correct |

## Tolerances and observation rules

HP, K/D, actual damage, life IDs, session, phase, item count, Ready, winner/rank
must agree exactly after convergence, never average. Use server monotonic time for
cooldown/protection/respawn/loot/deadline boundaries; no early mutation allowed.
Floating clock boundary unit tests use explicit epsilon1e-6s. Localhost harness
allows clock spread <=.75s and checkpoint convergence <=90s (watchdog, **not** a
latency target); snapshots publish about.1s at30fps. Placement/update/network
delays may make respawn/loot late, never early; record actual delta and reason.
Pose convergence tolerance .15m is provisional for stopped actors after2s;
TV1 must approve against send/interpolation settings. A surviving upward impulse
or sustained old horizontal intent is failure regardless of visual tolerance.
LAN/latency-loss thresholds require measured network profile and team agreement.

## Runnable matrix and coverage limits

Run from a checkout containing current #38 tools, with the pinned installed editor:

```powershell
$sha = '<agreed full integrated commit SHA>'
powershell -ExecutionPolicy Bypass -File tools/Build-FourPlayerHarness.ps1 -Commit $sha
powershell -ExecutionPolicy Bypass -File tools/Invoke-FourPlayerHarness.ps1 -ExpectedCommit $sha -Rounds 3
```

Build exports the exact commit into ignored validation project; instrumentation
and player content hashes recorded separately. One host and3 separate SmallArena
processes, localhost KCP; no second production collector or gameplay changes.
Existing runner covers spawn/countdown, movement convergence, attacks,5kills,
death/respawn, loot30s, standings, Lobby resets,3rounds/client+host leave/cleanup.
It drives legacy melee Commands via instrumentation; **does not certify mouse
held-fire/normal device input, real-window HUD, all rejection boundaries,
environmental death, full180s timeout/tie or unsafe-spawn branch**. Use focused
native tests for deterministic boundaries; TV1 manually extends runs/records
O03/O09/O11/O15 as required rather than claiming runner coverage it lacks.

TV1 #27 records exact integrated SHA, config dump, full XML gate, build+content+
tool hashes, four command lines, logs, snapshots, monotonic timings and teardown
(all exits, zero owned survivors, UDP reusable). TV3 #26 uses the **same player
content** with real windows at both resolutions for O01–O19 visible assertions,
especially O19 after #13; capture expected/actual + image/video/time/PID.
Review packet: TV1 authority/life/time/order/watermarks and oracle measurability;
TV3 axes/jump/aim/HUD/copy/focus and oracle visibility. Actual approvals pending.

Localhost/headless results, LAN across real machines, latency/loss measured runs,
visual/input QA and novice playtest #28 are separate reports. No issue closure
from a provisional localhost pass. #20 final publication follows same-build
#26/#27 evidence. Retained reconnect/new FR-204 implementation are later work.
