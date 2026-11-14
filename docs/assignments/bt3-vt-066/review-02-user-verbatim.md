# Review tiếp tục — nguyên văn R1–R3

Người review: Nguyễn Chí Hoàng Tú — 23001947. Nguồn: tin nhắn tiếp tục ngày 2026-11-14. Giữ nguyên review trước ở `review-01-user-verbatim.md`; đây là phản hồi bổ sung, không thay thế lịch sử.

```text
R1 — Sửa phạm vi overlay. Tôi đồng ý dùng overlay local tồn tại xuyên scene, với vòng đời giới hạn trong một thao tác Start. Overlay hiện khi client bắt đầu chuyển sang Arena_MVP, cập nhật bằng các mốc lifecycle thực sự quan sát được, rồi được dọn khi scene của client sẵn sàng, khi thao tác thất bại hoặc khi client mất kết nối. Dựa vào `NetworkManager.cs:828`, tái sử dụng async load của Mirror. Vì UI lobby thuộc MainMenu, cần test PlayMode rằng overlay vẫn hiện sau khi RoomPanel bị hủy và được dọn đúng lúc. Mốc hoàn tất của guest dựa vào lifecycle của chính guest. Chỉ hiển thị phần trăm nếu có nguồn đo đáng tin cậy.

R2 — Sửa chính sách lỗi. Theo `ServerLobbyController.cs:67–69`, room vào Loading trước khi gọi `LoadRoomArena()`. Nếu server phát hiện lỗi ở boundary khởi động tải, thao tác phải kết thúc ở trạng thái lỗi tối đa một lần. Retry trong cùng phiên không được gọi load lần nữa. Kết thúc phiên an toàn theo lifecycle hiện có. Client dọn overlay và hiển thị thông báo lỗi hoặc mất kết nối ở màn hình người dùng đọc được. Một guest mất kết nối chỉ kết thúc overlay của guest đó, còn phiên của các thành viên khác tiếp tục theo lifecycle server. Ghi rõ loại lỗi nào thực sự phát hiện được và test được. Phiên mới đủ điều kiện có thể Start lại.

R3 — Giữ guard theo room/session. `LobbyStartRequest` hiện rỗng và mỗi phiên chỉ chấp nhận một lần Start. Giữ contract hiện tại và chưa thêm operation ID vào protocol. Test phải chứng minh các lần Start lặp trong cùng phiên chỉ kích hoạt một lần tải và phiên mới đủ điều kiện có thể Start bình thường. Snapshot trạng thái được gửi lại theo contract hiện có.
```

Chỉ dẫn runner nguyên văn:

```text
Dùng Unity PlayMode runner làm evidence chính cho test lobby. Ghi lại vì sao `Run-DomainTests.ps1` không discover test lobby mới và kiểm tra Editor version, license, import và test discovery khi chạy.
```
