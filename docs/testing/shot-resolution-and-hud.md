# TV2-004 / PR #22: server raycast, phản hồi phát bắn và HUD

Baseline `1c745c8` (local main sau PR #36/revert Phase 3), nhánh
`feat/tv1-complete-tv2-tv3-integration`. Không sửa workflow hoặc khôi phục scripts
multi-process Phase 3. Giữ scene, Player prefab, settings và gameplay offline/melee.

## Kiểm toán và kiến trúc

HEAD đã có `ShotIntent`, `NetworkShotIntentAdapter` và server raycast trong
`NetworkCombat`. `ShotRequestPayload` / `ShotResultPayload` trong hồ sơ TV2-004 là
logical contract đề xuất, chưa phải kiểu C# production. Giữ request direction-only
hiện hữu; mở rộng reliable `TargetFeedback` với outcome, không nhận origin, target,
damage hoặc hit từ client. TargetRpc chỉ trả cho shooter, thay vì broadcast kết quả
riêng tư tới mọi observer. Không khẳng định thay thế toàn bộ các CT/reconnect proposal.

`GameplayConfiguration` → `NetworkHealth.PublishRuntime` → range/base damage/cooldown
đã được nối trước nhiệm vụ. Arena giữ **10 HP, base 1/buff 2 DMG, cooldown 1 giây,
range 1,8 m**. Không đổi sang defaults 100/25/30 m của constructor Domain; server
có thể cấu hình khác cho match mới.

Giữ **7 asmdef**: Shared.Domain, Shared.Protocol, Networking, Server, Client,
EditMode.Tests, PlayMode.Tests; `Client.UI` là namespace. Hai Shared engine-free.
Domain chứa so sánh khoảng cách/feedback thuần C#; physics/owner/RPC ở Networking,
runtime match ở Server, view ở Client; không thêm tham chiếu ngược.

## Luồng authority và feedback

1. Held-fire TV3 gửi hướng XZ qua `IShotIntentSink`/reliable Command hiện hữu. Giữ
   các guard authentication/owner, session/life, phase/barrier, sequence, age 0,25s,
   aim và cooldown. Không có client prediction hay retry/catch-up application loop.
2. Origin do server tính `transform.position + (0, .25, 0)`, khớp ShotMuzzle của
   capsule có pivot ở tâm. Ray dùng range config, mask Environment **6**, Player **7**,
   Bot **8**, bỏ trigger và mọi collider thuộc shooter. ArenaObstacles dựng cover
   trên Environment. Check sphere nhỏ tại origin xử lý ray bắt đầu bên trong cover.
3. Chọn target Alive/cùng session/LifeId hợp lệ gần nhất và blocker gần nhất.
   `ShotHitRules.Resolve` ưu tiên cover nếu bằng khoảng cách. Collider actor không
   đủ điều kiện cũng chặn ray. Chỉ target đầu tiên được damage, không xuyên tường.
4. `TakeDamageFrom` giữ đường runtime M3: bảo hộ spawn, actual damage, HP/death,
   kills/deaths và Kill Feed. Nếu runtime từ chối damage thì không báo Hit.
   Miss/Blocked vẫn dùng cooldown của một phát hợp lệ.
5. Reliable TargetRpc trả session, LifeId, sequence, outcome **Hit / Blocked / Missed /
   Rejected** và `ShotRejection` khi bị từ chối. `ShotFeedbackState` hết hạn **1,5s**
   mà không reset watermark; xóa khi chết/respawn, life/session/phase đổi, Ended,
   baseline mất, disable hoặc Lobby reset. Kết quả life/session cũ bị bỏ.

## HUD và prefab

Thêm optional `ShotOutcome` cuối constructor `PlayerPresentationSnapshot` để giữ
tương thích consumer cũ. `IPlayerPresentationSource` và binding tiếp tục cập nhật.
`NetworkHealthHUD` không in lại HP/K/D/rank/phase/timer/cooldown thành hai dòng debug.
Label PlayerPresentation chỉ hiện `Hit`, `Missed`, `Shot blocked by cover` hoặc
`Shot rejected: <reason>` trong InProgress, mặc định rỗng. NetworkHUD.prefab đặt
anchor/pivot bottom-left, offset **(32,32)**, size **480×40**, font 22, không raycast UI;
không trùng HP trên-trái, clock trên-giữa, controls/feed trên-phải hoặc Tab scoreboard.
Helper `tools/Build-NetworkAdapters.cs` tạo lại cùng layout, không sinh overlay cũ.

## Kiểm chứng

Tệp thay đổi (các đường dẫn script bắt đầu từ `Assets/_Project/`):

| Nhóm | Tệp |
|---|---|
| Domain | `Scripts/Shared/Domain/ShotResolution.cs` (mới + `.meta`); `Scripts/Shared/Domain/PlayerPresentationSnapshot.cs` |
| Networking | `Scripts/Networking/NetworkCombat.cs`, `NetworkShotIntentAdapter.cs`, `NetworkPlayerPresentationAdapter.cs` |
| UI | `Scripts/Client/UI/NetworkHealthHUD.cs`; `Prefabs/UI/NetworkHUD.prefab` |
| EditMode | `Tests/EditMode/ShotResolutionAndPresentationCleanupTests.cs` (mới + `.meta`) |
| PlayMode | `Tests/PlayMode/NetworkRaycastOcclusionAndHudTests.cs` (mới + `.meta`); thêm assertions trong `NetworkShotAndPresentationIntegrationTests.cs` |
| Công cụ | `tools/Build-NetworkAdapters.cs`, `tools/Run-DomainTests.ps1` |
| Tài liệu | Tệp này, `docs/testing/network-adapters-tv3.md`; `PROJECT_CONTEXT.md` cục bộ, tiếp tục ignore |

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Compile-UnityAssemblies.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-DomainTests.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-Tv31Unity.ps1 -Phase EditMode
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-Tv31Unity.ps1 -Phase PlayMode
# Nhóm Mirror mới:
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-Tv31Unity.ps1 -Phase PlayMode -TestFilter SmallArena.Tests.PlayMode.NetworkRaycastOcclusionAndHudTests
```

EditMode mới **16 cases**: target/cover/range/bằng nhau, feedback snapshot, expiry,
watermark và life/death/end/session/Lobby reset. Domain runner đã thêm suite mới.
PlayMode mới **6 cases**: năm ca trên loopback Mirror thực với hai connection, client replica
tách server và prefab production: cover-before-target, target-before-cover/self-ignore,
origin trong cover, range/cooldown/miss/expiry, config damage/score, respawn/Lobby
clear, anchor/text HUD; một ca qua production Lobby Host + virtual peer vào scene
Arena_MVP, chọn generated cover từ ArenaObstacles rồi xác nhận ray bị chặn.
Không ghi HP giả lên client để tạo kết quả. Test Ended hiện hữu cũng thêm assertion
xóa outcome/rejection ngay khi bảng xếp hạng chuyển Victory/Defeat.

- Strict Roslyn: **7/7**, **0 project warnings/errors**; Domain engine-free.
- Scaffold: **1.530** cặp metadata/GUID, graph không vòng, scene order đúng.
- Domain runner: **277 passed, 0 failed, 0 skipped**.
- Native EditMode Unity **6000.6.8f1**: **277 passed, 0 failed, 0 skipped**, exit 0.
- Focused native PlayMode: **5/5 passed**, saved-Arena test riêng **1/1 passed**, exit 0.
- Full native PlayMode lượt đầu: **415 total, 413 passed, 0 failed, 2 vendor skips**.
  Full suite cuối sau khi thêm saved-Arena case và assertion Ended:
  **416 total, 414 passed (412 project + 2 vendor), 0 failed, 0 inconclusive,
  2 vendor skips**, exit **0**; cả **6/6** ca mới đạt. Hai skip vẫn là
  `WindowsInput_MouseMovements_AreDetected` và
  `WindowsInput_RemoteDesktopMouseMovements_AreDetected` của Input System.
- XML validator chặt hiện hữu chấp nhận EditMode minimum **277** và PlayMode minimum
  **414**. Native logs không có C# warning/error; headless shader/hardware diagnostics
  không phải project compiler warnings. Không suppress hoặc hạ test threshold.

Logs/XML cuối: `TestResults/ShotIntegration/EditMode.xml`, `PlayModeFinal.xml`,
`FocusedRetry.xml`, `ArenaFocused.xml`; strict compiler ở `CompilationFinal/results.json`,
XML gate copies ở `ValidatedReports/`. Domain results ở
`TestResults/Domain/`. Các bằng chứng tạm bị ignore. Lượt đầu sandbox bị lỗi IPC
licensing; dừng đúng worker đó và retry ngoài sandbox mới chạy native thành công.
Runner đã tự dọn InitTestScene; khôi phục riêng line endings ProjectSettings sau
khi xác nhận nội dung chuẩn hóa trùng HEAD, không sửa setting. Chưa chạy standalone
hai process/KCP/LAN hoặc kiểm visual GUI thủ công trong nhiệm vụ này; checklist dưới
đây dành cho review bằng hai client dùng cùng build. Thay RPC signature yêu cầu
client/server build đồng bộ. Các kiểm chứng này không thay review CT liên thành viên.

## Checklist thủ công 2 client

1. Dùng cùng bản build cho hai client. MainMenu: Host + Join localhost, cả hai Ready,
   Host Start; chờ InProgress.
2. Đứng hai phía cover/wall trong range; LMB hướng target: HP không giảm, shooter
   thấy `Shot blocked by cover`; target không nhận feedback của shooter.
3. Ra khỏi cover, trong range 1,8 m: mỗi phát hợp lệ giảm **1 HP**, có `Hit` và
   DamageDealt tăng. Giữ LMB tuân cooldown **1 giây**; quá range báo Missed.
4. Nhặt buff E: damage **2**, giữ race guard/loot 30 giây sau carrier death.
5. HP bar/clock/Tab giữ nguyên; không còn hai dòng debug giữa-trái. Feedback góc
   dưới-trái tự biến mất sau 1,5 giây.
6. Chết/respawn xóa feedback cũ, HP/life mới vẫn cập nhật. Ended xóa feedback;
   về Lobby/rematch không còn result/rejection vòng trước.
