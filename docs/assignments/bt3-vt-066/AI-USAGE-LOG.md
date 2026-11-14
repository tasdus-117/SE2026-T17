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

## Generate → verify → diff review

Prompt đang có: user yêu cầu tiếp tục Câu 3 sau xóa T17/T18, dùng Unity 6000.6.8f1 PlayMode, lưu RED→patch→GREEN, đo true boundary failure, trình diff. Output: LobbyStartOperation (production coordinator dùng cùng logic với harness), local overlay, bridge lifecycle, guard/preflight và recovery.

- Test-first commits: `267bbca668c50975556780662787ad7760c4d177`, sửa UnityAction compile tại `c9a27babe977155c9c7b4e95c8d7c4dd2423325a`. Lỗi compile lưu `evidence/red`, không tính RED. Genuine assertions ở `evidence/red-assertions`; cùng filter GREEN ở `green-attempt1`.
- Sinh viên ban đầu chọn nguyên văn **“Yêu cầu sửa patch”**, rồi hỏi **“patch co nhung gi de sua”**. AI rà và đề xuất guard cho valid wrong scene, bỏ constant fakeLoads assertion, làm rõ giới hạn guest test; không ghi những đề xuất cụ thể đó thành lời sinh viên.
- Sinh viên chấp nhận nguyên văn **“Chấp nhận patch local v2”** cho diff SHA256 `9ed34a2675494e973550301a8a0d9ee4c368a93c7a1e9807c2e7e98b12533906`. Feature commit `c4719b4e998f83c1c6eb784ea5b5c239202573d9`.
- AI tiếp tục kiểm assumption recovery, phát hiện manager không được tái tạo nếu failure ngay MainMenu. Test bổ sung RED→GREEN, giữ nguyên AC đã chốt; ghi evidence thay vì sửa âm thầm Câu 1. Sinh viên chấp nhận nguyên văn **“Chấp nhận recovery bổ sung”** cho diff SHA256 `0dd4d87bc2a2643522c5494797a2b9858ca30f036675c8b9469814da786ab9e9`. Commit `02db6c3affce0252e60c3abf20d8bc3f48ef11e7`.
- Final regression 57/57, 20 real scene cycles; mutation clean→mutant→restored bắt count4; load harness 20×100 với fake dependency, không gọi là arena p95 thật. Chi tiết `evidence-summary.json`, `traceability.md`, README.

## Điểm cần challenge và kết quả

RoomPanel label/fixed sleep và bool-only retry bị sinh viên xóa (T18/T17). Constant fakeLoads unit assertion bị AI bỏ, thay evidence bằng integration native sceneLoaded. Recovery UI-readable chưa đủ AC phiên mới: test thật fail trước fix navigation. Matrix data coverage 32/32 không được dùng thay runtime coverage. Guest local hook không được gọi là native guest E2E.

## Trạng thái ownership

Plan đã chốt, test cases có user trực tiếp xóa, local production/recovery có user duyệt. Reflection và cam kết vẫn chờ sinh viên gửi/xác nhận. Chưa có quyền push/PR ở thời điểm ghi mục này; không merge.
