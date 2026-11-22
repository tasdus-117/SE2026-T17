# TV1 ↔ TV3 — Shot intent và authoritative presentation qua Mirror

Ngày thực hiện: 22/11/2026. Nhánh `feat/tv1-network-adapters`, baseline sạch **995e6a47931637fdcb1cd9a90a47a5c377f8f46a** (main sau merge #23), đã chứa TV3 #22 `d5004dc` và M1/M2/M3.1/loot30s/M3.2/Audit F1–F3. Đã đối chiếu GitHub main qua read API, SHA trùng baseline; đã kiểm tra contracts/mã và lịch sử trước sửa, không lấy prototype chưa merge từ nhánh khác. Thay đổi chưa commit/push.

## Điểm tích hợp và authority

```text
Client HeldFireInput → IShotIntentSink / NetworkShotIntentAdapter
    → reliable CmdSubmitShot(ShotIntent)
    → ownership + lifecycle + ShotRequestGate
    → NetworkCombat.PerformShot (server raycast + shared cooldown)
    → NetworkHealth.TakeDamageFrom → MatchSessionRuntime (HP/score/death/feed)

Health/Combat/MatchController SyncVars + scoreboard SyncList
    → NetworkPlayerPresentationAdapter / IPlayerPresentationSource
    → PlayerPresentationBinding → NetworkHealthHUD
```

TV3 yêu cầu đường aim/raycast riêng, **không chuyển SubmitAim sang RequestAttack melee**. PR #22 chưa có ShotIntent wire DTO hoặc server raycast resolver; TV2-004 cung cấp `CombatAim` và model config/state. Vì vậy bổ sung DTO nhỏ và dùng `CombatAim` hiện hữu, không tạo session/health store song song.

Giữ bảy asmdef: Shared.Domain/Shared.Protocol/Networking/Server/Client/EditMode.Tests/PlayMode.Tests. Client.UI là namespace trong Client. Domain/Protocol không engine; Networking không import Server/Client; Server/Client không tham chiếu nhau. TV1 điều phối mạng/server/adapters; TV2 sở hữu luật/config/aim contract; TV3 sở hữu input/camera/presentation/HUD. Không sửa Mirror/vendor/package/asmdef.

## Shot wire và thứ tự kiểm server

`IShotIntentSink.SubmitAim(float worldX, float worldZ)` giữ nguyên API TV3. Adapter chỉ submit khi local player, client active/ready/authenticated, health/life đang được phép và có complete presentation baseline cùng session/life/phase. Public sink normalize/reject zero/NaN/Infinity trước gửi; collector vẫn đọc ray mới mỗi lần và không queue/catch-up/retry.

DTO `Shared/Protocol/ShotIntent`: AimX/AimZ (float), Timestamp (double), Sequence (ulong), LifeId (long): **32 byte trước Mirror headers**. Timestamp dùng Mirror `NetworkTime.predictedTime`, cùng miền clock server ước lượng. **Không gửi origin/position/range/damage/actor ID tự khai**; server dùng netIdentity/connection và pose/config hiện hành. Server muzzle là `position + (0, .25, 0)`; client anchor cùng offset chỉ để dựng aim plane.

Command dùng `Channels.Reliable`, `requiresAuthority=true`; không thêm retry ứng dụng. Server kiểm:

1. Connection authenticated, còn trong NetworkServer, đúng instance owner và identity của player. Mirror cũng kiểm authority; request giả quyền không được nhận feedback từ avatar khác.
2. Bound session, LifeId đúng, participant Alive; health runtime callback xác nhận active binding và phase InProgress/spawn barrier.
3. Sequence lớn hơn số đã xử lý của life hiện hành. Sai life không làm reset counter; life/session mới reset counter, không wrap ulong.
4. Timestamp hữu hạn, không âm; age **≤0,25 s**. Future timestamp hữu hạn được clamp về receive-time server trước tính age, không cấp thêm cooldown/thời gian mô phỏng. Client có thể khai timestamp sai; đây không phải bằng chứng thời điểm vật lý hoặc lag compensation.
5. Cooldown authoritative và valid horizontal aim trước khi commit attack. Invalid aim/expired/wrong-life/replay không tiêu cooldown. Sequence của một request đã xử lý bị tiêu ngay cả khi request bị age/aim/cooldown reject, không thể retry cùng số để biến thành request mới.

Server raycast XZ dùng range từ config, nearest-first, mask Environment(6)/Player(7)/Bot(8), bỏ self và trigger; blocker/actor không hợp lệ gần nhất chặn ray. Origin nằm trong Environment cũng không bắn xuyên. Một ray tối đa một target; damage vào đúng session qua TakeDamageFrom, nên bảo hộ, death idempotence, actual DamageDealt/Kills/Deaths và Kill Feed đi cùng đường M3 hiện hữu. Không tính hit trên client.

**Tuning arena giữ nguyên:** 10 HP, base1/buff2, cooldown1 s. `ServerMatchLifecycleController` hiện cấu hình `ShotRangeMetres=1,8`; adapter dùng đúng giá trị này, không tự nâng lên 30 m theo mặc định model TV2. Server có thể cung cấp config khác cho match sau. Melee J và raycast LMB chia sẻ cooldown NetworkCombat và runtime Domain, không thể xen kẽ để tăng rate.

Trong multiplayer bound, LMB dùng held-fire/aim; `NetworkCombatInput` bỏ mouse punch để tránh collector trùng, vẫn xử lý E và J. Offline và network debug unbound giữ fresh-press melee cũ. Sau countdown/respawn/menu/focus/authority invalidation, phải quan sát release rồi repress theo chính sách TV3; giữ nút qua respawn không tự bắn lại.

Bound aim dùng cursor **None/visible**, kể cả sau Resume: screen-position aim cần con trỏ tự do, không phải cursor cố định giữa màn hình của camera orbit. InGameMenuController theo dõi local health lifecycle để đổi đúng chế độ khi baseline tới. Offline/unbound vẫn Locked/hidden khi Resume như trước; pause/result/menu vẫn mở cursor. Listener được tháo khi despawn/disable.

## Read model và feedback

Snapshot giữ constructor cũ tương thích, thêm optional fields: LifeId, LifeState/IsAlive, Phase, WeaponDamage/BaseDamage/IsWeaponBuffed, CooldownRemainingSeconds, Kills (alias Score), Deaths, DamageDealt, Rank và RespawnRemainingSeconds. Context là object token cục bộ, không phải wire ID/quyền sở hữu.

Provider chỉ có Current khi Health và MatchController cùng session/phase và scoreboard row khớp LifeId/life state; scoreboard được thêm LifeId để tránh ghép baseline của hai mạng sống. HP/max từ NetworkHealth; buff/deadline từ NetworkCombat; stats/rank/winner/clock từ replicated MatchController. Clock/cooldown là projection từ authoritative sample/deadline, không khởi động timer hoặc HP dự đoán mới trên client.

Provider publish null khi invalidated/disable/despawn/authority change; tạo Context mới sau baseline/life/phase replacement, Revision tăng trong Context, không wrap. Held-fire nhận Invalidated từ lifecycle/authority bridge; server health cũng phát notification sau PublishRuntime để Host/dedicated lifecycle được reset cùng quy tắc với replica.

SessionId có SyncVar hook riêng: thay session trên cùng avatar và LifeId vẫn bằng 1 cũng xóa context/feedback/counter đồng bộ, không chỉ dựa vào destroy ở Lobby. Provider cũng xóa baseline khi NetworkMatchController instance/session bị thay. Native regression kiểm trường hợp này bằng ResetForLobby/BeginSession thật rồi bắn sequence mới.

TargetRpc feedback chỉ cho owner, gắn session/life/sequence; không nhận reply đời cũ, không hiện rejection cũ sau chết/Ended/Lobby. Accepted request tiếp theo xóa rejection. Feedback không chứng nhận hit (miss/block cũng là accepted shot). Provider remote không hiển thị feedback riêng của owner khác.

NetworkHealthHUD bind PlayerPresentationBinding thật, thêm label HP/K/D/Damage/Rank/phase/clock/life/cooldown/respawn/rejection. HP bar và result/scoreboard M3.2 vẫn dùng nguồn authoritative hiện hữu; không tạo HUD fake hoặc điều khiển state từ UI. Unbind xóa read model/text và gỡ listener.

## Prefab và offline

- Player.prefab thêm NetworkShotIntentAdapter, NetworkPlayerPresentationAdapter, HeldFireInput; references input/sink/muzzle đầy đủ.
- Child `ShotMuzzle`: local `(0,.25,0)`, **Default layer**, không renderer/collider/logic. Root Player vẫn Layer7 và collider cũ. Default layer tránh bị các check role đếm nhầm anchor thành Player thứ hai.
- Arena_MVP giữ offline removed-component overrides, bổ sung gỡ cả ba component mới; helper OfflinePlayerFixture làm cùng thứ tự. Không chạy NetworkBehaviour thiếu identity trong single-player.
- NetworkHUD.prefab thêm label non-raycast; helper tái tạo `tools/Build-NetworkAdapters.cs`. ArenaNetworkRoot/config/scene build order không đổi.
- Layout verifier kiểm chính xác một root và anchor này, offset/ownership/component references và offline overrides; vẫn kiểm collider/layer/spawn/GUID, không bỏ gate vì cấu trúc mới.

## Kiểm chứng

Domain runner: **261/261 đạt** (20 test mới). Native focused trước bước siết client aim: **65/65 đạt**, gồm 8 integration test mới, TV3 presentation/input và legacy combat/HUD. Full-run đầu **409 total, 406 passed, 1 failed, 2 vendor skipped**: duy nhất ArenaLayoutTests đếm anchor Layer7 thành Player thứ hai. Đã chuyển anchor sang Default, giữ nguyên assertion entity count; đồng thời cập nhật layout verifier hiểu root/anchor và public sink reject malformed aim trước gửi. Raw-command test vẫn cố ý bypass local guard qua woven client proxy và đi qua Mirror transport thật.

Nguồn cuối được chạy full native ngày **22/11/2026**, Unity **6000.6.8f1**, hai invocation đều **exit 0**, XML gate đạt:

| Gate | Kết quả |
|---|---|
| EditMode native | **261/261 passed**, 0 failed/skipped |
| PlayMode project | **406/406 passed**, 0 failed/skipped |
| PlayMode gồm package | **410 total: 408 passed, 0 failed, 2 vendor skipped** |
| Adapter tests mới | **20/20 EditMode + 9/9 PlayMode**, đã nằm trong full totals |
| Strict compilation | **7/7 asmdef**, 0 project compiler warnings/errors; native compiler/weaver diagnostics 0 |
| Scaffold | Graph không vòng, **1.527 cặp metadata/GUID**, scene build order 0/1/2 đúng |
| Layout | **4.627 assertions / 18 YAML**, gồm muzzle/prefab refs và offline overrides |
| Domain runner riêng | **261/261 passed**, không thay thế native |

Hai vendor skip là Input System mouse Windows/RDP, lý do `Unstable due to 1252825`; không tính skip thành pass. XML cuối và summary.json ghi chính xác UTC start/end ngày 22/11. Không có test nhóm bị ignore/skip để đạt gate. Manifest **288 file source/asset/meta** khớp bản native; **48 file package/settings** giữ nguyên hash.

Raw artifacts (ignored): `TestResults/NetworkAdapters/{Focused.xml,PlayMode-initial.xml,playmode-results.xml,editmode-results.xml,summary.json,Compilation,Domain.log,scaffold.txt,layout.txt,tested-source-hashes.json,preserved.json,remote-main.json}`; log `Logs/NetworkAdapters-*`. Bản sao ấm `TestResults/TV11/ValidationProject` không dùng chung project với Editor người dùng. Hash nguồn/asset/meta được so với bản native; package/ProjectSettings/URP giữ nguyên. Không stage/commit/push hoặc thay main.

Coverage mới:

- EditMode: monotonic sequence, duplicate/zero/overflow, life/session reset, old-life không rollback; age .249/.250/.251, future clamp và timestamp lỗi; snapshot Alive/Respawning/buffed/Ended Victory/Defeat và invalid cooldown.
- PlayMode: client public aim guard + server invalid/expired/wrong-life/replay/cooldown, foreign owner/remote sink; held LMB hướng ngang khác +Z melee và cursor/Resume; wall/range; HP/buff/clock/score/winner/loser; respawn context/feedback/counter reset; raw Countdown Command; cùng avatar/session mới LifeId=1; Lobby teardown và round hai context/score mới.
- Các assertion dùng server/client object khác nhau, actual Commands/SyncVars/SyncList và HUD binding thật. Không chỉ kiểm flag hoặc mirror private implementation.

Chạy lại qua Unity Test Runner Run All hoặc [CLI README](../../README.md#5-kiểm-thử-và-biên-dịch). Filter riêng:

```text
-testPlatform EditMode -testFilter SmallArena.Tests.EditMode.NetworkAdaptersUnitTests
-testPlatform PlayMode -testFilter "SmallArena.Tests.PlayMode.NetworkShotAndPresentationIntegrationTests;SmallArena.Tests.PlayMode.NetworkPresentationLobbyResetTests"
```

Đây là arguments cho Unity.exe, không phải executable riêng. Không dùng -quit cùng -runTests; full regression mới là cổng bàn giao.

## Inventory

| Phần | File dưới Assets/_Project/ nếu không ghi khác |
|---|---|
| Mới Domain / Protocol | Scripts/Shared/Domain/ShotRequestGate.cs; Scripts/Shared/Protocol/ShotIntent.cs; mỗi file có .meta |
| Mới Networking | Scripts/Networking/NetworkShotIntentAdapter.cs; NetworkPlayerPresentationAdapter.cs; mỗi file có .meta |
| Sửa Networking | NetworkCombat.cs, NetworkHealth.cs, NetworkMatchController.cs |
| Sửa contracts/client | Shared/Domain/PlayerPresentationSnapshot.cs; Client/Input/HeldFireInput.cs, NetworkCombatInput.cs, PlayerInputHandler.cs; Client/UI/NetworkHealthHUD.cs, InGameMenuController.cs |
| Asset | Prefabs/Characters/Player.prefab, Prefabs/UI/NetworkHUD.prefab; Scenes/Arena_MVP.unity (chỉ offline component overrides) |
| Test mới | Tests/EditMode/NetworkAdaptersUnitTests.cs; Tests/PlayMode/NetworkShotAndPresentationIntegrationTests.cs; mỗi file có .meta |
| Fixture | Tests/PlayMode/MultiClientIntegrationTests.cs, OfflinePlayerFixture.cs |
| Tools/docs | tools/Run-DomainTests.ps1, ArenaLayoutVerifier.cs; thêm tools/Build-NetworkAdapters.cs; README.md, báo cáo này và PROJECT_CONTEXT.md cục bộ tiếng Việt |

## Manual hai client

1. Build lại cả hai phía cùng protocol/prefab (ShotIntent + scoreboard LifeId mới). MainMenu Host/Join → Ready → Start. Con trỏ phải tự do/hiện, kể cả sau Escape → Resume. Trong countdown nhả LMB; sau FIGHT ngắm người kia bằng chuột, giữ LMB: mỗi 1 s một shot. Range mặc định 1,8 m từ muzzle tới collider, không phải súng 30 m.
2. Ngắm theo ±X/±Z, không cần quay avatar. Mục tiêu trong reach mất 1 HP; tường/cover chặn ray. Click UI/menu không bắn. J vẫn melee cố định +Z; xen J/LMB không vượt cooldown chung.
3. E nhặt hộp: damage2, HP đối thủ giảm2; quan sát HP, cooldown, K/D/actual Damage, Rank và clock ở HUD/Tab. Kill hiện feed và stats server; đạt target kills/hết giờ vẫn Victory/Defeat theo M3.2.
4. Carrier chết: buff mất, HP0/Respawning và countdown đúng. Respawn HP10/base1, không còn rejection/life effects cũ; nhả rồi giữ LMB lại. Loot hồi sau30 s như trước.
5. Ended → 10 s hoặc Host Return to Lobby: snapshot/label cũ bị clear; Ready=false. Ready/Start round hai: context mới, score0/base1, không có feedback cũ. Leave Match vẫn teardown đúng.

Native harness xác minh client engine thật + peer/observer ảo cùng tiến trình. Không tuyên bố đã chạy hai executable KCP/LAN, visual QA, lag compensation hoặc retained-session reconnect trong nhiệm vụ này.
