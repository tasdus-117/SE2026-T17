# M2 TV1 — phòng chờ Host/Join

## Luồng vào phòng

MainMenu giữ nguyên Single Player → Arena_MVP offline. Multiplayer mở ConnectionPanel ngay trong MainMenu, gồm địa chỉ mặc định localhost, Host Room, Join Room và Back. Không cần NetworkManagerHUD hoặc lệnh debug. Tutorial vẫn Coming Soon.

Host Room tạo phiên Mirror host; Join Room trim địa chỉ rồi kết nối client. Khi nhận snapshot server, UI chuyển sang RoomPanel, hiển thị tối đa bốn thành viên và trạng thái Ready. Host cũng phải Ready; chỉ host được Start Match khi mọi thành viên hiện tại đều Ready. Server chuyển cả nhóm sang Arena_MVP và chỉ cho tạo Player sau khi scene server đã tải xong.

## Ranh giới mã

| Assembly | Thành phần | Trách nhiệm |
| --- | --- | --- |
| SmallArena.Client | LobbyRoomView, LobbyRoomController, MainMenuController | View chỉ trình bày snapshot; controller nối nút UI với API mạng, quản lý listener và điều hướng |
| SmallArena.Networking | LobbyMessages, ArenaNetworkManager | Request Ready/Start, snapshot/rejection, lifecycle event, scene bridge và gate spawn |
| SmallArena.Server | Sessions/LobbyRoom, Sessions/ServerLobbyController | Danh sách thành viên, giới hạn bốn người, readiness, quyền host, chuyển pha và cleanup disconnect |

Vẫn bảy asmdef. Networking không tham chiếu Server/Client; Server phát chính sách spawn qua delegate và nhận lifecycle event từ bridge. Client không tham chiếu Server. Domain/Protocol thuần C# không bị thêm Unity/Mirror. NetworkPlayer vẫn là bridge mỏng, không chứa lobby UI hoặc mô phỏng mới.

## Quyền quyết định và vòng đời

- NetworkServer giới hạn bốn connection, gồm host local. Mirror từ chối connection thứ năm trước khi thêm vào danh sách; LobbyRoom cũng từ chối vượt bốn thành viên nếu có lời gọi bỏ qua tầng transport.
- Request Ready chỉ chứa bool, Start không chứa ID hoặc quyền host. Connection gửi do Mirror cung cấp; chỉ local connection của host được server đánh dấu IsHost. Client không thể chọn ID, sửa người khác hoặc tự khai host.
- Pha server: Offline → Waiting → Loading → Playing. Ready lặp giống giá trị hiện tại không phát cập nhật thừa. Start lặp/không đủ Ready/từ remote bị bỏ. Không nhận thành viên mới sau khi bắt đầu tải arena.
- Lobby không spawn avatar. Request AddPlayer sớm không vượt được server gate. Khi server tải Arena_MVP xong, client nhận scene và gửi Ready/AddPlayer theo luồng chuẩn Mirror; vị trí vẫn lấy từ bốn NetworkStartPosition hiện hữu.
- Disconnect xóa thành viên và trả slot; người thay thế luôn bắt đầu Not Ready. Leave của host dừng phòng; guest trở lại màn hình kết nối với trạng thái thân thiện khi server đóng hoặc không vào được.
- Root mạng chỉ persistent khi có phiên. Khi dừng phiên và rời arena, Mirror hủy root cũ và menu tạo root mới; các tham chiếu singleton đã bị Unity hủy được kiểm tra bằng Unity-null. Root idle thuộc scene menu và không đi theo Single Player. Rời phòng khi vẫn ở menu có thể tạo lại phiên trên root hiện tại.

## Tài nguyên và tương thích

`MainMenu.unity` có ConnectionPanel/RoomPanel cùng InputSystemUIInputModule hiện hữu. `Prefabs/Network/ArenaNetworkRoot.prefab` gồm ArenaNetworkManager, KcpTransport và ServerLobbyController; không có debug HUD. Root cấu hình roomEnabled=true, maxConnections=4, autoCreatePlayer=false, không auto-load online scene trước Start.

Bootstrap giữ entry M1 trực tiếp và cấu hình cũ, roomEnabled=false. Build order không đổi: MainMenu 0, Bootstrap 1, Arena_MVP 2. Player/Bot/pickup prefab, arena và Input Actions được giữ nguyên. `tools/Build-LobbyUI.cs` dựng lại UI từ menu hiện hữu, dùng helper Build-OfflineMatchUI để tạo control nhưng không gọi thao tác dựng lại arena.

## Kiểm tra thủ công

1. Chạy MainMenu, chọn Multiplayer → Host Room. Xác nhận Room 1/4, chưa có Player, chưa chuyển arena.
2. Chạy một build khác, chọn Multiplayer, nhập IP host và Join Room. Host/guest phải cùng thấy Room 2/4.
3. Ready từng người; host mới có Start Match, guest không có quyền Start. Nhấn Start, cả hai vào arena và chỉ điều khiển avatar của mình.
4. Trong lobby, đóng một guest rồi kết nối guest mới; slot được trả và người mới Not Ready. Với tổng bốn người, kết nối thứ năm bị từ chối.
5. Leave/Back trở lại menu; Single Player vẫn tạo trận offline mới. Không dùng Root network đang active cho Single Player.

Bộ PlayMode kiểm tra protocol, UI, capacity và scene flow trong engine. Smoke standalone dùng hai player riêng qua KCP, kích hoạt đúng nút menu/Ready/Start; công cụ instrument chỉ được chép vào bản sao kiểm chứng với define riêng, không thêm vào assembly production ở repository. Kết quả và giới hạn cụ thể ghi ở [báo cáo M2](../testing/m2-lobby-results.md).

Chưa triển khai tìm phòng tự động, mật khẩu, NAT traversal/relay, server room service riêng, reconnect có token, chuyển host hoặc combat/HUD mạng. Đây là phòng hosted cơ bản theo phạm vi TV1 của nhiệm vụ.
