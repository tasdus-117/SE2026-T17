# Ghi chú chứng cứ AD-065

Ngày kiểm tra: 24/10/2026, Asia/Bangkok. Tài liệu này hỗ trợ báo cáo, không phải bài nộp thay thế.

## Kiểm kê nguồn và phạm vi

- Yêu cầu trực tiếp: `C:/Users/hoang/.codex/attachments/bb847455-71ff-4c0f-bcfe-321118258753/Pasted text.txt`; đã đọc, gồm quy trình đúng năm phiên và chính sách chưa xuất PDF.
- ZIP nguồn: `C:/Users/hoang/OneDrive/Desktop/lecture_and_srs.zip`; giải nén vào `assignment_sources/lecture_and_srs/`, không sửa các tài liệu gốc.
- Lecture 04: `Lecture_04_Software_Architecture_and_System_Design.md`, 11171 byte; đã đọc đầy đủ. Nguồn khái niệm, không phải chứng cứ triển khai.
- Lecture 05: `Lecture_05_Design_Code_Quality_Technical_Debt_v2.md`, 11154 byte; đã đọc đầy đủ. Nguồn phương pháp thiết kế/refactoring/debt/review.
- `srs.md`, 72321 byte, v0.2 ngày 24/10/2026; đã đọc toàn bộ Parts 1–8, FR-001–017, NFR-001–007, TC-001–037, milestones, future backlog và reference/control. SHA256: `14739BF4EE42C6D3DBD1DCCB1DF392873F013441F6853096850111D458581ACC`. Trùng bản `srs.md` sẵn có ở workspace.
- `learning-roadmap.md` có trong workspace nhưng không nằm trong ZIP; không dùng làm nguồn yêu cầu bổ sung.
- Không thấy AGENTS.md tại workspace hoặc thư mục cha được kiểm tra.

## Chứng cứ repository trực tiếp qua GitHub connector

1. `GET https://api.github.com/repos/tasdus-117/SE2026-T17`: public, default_branch `main`.
2. `GET https://api.github.com/repos/tasdus-117/SE2026-T17/branches?per_page=100`: đúng một nhánh `main`, SHA `b5054a61258e8a4d274fe56198dd5d0a5304124b`; không có trang tiếp cần đọc trong kết quả một phần tử.
3. `GET https://api.github.com/repos/tasdus-117/SE2026-T17/git/trees/b5054a61258e8a4d274fe56198dd5d0a5304124b?recursive=1`: `truncated:false`, đúng một blob `README.md`, 8 byte, SHA `c02ccbb56ab9408630265a84c7a01d1bd6e8761f`.
4. Đọc `README.md` ở SHA trên: `git init`. Không có symbol gameplay hoặc test để dẫn.

Không sửa repository, không push/PR, không tạo file mã giả để làm chứng cứ. Kết luận chỉ áp dụng snapshot đã kiểm tra, không khẳng định cho commit tương lai.

## Bản đồ chứng cứ và trạng thái

| Nguồn | Trạng thái nguồn | Khái niệm / cách dùng | Giới hạn |
|---|---|---|---|
| SRS §1.1–1.3, §4.1.2 | Confirmed MVP, Proposed tuning, Future Scope networking | Phân biệt phạm vi | Không có implementation |
| SRS §2.1–2.3 | Proposed technology | Unity/C#; không dùng records trong ví dụ | Không xác minh package đã cài; không lặp lại tuyên bố vòng đời sản phẩm như thực trạng |
| SRS §3.1, §3.4–3.7, §4.3 FR-008–017 | Quy tắc Confirmed + chi tiết Proposed | Combat/Health/Pickup/Match ownership | Tên component là thiết kế SRS, không phải file thật |
| SRS §4.4, §5.3 TC-031–037 | Proposed, Planned | Local performance/testability | Không thay thế benchmark AD-065 |
| SRS §5.1–5.4 | Planned | Characterization, integration, regression | Không có test đã chạy |
| SRS §7.1–7.4 | Future Scope | Authority, bốn người/trận, không tin damage client | Database/rollback do AD-065 bổ sung |
| AD-065 | SPECIFIED | 764; ≤4,5 giây; 7 ngày; 12 giờ; schema rollback | Workload/hardware/retention categories cần quyết định |
| Lecture 04/05 | Nguồn khái niệm | Drivers, views, ownership, ADR, smells, debt, review | Không chứng minh kiến trúc đã hiện thực |

## Nhật ký năm phiên

1. Hoàn thành phần 1 trong master Markdown: đúng ba driver, sáu phần scenario, workload/measurement và trade-off; kiểm tra không nhập nhằng FPS/p95 server. Thiếu benchmark và workload được duyệt.
2. Hoàn thành phần 2: hai Mermaid, sáu module kể cả client (năm module server), port theo dependency inversion, ownership/ACL, transaction và invariant. Đối chiếu hai view: MatchApp gọi MatchStore qua port, domain không ghi DB, rollback binary giữ schema. Tất cả server/database là PROPOSED; còn thiếu migration rehearsal, policy hủy trận, RPO/RTO và cấu hình thực thi.
3. Hoàn thành phần 3: so sánh ba phương án, ADR-001 đủ bối cảnh/quyết định/phương án/hệ quả/trigger; giữ baseline §2. Dedicated authority là mở rộng AD-065, không phải yêu cầu offline hoặc triển khai sẵn. Chưa có xác nhận nhóm, benchmark, chính sách backup/kết quả trận; 12 giờ là phản hồi yêu cầu, không là hoàn thành lập trình.
4. Hoàn thành phần 4: feature FR-008–010/015–017; ba rủi ro Q1–Q3 được đánh dấu UNVERIFIED; hai đoạn minh họa C# cùng public adapter và policy âm→0. Có kế hoạch T1–T6, ba debt item có lý do/owner/trigger và checklist bổ sung. Chưa có mã thật/compile/gameplay test; rubric code thực vẫn thiếu. Không coi absence implementation tự động là shortcut kỹ thuật.
5. Hoàn thành phần 5 và Phụ lục A: đầu ra sơ bộ đã hiển thị thật bằng tiếng Anh trong commentary; sửa per-hit SQL, bác bỏ đổi exception policy. Đánh giá là tự review bởi cùng AI, không gán cho reviewer/người học độc lập. Có traceability, trạng thái rubric và việc chưa tạo PDF. Đã hoàn tất audit Mermaid/hình thức như dưới đây.

## Kiểm tra cuối

- Đúng năm phiên, đúng ba driver D1–D3; scenario đủ Source/Stimulus/Environment/Artifact/Response/Measurable Response. Các giá trị 764, 4,5 giây, 7 ngày, 12 giờ được giữ.
- Hai Mermaid editable, đều là flowchart (Module View và Runtime View). Parse và render thật bằng Mermaid **12.2.0**, qua Chrome headless và Playwright; đã kiểm tra trực quan hai PNG. Sửa dấu chấm phẩy trong bản sequence sơ bộ sau lỗi parser; bản cuối dùng flowchart để tăng khả năng đọc A4. Không khẳng định đã kiểm tra mọi phiên bản Mermaid cũ.
- Rút tên module đề xuất thành MatchApp/Combat, nhất quán trong hai view, ADR, ownership và traceability. Port trong Module View chính là IMatchResults, thuộc MatchApp; dòng giải thích ánh xạ được giữ.
- Profile preview: A4, lề 12 mm, nội dung 186×273 mm, Arial 9,5 pt, bảng 9 pt, code 8,6 pt; Mermaid font nguồn 20 px (sau co theo chiều rộng khoảng 8,3 pt). Profile print được ghi ngay trong Markdown. Bảng không tràn ngang; cả hai ảnh đã xem, nhãn tiếng Việt không mất dấu.
- Kết quả HTML cuối: chiều cao bảy khối chính lần lượt **261,7; 242,9; 271,4; 229,4; 242,7; 244,7; 251,3 mm**. Khối thứ tám là Phụ lục A, 246,1 mm. Cần xác nhận lại sau mọi thay đổi hoặc khi đổi renderer; đây không phải số trang PDF đã kiểm chứng.
- File kiểm tra trung gian nằm trong `.assignment-validation/`: script tái lập, preview HTML, SVG/PNG sơ đồ và audit JSON; không phải tài liệu nộp. Npm cài Mermaid vào thư mục tạm sau khi lần sandbox báo ENOTFOUND; không thêm dependency vào dự án game.
- Không có gameplay/load test nào được chạy, không compile C#, không rehearsal migration/rollback. Các test T1–T6 và TC trong SRS giữ PLANNED.
- Chưa tạo PDF, không triển khai game, không sửa/push repository chính thức. Nội dung chính tiếng Việt; quote AI/prompt và identifier giữ nguyên tiếng Anh.
- Rubric: phần kiến trúc/ADR đủ nội dung đề xuất; code thực chưa đáp ứng, debt là rủi ro có điều kiện/khoảng trống chứng cứ; review AI mới xác minh requirements, chưa implementation. Không tự gán điểm hoặc claim đủ 100%.
