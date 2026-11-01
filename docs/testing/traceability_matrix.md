# Requirements-to-test traceability

Source: `srs.md` v0.2, sections 4.3, 4.4 and Part 5. All SRS cases remain
**Planned**. Task 1.1 provides project/assembly infrastructure only; it does not
satisfy gameplay or quantitative quality acceptance. Proposed D-values and NFRs
retain their SRS approval status. TC-028 is an integrated subset, not a substitute
for each requirement's focused cases.

| Requirement | Acceptance criterion | Planned test IDs |
| --- | --- | --- |
| FR-001 Start Match | AC-001 | TC-001, TC-029, TC-028 |
| FR-002 Load 3D Arena | AC-002 | TC-001, TC-004, TC-028 |
| FR-003 Initialize Player | AC-003 | TC-001, TC-002, TC-012, TC-028 |
| FR-004 Stationary Bot / 10 HP | AC-004 | TC-001, TC-009, TC-028 |
| FR-005 Third-Person Camera | AC-005 | TC-007, TC-008, TC-028 |
| FR-006 Keyboard Movement | AC-006 | TC-002, TC-003, TC-004, TC-028 |
| FR-007 Jump | AC-007 | TC-005, TC-006, TC-008, TC-028 |
| FR-008 Basic Punch | AC-008 | TC-010, TC-011, TC-022, TC-028 |
| FR-009 Valid Punch Hit | AC-009 | TC-013, TC-014, TC-022, TC-030, TC-028 |
| FR-010 Base Damage | AC-010 | TC-012, TC-015, TC-028 |
| FR-011 Bot HP Display | AC-011 | TC-001, TC-012, TC-025, TC-028 |
| FR-012 Collect Weapon | AC-012 | TC-017, TC-018, TC-020, TC-023, TC-028 |
| FR-013 Modified Damage | AC-013 | TC-019, TC-021, TC-023, TC-028 |
| FR-014 Collect Once | AC-014 | TC-020, TC-028 |
| FR-015 Defeat Once / HP Clamp | AC-015 | TC-015, TC-016, TC-024, TC-028 |
| FR-016 Victory Display | AC-016 | TC-025, TC-028 |
| FR-017 Terminal Match / Action Gating | AC-017 | TC-026, TC-027, TC-028 |
| NFR-001 Performance | AC-N01 | TC-031 |
| NFR-002 Input Responsiveness | AC-N02 | TC-032 |
| NFR-003 Stability | AC-N03 | TC-033 |
| NFR-004 Maintainability | AC-N04 | TC-034 |
| NFR-005 Usability | AC-N05 | TC-035 |
| NFR-006 Windows / Offline Compatibility | AC-N06 | TC-036 |
| NFR-007 Testability | AC-N07 | TC-037 |

EditMode will cover isolated arithmetic, cooldown and match invariants. PlayMode
will cover initialization, physics, transactions and event integration. Manual,
standalone performance and usability cases retain the methods in the SRS.

For executed cases, append commit/build ID, Editor/package versions, approved
parameters, tester/date, machine, actual outcome, log/result path and defect link.
Do not mark a whole TC Passed from a partial scaffold check.

## Task 1.2 executed domain coverage

The standalone NUnit run executes 30 cases in `MatchStateTests.cs`. These are
domain-level evidence only; the full SRS scenario statuses above remain Planned.

| SRS mapping | Executed coverage | Remaining integration evidence |
| --- | --- | --- |
| AC-001 / TC-001, TC-029 | Initializing, immutable/default/failed readiness, successful startup, repeated start rejection | Actual missing references, entity counts, HP/stats/pickup/HUD readiness and blocked input |
| AC-015 / TC-024 | Once-only Playing-to-Victory transition, duplicate and reentrant requests | Health clamp, once-only defeat emission and GameManager event wiring |
| AC-017 / TC-026 | Terminal Victory, rejected restart/victory, IsPlaying false | Actual punch/damage/collection rejection and pending-hit cancellation (TC-027) |
| AC-N04, AC-N07 / TC-034, TC-037 | Encapsulation, local events, isolated instances, engine-free compiled domain, automated tests and targeted mutation detection | Remaining module ownership and health/damage/cooldown/pickup test suites |

Execution environment, mutation results, reproduction command and artifact paths:
[`task_1_2_verification.md`](task_1_2_verification.md).

## Task 1.4 structural arena coverage

`ArenaLayoutTests.cs` adds 22 PlayMode cases for scene loading, counts, layers,
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
