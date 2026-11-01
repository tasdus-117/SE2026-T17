# Assembly boundaries

Task 1.1 creates four project assemblies, with assembly identity attributes only.
There are no gameplay components, test cases, or domain rules yet.

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

The task's directory diagram includes `_Project/Core/Domain`, while its explicit
assembly path is `_Project/Scripts/Core/Domain`. Both folders exist as requested.
**All domain C# belongs in `Scripts/Core/Domain`**. The other folder is reserved
and contains no C#; putting code there would bypass the domain assembly boundary.

The bootstrap scene is the installed URP template's camera, directional light,
and global volume. It contains no arena, entities, or match behavior. Template
tutorial code and default multi-device Input Actions were removed. URP asset
GUIDs were preserved when moving assets into `_Project/Settings`.
