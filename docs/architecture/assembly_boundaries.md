# Assembly boundaries

Task 1.1 created four project assemblies. Domain contains the Task 1.2 pure match
rules. Runtime contains the input adapter and Task 1.6 scene coordinator/readiness
contract. PlayMode.Tests covers configuration, layout, input and initialization.
Input/motor/combat composition into the placeholder Player remains deferred.

| Assembly | Allowed assembly references | Engine access |
| --- | --- | --- |
| SmallArena.Domain | None | Disabled (`noEngineReferences: true`) |
| SmallArena.Runtime | SmallArena.Domain, Unity.InputSystem | Enabled |
| SmallArena.EditMode.Tests | Domain, Runtime, UnityEngine.TestRunner, UnityEditor.TestRunner | Editor only |
| SmallArena.PlayMode.Tests | Domain, Runtime, UnityEngine.TestRunner, UnityEditor.TestRunner, Unity.InputSystem, Unity.InputSystem.TestFramework | Editor PlayMode; test assembly |

Both test assemblies explicitly reference `nunit.framework.dll`, use
`overrideReferences: true`, and retain `optionalUnityReferences: ["TestAssemblies"]`.
They are test-only assemblies, not shipping gameplay dependencies. Device/player
test builds are outside this task's verification scope.

Domain also overrides automatic precompiled references with an empty list to
prevent implicit third-party plugin dependencies. Runtime must never reference
either test assembly. Use scene/prefab references and local instance events later;
do not introduce global singleton, service-locator, or event-bus dependencies.

## Scene startup contract (Task 1.6)

`GameManager` owns one instance of the Domain state machine and forwards its
instance event through `OnMatchStateChanged`. `CurrentState` and `IsPlaying` are
read-only queries. It has no static Instance, Update loop, movement, damage or
physics-query responsibility. Awake binds forwarding idempotently; destruction
unsubscribes. Calls before Awake are supported without duplicate subscriptions.

Inspector/Configure wiring supplies three distinct entity GameObjects, a Camera,
and an optional full-readiness provider. Structural validation checks active
same-scene role roots on named layers, exactly one root per role (including
inactive extras), and an active same-scene Camera. Same-role visual descendants
do not count as separate entities. Traversal is startup-only and scoped to the
manager's own scene; no global object discovery is used.

The task requests structural success with three valid entity references while
also prohibiting Playing before real Health/HUD initialization. Two probes
preserve both requirements:

| Probe | Meaning |
| --- | --- |
| ValidateSceneReadiness | Entities/counts/layers/camera are structurally valid; returns Success without changing state |
| ValidateMatchReadiness | Structural validation plus an explicitly wired IMatchReadinessProbe's real dependency validation |

TryStartMatch uses the full probe, rechecks structure after the provider returns,
and delegates its result to Domain.StartMatch. Failures log a diagnostic and stay
Initializing. Reentrant starts/configuration and repeated starts cannot cause
duplicate transitions. Provider exceptions become failed readiness; state-event
subscriber exceptions preserve normal C# propagation and the committed state.

`Arena_MVP` now has `_GameManager` wired to the actual prefab GameObjects and
Camera. Its autoStart is false and readinessProvider is unassigned. It stays
Initializing in M1, even when structural validation succeeds. Enabling autoStart
without full readiness also fails closed. The future provider must validate real
bot HP 10, player stats, initialized/uncollected pickup and bound initial HUD;
there is no production dummy provider or serialized readiness bypass. Test-only
provider substitutes exercise the successful transition without adding gameplay
components to the production scene.

## Input adapter contract (Task 1.5)

`PlayerInputHandler` references the Input System directly and performs no physics,
damage, pickup or match-state decisions. Its serialized source asset is assigned
through the Inspector or `Configure` before first enable. Each instance clones
the source once and owns its action state. Disable unsubscribes callbacks,
disables the owned Gameplay map and clears movement/all pending press bits;
destroy releases the owned asset. The shared source stays disabled and reusable.

Move is a normalized WASD 2D composite; the adapter also clamps magnitude to one.
Space/J/E use press-only Button actions. Held buttons do not auto-repeat, and
initial-state checks are disabled for buttons so enable/re-enable does not replay
a held key. Value-action movement can resume from a held direction after the next
input update. Consume methods return and clear one pending bit, not a press queue.
Consumers must drain requests each update and reject ineligible actions; cooldown,
grounding, initialization and terminal-match gating remain outside this adapter.

PlayMode input tests use the real project action JSON and Input System's isolated
`InputTestFixture`/simulated keyboard. The existing Input System package is listed
in manifest `testables` so its test framework is available; no package versions
or dependency declarations were changed. Test-only package references do not flow back
into Runtime or Domain.

## Match lifecycle contract (Task 1.2)

`MatchStateMachine` starts at Initializing. Only a ready `MatchReadinessResult`
allows Initializing to Playing, and only Playing permits terminal Victory.
State has no public setter. `IsPlaying` is the action-gating query; this task does
not implement the runtime consumers that will enforce it.

Readiness is an immutable struct. The caller supplies its validation flag and
diagnostic; the domain does not inspect objects or infer readiness from a message.
Default readiness is false, and default/null diagnostics read as an empty string.

Transitions commit state before invoking the local C# `OnStateChanged` event.
Rejected requests emit nothing. Requests made from a notification callback are
rejected synchronously, with no queue, including victory requested during the
Playing notification. This keeps later listeners consistent with the event state.
The caller may request victory after Playing notification completes.

Subscriber exceptions follow normal C# multicast-event behavior: the exception
propagates, subsequent listeners in that invocation are not called, and the
committed state is retained. A `finally` releases the notification guard. Failed
notifications are not replayed. Runtime subscribers must handle their own errors
if delivery to every listener is required. The machine is single-threaded and
does not promise thread safety; it has no engine, input, physics or clock access.

The task's directory diagram includes `_Project/Core/Domain`, while its explicit
assembly path is `_Project/Scripts/Core/Domain`. Both folders exist as requested.
**All domain C# belongs in `Scripts/Core/Domain`**. The other folder is reserved
and contains no C#; putting code there would bypass the domain assembly boundary.

The bootstrap scene is the installed URP template's camera, directional light,
and global volume. It contains no arena, entities, or match behavior. Template
tutorial code and default multi-device Input Actions were removed. URP asset
GUIDs were preserved when moving assets into `_Project/Settings`.

Task 1.4 adds `Arena_MVP.unity` as the enabled startup scene. It uses the same
rendering infrastructure, a fixed overview camera, five static geometry colliders,
and one linked primitive prefab instance each for Player, Bot and WeaponPickup.
The prefabs contain no MonoBehaviours. Camera following, locomotion, Health,
combat, input, collection and full match integration remain for later tasks.
