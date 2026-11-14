# AI Usage Log

| Lượt/công cụ | Prompt/nguồn | Output dùng | Review/sửa/bác bỏ | Evidence |
|---|---|---|---|---|
| Codex — plan trước review | Prompt lịch sử không có trong bản ghi hiện truy cập; không dựng nguyên văn. | plan-v1, khảo sát call path | Sinh viên sửa R1/R2, giữ R3; chốt Câu 1 ngày 2026-11-14. | plan-v1.md, review-01-user-verbatim.md, review-02-user-verbatim.md, question-1.md |
| Codex + local Git/Python | User yêu cầu tiếp tục Câu 2: sinh ≥12 cases, pairwise/negative/failure, gửi case yếu để sinh viên sửa/xóa; giữ AC1–AC5 và baseline. | 18 cases AI v1; constrained matrix 12 rows; verifier 32 feasible pairs | Sinh viên trực tiếp xóa T17/T18. Nội dung gốc, phản hồi nguyên văn, before/after lưu riêng; còn 16 cases. | test-cases-ai-v1.md, test-review-user-verbatim.md, test-cases-reviewed.md, pairwise-review-verification.json |
| Codex + Unity 6000.6.8f1 PlayMode | User yêu cầu worktree riêng, baseline đúng hai suite | Nhánh codex/bt3-vt-066 từ full baseline; 19/19 pass, exact suites 12+7 | Chưa là bằng chứng tính năng mới. License warnings được lưu, import settings diff tách riêng. | evidence/baseline/combined.xml, combined.log, combined-manifest.json, summary.md |

## Quyết định sinh viên sửa/xóa AI-generated tests

Phản hồi nguyên văn đầy đủ tại `test-review-user-verbatim.md`, bao gồm:

```text
- Xóa T17. Case này trùng T03 và assertion `TryStart(host) == false` chưa đủ để chứng minh retry chỉ kích hoạt một lần tải. Hãy kiểm tra bảng cuối vẫn có test AC2 đếm số lần gọi boundary tải khi gửi Start lặp trong cùng phiên.
- Xóa T18. Case này chờ cố định 2 giây và tìm text trong RoomPanel, vốn có thể bị hủy khi chuyển scene. Giữ T08/T09 để kiểm tra overlay qua các mốc lifecycle của client, đặc biệt là sau khi RoomPanel bị hủy và trước khi client sẵn sàng.
```

T17 gốc: Unit/AC2/P2; Host đã Start thành công; gọi TryStart(host) lần nữa; expected TryStart trả false. Sau review: xóa.

T18 gốc: UI/AC3/P2; Bấm Start hợp lệ; chờ cố định 2 giây; expected tìm thấy text “Loading arena...” trong RoomPanel. Sau review: xóa.

Không thay lời sinh viên bằng nhận xét tự động. AI v1 được giữ nguyên; bảng reviewed chỉ giữ T01–T16. Kiểm lại pairwise và điều kiện số lượng/negative/failure được lưu, không coi xóa hai case là tự động đủ coverage.
