# Evidence để sinh viên tự viết reflection 250–350 từ

Không có reflection AI viết thay trong file này. Sinh viên viết bằng lời của mình, dựa trên các facts đã kiểm dưới đây, rồi gửi nguyên văn để đưa vào PDF.

## Những gì đã chạy thật

- Baseline đúng hai PlayMode suites: 19/19 pass (12+7), exit 0; Unity 6000.6.8f1, licensing ban đầu warning rồi update thành công.
- Test-first: lần đầu lỗi compile UnityAction, không gọi là RED. Sau sửa compile, 20 tests discover, 19 fail đúng assertion/thiếu behavior và 1 pass. T08 fail overlay không tồn tại sau RoomPanel destruction trước local-ready. Cùng filter sau patch 20/20 pass.
- Patch local v2 được sinh viên chấp nhận. Rà tiếp thấy startup failure tại MainMenu có thể làm mất manager. Test mới RED thực sự; sửa navigation rồi GREEN và Start được phiên mới. Sinh viên duyệt recovery bổ sung. Final regression 57/57 pass.
- T05 integration chạy scene preflight không tồn tại: B=1, native arena load L=0, failure terminal F=1; retry và callback muộn không reopen, Failed trước teardown rồi Offline. T02 healthy dùng Mirror messages + native scene: B=L=S=1.
- Capacity >→>=: sạch 3/3, mutant chỉ fail count4, restored 3/3. Mutant SHA `db5d5efb69c6616e07892ab0583790b7152d9131`; production `02db6c3affce0252e60c3abf20d8bc3f48ef11e7`, operator > restored.
- 20 vòng scene thật liên tiếp: 20/20 pass cho timing window và observer/singleton shared state. Không dùng fixed 2s sleep.
- Load harness: 100 submitted in-flight/round ×20 rounds, 2000 ops/6000 requests; 200 failures có chủ đích, 0 unexpected errors/timeouts/duplicates; p95 mỗi round 26.03–32.20 ms. Đo caller submit → observed terminal, gồm dispatch wait.

## Điều chưa được chứng minh

- Harness dùng fake async load delay 12–24 ms và marker Mirror; p95 không phải multiplayer/arena thật.
- Guest T09 chỉ manual client-only lifecycle, guest virtual chỉ message/roster. Chưa có E2E guest process riêng qua scene và Internet.
- Không có line/branch percentage do chưa có Code Coverage package. Native load hang/crash sau startup, reconnect và randomized test order chưa có evidence.

## Các điểm AI trông hợp lý nhưng phải kiểm

1. Assert text Loading trong RoomPanel sau fixed delay trông hợp lý nhưng sai vòng đời UI. Sinh viên trực tiếp xóa T18; T08 native scene từng RED rồi GREEN chứng minh oracle tốt hơn.
2. Assert TryStart=false không chứng minh side effect đúng một lần. Sinh viên xóa T17; T02/T05 tách B/L/terminal và phát hiện mutation riêng kiểm capacity.
3. Một biến fakeLoads luôn 0 là assertion yếu. AI tự nhận ra, bỏ; integration T05 quan sát native sceneLoaded thay thế.
4. Thông báo lỗi đọc được không đồng nghĩa phiên mới hoạt động. Test recovery bổ sung từng RED vì manager đã bị hủy, rồi GREEN sau sửa navigation.

## Gợi ý câu hỏi tự phản tư

- Evidence nào làm confidence tăng nhiều nhất, và vì sao nó có thể fail khi code sai?
- Vì sao green suite hoặc p95 harness chưa đủ để kết luận toàn hệ thống đúng?
- Test tiếp theo giá trị nhất là guest process riêng, native loader failure sâu hơn, hay workload thật? Chọn và giải thích theo residual risk.

Vui lòng gửi 250–350 từ và xác nhận có đưa cam kết này vào trang đầu không:

“Tôi chịu trách nhiệm về toàn bộ code, test và nội dung bài làm; tôi đã khai báo việc sử dụng AI.”
