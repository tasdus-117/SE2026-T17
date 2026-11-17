# TV2-003 — movement intent revocation

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
