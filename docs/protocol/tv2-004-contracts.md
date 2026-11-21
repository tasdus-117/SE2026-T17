# TV2-004: input, shot, result and presentation contracts

Revision 2026-11-19, prepared by Codex for TV1 network/ownership, TV2 gameplay
(Long / 23001901), TV3 aim/HUD. **DRAFT: independent validation implemented;
raycast/session integration and actual three-member review pending.**

## Sources, baseline and scope

The owner's current local records are outside Git at `C:/Long/3D_arena_game/docs`.
Source meanings come from `reference/TV2-001/Long_document/srs.md` v0.5,
`reference/TV2-001/docs/{TASKS,TESTING,PROTOCOL,DECISIONS}.md` and
`GAME_RULES.md`. These are source locators, not repo-relative links. CT IDs below
preserve PROTOCOL's 08-Nov meanings; CT IDs are not the similarly numbered TC IDs.
GD-01–08 gameplay was accepted through the owner on 08-Nov; separate technical
interfaces and reviewer signatures were not accepted by implication.

Execution base is fetched `origin/main` **9ee7ea715b6fc5d07156411aac543a481585cee8**.
TV2-002 PR #9 is merged (`d939568aae110668df5f0114541e4498593b6108`, ancestor).
TV2-003 [PR #13](https://github.com/tasdus-117/SE2026-T17/pull/13) remains open/draft
at `db133b0e8b99e3dc2ba065b6964ac818d12c1e80`, same base. This PR targets main,
is not stacked and does not copy PR #13. Main supplies Shared.Protocol, the normal
owner Commands, Domain state/config and HUD hooks for independent work. It does
not supply the current-state/session adapters needed for full FR-202/204/208.
TV2-003 historical movement38/PlayMode330/EditMode142 are not results for this PR.

Implementation adds `SmallArena.Protocol.CombatAim`, an engine-independent
horizontal aim normalization contract and a compatibility validator used by the
existing `NetworkCombat.PerformAttack` entry. It preserves the melee wire and
all existing geometry/cooldown/health behavior. A logical horizontal direction
is not a new serialized message or proof of authority. No raycast, scoring,
reconnect, scene, prefab or new assembly is introduced. The source task explicitly
excludes writing a Mirror adapter; missing additions below remain proposals.

## Prerequisite audit

All main symbols in this table were inspected at the execution base above (B).
Checks refer to candidate suites in the [verification report](../testing/tv2-004-results.md).

| Dependency / current symbol (B unless noted) | Established behavior and verification | Remaining owner/action |
| --- | --- | --- |
| `NetworkServer.OnCommandMessage`, `NetworkPlayer.CmdPerformAttack`, `NetworkMovement.CmdSubmitMovement` | Registered authenticated Command handler; ready connection, spawned identity, `identity.connectionToClient == conn` when authority required; forged-target tests use serialized Commands | TV1: supply stable participant/match/life binding; object ownership is only current-avatar authority |
| `MovementInput`, `MovementInputValidator`, `ServerPlayerMovement` | Four-field world-XZ intent, finite/clamp/uint serial order, fixed-step V5, server receipt clock, inclusive 0.25s freshness; movement suite | TV1/TV2: config provider and current-state consumer. PR13 observed-revocation clearing is still unmerged |
| `GameplayConfiguration`, `OnlineMatch`, `OnlineCharacterState` | Immutable config/snapshot, `CanReceiveControl`, `CanFire`, `IsCurrentLife`, retained cooldown and life transitions; EditMode suites | TV1+TV2: authoritative instance/provider and snapshot lookup in ingress/simulation; currently no production consumer outside Domain |
| `ArenaNetworkManager.OnServerAddPlayer/OnServerDisconnect`, `NetworkPlayer.ConnectionId` | Connection-owned avatar, destroy on disconnect, server disconnected/stopped events; lifecycle suite | TV1: stable identity, proof, generation, revoke-before-rebind and retained-session continuity; connId is not proof |
| `ServerLobbyController`, `LobbyRoom`, `LobbyStartOperation` | Ready/loaded room state and host start/loading rollback; lobby suites | TV1+TV2: bridge to `OnlineMatch`, automatic 2–4 ready+loaded start, match clock/order |
| `NetworkCombat.PerformAttack`, `NetworkHealth.TakeDamage` | Reliable owner melee request, server clock/cooldown/geometry, HP10 damage1, server-only health mutation; combat/HUD suites | TV2-005/006/007/009: raycast100/25, one target, cooldown0.5, score/result ledger. Legacy melee may damage several targets |
| `ThirdPersonCameraController`, `NetworkCombatInput` | Camera yaw0/pitch30; client sends fixed +Z melee aim; input/HUD suites | TV3+TV1: mouse-plane aim, server muzzle anchor, layers and inside-collider policy; jump and camera basis confirmation |
| `NetworkHealthHUD`, `NetworkHealthBarView`, `OpponentHealthPresenter` | Initial snapshot and HP hooks, result hooks lock input, disable/despawn unbind; combat/HUD suites | TV3+TV1: life/score/timers/config-revision snapshot and result/reject consumers |
| `NetworkTime.localTime`, `OnlineMatch` time parameters | Live ingress receipt/physics use server local seconds; Domain requires finite nonnegative monotonic server seconds | TV1: bridge one clock epoch, lifecycle ordering and notifications; TECH-001 dedicated build/transport/budgets confirmation |

Stale source links: archived `PROJECT_CONTEXT.md` is absent from current main and
is intentionally ignored personal context. No runtime authority is inferred from
it. Historical uppercase `docs/PROTOCOL.md` is available in the local reference
and TV2-001 worktree, not main; this document lives in main's existing protocol
structure. No required FR/CT source was missing after worktree/`rg` searches.

## Actor and ingress authority

Current trace: authenticated transport connection → Mirror registered Command
handler → readiness/spawn/ownership check → owned `NetworkPlayer` or
`NetworkMovement` → server gameplay component. The Command envelope `netId`,
component index and function hash route the call; they do not grant ownership.
Changing netId to B's avatar on connection A fails before gameplay. No actor,
HP, damage, score, hit target or hit point exists in the current attack payload.
`RequestPickup(itemNetId)` is a separate target reference with server collection
validation, not an actor claim or combat outcome.

Required future ingress contract (PROPOSED, TV1 owner): resolve the authenticated
connection to a current binding containing server session/match identity,
participant identity, current avatar/life and binding generation. Check it against
the active authoritative match and current `OnlineCharacterState` at ingress and
again before executing delayed work. A client context identifier can only be an
equality guard against this binding. Reject missing/revoked bindings, another
participant, wrong match, old generation or stale life before mutation. Match
object identity exists in Domain; there is no accepted wire match/session ID yet.

Outcome fields are never ingress authority. Current fixed Command readers accept
only their declared fields; bytes cannot turn into an HP/damage setter. There is
no arbitrary dictionary deserializer. **Strict rejection of surplus payload bytes
is not established by this task**; do not claim malformed framing tests passed.
Any proposed extensible shot decoder must reject outcome/unknown fields and
unsupported versions before dispatch, with a fresh framing test. Existing
`TakeDamage` is server-only, not a client Command; native proxy tests cover this.

## CT-201 — movement intent

| Field / owner | Type / units / domain | Default and invalid behavior |
| --- | --- | --- |
| Sequence / client input sender | uint serial counter per avatar, modulo 2^32 | Starts0 locally, first send1; current server accepts first finite packet including0. Newer iff unchecked signed difference >0; duplicate/older/exact half-range rejected |
| MoveX, MoveZ / client | float dimensionless world X/Z; any finite values | Zero means neutral; magnitude≤1 preserved; >1 normalized in double precision; NaN/±Infinity rejected without refreshing receipt time |
| JumpSequence / client | uint edge counter, same serial comparison | Starts0; heartbeat repeats edge; new airborne edges consumed, not queued for landing; grounded jump server-owned |

No timestamp, delta, speed, position, actor or rotation is accepted. All four
fields are serialized in declaration order by Mirror. This change adds no fields,
defaults, message IDs, hashes or version negotiation. Old callers remain supported.
Default struct is neutral axes/counters; it is not a lifecycle reset request.

`NetworkMovement` sends current intent at up to60Hz on `Channels.Unreliable`;
this is sender traffic control, not an ingress flood budget. Later heartbeats
replace lost movement and repeat jump edges. Server latest intent only; no
replay queue and no additional physics step per packet. `NetworkTime.localTime`
at valid receipt is seconds in the server epoch, not client time. Fresh while
age≤0.25s, then horizontal intent0; invalid/old packets never refresh. V=5m/s
and fixed delta are server-owned. Serial ordering requires less than 2^31 forward
distance; no bounded application resend window is invented.

Current reset boundary: new `OnStartServer` avatar resets server counters;
`OnStartLocalPlayer` resets client counters. These do not define reconnect/new-life
policy. Main does not clear all held input through temporary revoke/restore;
PR13 fixes *observed* revoke while preserving watermarks. A revoke occurring
entirely between observations requires an explicit synchronous lifecycle
notification. TV1 must provide it; polling `CanAct` cannot establish the guarantee.

Concrete example (logical match/life context is server-side, not extra fields):
connection A authenticated/ready owns avatar A, match M Playing, participant P,
life7 Alive+Connected+protected; last accepted Sequence40 at server10.000s.
Unreliable packet `{Sequence:41,MoveX:3,MoveZ:4,JumpSequence:0}` arrives at10.010.
Ownership and finite/order pass; accepted axes(.6,.8), at V5 velocity(3,4)m/s.
Protection permits movement. Duplicate41 is ignored; invalid42 with NaN does not
refresh. At10.260 exact boundary it is fresh, after10.260 it produces no horizontal
motion. Server transform replication drives owner/observer pose; no client pose
write. Domain eligibility for M/P/life is required integration, not currently in
the motor. Existing movement vectors/Command tests verify the legacy subset.

## CT-202 — shot request and aim

Current `CmdPerformAttack(Vector3 aimDirection)` is reliable (Mirror's default),
owner-only, no application resend/ack/backlog. `NetworkCombatInput` consumes press
edges; held mouse does not repeat. Early clicks and loading-blocked requests are
dropped. Legacy payload is exactly float x/y/z, dimensionless; all finite,
squared magnitude in[.99,1.01], z≥.99. Zero/nonfinite/nonunit/backward reject
without cooldown mutation. `CombatAim.IsLegacyMeleeDirection` preserves this
validation through the normal handler. It deliberately does not normalize bad
legacy input into an accepted attack.

Raycast logical addition (PROPOSED mapping, not installed wire): required
`aimX/aimZ` float world-XZ intent, finite/nonzero, Y implicit0, normalize using
`CombatAim.TryNormalizeHorizontal`; no default valid direction. No client origin,
hit, damage, HP, score or client time. Aim is independent of movement. Future
optional correlation/context fields require TV1-approved serialization/version
and binding rules, not a silent change to the existing Command signature/hash.

Accepted rules §4 require holding LMB to fire at the server cooldown. The current
press-edge melee collector does not implement that requirement. TV3 must provide
fresh held-fire collection for the raycast path, with no queued catch-up shots;
TV2 enforces the server cooldown and TV1 agrees its send cadence/rate budget.
Clear held fire on revoke/life change and require fresh input after reconnect.
The current press-edge behavior remains compatible until that integration.

TV3 camera contract proposal: project camera forward/right onto world XZ and
normalize; world movement = right*horizontal + forward*vertical, then cap1.
Yaw0 gives forward+Z/right+X. Mouse ray intersects a horizontal plane through the
authoritative muzzle-height presentation plane; direction is world intersection
minus muzzle projected on XZ. Parallel/no intersection/coincident point gives
no shot (do not use a zero/default direction). Server reconstructs origin from
its current authoritative pose/muzzle and validates horizontal direction; it
does not trust the client intersection or camera position. Exact muzzle anchor,
plane choice, layers, inside-collider and permitted origin tolerances remain
TV3+TV1/TV2 review items. No lag compensation or client timestamp authority.

Required gates reuse `OnlineCharacterState.CanFire(now)`: Playing before deadline,
current Alive+Connected life, unprotected and now≥NextShotAtSeconds. Successful
shot uses one server ray, nearest blocker, range30m inclusive, accepted miss
consumes0.5s. Rejection consumes nothing. Protected target blocks without damage,
dead/self/irrelevant triggers ignored. Execution belongs to TV2-005/006.

Concrete example: A authenticated owns P in server session S, M/life7, unprotected,
HP100, now12s, NextShotAt12s. Logical reliable intent aim(3,4) arrives once;
normalized(.6,.8) from shared validator, muzzle server-only. Server ray finds
opponent Q/life2 at20m, unobstructed/unprotected: accept, next-shot12.5, one
25HP effect to Q75. A miss still sets12.5; aim(0,0), stale life6 or forged actorQ
reject with unchanged HP/cooldown. Current melee wire cannot execute this
raycast example; unit validation and real legacy Command rejection are verified
independently. TV3 may show a provisional muzzle flash/crosshair, never a
confirmed hit marker or HP deduction until authoritative acceptance/result.

Delivery/replay: reliable transport retransmission is handled below Command
dispatch and is not an application retry protocol. Do not add a shot-ID database
for the current path. Two separately submitted Commands are separate attempts,
subject to server cooldown, even with identical aim. No exactly-once claim across
disconnect/reconnect. If TV1 later adds app retries, accept a bounded identity
`(server match, participant, life, binding generation, action sequence)` with
dedup lifetime/reset and ack semantics agreed before enabling retries. No such
key/lifetime/window is currently accepted or implemented. Server-internal effect
idempotence is still required independently of transport (CT-203).

## CT-203 — authoritative combat result

Existing server health/results use SyncVars, not a shot result message. No score,
shot correlation, rejection response or raycast event currently exists.
Logical result schema below is a proposal for TV1 serialization and TV3 consumers:

| Field / owner | Proposed representation / constraints / absence |
| --- | --- |
| context / server | Current session+match+participant+positive long LifeId from binding; no wire identity types accepted yet |
| disposition / server | AcceptedHit, AcceptedMiss, Rejected plus reason; no optional default acceptance |
| reason / server | Closed set: InvalidAim, NoBinding, StaleContext, NotPlaying, CannotAct, Protected, Cooldown; malformed framing handled before gameplay |
| target context / server | Optional only for resolved hit; not an actor authority claim |
| state revision / server | Proposed monotonic ulong within match, no wrap/reuse; exhaustion terminates/rebases only with agreed new context |
| health/score/deaths / server | int HP[0,MaxHealth], score/deaths≥0; from committed state, not independent client deltas |
| correlation / server echo | Optional future action key only if accepted delivery design needs it; grants no authority |

Concrete example: internal authoritative shot from A/P life7 against Q life2
at12s resolves HP25→0. Server applies one effect, death once for Q/life2 and
P score3→4; duplicate collider or delayed same effect cannot award again.
Authenticated sender is the server for the outbound result; original actor was
derived from A's connection. At Q life3, a callback for life2 is stale and must
not reduce health or award score. TV3 shows confirmed hit/death/score only from
the committed result/state revision; duplicate events cannot replay effects.
Receive order applies immediately; an actor killed before its turn cannot fire.
One target/shot and death/credit idempotence are TV2-006/007/009 ledger work,
not established by legacy melee's per-call collider HashSet or this aim helper.

## CT-204 — gameplay state and reconciliation

Reuse `OnlineCharacterState` for the server state, not as a wire DTO. It contains
participant string(nonempty opaque identity), positive increasing long LifeId,
enum LifeState(Alive/Dead/RespawnPending), enum ConnectionState, Health/Score/Deaths
int, NextShotAtSeconds/ProtectionUntilSeconds finite nonnegative double, and
nullable RespawnAtSeconds(required for Dead/Pending, absent for Alive).
Its `Match` owns immutable config and Waiting/Playing/Finished/Aborted, optional
start/deadline seconds. Config revision is `rules-v0.1`; ranges/units/defaults
remain those validated by `GameplayConfiguration` (100HP,25HP,.5s,30m,V5,.25s,
180s,3s,5m,.5s,1s,15s). Preserve zero configured speed/safe distance where its
constructor permits them; current motor does not yet consume those values.

Proposed state payload adds the binding context, match revision and terminal
result(Win/Draw/Aborted, optional winners only for Win). Timers are absolute
double **server seconds**, not local client deadlines. TV1 must establish clock
offset/epoch before UI countdown integration. Initial/reconnect baseline contains
full state+config revision; only apply newer revisions in the same context.
New life replaces life-bound effects; same-life stale state never rolls HP back.
Match/binding context change clears pending visuals before accepting a baseline.
TV1 must select ordered reliable snapshot/delta semantics and version rejection;
no accepted wire defaults for absent required context exist yet.

Presentation policy proposal: an immediate crosshair/muzzle animation is cosmetic
and expires without implying acceptance. AcceptedHit confirms hit feedback once;
AcceptedMiss clears pending hit feedback; Rejected cancels pending feedback and
shows a simple unavailable indication where useful. Authoritative snapshots
always replace displayed HP/score/timers even if an event was missed. Without a
result/correlation path, let cosmetic feedback expire and infer no hit; never
write predicted HP/score into gameplay state. Current HUD does not speculate.

Concrete example: server authenticated outbound baseline S/M/P life7 revision21
has HP75, score4, deaths1, next-shot12.5, protection0, match deadline180.
TV3 replaces its provisional display with75HP/4 and derives countdown from
server-clock estimate. Later revision20 cannot restore HP100. After life8 baseline,
life7 effects cannot change it. This snapshot path is pending. Current compatible
subset is `NetworkHealth.OnStartClient` plus HP/result SyncVar hooks consumed by
`NetworkHealthHUD`/overhead views. Those show authoritative HP10 melee values,
bind on enable, refresh initial state, unbind on despawn; no life/revision filtering
is claimed. Rejected legacy aim yields no HP hook/result and leaves HUD unchanged;
the new real Command test checks rejection followed by a valid replicated hit.

## CT-205 — lifecycle bridge

Required order: commit deadline/terminal and binding revocation first; before
deadline process movement/environment, then shots in server receive order with
immediate application, then respawn/protection transitions, then replication.
TV1 must define a stable secondary ingress order for simultaneous callbacks.
No runtime scheduler connecting these tasks currently establishes this order.

Proposed bridge notifications must be synchronous on the server simulation
thread and include old/new binding/life plus transition reason and server time.
Revoke must clear held axes, freshness, pending jump/fire and provisional
presentation; deliver it even if restore occurs before the next ingress/fixed
step. Retain replay watermarks through temporary revocation (PR13 behavior).
For new match/life/rebind, reject all old-context messages before resetting any
per-context counters. TV1 must define counter continuity/reset, reliable baseline
barrier and old-connection rejection before implementation. No observer polling
or reconnection of connId alone can meet this obligation.

Concrete example: A authenticated P/S/M/life7 disconnects at50s with HP75,
score4, NextShotAt50.4; server revokes A immediately, clears all pending intent,
retains life/HP/score/cooldown, avatar remains damageable. Authenticated new B
presents valid session-bound proof at64.99 (<65): revoke old binding before
bind B, provide current snapshot before accepting fresh input. Old A or life6
messages reject. At65 exact expiry, invalid proof or voluntary leave: no restore.
If P died while offline and respawn delay elapsed, remain Pending until connected
and safe spawn available; respawn creates life8, full HP,1s protection, retains
cooldown/score/deaths and clears held fire. Fewer than2 eligible participants
after leave/expiry abort; unexpired grace remains eligible at deadline; terminal
result freezes. Outbound lifecycle/state is server-authenticated and TV3 displays
reconnecting/pending/terminal without resetting timers. Required delivery is a
reliable current-baseline barrier, not implemented in present destroy-on-disconnect
manager. Domain transitions are independently testable; retained-session execution
is TV1 session + TV2-008/009/012, not a completed scenario here.

## Traceability and review packet

| Source requirement / task criterion | Contract / symbols | Examples / verification / gap |
| --- | --- | --- |
| FR/AC-201 dedicated server, capacity, auto-start, roster | Actor authority, CT-205; Mirror/manager/lobby | CT-201/205; in-process ownership/lobby tests; dedicated topology/auto-start Domain bridge pending TV1 |
| FR/AC-202 finite/clamp/speed/timeout/state | CT-201; MovementInput/validator/motor, Domain CanReceiveControl | CT-201; movement suite; configured state/lifecycle seam and PR13 pending TV1/TV2 |
| FR/AC-204 server muzzle/horizontal aim/raycast/outcomes | CT-202/203; CombatAim, legacy PerformAttack | CT-202/203; aim + real Command/HUD checks; raycast execution TV2-006 and muzzle TV3 pending |
| FR/AC-208 proof/grace/current state/no reset/revoke | CT-204/205; OnlineCharacterState/manager | CT-204/205; Domain/lifecycle tests cover only existing seams; session transport pending TV1 |
| Actor derived from connection | Actor section and Mirror owner check | ForgedCommandForOtherOwner and multi-client forged movement tests; stable binding pending TV1 |
| No client damage/HP/score | Actor, CT-202/203/204; fixed wire + server TakeDamage | NativeClientProxy_CannotApplyHealthChanges; framing extensions pending TV1 |
| Retry/dedup matches actual delivery | CT-201 and CT-202 delivery section | Existing unreliable serial/jump tests; reliable Command rejection/HUD test; no app resend or new wire |
| Fields/types/units/reasons + adapter checklist | CT-201–205 tables, prerequisites and this matrix | Five concrete examples documented; new shared validator integrated only where established |
| Walkthrough input→server→HUD and reconnect | All five examples | In-process current wire walkthrough exercised; logical raycast/reconnect walkthrough is review material, not executed acceptance |
| Actual TV1/TV2/TV3 review | Review register below | Pending all three; keeps task/PR draft |

| Reviewer / role | Requested review scope / next action | Date / revision / outcome |
| --- | --- | --- |
| TV1 network/ownership (named reviewer not supplied) | Binding/current-state/config provider, clock/order, generation/reset, notifications and baseline delivery; approve concrete serialization before new adapter | No actual review received |
| TV2 Long /23001901 | CT gates/ledger/task scope, timeout boundary, one-effect/life and Domain integration | No actual review of this revision received; task authorization is not review |
| TV3 aim/HUD (named reviewer not supplied) | Camera/world vectors, zero aim, muzzle/layers/inside collider, snapshot/reject/reconcile consumers | No actual review received |
| Codex automated implementation inspection | Compatibility gate, five examples, source/FR/CT mapping and tests | 19-Nov-2026; candidate identified in verification report; not human sign-off |

Adapter checklist for the next integration revision: obtain those three reviews;
accept binding/provider/clock and revocation symbols; establish wire version and
reader/framing limits; implement raycast/cooldown/ledger in assigned tasks; add
state/reject/lifecycle consumers; exercise stale context and revoke-between-polls
through real ingress; run dedicated server+two independent clients and reconnect
with old-connection traffic. Mark DONE only after required checks and actual
three-member reviews have evidence with reviewer/date/revision/outcome.
