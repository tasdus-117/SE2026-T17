# Menu trong trận và kết thúc phiên mạng

## Phạm vi triển khai

`InGameMenu.prefab` là Canvas overlay độc lập trong `Arena_MVP`, nằm ngoài
`OfflineMatchScope`. Hai panel tạm dừng và kết quả mặc định đều tắt. View chỉ
hiển thị; `InGameMenuController` thuộc Client.UI quản lý Escape, nút bấm, con trỏ
và khóa input của đúng người chơi local. Mở menu không đổi `Time.timeScale`;
server, gravity và những người chơi khác tiếp tục chạy. Resume xóa lựa chọn UI,
khóa/ẩn chuột và khôi phục input; các phím hành động đang giữ không bị phát lại.

`ShowMatchResult(bool isWinner)` hiển thị VICTORY hoặc DEFEAT và khóa input local.
Đây là điểm nối hiển thị cho M3, chưa có luật thắng/thua multiplayer. Luồng
Victory/HUD/replay offline cũ vẫn hoạt động riêng, không bị menu Escape che lại.

Mọi nút về menu dùng `MatchSessionNavigation` trong Networking. Bộ điều phối
chặn yêu cầu lặp, sống qua đổi scene, bỏ việc tải offline scene trễ của Mirror,
chờ callback ngắt kết nối và tải scene đang dở, dọn manager cũ rồi mới tải
MainMenu. Host gọi StopHost; client gọi StopClient. Khi server ngắt kết nối
trong Arena, callback client tự gọi cùng đường điều hướng. Room state được
xóa; manager mới của MainMenu cho phép Host/Join lại. Không có tham chiếu
Networking → Client/Server hoặc Client → Server.

## Kiểm tra thủ công

1. Chạy MainMenu → Single Player. Nhấn Escape: menu hiện, chuột tự do, WASD,
   Space, J, E không tạo ý định gameplay. Bấm Resume hoặc Escape: menu đóng,
   chuột khóa/ẩn, input hoạt động lại. Nhấn Leave Match: trở về MainMenu.
2. Hai cửa sổ độc lập: Host/Join → Ready → Start. Host mở Escape: client kia
   vẫn di chuyển được. Client chọn Leave Match: host còn trong trận, avatar
   client bị xóa; client có thể mở Multiplayer và kết nối phiên mới.
3. Tạo lại hai người trong trận. Host chọn Leave Match: cả hai về MainMenu,
   không còn avatar/room cũ và có thể Host/Join lại. Kiểm tra Console không có
   NullReferenceException/MissingReferenceException hoặc cảnh báo singleton trùng.
4. Gọi `ShowMatchResult(true)` hoặc `ShowMatchResult(false)` trên controller
   trong Play Mode: kiểm tra tiêu đề, chuột và nút Return to Main Menu. Escape
   không đóng kết quả. Đây là kiểm tra view, không phải nghiệm thu luật M3.
5. Offline: đánh Bot về 0 HP; banner Victory và các nút Replay/Main Menu cũ
   vẫn dùng được. Từ MainMenu vào Single Player lần nữa để kiểm tra reset.

## Kiểm thử tự động

`InGameMenuAndTeardownTests` có mười UnityTest native: Escape/cursor, xóa input
và không phát lại phím giữ, hai trạng thái kết quả không đổi luật, hủy/đăng ký
listener, rời offline, điều hướng lặp chỉ tải một lần, host pause cô lập local,
host teardown và tạo phòng lại, client teardown giữ server độc lập, server
disconnect tự trả client về menu. Transport loopback chuyển gói qua Mirror
thật; client test có proxy riêng, không giả cờ ownership. Một NetworkClient
static trong engine không thay thế kiểm chứng KCP giữa hai tiến trình.

## Kết quả thực thi ngày 08/11/2026

Unity `6000.6.8f1`, chạy Test Runner native trong bản sao
`TestResults/TV11/ValidationProject`, giữ nguyên Editor/workspace của người dùng.
License đã hoạt động sau khi người dùng kích hoạt lại Hub; hai runner cuối trả
mã **0**, không còn lỗi khởi động 198.

| Kiểm chứng | Kết quả |
| --- | --- |
| Full PlayMode | 238 ca: **236 đạt, 0 lỗi, 2 bỏ qua** |
| PlayMode của nhóm | **234/234 đạt**, không bỏ qua |
| Menu/teardown mới | **10/10 đạt** |
| TV1.3 multi-client cũ | **7/7 đạt** |
| EditMode của nhóm | **34/34 đạt**, không lỗi/bỏ qua |
| Roslyn bảy asmdef của nhóm | **7/7 đạt**, 0 cảnh báo/lỗi mã nhóm |
| Kiểm tra file đã lưu | **3.517 assertion / 13 YAML** đạt |
| Scaffold/GUID | **1.455 cặp metadata** hợp lệ, không vòng assembly |

Hai ca bỏ qua thuộc package Input System, lý do có sẵn “Unstable due to
1252825”; không phải test mới bị bỏ. `-nographics` không có con trỏ vật lý nên
getter lockState giữ None: test vẫn kiểm tra ẩn/hiện chuột, mở khóa menu và
chặn/khôi phục input; assertion khóa vật lý chỉ chạy với Test Runner tương tác.
Cần thực hiện bước thủ công Resume ở trên để xác nhận khóa chuột trên màn hình.

Lần chạy đầu bộc lộ lỗi harness: Press/Release trong UnityTest chỉ xếp event,
cần InputSystem.Update trước assertion. Transport loopback cũng được sửa để bỏ
gói gửi còn đệm khi đã ngắt, tương tự KCP; toàn bộ bảy ca TV1.3 đã chạy lại đạt.
Không bỏ qua log lỗi toàn cục hoặc giả cờ ownership để làm test đạt.

Bằng chứng cục bộ (được Git ignore):
`TestResults/InGameMenu/PlayMode-final.xml`, `EditMode.xml`,
`Compilation-final/results.json`; log `Logs/InGameMenu-PlayMode-final.log` và
`Logs/InGameMenu-EditMode.log`. XML PlayMode cuối có:

```text
total="238" passed="236" failed="0" skipped="2"
EditMode: total="34" passed="34" failed="0" skipped="0"
```

Biên dịch native và Mirror weaving thành công. Unity vẫn có cảnh báo analyzer
vendor/Mirror có sẵn; kết quả 0 cảnh báo áp dụng cho mã bảy assembly của nhóm,
không phải cam kết toàn bộ package/vendor không có cảnh báo.

## Kiểm chứng hai tiến trình

Công cụ `tools/LobbyStandaloneSmoke.cs` có tùy chọn `--menu-teardown host|client`;
chỉ chép vào bản sao kiểm chứng và biên dịch với define riêng của smoke build.
Không thêm worker vào mã production. Chạy bằng:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Run-LobbyStandaloneSmoke.ps1 -Player TestResults/InGameMenu/Standalone/SmallArenaMenu.exe -Results TestResults/InGameMenu/HostLeave -Teardown host
powershell -ExecutionPolicy Bypass -File tools/Run-LobbyStandaloneSmoke.ps1 -Player TestResults/InGameMenu/Standalone/SmallArenaMenu.exe -Results TestResults/InGameMenu/ClientLeave -Teardown client
```

Hai trường hợp đều đi qua nút Multiplayer/Host/Join/Ready/Start thật, quan sát
hai avatar rồi dùng nút Return của modal. Trường hợp host rời không cho guest
gọi Leave; guest phải tự trở về MainMenu qua callback disconnect. Trường hợp
client rời yêu cầu host vẫn còn chạy với một avatar, trước khi host tự rời.
Báo cáo kiểm tra manager mới, cursor menu, không còn identity và không có lỗi
runtime. **Cả hai trường hợp đã chạy đạt qua KCP thật**:

| Trường hợp | Bằng chứng | Kết quả |
| --- | --- | --- |
| Host rời | HostLeave/f6a821cb5b2048d393bc8e98f296e9b5 | Guest tự về MainMenu; hai tiến trình sạch |
| Client rời | ClientLeave/11b7acf4c9a349dd9c52ed06561faf7c | Host còn một avatar/connection trước khi tự rời; hai tiến trình sạch |

Đường dẫn trên nằm trong `TestResults/InGameMenu/`; mỗi trường hợp có
`host.menu.json`, `client.menu.json`, log và `menu-summary.json`. Tổng cộng
**4/4 báo cáo tiến trình đạt**, `errors: []`, các worker thoát mã 0. Không cộng
bốn báo cáo này vào số test NUnit. Hai tiến trình đều dùng MainMenu và thấy
hai avatar trước teardown, không dùng NetworkManagerHUD.

Standalone Windows Mono build cuối: **Succeeded, errors=0, warnings=489,
bytes=219021839**, Editor thoát mã 0. Cảnh báo thuộc shader/vendor/Mirror;
không có cảnh báo nguồn `Assets/_Project`. Lần build kiểm chứng trước đó gặp
worker smoke trùng trong bản sao; đã xóa bản trùng cùng metadata và build lại
thành công, không thay đổi source production để che lỗi.

Đã so sánh SHA-256 **144 tệp nguồn/asmdef/scene/prefab/input và metadata** với
bản sao chạy native; tất cả khớp. **48 hash package/settings** trước nhiệm vụ
giữ nguyên. Context và kết quả chạy cục bộ được ignore; không stage/commit/push.
