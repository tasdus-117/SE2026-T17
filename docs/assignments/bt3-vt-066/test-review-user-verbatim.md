# Test review — nguyên văn sinh viên

Người review: Nguyễn Chí Hoàng Tú, MSSV 23001947. Ngày 2026-11-14. Nguồn: trả lời trực tiếp câu hỏi review T17/T18 trong cuộc hội thoại.

```text
Tôi đã review bảng test do AI sinh và quyết định:

- Xóa T17. Case này trùng T03 và assertion `TryStart(host) == false` chưa đủ để chứng minh retry chỉ kích hoạt một lần tải. Hãy kiểm tra bảng cuối vẫn có test AC2 đếm số lần gọi boundary tải khi gửi Start lặp trong cùng phiên.
- Xóa T18. Case này chờ cố định 2 giây và tìm text trong RoomPanel, vốn có thể bị hủy khi chuyển scene. Giữ T08/T09 để kiểm tra overlay qua các mốc lifecycle của client, đặc biệt là sau khi RoomPanel bị hủy và trước khi client sẵn sàng.

Lưu nguyên văn hai quyết định này vào AI Usage Log, kèm nội dung gốc của T17/T18 và bảng sau chỉnh sửa. Xác minh bảng cuối còn ít nhất 10 case, có ít nhất 2 negative tests và 1 failure-path test, đồng thời vẫn bao phủ mọi cặp mức khả thi trong ma trận pairwise. Sau khi hoàn thiện Câu 2, tiếp tục Câu 3 theo plan đã chốt và lưu bằng chứng RED → patch → GREEN.
```

| ID | Before (AI gốc) | After | Lý do sinh viên |
|---|---|---|---|
| T17 | Unit, AC2, P2. Host đã Start thành công; gọi TryStart(host) lần nữa. Expected: TryStart trả false. | Xóa; T02/T03 tiếp tục đếm boundary tải và terminal. | Trùng T03, bool chưa chứng minh chỉ một lần tải. |
| T18 | UI, AC3, P2. Bấm Start hợp lệ; chờ cố định 2 giây. Expected: tìm text “Loading arena...” trong RoomPanel. | Xóa; giữ T08/T09 kiểm lifecycle sau RoomPanel destruction và trước client-ready. | Fixed sleep dễ flaky; RoomPanel có thể bị hủy, sai oracle overlay. |

AI-before giữ nguyên ở `test-cases-ai-v1.md`. Bảng sau review: `test-cases-reviewed.md`. Không đổi T01/factors/constraints. Sau xóa còn 16 IDs, T06/T07 negative, T05 failure path đếm B/L/S/F. Script xác minh lại pairwise sau review: 32/32 cặp khả thi, 12 matrix rows, không thiếu cặp.
