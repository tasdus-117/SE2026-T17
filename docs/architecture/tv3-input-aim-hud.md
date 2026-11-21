# TV3 input, aim and presentation integration

Status: draft integration; implementation choices below are TV3 proposals for review,
not signatures or agreement by TV1/TV2. See [execution evidence](../testing/tv3-input-aim-hud.md).

## Baseline audit (21 Nov 2026)

- Fetched main: `9ee7ea715b6fc5d07156411aac543a481585cee8`.
- PR #14 open/draft: `24bdf3eddc2975a7a8646f343ca62af0311badec`,
  branch `feature/TV2-004-input-shot-presentation-contracts`. Description, all nine
  changed files, CT-201–205, results, discussion, reviews and review threads inspected;
  no comments/reviews/threads present at inspection.
- PR #13 open/draft: `db133b0e8b99e3dc2ba065b6964ac818d12c1e80`.
  Movement clearing/watermarks remain its responsibility; not imported here.
- PR #21 open, not draft: `ae90f0705c6b29fc2f3e851077e3f5a35bc64634`.
  Inspected lifecycle/score/clock/HUD symbols on that branch. It is unmerged;
  do not assume its providers exist on this baseline or copy its UI into TV3.
- TV3 PRs #11 and #12 are merged into main. Existing arena/loading work is retained.
- No AGENTS.md found in repository. README and architecture authority/structure inspected.
  ProjectVersion, rather than stale README version, pins Unity 6000.6.8f1; Mirror unchanged.

| Category | Current baseline |
| --- | --- |
| Available and callable | World-XZ movement, grounded server jump, yaw0/pitch30 camera; owner melee Commands; HP/result SyncVars; health/menu/spawn/despawn views; immutable Domain config/state; #14 CombatAim |
| TV3 additions | Horizontal mouse-plane aim; fresh held-fire adapter and cadence; immediate input invalidation; guarded HP callbacks; authoritative presentation read-model binding |
| Missing runtime dependencies | Raycast shot sink/wire and server muzzle/hit validation; config/current-life/control provider; ordered baseline/state/rejection adapter; reconnect/life invalidation |

## Decision and implementation slices

Use #14 as the PR base to reuse CombatAim without duplicating validation. This is a
stacked PR, not a cherry-pick or a copy of another branch. Main alone would require
duplicating or selectively vendoring that helper; importing #13/#21 would expand scope
into movement/server/session and conflict with unmerged work. After #14 merges,
maintainers can retarget/rebase TV3 in a separate authorized integration step.

Slices: test aim/cadence first; verify input and health lifecycle; build provider-facing
held-fire/presentation consumers; run project regressions; review and deliver draft.
No new dependency, asmdef, scene/prefab, settings, network signature or gameplay store.

## Movement, aim and geometry

Keep world XZ and fixed yaw0: W=(0,0,+1), D=(+1,0,0), W+D=(.7071,0,.7071).
Analog (.3,.4) remains (.3,0,.4), magnitude .5; cap only above1 in existing code.
No camera-relative movement conversion is needed or introduced. Existing offline and
server movement consume airborne jump edges without deferring them to landing.

`MouseAim.TryDirection(Ray, Vector3 muzzle, out Vector3)` intersects the horizontal
plane at the explicit muzzle Y, subtracts muzzle, and uses CombatAim normalization.
Example: down ray from (3,10,4), muzzle(0,0,0) → (.6,0,.8).
Zero/coincident, backward intersection, nonfinite inputs/results and near-parallel
(|ray.y|<1e-6) return false/zero. No default +Z shot on invalid input.

Player prefab has capsule height2/radius.5/center0, no child muzzle. Legacy sphere
offset is (0,.25,.5). **No new muzzle offset is selected or installed**: HeldFireInput
requires an explicit Transform supplied with a real provider. Plane-through-anchor
is the TV3 choice matching CT-202; it is not a server-origin decision.
Mouse aim performs no Physics raycast, so has no layer mask and cannot infer hits.
Existing melee uses character layers7/8, blocker6 and pickup9 unchanged.
TV1/TV2 must review server anchor, self-collider/inside-collider policy, nearest blockers
and permitted layers before installing the raycast adapter. Do not map this to melee.

## New local contracts and ownership

All symbols below are additions in this PR, not previously approved network interfaces.
Shared types are engine independent; dependencies remain Client → Protocol/Domain.

| TV3 supplies | File / entry | Caller obligation |
| --- | --- | --- |
| Fresh collection | Client/Input/HeldFireInput.cs: Configure(input,camera,muzzle,sink) | Explicit references; attach only after real raycast provider exists; do not enable alongside a second LMB combat collector |
| Rate limiter | Client/Input/HeldFireCadence.cs: TryRequest(allowed,held,now,interval), Reset | Positive finite provider interval; client traffic control only |
| Input gate | Client/Input/PlayerInputHandler.cs: TryReadHeldFire, CanCollectGameplayInput, GameplayInvalidated | IsInputReady retains initialization-readiness semantics used by offline startup |
| Read model | Shared/Domain/PlayerPresentationSnapshot.cs | Immutable authoritative values; no predicted HP/score or locally restarted timer |
| HUD consumer | Client/UI/PlayerPresentationBinding.cs: Bind, State, Changed, Dispose | One per view lifetime; Dispose on disable/despawn; null Changed clears life-bound effects |

| TV1/TV2 must supply | Exact boundary and required behavior |
| --- | --- |
| TV1 + TV2 raycast sink | Implement Shared/Protocol/IShotIntentSink.cs: CanSubmit, RequestIntervalSeconds, event Action Invalidated, SubmitAim(float worldX,float worldZ). Only directions cross this local boundary. Provider handles authenticated ownership/current match/life, reliable delivery, and server revalidation; never forward to RequestAttack(Vector3) |
| TV1 control/lifecycle bridge | CanSubmit false before baseline, on disconnect/revoke/death/menu-independent authority loss; Invalidated synchronous on main simulation/client thread, even revoke+restore between frames, on life/binding/context/baseline replacement. Do not rely only on polling |
| TV1 config + TV2 cooldown | RequestIntervalSeconds from the installed authoritative configuration/cadence policy. TV3 supplies no guessed default. Server still decides cooldown/acceptance |
| TV1 state + TV2 results | Implement IPlayerPresentationSource: Current snapshot or null until baseline, event Action<PlayerPresentationSnapshot> Changed. Publish null on invalidation, use a new opaque Context token per binding/match/life and monotonically increasing ulong Revision within it; do not wrap. Supply HP/max/score/result/rejection from committed state |
| TV1 clock projection | RemainingSeconds from current authoritative clock/deadline projection, including reconnect baseline. Consumer does not create a timer or reset HP. Publish timer updates with newer revisions |
| TV1 + TV3 #21 follow-up | Adapt #21 NetworkMatchController/NetworkHealth once merged/reviewed; map existing authoritative values, add life/context/revision and rejection only when their producers exist. Do not create a parallel session store |

Context is an opaque **local object identity**, allocated once by the adapter for a
current binding/life, not an actor claim, wire DTO or authentication proof. Result
and Rejection strings are presentation labels, not executable dispositions; future
views should render them as plain text. No shot correlation/confirmed-hit effect
is claimed by this read model.

Held-fire reads LMB and mouse ray anew for each attempt. No queue, catch-up loop,
application retry or accumulated burst exists. Disable/despawn/menu/focus/pause,
missing/destroyed camera/muzzle/sink, device or source replacement, observed authority
loss and provider Invalidated reset the release gate. Restoring control requires an
observed release then repress. Repeated hold cannot resume an old request.
Invalid aim consumes only a client sampling opportunity, never server cooldown.

PresentationBinding refreshes initial Current, replaces values without prediction,
ignores duplicate/older revision, captured callbacks from replaced/disposed bindings
and old Context, and clears before presenting a new context. No fake production
provider or fabricated scoreboard/timer is installed. NetworkHealthBarView also
guards captured callbacks by binding generation, refreshes on enable and clears
text on unbind. Existing NetworkHealthHUD still consumes the real legacy HP/result
path; it has no revisioned-life protocol on this baseline.

## Review and acceptance limits

Security boundary inspection: no client origin/pose/HP/damage/score added to ingress,
no authority gate removed, no server code changed. Client checks cannot grant server
permission. No gameplay mutation or session/reconnect manager is implemented.

Draft remains necessary: new raycast and extended state consumers are independently
implemented/tested modules, not end-to-end integration. No human review/signature is
inferred from automated review or task authorization. Review plane/anchor/layers,
release policy, new local interface names/semantics, #21 adapter ownership and the
cadence supplied by TV1/TV2 before enabling them in a prefab.
