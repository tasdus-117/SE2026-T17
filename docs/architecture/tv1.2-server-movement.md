# TV1.2 — mô phỏng di chuyển có thẩm quyền trên server

## Luồng dữ liệu và ranh giới

`PlayerInputHandler` và `PlayerController` trong Client đọc input của local owner. Khi có NetworkIdentity, PlayerController chỉ gọi NetworkMovement, không đổi position/rotation hoặc gọi CharacterController.Move. Khi không có identity, giữ nguyên di chuyển/nhảy offline.

`NetworkMovement` trong Networking gửi MovementInput qua Mirror Command, `requiresAuthority = true`, kênh Unreliable. Bridge chỉ giữ input tick/jump counter và giới hạn gửi 60 Hz; không đọc thiết bị, mô phỏng vật lý hoặc tham chiếu Server/Client. Khi gói bị mất, heartbeat tiếp theo mang hướng hiện tại và jump counter mới nhất.

`MovementInput` thuộc Shared.Protocol, chỉ có bốn trường thuần C#: Sequence, MoveX, MoveZ, JumpSequence. Không có position, rotation, speed hoặc delta time do client quyết định. Sequence là số thứ tự tick input, không phải số bước vật lý được phép chạy.

`ServerPlayerMovement` trong Server/Simulation đăng ký sự kiện server từ bridge. `MovementInputValidator` trong Server/Validation loại NaN/Infinity, clamp độ dài về tối đa 1 bằng phép chuẩn hóa không overflow và bỏ sequence cũ/lặp theo số học uint có wrap. Chỉ input hợp lệ mới làm mới timeout.

Server giữ ý định mới nhất. Mỗi FixedUpdate chỉ chạy một bước bằng Time.fixedDeltaTime của server, không chạy thêm bước khi nhận thêm Command. Các hằng số server: tốc độ ngang 5 m/s, jump height 1,2 m, gravity -20 m/s², grounded velocity -2 m/s, yaw +Z. Xử lý va trần và grounding bằng CharacterController thật. Sau 0,25 giây không nhận input hợp lệ, dừng chuyển động ngang nhưng tiếp tục gravity.

JumpSequence là bộ đếm cạnh nhấn, được lặp trong heartbeat. Server chỉ nhận cạnh mới khi grounded; cạnh nhận trong không trung bị tiêu thụ và bỏ, không giữ để nhảy khi chạm đất. Nhiều gói trong cùng tick không tạo thêm bước mô phỏng hoặc nhiều lần nhảy.

## Replication và trải nghiệm owner

Player prefab có NetworkTransformReliable của Mirror, syncDirection = ServerToClient, syncMode = Observers, target là root world transform, position/rotation bật, scale tắt. Interpolation bật cho position/rotation, độ chính xác position 1 mm; syncInterval = 0 dùng chu kỳ gửi của NetworkManager (60 Hz), không cộng thêm timelineOffset của component.

Host/server giữ CharacterController hoạt động. OnStartClient trên replica thuần client, kể cả owner, tắt CharacterController; NetworkTransform nhận trạng thái server và nội suy transform. Owner vẫn có quyền gửi ý định, không có quyền upload pose. NetworkTransform của server bỏ qua dữ liệu position do client cố gửi ngược chiều.

CameraOcclusionController dùng renderer bounds khi collider của replica bị tắt; không dùng bounds rỗng ở world origin. Nhờ đó tường che owner vẫn được xử lý theo capsule đang hiển thị, trong khi collider môi trường giữ nguyên.

Input gửi thường xuyên và server mô phỏng theo physics tick để phản hồi nhanh trong môi trường LAN. Phiên bản này không có client prediction/reconciliation và không ghi vị trí dự đoán lên root; độ trễ của owner vẫn chịu RTT và buffer interpolation. Kiểm chứng cảm giác điều khiển/jitter bằng nhiều tiến trình và điều kiện mạng thật thuộc TV1.3.

## Bảo toàn offline và assembly

Prefab giữ GUID/các component cũ; thêm NetworkMovement, ServerPlayerMovement, NetworkTransformReliable. Scene Arena_MVP gỡ đủ năm component mạng bằng override: ba component mới cùng NetworkPlayer/NetworkIdentity. Fixture offline gỡ theo thứ tự dependency trước khi bật lại Player; giữ nguyên assertion gameplay cũ.

Vẫn bảy asmdef: Domain/Protocol thuần C#, Networking dùng Shared/Mirror, Server và Client dùng Shared/Networking, không tham chiếu nhau. PlayMode.Tests thêm Mirror.Components để kiểm tra transform vendor thật; không tạo assembly production mới hoặc di chuyển simulation vào bridge. Các scene và build index không đổi.

TV1 sở hữu pipeline mạng/mô phỏng/tích hợp; TV2 giữ combat/rules/xác thực combat, TV3 giữ input/presentation/UI/arena. Chưa triển khai combat/HUD/lobby mạng hoặc phục hồi phiên có token.

Kết quả kiểm chứng ở [báo cáo TV1.2](../testing/tv1.2-results.md). Bước tiếp theo: TV1.3 — nhiều client thực, quan sát replication và phục hồi khi disconnect.
