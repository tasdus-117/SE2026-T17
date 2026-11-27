# #28 — Phiếu playtest ba người mới

Chuẩn bị ngày 27-11-2026, TV3 / agent hỗ trợ tasdus-117. **Chưa có người thử; BLOCKED.**
Không dùng kết quả agent, automation hoặc người đã đọc tài liệu thay người mới.
Chỉ chạy buổi thử sau khảo sát UI/input cơ bản #26. Ghi lại cùng build đã kiểm UI;
nếu đổi build, kiểm lại các ca ảnh hưởng trước buổi thử. Không suy đây là nghiệm thu #27.

## Cách chạy

1. Người điều phối mở cùng một player directory trên máy host và client, ghi SHA,
   hash toàn bộ nội dung, cấu hình, topology/port, độ phân giải. TV1 hỗ trợ kết nối
   và chuẩn bị người chơi đối thủ; không sửa luật/tuning hay dùng debug spawn.
2. Mỗi lượt chỉ đánh mã P1/P2/P3, xác nhận chưa đọc docs và chưa chơi bản này.
   Không ghi tên, email, giọng nói hoặc thông tin cá nhân. Xin đồng ý trước khi
   quay video; ưu tiên ghi chú và ảnh chỉ chứa game.
3. Đọc đúng lời giao việc bên dưới, không giải thích phím/luật trước. Cho tối đa
   5 phút mỗi nhiệm vụ; ghi thời gian thực tế và điểm người chơi dừng lại.
4. Khi họ cần trợ giúp, ghi câu hỏi và gợi ý đã đưa. Hỏi họ muốn tiếp tục trước
   khi đưa gợi ý; sau gợi ý đánh dấu “có trợ giúp”, không ghi tự hoàn thành.
5. Sau lượt thử hỏi họ diễn giải death/respawn và điều kiện thắng bằng lời của họ.
   TV2 xác nhận diễn giải luật/copy theo oracle; không dạy trước rồi chấm hiểu đúng.

Lời giao: “Hãy tự tìm cách vào một trận multiplayer. Trong trận, tìm và nhặt vật
phẩm tăng sức mạnh. Tiếp tục chơi qua một lần chết và trở lại trận. Cuối trận,
hãy cho biết ai thắng và vì sao, rồi quay về phòng chờ.”

## Identity bắt buộc trước buổi thử

| Trường | Giá trị |
|---|---|
| Agreed/provisional, production SHA đầy đủ | Chưa chọn |
| Player-directory SHA256, đường dẫn build | Chưa chọn |
| Unity / config / instrumentation flag | 6000.6.8f1 / ghi config thực / ghi flag thực |
| Host + clients, topology/port, ngày giờ | Chưa chạy |
| Độ phân giải, khảo sát #26 tham chiếu | Chưa chạy |

M3 đang tham chiếu H10/D1/C1/R1.8, pickup +1, thắng 5 kills hoặc hết180s,
respawn3s/protection1s, postmatch10s. Đây là hướng dẫn người quan sát; **không đọc
cho người thử trước**. Đối chiếu [oracle](tv2-four-player-oracle.md) và TV2 nếu build khác.

## Phiếu quan sát (để trống đến khi có người thật)

Mỗi ô ghi: thời gian; tự hoàn thành/có trợ giúp/chưa xong; thao tác/câu hỏi thực;
ảnh/log/timecode. “Chưa chạy” không có nghĩa người chơi thất bại.

| Người | Vào trận | Nhặt buff | Hiểu death/respawn | Hiểu thắng/thua | Về lobby |
|---|---|---|---|---|---|
| P1 | Chưa chạy | Chưa chạy | Chưa chạy | Chưa chạy | Chưa chạy |
| P2 | Chưa chạy | Chưa chạy | Chưa chạy | Chưa chạy | Chưa chạy |
| P3 | Chưa chạy | Chưa chạy | Chưa chạy | Chưa chạy | Chưa chạy |

## Bàn giao sau buổi thử

| Quan sát thật + evidence | Giả thuyết nguyên nhân (tách riêng) | Owner / bước tái hiện | Tối đa hai sửa nhỏ / kiểm lại |
|---|---|---|---|
| Chưa có dữ liệu | Chưa kết luận | Chưa có finding | Chưa đề xuất |

Chỉ chọn tối đa hai sửa copy/hiển thị từ quan sát có evidence. Copy về luật cần
TV2 xác nhận; không dựng tutorial scene hoặc hệ thống tracking. Kiểm lại cùng
nhiệm vụ với người mới hoặc ghi rõ người cũ đã được học. Giữ #28 mở đến đủ ba
người và review bắt buộc. Bước cần người dùng: bố trí ba người chưa đọc docs và
buổi thử sau khi #26 hoàn thành khảo sát cơ bản; agent không thể tự cung cấp người thật.
