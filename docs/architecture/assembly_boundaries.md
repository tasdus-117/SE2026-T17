# Assembly boundaries

Task 1.1 created four project assemblies. Task 1.2 adds pure match-state rules in
Domain and their NUnit fixture in EditMode.Tests. Runtime and PlayMode.Tests
still contain assembly identity attributes only; no scene components are added.

| Assembly | Allowed assembly references | Engine access |
| --- | --- | --- |
| SmallArena.Domain | None | Disabled (`noEngineReferences: true`) |
| SmallArena.Runtime | SmallArena.Domain, Unity.InputSystem | Enabled |
| SmallArena.EditMode.Tests | Domain, Runtime, UnityEngine.TestRunner, UnityEditor.TestRunner | Editor only |
| SmallArena.PlayMode.Tests | Domain, Runtime, UnityEngine.TestRunner, UnityEditor.TestRunner | Editor PlayMode; test assembly |

Both test assemblies explicitly reference `nunit.framework.dll`, use
`overrideReferences: true`, and retain `optionalUnityReferences: ["TestAssemblies"]`.
They are test-only assemblies, not shipping gameplay dependencies. Device/player
test builds are outside this task's verification scope.

Domain also overrides automatic precompiled references with an empty list to
prevent implicit third-party plugin dependencies. Runtime must never reference
either test assembly. Use scene/prefab references and local instance events later;
do not introduce global singleton, service-locator, or event-bus dependencies.

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
combat, input, collection and match lifecycle wiring remain for later tasks.
