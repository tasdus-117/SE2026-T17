# M2 TV1 — kết quả phòng chờ cơ bản (2026-11-06)

## Kết luận

Đã kích hoạt Multiplayer trên MainMenu, hoàn thành Host/Join bằng địa chỉ IP, phòng tối đa bốn người, Ready và Start do server kiểm soát, cùng cleanup khi rời phòng. Single Player và các hồi quy M1/offline vẫn đạt. Kiểm chứng bằng **Unity 6000.6.8f1** trong bản sao `TestResults/TV11/ValidationProject`; không dùng debug NetworkManagerHUD cho luồng menu mới.

## Kết quả thực thi

| Phạm vi | Tổng | Đạt | Lỗi | Bỏ qua |
| --- | ---: | ---: | ---: | ---: |
| PlayMode native đầy đủ | 228 | 226 | 0 | 2 |
| SmallArena.PlayMode.Tests | 224 | 224 | 0 | 0 |
| LobbyRoomManagementTests mới | 12 | 12 | 0 | 0 |
| EditMode native của nhóm | 34 | 34 | 0 | 0 |

Hai skip vẫn thuộc package Input System: WindowsInput_MouseMovements_AreDetected và WindowsInput_RemoteDesktopMouseMovements_AreDetected, lý do “Unstable due to 1252825”. Không tính skip thành pass. Các test M1 và mẫu offline đều được chạy lại trong tổng 224 ca nhóm; không cộng trùng các dòng trong bảng. Cả hai Test Runner kết thúc mã **0**.

```text
PlayMode: total=228 passed=226 failed=0 skipped=2
Project PlayMode: 224/224 passed
LobbyRoomManagementTests: 12/12 passed
EditMode: total=34 passed=34 failed=0 skipped=0
Test run completed. Exiting with code 0 (Ok). Run completed.
```

Bằng chứng: `TestResults/M2/PlayMode.xml`, `EditMode.xml`, `summary.json`; log `Logs/M2-PlayMode.log`, `Logs/M2-EditMode.log`. Nguồn/test/scene/prefab/Input Actions và metadata có **169 tệp** khớp hash với bản sao thực thi, lưu tại `TestResults/M2/tested-source-hashes.json`.

## Bao phủ của 12 ca mới

- Nút Multiplayer đã bật, mở ConnectionPanel có địa chỉ localhost và Host/Join; Back trả về menu mà không mở kết nối.
- Host Room tạo host và một thành viên Not Ready, giữ MainMenu, không spawn avatar trong lobby.
- Bốn connection được chấp nhận, connection thứ năm bị NetworkServer từ chối; lớp luật LobbyRoom cũng từ chối vượt giới hạn. Log Error “Server full” chuẩn của Mirror được kỳ vọng rõ bằng LogAssert.Expect, không phải lỗi bất ngờ hoặc cảnh báo bị che.
- Disconnect giải phóng slot, thành viên thay thế bắt đầu Not Ready, không giữ dữ liệu người cũ.
- Host chưa được Start nếu có người chưa Ready; remote dù Ready cũng không có quyền Start.
- Host/all-ready chuyển sang Arena_MVP, marker đăng ký đủ bốn, player chỉ spawn sau khi server tải scene xong.
- Request AddPlayer trong lobby không vượt server gate.
- Leave rồi Host lại không giữ roster/ready cũ.
- Join trim địa chỉ, Cancel/Back dọn client; kết nối lỗi có trạng thái thân thiện và nút được bật lại.
- Dừng phiên ở arena rồi trở về menu có manager mới sạch, không singleton trùng, có thể Host lại.
- Luật server bỏ Ready lặp/ID không tồn tại và khóa các thay đổi không hợp lệ sau Start.

## Standalone Windows và KCP thật

Build **Windows x64 Development/Mono** thành công: **0 lỗi**, kích thước báo cáo **219.001.478 byte**. Build có **520 cảnh báo từ vendor/shader/analyzer**, gồm kernel ConvGeneric của AI inference và mã Mirror; không có chẩn đoán cảnh báo mã nhóm tại Assets/_Project trong log kiểm chứng. Không sửa hoặc gỡ package người dùng để che cảnh báo. Vì vậy không tuyên bố toàn bộ Unity build có 0 warning.

Hai player độc lập được chạy trên loopback KCP port tạm **55478**. Instrument kiểm chứng kích hoạt đúng Unity Button của menu: Multiplayer → Host Room/Join Room → Ready → Start Match. Cả host và client báo:

```text
scene=Arena_MVP; success=true; openedFromMenu=true; noDebugHud=true
roomMembers=2; visiblePlayers=2; captured errors=[]
host localPlayer=1; client localPlayer=2; cả hai process exit=0
```

Bằng chứng tại `TestResults/M2/StandaloneSmoke/a381c9662f52471082a03226a5477b24/`: host.json, client.json, summary.json và log từng process. Build tại `TestResults/M2/Standalone/SmallArenaM2.exe`, báo cáo `build-result.txt`; log Editor `Logs/M2-StandaloneBuild.log`.

Smoke dùng source instrument chỉ chép vào Client của bản sao với define `SMALL_ARENA_M2_SMOKE`; repository Assets không chứa worker này. Không có dependency player/CLI trong các PlayMode test. Build không truyền flag smoke vẫn mở MainMenu bình thường. Công cụ cleanup chỉ đóng process do chính runner tạo. Kết quả smoke hai process không được cộng vào số NUnit ở trên và không chứng nhận bốn process thật, LAN/firewall/NAT hoặc độ trễ lớn.

Để tái hiện build kiểm chứng, chép `tools/Build-LobbyStandalone.cs` vào Assets/Editor của bản sao và `tools/LobbyStandaloneSmoke.cs` vào Scripts/Client của bản sao; đặt `SMALL_ARENA_M2_BUILD` thành đường dẫn output rồi gọi native Editor `-executeMethod SmallArena.Tools.LobbyStandaloneBuilder.Build -quit`. Khi có executable:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-LobbyStandaloneSmoke.ps1
```

## Biên dịch, tài nguyên và diff

- **7/7 asmdef nhóm biên dịch với 0 warning/error** bằng Roslyn strict, warnings-as-errors; native Unity compilation/Mirror weaving không lỗi. Source smoke cũng qua kiểm tra API độc lập và build player thực tế. Báo cáo tại `TestResults/M2/Compilation/results.json` và `ClientSmokeApi.rsp`.
- Scaffold: bảy assembly không vòng, **1.450 cặp metadata/GUID** hợp lệ; MainMenu 0 / Bootstrap 1 / Arena_MVP 2. Saved layout: **3.228 assertion / 12 YAML** đạt.
- Mới: Networking/LobbyMessages.cs; Server/Sessions/LobbyRoom.cs và ServerLobbyController.cs; Client/UI/LobbyRoomView.cs và LobbyRoomController.cs; Tests/PlayMode/LobbyRoomManagementTests.cs; đủ sáu .meta. Thêm Prefabs/Network/ArenaNetworkRoot.prefab và metadata prefab/folder.
- Sửa: ArenaNetworkManager, MainMenuController, MainMenu.unity, MenuNavigationTests. MultiClientIntegrationTests chờ điều kiện destroy message tới observer thay vì giả định chỉ một frame; vẫn giữ đủ bảy assertion integration M1.
- Công cụ: Build-LobbyUI.cs, Build-LobbyStandalone.cs, LobbyStandaloneSmoke.cs, Run-LobbyStandaloneSmoke.ps1; ArenaLayoutVerifier cập nhật trạng thái Multiplayer và GUID TMP input/mask hợp lệ.
- Native regression đã bắt được sai binding/transport test, HUD debug bị clone, Unity fake-null ở singleton và thời điểm callback disconnect. Đã sửa và chạy lại toàn bộ suite đạt; giữ log/XML các lần lỗi để truy vết.
- Không thay đổi Player/Bot/WeaponPickup, Arena_MVP, Bootstrap, Input Actions, asmdef hoặc build settings. Hash Packages/ProjectSettings/URP của người dùng giữ nguyên. Không stage/commit/push; context, log, executable và XML được ignore.

Xem [kiến trúc và hướng dẫn thao tác](../architecture/m2-lobby-rooms.md). Phạm vi hoàn tất là hosted lobby cơ bản của TV1; tìm phòng, server room service riêng, reconnect token, chuyển host và combat/HUD mạng vẫn là công việc tiếp theo.
