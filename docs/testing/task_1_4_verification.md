# Task 1.4 - Bounded placeholder arena and entity prefabs

Date: 2026-11-02 (Asia/Saigon). Starting commit: `7b205c6`.
Assets and supporting changes remain uncommitted for Lead Engineer / PM review.

## Delivered assets

| Asset | Configuration |
| --- | --- |
| `Assets/_Project/Scenes/Arena_MVP.unity` | Enabled startup scene; one linked Player, Bot and WeaponPickup prefab instance; fixed overview camera, URP directional light and global volume |
| `Prefabs/Player.prefab` | Blue built-in capsule; Player layer 7; height 2m, radius 0.5m; one CharacterController; no Rigidbody or duplicate capsule collider |
| `Prefabs/Bot.prefab` | Red built-in capsule; Bot layer 8; solid static CapsuleCollider; no Rigidbody, AI, Health or other scripts |
| `Prefabs/WeaponPickup.prefab` | Gold cube, uniform scale 0.6; Pickup layer 9; enabled trigger BoxCollider; kinematic Rigidbody with gravity disabled |
| `Art/Materials/*.mat` | Five project-owned URP Lit materials: PlayerBlue, BotRed, PickupGold, FloorSlate, WallGray |
| `Tests/PlayMode/ArenaLayoutTests.cs` | 22 native-engine NUnit cases, with isolated scene loading and teardown |

All asset paths in the table after the scene are relative to `Assets/_Project/`.
All ten new Assets files have matching `.meta` sidecars. No gameplay or input
MonoBehaviour is attached to any prefab. The scene retains only the template's
URP camera/light/volume adapters as rendering infrastructure.

CharacterController configuration: slope limit 45, step offset 0.3, skin width
0.08, min move distance 0.001. Player and Bot roots retain unit scale and fixed
identity rotation. No movement, combat, Health, pickup interaction or lifecycle
coordinator is implemented by this task.

## Geometry and spawn report

The floor's top is Y=0. Its box is centered at `(0,-0.5,0)` with dimensions
`(16,1,16)`, yielding exactly `[-8,8]` in X and Z.

| Geometry | Center | Dimensions | Layer |
| --- | --- | --- | --- |
| Floor | `(0,-0.5,0)` | `(16,1,16)` | Environment / 6 |
| WallNorth | `(0,1.5,8.25)` | `(17,3,0.5)` | Environment / 6 |
| WallSouth | `(0,1.5,-8.25)` | `(17,3,0.5)` | Environment / 6 |
| WallEast | `(8.25,1.5,0)` | `(0.5,3,17)` | Environment / 6 |
| WallWest | `(-8.25,1.5,0)` | `(0.5,3,17)` | Environment / 6 |

Wall inner faces sit at the floor perimeter. Adjacent walls overlap at all four
corners, extend from Y=0 to Y=3, and preserve the full 16m square playable surface.
Three metres exceeds the proposed 1.2m jump plus current step/skin allowance;
actual repeated walking/jumping containment remains a later M2 verification.

| Entity instance | Position | Geometric collider bottom |
| --- | --- | --- |
| Player | `(0,1,-4)` | Y=0 |
| Bot | `(0,1,4)` | Y=0 |
| WeaponPickup | `(-3,0.3,0)` | Y=0 |

The pickup uses Y=0.3 because its cube is 0.6m high. All three collider volumes
are strictly inside the perimeter, supported geometrically by the floor, and
separated from each other and all walls. Entity roots use their dedicated layers;
the supporting floor and boundary geometry use Environment.

## Executed verification

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Verify-ArenaLayout.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Verify-ArenaConfiguration.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Verify-Scaffold.ps1
```

| Verification | Actual result |
| --- | --- |
| Saved layout assertions | **584 passed**, inspecting 11 Unity YAML files (scene, three prefabs, five materials, layer settings and build settings) |
| Asset/reference integrity | Passed: local fileIDs, project/package GUID availability, unique metadata, component ownership, root/child links, three linked prefab sources, allowed prefab component sets and intended material assignments |
| Geometry | Passed: 16x16 floor, 3m walls, corner overlap, bounded and flush spawns, no entity/wall overlap |
| PlayMode source compilation | Passed against installed Unity/NUnit APIs using C# 9 and warnings as errors; includes both configuration and layout fixtures |
| Existing saved physics/render configuration | **2,321 assertions passed**; 32 Unity YAML files plus the existing JSON XR settings file parsed; matrix/URP settings preserved |
| Scaffold/metadata | Passed: four asmdefs, 66 asset/folder metadata pairs, unique GUIDs and existing 24 requirement mappings |
| Git whitespace check | `git diff --check` passed |
| Unity import / native layout tests | **Blocked**: fresh batch-mode attempt exited **198** with `No valid Unity Editor license found. Please activate your license.` |

The saved-file checks use the Editor-bundled YamlDotNet parser and independent
bounding-box calculations. Unity's header tags and `stripped` markers are
normalized only in parser input; on-disk files retain Unity serialization.
These checks establish saved asset structure, not successful Unity import,
native collision behavior, visible rendering or a passing Windows build.

## Native PlayMode case catalogue

The following **22 cases compile but have not executed**:

| Assertion group | Cases |
| --- | ---: |
| Scene load and absence of missing components | 1 |
| Exactly one entity per role with matching named layer | 3 |
| Solid 16x16 floor with surface Y=0 | 1 |
| Solid Environment walls, at least 3m high, outside the playable area | 4 |
| Exactly floor plus four wall colliders and closed corners | 1 |
| Player CharacterController configuration with no Rigidbody/duplicate collider | 1 |
| Bot static capsule collider | 1 |
| Pickup trigger and kinematic/no-gravity body | 1 |
| Structural-only components on each entity | 3 |
| Correct, bounded, flush spawns and floor-support raycasts | 3 |
| No spawn-to-spawn or spawn-to-wall overlap | 1 |
| Distinct URP Lit entity materials | 1 |
| Camera and directional light presence | 1 |

After activating a valid Editor license, execute
`SmallArena.Tests.PlayMode.ArenaLayoutTests` using Unity 6000.6.7f1. The fixture
loads `Arena_MVP` independently for each case and removes it during teardown.
The structural-only tests intentionally describe this milestone and require
review when later tasks add gameplay components.

## Evidence and acceptance limits

- `TestResults/ArenaLayout/results.json` and `results.log`: saved-file assertions.
- `TestResults/ArenaLayout/PlayMode.compile.log`: compile-only diagnostics.
- `TestResults/ArenaLayout/Unity-playmode.log`: attempted native run and license failure.
- No native NUnit result XML was produced; no native case is marked Passed.
- Generated diagnostics, binaries and temporary authoring files remain ignored.

This contributes to FR-002 / AC-002 and structural portions of TC-001 / TC-004.
TC-001 still needs real initialization, HP/stats/pickup/HUD readiness and Playing.
TC-004 still needs actual locomotion/jump containment tests. Neither full SRS case
is claimed complete. Asset import and native verification remain an acceptance
gate; the existing license blocker is not waived.

## Diff summary

Added three prefabs, five materials, the arena scene, one layout fixture and all
matching metadata, plus saved-file verification tools and this report. Updated
startup scene settings, the scaffold scene assertion, folder descriptions, setup,
architecture and traceability documentation. Existing domain code, asmdefs,
packages, layer/collision policy and URP pipeline assets were not modified.

## Working tree status

```text
 M Assets/_Project/Art/Materials/README.md
 M Assets/_Project/Prefabs/README.md
 M ProjectSettings/EditorBuildSettings.asset
 M ProjectSettings/ProjectSettings.asset
 M docs/architecture/assembly_boundaries.md
 M docs/setup.md
 M docs/testing/traceability_matrix.md
 M tools/Verify-Scaffold.ps1
?? Assets/_Project/Art/Materials/BotRed.mat
?? Assets/_Project/Art/Materials/BotRed.mat.meta
?? Assets/_Project/Art/Materials/FloorSlate.mat
?? Assets/_Project/Art/Materials/FloorSlate.mat.meta
?? Assets/_Project/Art/Materials/PickupGold.mat
?? Assets/_Project/Art/Materials/PickupGold.mat.meta
?? Assets/_Project/Art/Materials/PlayerBlue.mat
?? Assets/_Project/Art/Materials/PlayerBlue.mat.meta
?? Assets/_Project/Art/Materials/WallGray.mat
?? Assets/_Project/Art/Materials/WallGray.mat.meta
?? Assets/_Project/Prefabs/Bot.prefab
?? Assets/_Project/Prefabs/Bot.prefab.meta
?? Assets/_Project/Prefabs/Player.prefab
?? Assets/_Project/Prefabs/Player.prefab.meta
?? Assets/_Project/Prefabs/WeaponPickup.prefab
?? Assets/_Project/Prefabs/WeaponPickup.prefab.meta
?? Assets/_Project/Scenes/Arena_MVP.unity
?? Assets/_Project/Scenes/Arena_MVP.unity.meta
?? Assets/_Project/Tests/PlayMode/ArenaLayoutTests.cs
?? Assets/_Project/Tests/PlayMode/ArenaLayoutTests.cs.meta
?? docs/testing/task_1_4_verification.md
?? tools/ArenaLayoutVerifier.cs
?? tools/Verify-ArenaLayout.ps1
```
