# Review 01 — phản hồi nguyên văn của sinh viên

Người review: Nguyễn Chí Hoàng Tú (user trong cuộc hội thoại).
Ngày ghi nhận: 2026-11-14 (Asia/Saigon).
Nguồn: tin nhắn user ngay sau plan-v1. Phần trong khối dưới đây giữ nguyên nội dung tin nhắn, không diễn giải hoặc thay lời người review.

```text
Tôi review plan VT-066 và có các quyết định sau:

R1 — Sửa phạm vi overlay. Tôi đồng ý dùng overlay local tồn tại xuyên scene, nhưng giới hạn vòng đời của nó trong một thao tác Start. Overlay chỉ hiện khi client thực sự bắt đầu chuyển sang Arena_MVP, cập nhật theo mốc lifecycle có thể quan sát, rồi được dọn khi scene của client sẵn sàng hoặc khi client mất kết nối. Dựa vào NetworkManager.cs:828, dùng async load sẵn có của Mirror. Vì UI lobby thuộc MainMenu, cần kiểm tra PlayMode rằng overlay vẫn hiện sau khi RoomPanel bị hủy và được dọn đúng lúc. Với guest, mốc hoàn tất trên UI phải dựa vào lifecycle của chính guest. Hiển thị trạng thái theo mốc, ví dụ “Đang tải arena…”, nếu chưa có nguồn đo phần trăm đáng tin cậy.

R2 — Sửa chính sách lỗi. Tại ServerLobbyController.cs:67–69, room vào Loading trước khi gọi LoadRoomArena(). Khi server phát hiện lỗi ở boundary khởi động tải, thao tác phải chuyển sang trạng thái lỗi tối đa một lần và retry trong cùng phiên không được gọi load lần nữa. Khi client quan sát được lỗi hoặc mất kết nối, dọn overlay và hiển thị thông báo ở màn hình có thể đọc được. Tách riêng việc một guest mất kết nối: sự kiện local đó chỉ kết thúc overlay của guest, còn phiên của các thành viên khác tiếp tục theo lifecycle server. Ghi rõ lỗi nào thực sự phát hiện được và test được, cùng tình huống nào còn nằm ngoài phạm vi.

R3 — Giữ đề xuất dùng guard theo room/session. LobbyStartRequest hiện rỗng và mỗi phiên chỉ chấp nhận một Start, nên chưa cần thêm operation ID vào protocol. Thêm test chứng minh Start lặp trong cùng phiên chỉ kích hoạt một lần tải và phiên mới đủ điều kiện có thể Start bình thường.

Dùng Unity PlayMode runner làm evidence chính cho test lobby. Ghi lại vì sao Run-DomainTests.ps1 không discover test lobby mới và kiểm tra license, import, test discovery khi thực thi.

Hãy lưu nguyên phản hồi này làm evidence review, giữ cả plan gốc và plan đã cập nhật. Ghi rõ thay đổi trước/sau cho R1 và R2, dẫn file:dòng đã xác minh, rồi trả lại plan cập nhật.
```
