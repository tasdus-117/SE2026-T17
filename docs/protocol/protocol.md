# Khung tài liệu giao thức

Hợp đồng logic cần được mô tả dù Mirror phụ trách serialization và truyền mạng.

| Thông điệp dự kiến | Hướng | Ý nghĩa | Kiểm tra chính |
|---|---|---|---|
| JoinLobby | Client → Server | Xin vào phòng | Phiên bản, quyền truy cập, còn chỗ |
| SetReady | Client → Server | Đổi trạng thái sẵn sàng | Đang ở lobby, đúng chủ thể |
| PlayerInput | Client → Server | Hướng di chuyển, input sequence | Số hữu hạn, phạm vi giá trị, thứ tự, tần suất |
| FireRequest | Client → Server | Xin thực hiện phát bắn | Còn sống, cooldown, đạn, hướng và nguồn bắn hợp lệ |
| MatchSnapshot | Server → Client | Trạng thái được xác nhận | Client bỏ bản cập nhật cũ theo quy tắc đã chốt |
| ResumeSession | Client → Server | Xin phục hồi phiên | Token hợp lệ, còn hạn, không chiếm phiên người khác |
| ActionRejected | Server → Client | Báo lỗi theo mã | Không lộ token hoặc thông tin nhạy cảm |

Đây là tên logic dự kiến; triển khai có thể dùng message, Command/RPC hoặc đồng bộ trạng thái của Mirror. Không tạo thêm message cho dữ liệu framework đã truyền đủ nếu không cần.

Với mỗi thông điệp, ghi rõ: trường, kiểu, đơn vị, khoảng hợp lệ, giới hạn kích thước, thứ tự/độ tin cậy cần thiết, giới hạn tần suất, xử lý lặp/mất/đến muộn và mã lỗi. Không tin timestamp client mà không kiểm tra giới hạn.

Không coi Player ID là bằng chứng sở hữu phiên. Không commit token thật hay ghi token đầy đủ vào log.

## MovementInput đã triển khai ở TV1.2

Mirror Command `NetworkMovement.CmdSubmitMovement` đi Client → Server trên kênh Unreliable, yêu cầu quyền sở hữu identity mặc định của Mirror. Payload thuộc Shared.Protocol:

| Trường | Kiểu | Ý nghĩa và xử lý |
| --- | --- | --- |
| Sequence | uint | Tick input tăng theo gói gửi; bỏ lặp/đến muộn bằng so sánh serial uint có wrap, không dùng để cấp thêm thời gian mô phỏng |
| MoveX, MoveZ | float | Trục thế giới X/Z; phải hữu hạn; server clamp độ dài ≤ 1, giữ magnitude analog dưới 1 |
| JumpSequence | uint | Bộ đếm cạnh Space; heartbeat lặp lại giá trị, server chỉ tiêu thụ cạnh mới và chỉ nhảy khi grounded |
| LifeId | long | Thế hệ sống hiện hành; server đối chiếu NetworkHealth.LifeId khi lifecycle đã bind; gói đời cũ bị từ chối |

Client gửi tối đa 60 heartbeat/giây theo clock cục bộ. Server chỉ cập nhật ý định mới nhất; physics chạy bằng fixed delta của server với tốc độ ngang tối đa 5 m/s. NaN/Infinity hoặc sequence không mới bị bỏ và không gia hạn timeout; input quá 0,25 giây không còn tạo chuyển động ngang. Không có client delta, tốc độ, vị trí hoặc rotation trong payload.

State position/rotation đi Server → mọi client quan sát, gồm owner, bằng NetworkTransformReliable cấu hình ServerToClient; không thêm RPC snapshot trùng lặp. Xem [thiết kế movement](../architecture/tv1.2-server-movement.md).

## TV2-004 logical contracts and integration review

Lịch sử 22/11/2026, main `995e6a4`: xem [CT-201–205/runtime/evidence](tv2-004-contracts.md). M3 đã thêm LifeId vào movement, attack và pickup; client/server phải cùng build. Attack hiện là reliable owner Command `(Vector3 aimDirection, long lifeId)`, pickup `(uint itemNetId, long lifeId)`. Các thay đổi tài liệu này không đổi wire. Raycast/reconnect và revisioned presentation adapter vẫn chờ scope/owner; không coi bảng thông điệp dự kiến đầu trang là danh sách API production.

Hiện hành 25/11/2026 tại main74a6c56: #31/#37 đã có CmdSubmitShot(ShotIntent), server-origin raycast, shooter TargetFeedback và production presentation provider. Xem [source audit CT-201–205](../testing/tv2-current-contract-audit.md); các câu chờ adapter ở trên là lịch sử. Retained reconnect vẫn ngoài scope.

## Draft giao thức Milestone 4

[M4.0 — Room Directory/Code, Public Multiplayer Hub và Retained-Session Reconnect](tv1-m4-contracts.md) đề xuất CT-401–408, baseline ACK barrier và checklist review TV1/TV2/TV3. Đây là draft chưa ký duyệt/chưa triển khai; không thay wire, luật hoặc phạm vi nghiệm thu M3 hiện hữu.
