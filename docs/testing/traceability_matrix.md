# Requirements-to-test traceability

Source: `srs.md` v0.2, sections 4.3, 4.4 and Part 5. Current checkpoint:
**M1 Structural Baseline**, local annotated tag `m1-structural-baseline`.
See [M1-verification.md](M1-verification.md) and its versioned evidence bundle for
source commits, tool versions, current counts and the license blocker.

"Implemented" below describes the named M1 code scope; it does not assert native
execution. "Verified (M1)" is limited to architecture/test infrastructure. No full
SRS gameplay TC is Passed. Proposed SRS parameters/NFRs retain their approval
status, and this checkpoint does not waive the original M1 Editor/build gate.

| Requirement | Acceptance criterion | Test IDs | M1 status and evidence | Deferred completion |
| --- | --- | --- | --- | --- |
| FR-001 Start Match | AC-001 | TC-001, TC-029, TC-028 | Partial: domain state machine, explicit GameManager validation and full-readiness gate; domain tests/saved scene verified, native tests compiled | Real start in M3/M6 integration, with stats/pickup readiness in M4/M5 |
| FR-002 Load 3D Arena | AC-002 | TC-001, TC-004, TC-028 | Partial: saved 16x16 floor, 3m walls, closed corners and supported spawns verified | Native scene/containment in M2; full gameplay acceptance M7 |
| FR-003 Initialize Player | AC-003 | TC-001, TC-002, TC-012, TC-028 | Structural placeholder: layer, capsule, CharacterController and spawn verified | Movement/camera M2; combat stats M4 |
| FR-004 Stationary Bot / 10 HP | AC-004 | TC-001, TC-009, TC-028 | Structural placeholder: dedicated layer, static collider, no behavior; HP not implemented | Health 10 and runtime stationary observation M3 |
| FR-005 Third-Person Camera | AC-005 | TC-007, TC-008, TC-028 | Planned: fixed overview Camera exists; following/framing not implemented | M2 |
| FR-006 Keyboard Movement | AC-006 | TC-002, TC-003, TC-004, TC-028 | Input layer implemented: WASD intent, opposing cancellation configuration, clamp; native fixture compiled, not executed | Actual motion/collision/speed M2 |
| FR-007 Jump | AC-007 | TC-005, TC-006, TC-008, TC-028 | Input layer implemented: fresh Space intent and consume/disable lifecycle; native fixture compiled | Grounding, gravity, jump and landing M2 |
| FR-008 Basic Punch | AC-008 | TC-010, TC-011, TC-022, TC-028 | Input layer implemented: fresh J intent; native fixture compiled | Attack acceptance, cooldown, timing and feedback M4 |
| FR-009 Valid Punch Hit | AC-009 | TC-013, TC-014, TC-022, TC-030, TC-028 | Planned: target/obstruction layers configured only | M4 |
| FR-010 Base Damage | AC-010 | TC-012, TC-015, TC-028 | Planned: no damage implementation | M4 |
| FR-011 Bot HP Display | AC-011 | TC-001, TC-012, TC-025, TC-028 | Planned: HUD absent; production full-readiness gate stays closed | M3/M6 |
| FR-012 Collect Weapon | AC-012 | TC-017, TC-018, TC-020, TC-023, TC-028 | Structural trigger/kinematic pickup verified; E intent implemented and fixture compiled | Actual collection/range/occlusion/feedback M5 |
| FR-013 Modified Damage | AC-013 | TC-019, TC-021, TC-023, TC-028 | Planned: no enhanced damage implementation | M5 |
| FR-014 Collect Once | AC-014 | TC-020, TC-028 | Planned: no pickup transaction implementation | M5 |
| FR-015 Defeat Once / HP Clamp | AC-015 | TC-015, TC-016, TC-024, TC-028 | Partial domain foundation: terminal state transition verified; no Health clamp/defeat component | M3/M4/M6 |
| FR-016 Victory Display | AC-016 | TC-025, TC-028 | Planned: victory HUD absent | M6 |
| FR-017 Terminal Match / Action Gating | AC-017 | TC-026, TC-027, TC-028 | Partial: terminal domain rules verified; GameManager IsPlaying/event/start guard implemented | Runtime consumers, Health-driven Victory and pending-hit cancellation M6 |
| NFR-001 Performance | AC-N01 | TC-031 | Planned: no standalone performance measurements | M7, approved reference PC |
| NFR-002 Input Responsiveness | AC-N02 | TC-032 | Planned: no measured input latency | M7 |
| NFR-003 Stability | AC-N03 | TC-033 | Planned: no ten-match or 15-minute native soak evidence | M7 |
| NFR-004 Maintainability | AC-N04 | TC-034 | Verified (M1 architecture): four asmdefs, pure Domain, explicit ownership/injection, no project singleton or frame-wide global search | Full AC-N04 review after gameplay/configuration modules exist |
| NFR-005 Usability | AC-N05 | TC-035 | Planned: no peer sessions or complete HUD | M7 |
| NFR-006 Windows / Offline Compatibility | AC-N06 | TC-036 | Planned: Editor import/Windows build blocked by license; no resolution/playthrough evidence | Licensed build validation and M7 |
| NFR-007 Testability | AC-N07 | TC-037 | Verified (M1 infrastructure): 34 domain cases executed, 13 targeted mutations detected, 116 native cases compiled across four fixtures | Native execution plus future HP/damage/cooldown/pickup coverage; full AC-N07 remains pending |

## Current SRS test catalogue status

The four named integration gates have exactly the requested partial status.
Pure NUnit case results and saved-file assertions are separate evidence; they do
not promote an entire TC to Passed. Owner milestones follow SRS Part 6.

| Test ID | Current status | M1 evidence / remaining work | Owner milestone |
| --- | --- | --- | --- |
| TC-001 | Planned / Partially Verified (M1 Structural Baseline) | Entities/layout and domain startup foundation verified; actual Health/stats/pickup/HUD/Playing unverified | M3-M6, final M7 |
| TC-002 | Planned / input layer implemented | Cardinal intent fixture compiled; actual movement not implemented | M2 |
| TC-003 | Planned / input layer implemented | Composite/clamp/release fixture compiled; movement speed not measured | M2 |
| TC-004 | Planned / Partially Verified (M1 Structural Baseline) | Saved walls/floor/corners verified; walking/jumping containment not executed | M2 |
| TC-005 | Planned | Grounding/gravity/jump absent | M2 |
| TC-006 | Planned / input layer implemented | Fresh Space/hold lifecycle fixture compiled; airborne/landing rules absent | M2 |
| TC-007 | Planned | Overview Camera only; follow/visibility/marker usability absent | M2 |
| TC-008 | Planned | Jump-follow stability not implemented or observed | M2 |
| TC-009 | Planned / structural placeholder verified | Static bot prefab exists; HP/60-second runtime observation absent | M3 |
| TC-010 | Planned / input layer implemented | Fresh J/hold/repress fixture compiled; actual attack feedback absent | M4 |
| TC-011 | Planned | Combat cooldown boundary not implemented | M4 |
| TC-012 | Planned | No unarmed hit, HP 9 or HUD integration | M3/M4 |
| TC-013 | Planned | Hit/miss/occlusion queries absent | M4 |
| TC-014 | Planned | Multi-collider deduplication absent | M4 |
| TC-015 | Planned | Ten-hit defeat sequence absent | M4/M6 |
| TC-016 | Planned | Health overkill/clamp and defeat event absent | M3/M4 |
| TC-017 | Planned / structural and input foundations | Saved pickup trigger/body and E adapter exist; native collection absent | M5 |
| TC-018 | Planned | No range/obstruction transaction validation | M5 |
| TC-019 | Planned | Enhanced damage sequence absent | M5 |
| TC-020 | Planned | Collected-once transaction and no-stacking rule absent | M5 |
| TC-021 | Planned | Collection/HP preservation integration absent | M5 |
| TC-022 | Planned | Timed hit sample and moving in/out of volume absent | M4 |
| TC-023 | Planned | Pickup-during-wind-up ordering absent | M5 |
| TC-024 | Planned / domain transition verified | Domain victory once tested; actual Health defeat/GameManager wiring absent | M3/M6 |
| TC-025 | Planned | HP zero and victory overlay absent | M6 |
| TC-026 | Planned / domain terminal guard verified | Domain cannot restart; real action consumers/terminal suppression absent | M6 |
| TC-027 | Planned | No pending hit scheduler/cancellation to execute | M6 |
| TC-028 | Planned / Partially Verified (M1 Structural Baseline) | Structure/domain foundations only; complete match impossible at M1 | M6/M7 |
| TC-029 | Planned / Partially Verified (M1 Structural Baseline) | Failed domain readiness tested; serialized wiring verified; missing-reference native fixture compiled only | M1 native rerun, full integration M3-M6 |
| TC-030 | Planned | No melee geometry boundary/native airborne query | M4 |
| TC-031 | Planned | No reference-PC standalone frame samples | M7 |
| TC-032 | Planned | No 100-input instrumentation/latency measurement | M7 |
| TC-033 | Planned | No complete matches or native soak | M7 |
| TC-034 | Planned / M1 architecture verified | Bounded NFR-004 audit passed; full gameplay/configuration review deferred | M1/M7 |
| TC-035 | Planned | No three-peer usability evidence | M7 |
| TC-036 | Planned | No licensed Windows build/offline/resolution execution | M1 build unblock/M7 |
| TC-037 | Planned / M1 test infrastructure verified | Domain execution/mutations and native fixtures exist; missing future invariant suites/native execution | M1-M7 |

Execution evidence records commit/tag, exact versions, command, counts, source
fingerprints and outcome in [M1-verification.md](M1-verification.md) and
`evidence/M1/verification-summary.json`. Earlier task reports below are historical
snapshots and their assertion counts must not be added together.

## Historical Task 1.2 domain coverage

At Task 1.2, the standalone NUnit run executed 30 cases in `MatchStateTests.cs`.
Task 1.6 adds four factory cases; the current total is 34. These are domain-only
results, not full SRS scenario passes.

| SRS mapping | Executed coverage | Remaining integration evidence |
| --- | --- | --- |
| AC-001 / TC-001, TC-029 | Initializing, immutable/default/failed readiness, successful startup, repeated start rejection | Actual missing references, entity counts, HP/stats/pickup/HUD readiness and blocked input |
| AC-015 / TC-024 | Once-only Playing-to-Victory transition, duplicate and reentrant requests | Health clamp, once-only defeat emission and GameManager event wiring |
| AC-017 / TC-026 | Terminal Victory, rejected restart/victory, IsPlaying false | Actual punch/damage/collection rejection and pending-hit cancellation (TC-027) |
| AC-N04, AC-N07 / TC-034, TC-037 | Encapsulation, local events, isolated instances, engine-free compiled domain, automated tests and targeted mutation detection | Remaining module ownership and health/damage/cooldown/pickup test suites |

Execution environment, mutation results, reproduction command and artifact paths:
[`task_1_2_verification.md`](task_1_2_verification.md).

## Historical Task 1.4 structural arena coverage

At Task 1.4, `ArenaLayoutTests.cs` added 22 PlayMode cases for scene loading, counts, layers,
primitive component restrictions, collider configuration, floor/wall geometry,
supported/separate spawns and render infrastructure. These compile independently;
Unity execution is blocked by license error 198. Saved-file geometry/reference
assertions are executed separately by `tools/Verify-ArenaLayout.ps1`.

This contributes structural evidence to FR-002 / AC-002 / TC-001 and TC-004,
and the placeholder positioning/component portions of FR-003/004. Full TC-001
still requires initialization, Health/stats/pickup/HUD readiness and Playing;
TC-004 still requires repeated actual movement/jumping against walls and corners.
No whole gameplay TC is marked Passed. See `task_1_4_verification.md`.

## Task 1.5 input adapter coverage

`PlayerInputHandlerTests.cs` defines 34 simulated-keyboard PlayMode cases using
the actual input JSON and isolated Input System test framework. They compile;
native execution is blocked by Unity license error 198. Saved JSON/importer
configuration is checked separately with `tools/Verify-Input.ps1`.

| SRS mapping | Input assertions | Still requires later integration |
| --- | --- | --- |
| AC-006 / TC-002, TC-003 | Cardinal directions, cancellation, diagonal normalization, adapter clamp, release clearing | Actual world movement, speed, camera frame and collision |
| AC-007 / TC-006 | Jump fresh press, consume-once, held-key and disable/re-enable behavior | Grounding, jump eligibility, gravity and landing |
| AC-008 / TC-010 | Punch fresh press, consume-once, held-key and release/repress behavior | Combat acceptance, cooldown, hit timing and feedback |
| AC-012 / TC-017, TC-018 | Interact fresh press and intent isolation | Range, obstruction, match state, one-time collection and feedback |
| AC-N04, AC-N07 / TC-034, TC-037 | Adapter-only ownership, action-state isolation, lifecycle tests and full actual-source compilation | Native test execution and remaining gameplay modules |

No whole SRS TC changes status. Details and execution evidence are in
`task_1_5_verification.md`.

## Task 1.6 initialization coverage

Domain readiness factories add four executed cases: the standalone suite now has
34 passing cases, with all 13 existing targeted mutations detected.
`MatchInitializationTests.cs` defines 34 native PlayMode cases; these compile but
execution is blocked by Unity license error 198. ArenaLayoutTests adds a
serialized-scene coordinator check (23 layout cases in total).

| SRS mapping | Added assertions | Remaining acceptance evidence |
| --- | --- | --- |
| FR-001 / AC-001 / TC-001 | Explicit references, structural readiness and successful one-time Playing/event foundation through a test-only full-readiness provider | Native execution and real Health/stats/pickup/HUD initialization |
| FR-001 / TC-029 | Missing/null, duplicate, aliased, inactive, wrong-layer and cross-scene references; diagnostics; no Playing on failure | Native PlayMode execution; eventual action-consumer gating |
| FR-017 / AC-017 | IsPlaying query and repeated/reentrant start rejection; event once | Health-driven Victory, combat/pickup rejection and cancellation in M6 |
| NFR-004/007 / TC-034/037 | Local state/event ownership, explicit injection, scene serialization checks and full assembly compilation | Complete runtime integration and native scenario evidence |

The production M1 scene intentionally stays Initializing: valid placeholders do
not replace real dependencies. Full TC-001/028/029 remain Planned. See
`task_1_6_verification.md` for gate interpretation and execution results.
