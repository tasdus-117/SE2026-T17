# Lobby → Arena: giao diện tải trận

Nền tích hợp: `775e96c` (`origin/main`, merge PR #11). Đối chiếu ba chấm với
`origin/codex/bt3-vt-066` (`6192e81`), chỉ lấy Scripts/Tests liên quan Start;
không lấy scene/prefab cũ, báo cáo bài tập hoặc bằng chứng lịch sử. Không có PR
mở trùng tại thời điểm bắt đầu. Nhánh bài tập được giữ nguyên.

## Hành vi

- Server chỉ nhận Start từ host khi có ít nhất hai người và tất cả Ready.
  Chuyển trạng thái Loading trước khi gọi tải, nên bấm lặp/reentrant không tải lại.
  Thành công và thất bại đều kết thúc thao tác một lần trong phiên.
- Snapshot phòng hiển thị “Starting match…”. Overlay “Loading arena…” bắt đầu
  từ callback chuyển scene cục bộ của Mirror (host dùng callback server vì chia sẻ
  lần tải đó); chỉ callback scene-ready của chính máy đó mới hoàn tất overlay.
- Observer nằm trên session root, canvas tồn tại qua việc hủy RoomPanel.
  Disconnect/StopClient/destroy dọn overlay. Lỗi preflight tải scene giữ thông báo
  qua teardown; MainMenu được tải lại để tạo manager dùng được cho phiên mới.
- Một client rời không quyết định việc hoàn tất tải của máy khác. Không thay đổi
  scene, prefab, combat, vật cản, đồng bộ collider hoặc fall defeat của PR #11.

## Tái chạy

Windows, Unity `6000.6.8f1`, giấy phép Unity hợp lệ:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Run-LobbyLoadingUnity.ps1 -Phase EditMode
powershell -ExecutionPolicy Bypass -File tools/Run-LobbyLoadingUnity.ps1 -Phase PlayMode
powershell -ExecutionPolicy Bypass -File tools/Run-LobbyLoadingUnity.ps1 -Phase Build
powershell -ExecutionPolicy Bypass -File tools/Run-LobbyLoadingStandaloneSmoke.ps1 -Faller client
powershell -ExecutionPolicy Bypass -File tools/Run-LobbyLoadingStandaloneSmoke.ps1 -Faller host
powershell -ExecutionPolicy Bypass -File tools/Run-LobbyLoadingStandaloneSmoke.ps1 -DisconnectDuringLoad
```

Runner build chỉ chép harness vào Assets trong lúc build, dùng define riêng rồi
dọn source/meta tạm. Binary/log/XML/JSON ở `TestResults/LobbyArenaLoading` (ignored).
Harness dùng callback nút UI thật trong MainMenu và hai player riêng qua KCP localhost;
không giả callback scene-ready. Host gửi mười Start liên tiếp. Mỗi player ghi số
callback bắt đầu/kết thúc/hủy, lần sceneLoaded, trạng thái canvas tại hai đầu và
so khớp mô tả/collider vật cản, fall defeat/victory như PR #11.

Ca disconnect chủ động gọi `NetworkClient.Disconnect()` ngay callback bắt đầu tải
trên client, dùng teardown và scene load thật. Kiểm tra thông báo ở menu, overlay
đã dọn, tạo được phòng mới; host còn lại vào Arena và dọn overlay độc lập.

Các fixture VT-066: `LobbyStartOperationTests`, `LobbyStartBoundaryTests`,
`LobbyStartProgressTests`. Ca 20 vòng tải scene ghi CSV vào TestResults.
Test disconnect trước khi Mirror lưu tên scene là kiểm thử callback boundary,
không được tính là bằng chứng mạng hai tiến trình.

## Kết quả thực tế

Unity 6000.6.8f1 / Windows, ngày 15/11/2026:

| Ca chạy | Kết quả |
| --- | --- |
| EditMode toàn bộ | 142 đạt, 0 lỗi, 0 skip |
| PlayMode toàn bộ | 316 đạt, 0 lỗi, 2 skip |
| Build standalone cuối | Succeeded, 0 lỗi, 0 cảnh báo ở incremental build (build đầu 15 cảnh báo dependency/analyzer) |
| Hai tiến trình, host gửi 10 Start, client rơi | Đạt; mỗi máy 1 start/1 finish/0 abort, 1 sceneLoaded; collider trùng, Defeat/Victory đúng, 0 lỗi runtime |
| Hai tiến trình, host gửi 10 Start, host rơi | Đạt, cùng các assertion trên |
| Hai tiến trình, client disconnect tại callback tải | Đạt; client 1 start/0 finish/1 abort, thông báo đúng, tạo phòng mới có 1 thành viên; host 1 start/1 finish/0 abort và vào Arena |

Evidence local (không track): `TestResults/LobbyArenaLoading`.
Hai lượt bình thường cuối: `StandaloneSmoke/2d7169a4e924469f88abae48766ac3c6`
(client rơi), `StandaloneSmoke/6b50988f8e134033b74dcb3b5c9355b7` (host rơi).
Disconnect sau sửa: `StandaloneSmoke/d74efbe7eff2448bbd7ec1a55fb0c3fd`.

Ca disconnect đầu tiên thất bại ở khôi phục phòng mới: thông báo và overlay đã
đúng, nhưng Mirror còn cờ `isLoadingScene` nên snapshot phòng mới không đến.
Sửa teardown gọi Shutdown sau khi native load hoàn tất, rồi chạy lại ca này đạt.
Thêm regression tại boundary disconnect trước khi Mirror lưu tên scene.
Lượt test bổ sung đầu tiên có lỗi biên dịch do setter Mirror không public; đã
sửa fixture và chạy lại. Hai test chuột dependency Input System có sẵn lý do
skip “Unstable due to 1252825”. Lượt Unity đầu bị sandbox chặn licensing, đã
chạy thành công ngoài sandbox; không thay đổi cấu hình giấy phép hoặc bảo mật.

Giới hạn: headless xác minh trạng thái canvas/text, chưa đánh giá bằng mắt hình ảnh,
font hoặc animation. Không mô phỏng mạng Internet/latency/loss, dedicated server,
client dùng build thiếu scene khác host, lỗi ổ đĩa/OOM hay quá trình native load
không bao giờ trả về. Ca lỗi tải chủ động kiểm tra preflight scene thiếu/sai và
khôi phục phiên; không đồng nghĩa đã thử mọi lỗi engine bất đồng bộ.
