# Kiểm chứng M1 trong Unity Test Runner

## Chạy trực tiếp trong Editor

1. Mở project bằng Unity `6000.6.8f1` (khớp ProjectVersion.txt), chờ import/compilation hoàn tất.
2. Mở **Window → General → Test Runner**, chọn **PlayMode**.
3. Tìm `SmallArena.Tests.PlayMode.MultiClientIntegrationTests`, chọn **Run Selected** để chạy đủ bảy ca.
4. Chọn **Run All** trong PlayMode để chạy regression, sau đó chuyển EditMode và chạy toàn bộ test của nhóm.
5. Dùng **Save Results** lưu XML vào `TestResults/TV13Native/`; ghi riêng số pass/fail/skip và phiên bản nguồn đã chạy.

Không cần build player, Windows Build Support, port UDP, ParrelSync, biến môi trường, executable worker hoặc CLI batchmode. Các test chạy trong Editor đang mở và không khởi chạy tiến trình khác. Unity Editor vẫn cần license hợp lệ để mở; chuyển sang loopback không loại bỏ điều kiện sử dụng Editor này.

## Harness thực hiện điều gì

Fixture tạo floor và bốn `NetworkStartPosition` trong scene PlayMode đang hoạt động, dùng Player prefab thật cùng `ArenaNetworkManager`. Mỗi ca tạo và dọn một phiên riêng. Transport trong bộ test đưa các batch Mirror vào hàng đợi bộ nhớ, rồi phát callback transport theo pha mạng của Mirror; không sửa vendor hoặc thêm test hook vào production.

Có **một NetworkClient thật** và **connection B ảo**. Client thật đi qua `StartClient`, không dùng `StartHost`: server object và client proxy là hai GameObject khác nhau. Native client nhận spawn, EntityState/NetworkTransformReliable và ObjectDestroy qua handler chuẩn. B gửi Ready/AddPlayer/Command đã serialize tới server và quan sát các message spawn/state/destroy server gửi cho nó.

Mirror có một NetworkClient static trong mỗi tiến trình. View B chỉ là bộ quan sát message; không phải NetworkClient thứ hai và không có camera/interpolation độc lập. Bảy ca không chứng nhận hai cửa sổ độc lập, KCP/UDP, timeout hệ điều hành hoặc độ mượt dưới latency. Kết quả loopback không được ghi thành bằng chứng hai tiến trình đã quan sát nhau.

## Bảy ca PlayMode

| Ca | Assertion chính |
| --- | --- |
| TwoClients_SpawnDistinctOwnedPlayers_AtRegisteredMarkers | Hai connection có netId/marker riêng; proxy tách server, local/owned đúng, chỉ server bật CharacterController |
| InputFromA_CannotControlB_EvenWithForgedCommandTarget | WASD từ A chỉ di chuyển A; replica B không đọc bàn phím; server từ chối Command của A giả target B |
| ServerMovement_ReplicatesBothPlayers_ToSeparateClientProxies | Input hợp lệ từ hai connection làm server di chuyển; native proxy nhận pose trung gian, hội tụ trong 3 cm; B nhận EntityState của A |
| VirtualDisconnect_UnspawnsB_AndNativeClientContinuesMoving | Callback disconnect transport hủy B trên server/native view, giải phóng owned; A vẫn gửi input và di chuyển |
| RepeatedDisconnect_ReleasesMarker_AndReplacementGetsFreshIdentity | Disconnect lặp an toàn; connection mới nhận marker đã trả, identity mới, không giữ netId cũ |
| HackedClientVector_IsClamped_WithoutMovingOtherAvatar | Command thô (3000,4000) clamp về magnitude 1, không vượt 5 m/s theo tick server, không làm B di chuyển |
| NativeClientDisconnect_PublishesOneDespawn_AndCleansBothSides | Local spawn/despawn đúng một lần qua lifecycle thật; native client và player A được dọn, observer B nhận destroy |

Mọi ca kiểm tra owned references và số server identity/connection; teardown dừng mạng, hủy object, reset static Mirror, khôi phục Input System/thời gian và kiểm tra log bất thường. Cảnh báo từ chối Command giả authority được kỳ vọng rõ ràng; không bỏ qua toàn bộ lỗi console.

## Kiểm tra hai cửa sổ thủ công, tùy chọn

Phần này không phải điều kiện để chạy bảy test trong Editor. Tạo build thông thường qua **File → Build Profiles** nếu cần kiểm tra KCP ngoài tiến trình.

1. Mở Bootstrap trong Editor, Play, chọn component ArenaNetworkManager → menu **TV1 → Start Host (Play Mode)**.
2. Chạy build thông thường với `--arena-client --arena-address 127.0.0.1 --arena-port 7777`; hoặc dùng clone Editor riêng và Inspector **TV1 → Start Client (Play Mode)**.
3. Đưa hai avatar gần tâm arena để camera cố định nhìn thấy nhau; mỗi cửa sổ chỉ điều khiển owner của nó, avatar kia nhận pose server.
4. Đóng một client, kiểm tra proxy bị xóa và client còn lại vẫn WASD/Space bình thường. Khi kết thúc đột ngột, chờ timeout KCP; không kết luận từ callback disconnect loopback rằng timeout KCP đã chạy đạt.
5. Kết nối lại, kiểm tra marker được trả và netId mới. Ghi riêng kết quả thủ công, không cộng vào số NUnit pass.

Tài liệu phân công `SE2026-T17-Huong-dan-va-phan-cong-nhom.md` chưa có trong checkout. Bốn checkpoint người dùng cung cấp vẫn cần đối chiếu: thấy nhau di chuyển, không lẫn điều khiển, clamp hack và thoát không crash. Loopback bao phủ luật/handler; kiểm tra cửa sổ độc lập bổ sung bằng chứng vận hành.

## Trạng thái và M2

Xem [kết quả TV1.3](tv1.3-results.md): M1 đã ký theo phạm vi loopback trong engine, với 212/212 ca PlayMode nhóm và 34/34 EditMode đạt, gồm 7/7 TV1.3. Toàn bộ PlayMode có thêm hai ca package đạt và hai ca package bỏ qua. Không ghi ca chưa chạy là đã pass hoặc ký M1 dựa vào compilation. Combat/HUD mạng, reconnect token, prediction/reconciliation và build Dedicated Server riêng chưa thuộc bằng chứng đã xác nhận.

Sau khi regression đạt, M2 cần TV1 làm lobby/session/ready, TV2 làm luật và validation phía server, TV3 làm Lobby UI/scene flow. Cần thêm kiểm chứng bốn client, từ chối người thứ năm, ready idempotent và disconnect trong lobby/đang tải scene.
