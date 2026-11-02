# SE2026-T17 — bộ khung dự án

Arena 3D tối đa 4 người, định hướng Unity + Mirror, server quyết định trạng thái quan trọng.

## Trạng thái bàn giao

Đây là cấu trúc thư mục và tài liệu kiến trúc, chưa phải game chạy được. Chưa có mã gameplay, scene, prefab, thư viện Mirror, file assembly definition hay workflow CI thực thi. Các file `.gitkeep` chỉ giữ thư mục rỗng trong Git.

## Bắt đầu triển khai

1. Nhóm thống nhất phiên bản Unity, Mirror và nền tảng đích rồi ghi lại trong tài liệu dự án.
2. Tạo một Unity 3D project bằng Unity Hub ở thư mục làm việc riêng. Sao chép nội dung bộ khung này vào thư mục gốc của project đó, giữ nguyên các file Unity sinh ra.
3. Cài Mirror theo tài liệu chính thức của phiên bản đã chọn. Không tự tạo manifest giả hoặc chép mã Mirror vào Scripts của nhóm.
4. Unity sẽ tạo hoặc cập nhật file `.meta`; commit chúng cùng tài nguyên tương ứng. Commit `Packages/` và `ProjectSettings/` do Unity sinh ra. Các thư mục này cố ý chưa được tạo trong bộ khung.
5. Tạo scene và prefab bằng Unity Editor theo `docs/architecture/structure.md`.
6. Khi bắt đầu viết code, tạo asmdef và cấu hình tham chiếu theo assembly thực tế của Mirror và Unity Test Framework. Phân tách thư mục chưa đủ để bảo đảm cách ly build.
7. Triển khai lát cắt nhỏ: kết nối → spawn → di chuyển do server quyết định → hai client nhìn thấy nhau. Sau đó thêm lobby, combat, reconnect và kiểm thử.

## Tài liệu

- `docs/architecture/structure.md`: giải thích từng thư mục và ví dụ file tương lai.
- `docs/architecture/authority.md`: quyền quyết định và hướng phụ thuộc.
- `docs/protocol/protocol.md`: khung tài liệu giao thức.
- `docs/testing/test-plan.md`: kiểm thử chức năng, tải, mạng và gian lận.
- `docs/operations/runbook.md`: chạy, build, CI và cấu hình.

Quy ước: tên file C# và lớp dùng PascalCase; một lớp chính trong một file. Không dùng thư mục chung tên `Managers` hoặc `Utils` để chứa mọi thứ. Thêm thư mục theo nhu cầu thật.
