# Arena configuration contract

| Named layer | Index | Enabled interaction partners |
| --- | ---: | --- |
| Default | 0 (built-in) | Existing built-in/default pairs preserved, plus Environment, Player and Bot; Pickup disabled |
| Environment | 6 | Default, Environment, Player, Bot |
| Player | 7 | Default, Environment, Bot, Pickup |
| Bot | 8 | Default, Environment, Player |
| Pickup | 9 | Player only |

The matrix is symmetric. Player/Player, Bot/Bot and Pickup/Pickup are disabled for
the one-player/one-bot/one-pickup MVP. Custom arena layers ignore unrelated built-in
and unused layers. Pairs between non-arena layers retain the template defaults.

## Pickup trigger contract

The collision matrix enables or ignores a pair; it cannot choose solid contact
versus trigger behavior. **Player/Pickup must remain enabled** for trigger
interaction. Disabling it would also suppress trigger interactions.

`Prefabs/Combat/WeaponPickup.prefab` hiện có trigger BoxCollider và Rigidbody
kinematic không gravity; Player ở `Prefabs/Characters` dùng CharacterController,
không có Rigidbody động. Pickup/Environment, Pickup/Bot và Pickup/Default được
bỏ qua. Hành vi E/range/nhặt một lần/buff đã được PlayMode kiểm chứng sau di trú;
đây là mẫu offline, chưa chứng minh interaction trên mạng.

## Named query masks

Resolve layers by name, never by copying the numeric indices from this document:

- Assignment: `LayerMask.NameToLayer("Player")` (validate before assigning).
- Solid ground/wall queries: `LayerMask.GetMask("Environment", "Default")`.
- Damageable target queries: `LayerMask.GetMask("Bot")`.
- Pickup queries: `LayerMask.GetMask("Pickup")`.

Use explicit trigger-query handling for each query's purpose. A physics collision
matrix does not replace query masks, pickup eligibility or collider configuration.

## Rendering

The default render pipeline is `PC_RPAsset.asset`. The current PC quality level
and Standalone platform select it explicitly. The retained Mobile quality profile
explicitly selects `Mobile_RPAsset.asset`. Both pipelines reference their matching
renderer data under this Settings folder; global URP settings are assigned.
Color space is Linear (`m_ActiveColorSpace: 1`). Existing rendering settings already
satisfy this contract and are verified without unrelated tuning changes.

## Verification

- `tools/Verify-ArenaConfiguration.ps1` parses actual Unity YAML using the
  Editor-bundled YamlDotNet, checks all 1,024 matrix entries and their symmetry,
  and resolves render pipeline/renderer GUIDs. It verifies saved configuration;
  it cannot prove Unity has imported or rendered it.
- `Tests/PlayMode/ArenaConfigurationTests.cs` contains 25 read-only NUnit cases
  checking imported named layers, collision flags, URP bindings and active color
  space. These require a licensed Unity Test Runner.
- No scripts modify settings at runtime. Temporary verification artifacts are
  written only to ignored `TestResults/ArenaConfiguration/`.

Sources: [Unity layer-collision API](https://docs.unity3d.com/ja/current/ScriptReference/Physics.IgnoreLayerCollision.html)
and [active render pipeline selection](https://docs.unity3d.com/6000.1/Documentation/Manual/srp-setting-render-pipeline-asset.html).
