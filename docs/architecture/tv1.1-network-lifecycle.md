# TV1.1 — vòng đời kết nối, spawn và giới hạn điều khiển

## Phạm vi

`ArenaNetworkManager` và `NetworkPlayer` thuộc `SmallArena.Networking`. Assembly không tham chiếu Server hoặc Client; bridge không chứa mô phỏng, combat hoặc HUD. Bảy assembly được giữ nguyên; PlayMode.Tests thêm tham chiếu Mirror để kiểm tra API thật.

- Server chọn marker qua `GetStartPosition()` với RoundRobin. Bốn vị trí `(±6; 1; ±6)` có `NetworkStartPosition`, hướng +Z, nằm trong sàn 16×16 m.
- Chỉ server đang hoạt động tạo Player, gán ConnectionId rồi gọi `NetworkServer.AddPlayerForConnection`. Không nhận tọa độ từ client, không dùng vị trí gốc prefab làm fallback khi thiếu marker.
- Mỗi connection giữ một marker; bỏ qua marker đang dùng, giải phóng khi disconnect, xóa reservation khi dừng server. Duplicate add không tạo Player thứ hai. Callback disconnect an toàn khi không có Player, khi gọi lại hoặc nhận null.
- `NetworkPlayer` chỉ cung cấp định danh connection phía server, cờ local và hai sự kiện spawn/despawn local. ConnectionId chưa phải SyncVar cho client từ xa.
- PlayerInputHandler chỉ bật action map cho local owner, chờ callback local spawn và tắt/xóa input khi despawn. PlayerController và CombatController trả về trước khi đọc/consume input nếu NetworkIdentity không phải local player. Server-only và remote player không đọc bàn phím.
- Camera quan sát sự kiện local, phục hồi target nếu bật sau callback, xóa target khi local despawn, từ chối target remote và hủy đăng ký khi disable. Follow vẫn ở LateUpdate với yaw cố định.

## Scene và prefab

Prefab chính thức ở `Assets/_Project/Prefabs/Characters/Player.prefab`, giữ GUID cũ và thêm NetworkIdentity/NetworkPlayer. Không tạo bản Player.prefab khác ở gốc Prefabs.

Bootstrap dùng ArenaNetworkManager, KCP, playerPrefab đã gán, autoCreatePlayer bật, RoundRobin và maxConnections = 4. Thứ tự build không đổi: MainMenu 0, Bootstrap 1, Arena_MVP 2. Kết nối vẫn do API `StartHost()`, `StartServer()` hoặc `StartClient()` của manager khởi động; menu Multiplayer vẫn Coming Soon, chưa có UI kết nối mới.

Arena giữ Player offline liên kết prefab bằng removed-component overrides cho riêng NetworkPlayer/NetworkIdentity. Vì không có identity, input/locomotion/combat offline tiếp tục hoạt động. `OfflineMatchScope` chạy sớm khi arena được tải trong phiên mạng từ Bootstrap và tắt Player/Bot/pickup mẫu, GameManager offline và MatchCanvas; camera, sàn và marker vẫn hoạt động. Không chạy luật trận offline cho các player mạng.

Các fixture kiểm tra combat/di chuyển/pickup offline gỡ identity/bridge trước khi bật lại Player, tương ứng chính xác override trong scene. Các assertion gameplay cũ được giữ.

## Giới hạn và bước tiếp theo

TV1.1 chuẩn bị lifecycle và ownership gate. Chưa có command gửi ý định, server simulation, transform replication, prediction/reconciliation, combat mạng hoặc HUD mạng. Di chuyển của local owner hiện là adapter mẫu; không được coi là server-authoritative movement. TV1.2 tiếp tục gửi ý định và mô phỏng/replication do server quyết định.

NetworkLifecycleTests sử dụng lifecycle server thật và hàng đợi LocalConnection thật của Mirror ở host. Transport fixture bỏ qua socket của các connection giả lập khác; do đó đây không phải bằng chứng UDP/KCP nhiều tiến trình, latency, reconnect có token hoặc dedicated build. TV1 sở hữu mạng/server/tích hợp; TV2 giữ combat/rules, TV3 giữ client/UI/arena.

Kết quả kiểm chứng và giới hạn lần chạy được ghi trong [báo cáo TV1.1](../testing/tv1.1-results.md).
