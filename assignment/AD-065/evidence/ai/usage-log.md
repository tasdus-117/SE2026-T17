## Thông tin và mức độ sử dụng AI

Sinh viên: Đồng Vũ Hoàng Long, 23001901; SE2026-T17; GAME-3D-03 – Small Arena; AD-065, 3TC, thành viên nhóm. Ngày phiên làm việc: 25/10/2026 (Asia/Bangkok). Công cụ: Codex CLI; không tự khẳng định phiên bản model nội bộ. Không có hội thoại reviewer độc lập hoặc xác nhận cá nhân của sinh viên.

Người dùng yêu cầu nguyên văn: “Read AD-065-prompt.md completely and execute all instructions.” Chỉ dẫn dài được đọc từ AD-065-prompt.md và được lưu đầy đủ trong original-user-prompt.md, đồng thời in ở Phụ lục C. Bản sao này là yêu cầu thật được cung cấp, không phải prompt do AI tái dựng. Các file báo cáo cũ tại Long_document không được dùng làm bài phân tích mới.

## Công việc AI đã thực hiện

AI đọc nhánh, SRS và môi trường; chọn Node/SQLite; viết mã mới, tests và load harness; refactor primitive baseline; tạo Mermaid; phân tích ADR, nợ, truy vết; viết tiếng Việt và dựng PDF. Tất cả mã/phân tích mới đều có hỗ trợ AI, không được trình bày như sinh viên tự viết không dùng AI. Mã baseline độc lập ở fd4df4c; mã sau refactor ở 1ff1033; SHA đầy đủ trong trang 5 và repository-baseline.md.

Các lệnh thực thi gồm git fetch/worktree/commit, node --test, node tools/check.mjs, node tools/load.mjs, Mermaid CLI, Chrome PDF và Poppler. Kết quả thật được lưu tại evidence/tests, evidence/performance, evidence/rollback và evidence/pdf. Không tạo số đo hay xác nhận kiểm tra của con người. Test không chạy được hoặc ngoài phạm vi được đánh dấu rõ trong báo cáo và verification-summary.md.

## Review có thật trong cùng phiên

Prompt phụ được lưu nguyên văn ở design-review-prompt.md; output thực do chính Codex phiên này tạo tại design-review-output.md, in trong Phụ lục B. Hai đề xuất được nêu rồi đánh giá: xóa command identity theo TTL vận hành bị bác bỏ vì phá chống lặp; suy ra scale nhiều writer từ p95 cục bộ bị bác bỏ vì chưa đo contention. Giữ giao dịch đơn và schema cộng thêm dựa trên test. Đây là tự review có AI hỗ trợ, không giả lập nhiều reviewer.

## Sai sót, sửa chữa và giới hạn

Ban đầu concurrency fixture thất bại ECONNREFUSED; thêm backlog chưa đủ. Tách server khỏi process tải đã giải quyết fixture mà giữ nguyên 764 tác vụ. Cleanup rollback gặp EPERM do handle DB mở; đã sửa thứ tự đóng. Mermaid sequence gặp lỗi cú pháp do dấu chấm phẩy trong label; đã thay văn bản và render lại. Lỗi và cách sửa được lưu, không bỏ qua test để tạo kết quả đẹp.

Chưa kiểm chứng Unity, PostgreSQL, WAN, nhiều writer, soak dài, ticket SLA 12 giờ, phục hồi production hoặc quyết định của nhóm. Chỉ số nợ và review trigger là đề xuất AI, không phải nhận xét cá nhân được gán cho sinh viên. Trách nhiệm cuối cùng thuộc người nộp; phiên này không có bằng chứng sinh viên đã tự kiểm tra mọi lập luận.
