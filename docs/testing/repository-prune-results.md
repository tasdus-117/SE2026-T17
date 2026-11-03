# Kết quả đổi tên ngữ cảnh và dọn repository

Ngày kiểm chứng: 2026-11-03, Unity 6000.6.7f1.

## Ngữ cảnh và phạm vi

Đã đọc ngữ cảnh tiếng Việt và toàn bộ `docs/architecture/` trước khi sửa/xóa. [PROJECT_CONTEXT.md](../../PROJECT_CONTEXT.md) thay thế tên AGENT.md; giữ đầy đủ nội dung trước đó, cập nhật chỉ dẫn trong [AGENTS.md](../../AGENTS.md), README, tài liệu kiến trúc, kế hoạch test và công cụ kiểm tra scaffold. Kiến trúc, ranh giới TV1–TV3 và trọng tâm TV1.1 không đổi.

Không xóa/di chuyển tài nguyên trong Assets, không sửa gameplay, scene, prefab, input actions, bảy asmdef hoặc test. Các thư mục scaffold, vendor, tài nguyên TMP và công cụ dựng scene có mục đích sử dụng nên được giữ lại.

## Các mục đã dọn

Tất cả đường dẫn dưới đây tương đối với `TestResults/`, là dữ liệu phát sinh được Git bỏ qua. Tổng cộng 18 mục, 24.473 tệp, 1.016.174.246 byte.

| Mục | Lý do |
| --- | --- |
| `WeaponPickupValidationProject/` | Bản sao project kiểm chứng cũ và cache riêng; regression chạy trên project thật |
| `TmpEssentialResources/` | Nguồn giải nén TMP tạm; giữ tài nguyên đã nhập và công cụ nhập |
| `MatchInitialization/`, `Task01/` | Log/trạng thái Git cũ không có tham chiếu từ tài liệu hiện hành |
| `Migration/Source/`, `Migration/feature-baseline.tar` | Bản nguồn tạm của lần di trú đã hoàn tất |
| `Combat-PlayMode.xml`, `MenuMouse-PlayMode.xml` | Báo cáo tính năng cũ được regression tổng hợp thay thế |
| `MenuNavigation-git-status.txt`, `MenuNavigation-PlayMode.xml`, `MenuNavigation-Snapshot.json` | Bằng chứng menu trước di trú |
| `PlayerController.xml`, `PlayerLocomotion-PlayMode.xml`, `ThirdPersonCamera-PlayMode.xml` | Báo cáo locomotion/camera trước di trú |
| `VictoryFlow-PlayMode.xml`, `VictoryFlow-Snapshot.json` | Báo cáo victory trước di trú |
| `WeaponPickup-PlayMode.xml`, `WeaponPickup-Snapshot.json` | Báo cáo pickup trước di trú |

Giữ các báo cáo được [baseline di trú](migration-results.md) tham chiếu: Migration/PlayMode.xml, EditMode.xml, asset-moves.json, git-status.txt, Domain/results.xml, Compilation/results.json và ArenaLayout/results.json. Không cần dọn `.meta` vì không xóa mục nào trong Assets. Không tìm thấy backup hoặc tệp rác hệ điều hành trong cây nguồn được kiểm toán.

## Kết quả regression

| Kiểm tra | Kết quả | Bằng chứng dưới TestResults/RepositoryPrune |
| --- | --- | --- |
| Roslyn, coi cảnh báo mã nhóm là lỗi | 7/7 assembly đạt; thêm hai dependency Mirror đạt | Compilation/results.json |
| PlayMode native | 178 ca: 176 đạt, 0 lỗi, 2 ca package bỏ qua như baseline; 174/174 ca nhóm đạt | PlayMode.xml |
| EditMode native, toàn bộ namespace test EditMode của nhóm | 34/34 đạt, không lỗi/bỏ qua | EditMode.xml |
| Scaffold/GUID/build settings | Không vòng; 1.427 cặp metadata; scene 0/1/2 đúng | scaffold.txt |
| Bảo toàn cây Assets/Packages/ProjectSettings | 2.616 tệp giữ nguyên SHA-256, không thêm/xóa tệp | protected-before.json, preservation.txt |
| Danh sách xóa và trạng thái Git | Giữ thay đổi di trú có trước; không stage/commit/push | pruned-items.json, git-status-before.txt, git-status-after.txt |

Hai log Unity mới ở `Logs/RepositoryPrune-PlayMode.log` và `Logs/RepositoryPrune-EditMode.log`; không phát hiện chẩn đoán biên dịch C# hoặc script/tham chiếu thiếu. Bằng chứng chạy được Git bỏ qua; tài liệu này là bản tóm tắt có thể review. Working tree vẫn có thay đổi di trú từ trước nhiệm vụ, không được mô tả là sạch.

Kiểm tra cuối: toàn bộ liên kết Markdown nội bộ trong các chỉ dẫn gốc và docs hợp lệ; `git diff --check` đạt. So với danh sách đường dẫn Git lúc bắt đầu, chỉ thay AGENT.md bằng PROJECT_CONTEXT.md và thêm báo cáo này; các chỉnh sửa nội dung khác nằm trong chỉ dẫn/tài liệu và công cụ kiểm tra tên tệp ngữ cảnh.
