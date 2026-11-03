# Small Arena (GAME-3D-03) — SE2026-T17

Arena 3D tối đa 4 người, định hướng Unity + Mirror, server quyết định trạng thái quan trọng.

## Trạng thái bàn giao

Project Unity thật và mẫu hành vi offline đã được phục hồi từ `9eed902` và di trú vào cấu trúc mới của main `adedfa1`. Có bảy asmdef, scene/prefab và bộ test; chưa có NetworkPlayer/spawn/lobby/reconnect được kiểm chứng hay workflow CI. Các file `.gitkeep` tiếp tục giữ chỗ cho phần nhóm chưa triển khai.

Xem [báo cáo kiểm toán/di trú](docs/architecture/main-migration-audit.md) và [kết quả kiểm chứng](docs/testing/migration-results.md). Các tệp ngữ cảnh tác nhân và cấu hình máy cá nhân được giữ cục bộ, không đưa vào Git.

## Bắt đầu triển khai

1. Dùng Unity Hub mở gốc repository bằng Unity `6000.6.7f1`; giữ `Packages/` và `ProjectSettings/` đã phục hồi từ project thật.
2. Mirror `96.0.1` giữ nguyên ở `Assets/Mirror`; URP, New Input System và TMP/uGUI có trong cấu hình package. Không tạo manifest giả hoặc chép Mirror vào Scripts của nhóm.
3. Mở `Assets/_Project/Scenes/MainMenu.unity`, vào Play và chọn Single Player. Build order: MainMenu 0, Bootstrap 1, Arena_MVP 2.
4. WASD di chuyển, Space nhảy, J đấm, E nhặt hộp vàng. Bot hết 10 HP sẽ hiện Victory; Play Again tạo trận mới. Multiplayer/Tutorial còn Coming Soon.
5. Player/Bot prefab ở `Prefabs/Characters`, pickup ở `Prefabs/Combat`. Di chuyển asset cùng `.meta`; không đổi GUID để sửa đường dẫn.
6. Domain/Protocol độc lập engine; Server và Client chỉ dùng Shared/Networking, không tham chiếu nhau. Runtime tổng hợp cũ đã loại bỏ. Chạy các lệnh kiểm tra trong runbook trước khi tích hợp.
7. Trọng tâm TV1 tiếp theo: kết nối → NetworkPlayer mỏng/NetworkStartPosition → spawn/ownership → ý định và xác thực server → hai client nhìn thấy nhau; sau đó lobby/reconnect. Mẫu offline không phải bằng chứng multiplayer đã xong.

## Tài liệu

- `docs/architecture/structure.md`: giải thích từng thư mục và ví dụ file tương lai.
- `docs/architecture/authority.md`: quyền quyết định và hướng phụ thuộc.
- `docs/protocol/protocol.md`: khung tài liệu giao thức.
- `docs/testing/test-plan.md`: kiểm thử chức năng, tải, mạng và gian lận.
- `docs/operations/runbook.md`: chạy, build, CI và cấu hình.

Quy ước: tên file C# và lớp dùng PascalCase; một lớp chính trong một file. Không dùng thư mục chung tên `Managers` hoặc `Utils` để chứa mọi thứ. Thêm thư mục theo nhu cầu thật.
