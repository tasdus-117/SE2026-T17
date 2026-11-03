# Quyền quyết định và hướng phụ thuộc

## Baseline đang áp dụng

Bảy asmdef hiện tại: Shared.Domain, Shared.Protocol, Networking, Server, Client, EditMode.Tests và PlayMode.Tests (tiền tố SmallArena.). Domain/Protocol độc lập engine; Protocol dùng Domain; Networking dùng Shared/Mirror; Server và Client dùng Shared/Networking, không tham chiếu nhau. Trạng thái offline do MatchStateMachine trong Domain quản lý; adapter Server nhận input/readiness/lệnh kết thúc qua interface Shared.

Mẫu offline không chứng minh authority trên mạng. [TV1.1](tv1.1-network-lifecycle.md) có server-owned spawn và gate local owner. [TV1.2](tv1.2-server-movement.md) chuyển di chuyển mạng sang Command ý định → validation → FixedUpdate/CharacterController trên server → NetworkTransformReliable ServerToClient. Client không gửi pose, speed hoặc delta được tin cậy; nhánh PlayerController offline vẫn giữ riêng. TV1.3 kiểm chứng nhiều tiến trình và phục hồi khi disconnect. Xem [báo cáo di trú](main-migration-audit.md) và ngữ cảnh cục bộ `PROJECT_CONTEXT.md`.

## Luồng xử lý

Input client → hợp đồng Protocol → adapter Mirror → kiểm tra server → mô phỏng/combat → trạng thái server → replication → client hiển thị.

Server quyết định vị trí hợp lệ, máu, sát thương, cooldown, trạng thái trận và điểm số. Client gửi thao tác, không gửi kết quả được tin cậy. Hiệu ứng tại client có thể xuất hiện trước khi xác nhận nhưng phải có chính sách sửa sai lệch.

## Phụ thuộc

- Shared/Domain là nền tảng: không tham chiếu Client, Server hoặc UI.
- Shared/Protocol có thể dùng Domain, không dùng UI hoặc scene.
- Server dùng Shared, không tham chiếu Client.
- Client dùng Shared, không gọi trực tiếp logic Server.
- Networking kết nối các phía qua API hoặc interface rõ ràng. Không để Server phụ thuộc ngược vào một lớp Networking vốn đang tham chiếu Server.
- Editor/Build phục vụ build; không được gọi từ runtime.
- Tests tham chiếu đúng phần cần kiểm tra.

Thư mục không tạo ranh giới biên dịch. Khi triển khai asmdef cần chọn tên assembly thực tế, cấu hình nền tảng và điều kiện build phù hợp phiên bản Unity/Mirror. Không gộp adapter dùng chung và adapter chỉ chạy server vào một assembly rồi loại Server mà vẫn giữ tham chiếu tới nó.

Host có cả client và server trong một tiến trình. Không giả định rằng mọi build client đều có thể loại sạch Server nếu vẫn muốn hỗ trợ host. Nhóm cần chốt hỗ trợ host hay dedicated-only trước khi cấu hình loại mã khỏi build.

## Những điều cần chốt

- Unity/Mirror và phiên bản cụ thể.
- Nền tảng client và server.
- Dedicated server hay có thêm host.
- Tick rate, tần suất đồng bộ và giới hạn số lệnh.
- Bắn raycast hay projectile.
- Có prediction, reconciliation, lag compensation ở mức nào.
- Thời gian giữ phiên reconnect và cách xác thực quyền lấy lại phiên.
