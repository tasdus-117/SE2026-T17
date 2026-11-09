# Kết quả TV1.2 — movement có thẩm quyền trên server

Ngày hoàn tất: **2026-11-04** (Asia/Saigon). Unity 6000.6.7f1, Mirror 96.0.1; nhánh `feature/tv1-m1-server-connect`.

Thiết kế chi tiết: [pipeline movement](../architecture/tv1.2-server-movement.md), [hợp đồng giao thức](../protocol/protocol.md).

## Thay đổi để review

- Thêm MovementInput trong Shared.Protocol, NetworkMovement trong Networking, MovementInputValidator trong Server/Validation và ServerPlayerMovement trong Server/Simulation. Bridge không chứa vật lý hoặc tham chiếu Server/Client.
- PlayerController có identity chỉ gửi ý định của local owner; không Move hoặc ghi transform. Server giữ input mới nhất, clamp hữu hạn/magnitude, bỏ sequence cũ/lặp, mô phỏng một bước bằng fixed delta server, tối đa 5 m/s, nhảy grounded khoảng 1,2 m và gravity -20 m/s². Command nhiều hơn không tăng thời gian mô phỏng.
- Command có requiresAuthority, kênh Unreliable, input tick và jump-edge counter; heartbeat tối đa 60 Hz. Không gửi position/speed/delta của client. Sau 0,25 giây không có input hợp lệ, chuyển động ngang dừng; gravity vẫn chạy.
- Player prefab thêm NetworkMovement, ServerPlayerMovement, NetworkTransformReliable ServerToClient tới tất cả observers, kể cả owner. Position/rotation bật, interpolation bật, position precision 1 mm, root world transform là target. Client replica tắt CharacterController; host/server giữ controller hoạt động.
- Scene offline/fixture gỡ đủ năm component mạng và giữ nguyên assertion gameplay cũ. Camera occlusion dùng renderer bounds khi controller client bị tắt, giữ collider môi trường.
- Giữ đúng bảy asmdef, không Client↔Server. Chỉ PlayMode.Tests thêm tham chiếu vendor Mirror.Components để kiểm tra NetworkTransform thật. Không sửa Mirror/vendor, Input Actions, build indices hoặc GUID cũ.

## Kết quả cuối

| Kiểm tra | Kết quả | Bằng chứng |
| --- | --- | --- |
| Biên dịch Roslyn, coi cảnh báo mã nhóm là lỗi | **7/7 asmdef nhóm đạt**, không lỗi/cảnh báo mã nhóm; hai dependency Mirror cũng biên dịch đạt | TestResults/TV12/Compilation/results.json |
| Unity native compilation/Mirror weaving | Thành công; không thiếu script/tham chiếu hoặc lỗi biên dịch mã nhóm | Logs/TV12-PlayMode.log |
| PlayMode đầy đủ | **209 ca: 207 đạt, 0 lỗi, 2 package bỏ qua** | TestResults/TV12/PlayMode.xml |
| NetworkMovementTests | **23/23 đạt**; nhóm có **205/205** ca PlayMode đạt, gồm toàn bộ **182** ca trước TV1.2 | Cùng PlayMode.xml |
| EditMode, toàn bộ namespace test của nhóm | **34/34 đạt**, không lỗi/bỏ qua | TestResults/TV12/EditMode.xml |
| Saved layout | **2.785 assertion đạt**, 12 YAML | TestResults/ArenaLayout/results.json |
| Scaffold/GUID/build | Bảy asmdef không vòng; **1.437** cặp metadata hợp lệ; MainMenu 0 / Bootstrap 1 / Arena_MVP 2 | TestResults/TV12/scaffold.txt |
| Đối chiếu mã và asset chạy native | **154 tệp** script/test/scene/prefab và metadata khớp SHA-256 với workspace | TestResults/TV12/tested-source-hashes.json |

Hai ca package bị bỏ qua vẫn là WindowsInput_MouseMovements_AreDetected và WindowsInput_RemoteDesktopMouseMovements_AreDetected, lý do Input System ghi “Unstable due to 1252825”. Không tính các ca này là đạt. Native import còn các cảnh báo analyzer vendor/Mirror; không sửa vendor để che cảnh báo.

## Assertion mới

1. Clamp vector quá lớn, gồm float.MaxValue, về unit magnitude; giữ analog dưới 1; từ chối NaN/Infinity trên cả trục; xử lý wrap uint và bỏ lặp/đến muộn.
2. Command quá magnitude không tạo displacement ngang vượt 5 × fixedDelta × số tick server.
3. WASD đi qua PlayerInputHandler → PlayerController → Command được Mirror weave/serialize/deliver thật → server simulation; sửa moveSpeed client thành 999 không đổi tốc độ server.
4. Một burst **200 Command** không mua thêm bước mô phỏng; NaN, replay và out-of-order không thay input hợp lệ.
5. Mất heartbeat dừng chuyển động ngang; jump counter tạo một jump grounded, bỏ cạnh trên không và không tự jump khi landing. Landing assertion tính đúng skin width của CharacterController.
6. Remote replica không đọc phím, không chạy character physics và không tự đổi position/yaw.
7. NetworkTransform thật serialize/deserialize trạng thái, owner/observer đều không có quyền upload pose, nội suy giữa snapshot; server bỏ dữ liệu pose gửi ngược chiều.
8. Camera owner vẫn phát hiện tường che nhân vật bằng renderer bounds khi collider replica bị tắt; collider tường không bị vô hiệu hóa.

## Môi trường và giới hạn

Workspace đã có hai tệp package được người dùng sửa và Unity Editor đang mở. Giữ nguyên các chỉnh sửa đó, xác nhận hash hai tệp package không đổi. Đồng bộ source mới vào bản sao kiểm chứng đã đóng tại `TestResults/TV11/ValidationProject` để dùng lại cache import; kết quả lần chạy mới nằm riêng dưới TV12. Không dừng Editor của người dùng. Không dùng kết quả TV1.1 thay lần chạy TV1.2.

Lần thử đầu chỉ có một assertion landing không tính skin width, đã sửa theo contact offset thực của CharacterController. Sau khi toàn bộ movement test đạt, bổ sung kiểm tra và sửa camera occlusion cho collider disabled, rồi chạy lại đầy đủ; bảng trên là kết quả cuối cùng. Log/XML lần thử trước được giữ riêng trong TestResults/Logs.

Command tests dùng hàng đợi LocalConnection của Mirror thật; replica flags trong kiểm tra deserialize/interpolation được thiết lập trong fixture của cùng tiến trình. Đây chưa phải kiểm chứng UDP/KCP nhiều tiến trình hoặc latency/jitter thực tế. Owner chưa có prediction/reconciliation, nên phản hồi vẫn chịu RTT và interpolation buffer. Combat/HUD/lobby mạng và phục hồi phiên có token chưa nằm trong TV1.2.

Baseline sẵn sàng cho **TV1.3 — kiểm chứng nhiều client, quan sát cùng trạng thái server và phục hồi khi disconnect**. Diff riêng nhiệm vụ: **12 tệp sửa, 12 tệp mới** (năm C#, năm `.meta`, hai tài liệu), không tính hai package có chỉnh sửa trước và context cục bộ. Chi tiết: `TestResults/TV12/task-files.json`, `TestResults/TV12/git-status.txt`. Không stage/commit/push; AGENTS.md/PROJECT_CONTEXT.md và dữ liệu chạy tiếp tục được ignore.
