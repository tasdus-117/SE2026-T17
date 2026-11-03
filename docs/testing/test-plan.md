# Kế hoạch kiểm thử

## Baseline offline sau di trú

Bộ test nguồn 9eed902 đã được phục hồi vào Tests/EditMode và Tests/PlayMode. Test tải prefab theo Characters/Combat và kiểm tra cùng hành vi di chuyển, nhảy, camera, Health/combat, pickup, Victory/HUD, menu/replay. Số liệu mới nhất ở [migration-results.md](migration-results.md) và [PROJECT_CONTEXT.md](../../PROJECT_CONTEXT.md); không dùng cache cũ thay lần chạy sau di trú.

Kiểm tra cấu trúc buộc Domain/Protocol không dùng engine, Server không dùng Client và ngược lại, không có vòng asmdef, GUID duy nhất và build index 0/1/2 đúng. Unit test Domain chạy độc lập; PlayMode cần Unity native. Các ca mạng/tải/gian lận bên dưới vẫn là kế hoạch M1/online, không được đánh dấu đạt nhờ test offline.

## Chức năng

- 4 người vào lobby, ready và bắt đầu cùng trận.
- Người thứ 5 bị từ chối khi phòng đủ.
- Input chỉ điều khiển nhân vật thuộc kết nối đó.
- Di chuyển, va chạm, tấn công và tính điểm do server xác nhận.
- Mất mạng rồi trở lại đúng phiên trong thời hạn; quá hạn bị xử lý theo luật.
- Token sai không lấy được nhân vật người khác.

## Tải

Đầu tiên kiểm tra một trận đủ 4 người. Chỉ kiểm tra nhiều trận đồng thời khi thiết kế server có hỗ trợ điều đó. Ghi phiên bản build, phần cứng, thời gian thử, số client, CPU/RAM, lưu lượng mạng, thời gian tick và lỗi. Bot chỉ gửi input không chứng minh đầy đủ chi phí render của client thật.

## Mạng

Ví dụ ma trận thử, chưa phải tiêu chí đạt: RTT 0/50/100/200 ms; jitter 0/20 ms; loss 0/1/5%. Ghi rõ công cụ áp dụng trễ một chiều hay RTT, phạm vi áp dụng và cách đo thực tế. Đo thời gian vào trận, độ phản hồi, độ lệch vị trí, số lần hiệu chỉnh và khả năng reconnect.

## Gian lận

Thử input vượt phạm vi, NaN/Infinity, spam lệnh, phát lại yêu cầu, bắn khi chết, bắn quá nhanh, sửa tốc độ cục bộ, giả danh nhân vật và lấy lại phiên bằng token sai. Kết quả cần kiểm tra ở trạng thái server: hành vi bị từ chối, trạng thái vẫn hợp lệ và server không bị crash.

## Vị trí kết quả

Lưu kết quả chạy vào `TestResults/` hoặc `reports/` và đính kèm CI artifact khi có CI thật. Chỉ commit bản tổng hợp có giá trị vào docs/testing. Mỗi test cần điều kiện ban đầu, thao tác, kết quả mong đợi và bằng chứng thực tế.
