# Kết quả kiểm chứng sau di trú main

Ngày kiểm chứng: **2026-11-03**. Nền main: `adedfa1`; nguồn MVP: `9eed902`; nhánh làm việc: `feature/tv1-m1-migration`. Kết quả dưới đây chạy trên working tree đã di trú của project thật, không lấy kết quả cache trước đó làm bằng chứng mới.

| Kiểm tra | Kết quả thực tế | Bằng chứng cục bộ |
| --- | --- | --- |
| PlayMode trong Unity 6000.6.7f1 | 178 ca, 176 đạt, 0 lỗi, 2 package bỏ qua | TestResults/Migration/PlayMode.xml; Logs/Migration-PlayMode.log |
| EditMode trong Unity | 34 ca, 34 đạt, 0 lỗi/bỏ qua | TestResults/Migration/EditMode.xml; Logs/Migration-EditMode.log |
| NUnit Domain độc lập | 34/34 đạt; là lần đối chiếu cùng bộ EditMode, không cộng trùng | TestResults/Domain/results.xml |
| Roslyn source compilation | Cả bảy asmdef của nhóm và hai dependency Mirror được biên dịch; cảnh báo mã nhóm là lỗi | TestResults/Compilation/results.json |
| Unity import/compilation/Mirror weaving | Thành công, không có lỗi biên dịch module nhóm | Logs/Migration-PlayMode.log |
| Cấu trúc/metadata | Bảy asmdef đúng chiều, không vòng; Domain/Protocol không engine; GUID duy nhất; build index 0/1/2 đúng | tools/Verify-Scaffold.ps1 |
| Layout và liên kết scene/prefab | 2.666 assertion đạt trên 12 YAML; Player/Bot từ Characters, pickup từ Combat | TestResults/ArenaLayout/results.json |
| Bảo toàn asset di chuyển | Chín asset giữ nguyên GUID gốc | TestResults/Migration/asset-moves.json |

PlayMode của nhóm gồm 174 ca và tất cả đạt. Bốn ca khác thuộc Unity.InputSystem.IntegrationTests: hai đạt, hai kiểm tra chuột Windows bị package đánh dấu bỏ qua sẵn. So với nguồn trước di trú, số ca PlayMode/skip không đổi. Tổng hai lượt Unity native là **210 đạt, 0 lỗi, 2 bỏ qua**; không tính thêm 34 ca runner độc lập lần nữa.

Các hành vi được bảo toàn: WASD/giới hạn chéo/hướng cố định, grounded jump và bỏ nhấn trên không, camera follow/không che nhân vật, Health/clamp/chết một lần, đấm mới/cooldown/range/layer, nhặt một lần/buff, Victory/HUD/event cleanup, Main Menu/replay/reset và UI mouse navigation.

2.666 assertion layout hiện tại khác 2.674 của baseline cũ vì đường dẫn và metadata/assembly đã đổi. Bộ kiểm tra vẫn tải đủ ba prefab, hai scene UI, wiring và hình học arena; không giảm số ca PlayMode để làm test xanh.

Đây không phải bằng chứng multiplayer, lobby, reconnect, input ownership mạng, chống gian lận, kiểm thử tải/latency, build client/server hoặc CI. Các mục đó thuộc cổng TV1 M1 và kế hoạch online tiếp theo. Cảnh báo có sẵn của vendor/Editor không được sửa bằng cách thay mã Mirror; compiler mã nhóm bật warnings-as-errors.

Dữ liệu chạy, bản sao nguồn và log nằm trong TestResults/Logs, đều được Git bỏ qua. Chỉ bản tóm tắt này thuộc tài liệu quản lý phiên bản. Chưa tạo commit/PR hoặc push trong nhiệm vụ này.
