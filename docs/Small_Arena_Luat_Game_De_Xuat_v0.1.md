# Small Arena — Luật game đã chốt (baseline v0.1)

**Phiên bản:** 0.1 · **Ngày:** 06/11/2026 (UTC+7)

**Người phụ trách đề xuất:** TV2 / 23001901 · **Người review:** TV1, TV2, TV3

**Trạng thái:** APPROVED gameplay baseline — owner TV2 / 23001901 xác nhận nhóm đã chốt trong chỉ thị ngày 08/11/2026 (UTC+7). Ngày họp và chữ ký riêng từng thành viên không được cung cấp; đây là ghi nhận qua owner.

> Bản v0.1 ngày 06/11 được owner xác nhận là luật nhóm đã chốt ngày 08/11/2026. Nội dung gameplay mục 1–13 giữ nguyên để truy vết; các chữ “đề xuất/cần duyệt” trong nội dung phản ánh lúc soạn và được xác nhận này supersede về lựa chọn luật. Riêng jump vẫn theo điều kiện TV1/TV3 xác nhận tại mục 4; toolchain/môi trường/ngân sách/lịch GD-09 không được suy đã duyệt. Xem [GD outcomes](DECISIONS.md#tv2-001--kết-quả-chốt-luật-2026-11-08). Duyệt luật không chứng nhận code hoặc test PASS.

## 1. Mục tiêu và cách chơi

Small Arena là game đấu trường 3D online trên Windows. Từ hai đến bốn người chơi thi đấu tự do (FFA), sử dụng cùng một loại súng để hạ đối thủ và tích lũy điểm trong thời gian giới hạn. Người chơi chết được hồi sinh để tiếp tục thi đấu. Khi hết giờ, người đủ điều kiện có điểm cao nhất thắng; nếu nhiều người cùng cao điểm nhất thì hòa.

Server chạy riêng trên máy nhóm, quyết định di chuyển hợp lệ, phát bắn, sát thương, HP, chết, hồi sinh, điểm và kết quả trận. Client gửi thao tác và hiển thị trạng thái server. Vị trí hoặc kết quả trúng đích do client tự khai không được coi là kết quả chính thức.

**Điểm cần duyệt đầu tiên:** chọn hướng raycast + hồi sinh + tính điểm của phân công TV2 hiện tại làm baseline cho bản online. Không kết hợp thêm luật loại vĩnh viễn, last survivor, vũ khí melee nhặt/E drop/2x hay host+3 của tài liệu lịch sử. Mẫu offline hiện hữu được giữ để tham khảo hoặc kiểm thử hồi quy, không quyết định luật online mới.

## 2. Phạm vi bản đầu

| Nội dung | Đề xuất |
| --- | --- |
| Chế độ | Một chế độ FFA, mọi người là đối thủ |
| Người chơi | Tối đa 4; tối thiểu 2 để bắt đầu |
| Không gian | Một arena, có sàn, tường, vật cản và các điểm spawn |
| Vũ khí | Mọi người có sẵn một súng raycast giống nhau |
| Tài nguyên chiến đấu | Đạn vô hạn, không reload |
| Vòng lặp | Di chuyển → ngắm/bắn → gây sát thương → hạ đối thủ → hồi sinh |
| Kết thúc | Hết thời gian; xếp hạng theo điểm hạ đối thủ |
| Phần sau | 2v2, thêm vũ khí/bản đồ, polish nếu nhóm duyệt riêng |

Bản đầu không bổ sung tài khoản/database, shop, inventory, pickup vũ khí, đòn melee, knockback, headshot, assist, combo, matchmaking hoặc phục hồi trận sau khi server khởi động lại. Nhảy đã có trong nền code nhưng được đề nghị giữ như tính năng bổ sung sẵn có, không mở rộng thành double jump/dash.

## 3. Bảng thông số khởi đầu — toàn bộ cần duyệt

Các số dưới đây được chọn để có một bản thử dễ hiểu và dễ điều chỉnh. Chúng chưa qua playtest, chưa phải kết quả cân bằng game, và không được suy từ HP/damage offline cũ.

| Thông số | Giá trị đề xuất | Ý nghĩa |
| --- | --- | --- |
| HP ban đầu / tối đa H | 100 HP | Mỗi lần sống bắt đầu đủ HP |
| Sát thương D | 25 HP/phát trúng | 4 phát hợp lệ hạ mục tiêu đủ HP, không hồi máu giữa các phát |
| Cooldown C | 0,5 giây | Khoảng cách tối thiểu giữa hai phát được server chấp nhận |
| Tầm bắn R | 30 mét | Tính từ đầu nòng phía server đến điểm va chạm |
| Tốc độ di chuyển V | 5 m/s | Tốc độ ngang tối đa, đi chéo không nhanh hơn |
| Input movement | Axes hữu hạn; độ dài tối đa 1 sau chuẩn hóa | Giữ mức analog dưới 1; từ chối NaN/Infinity |
| Timeout input τ | 0,25 giây | Thiếu input hợp lệ quá hạn thì dừng ý định di chuyển ngang |
| Thời lượng trận T | 180 giây | Tính từ lúc server chuyển vào Playing |
| Điểm cho một hạ đối thủ | +1 | Chỉ người tạo sát thương kết liễu hợp lệ được cộng |
| Thời gian chờ hồi sinh Δ | 3 giây | Từ lúc server xác nhận chết |
| Khoảng cách safe spawn S | 5 mét | Khoảng cách ngang tối thiểu đến đối thủ còn sống |
| Chu kỳ thử lại spawn | 0,5 giây | Khi chưa có điểm spawn đạt điều kiện |
| Bảo vệ sau spawn P | 1 giây | Không nhận damage; chỉ bắt đầu bắn sau khi bảo vệ kết thúc |
| Thời gian giữ phiên G | 15 giây | Từ lúc server phát hiện mất kết nối |

Server dùng cùng một bộ cấu hình cho cả trận, không đổi tuning giữa trận. Thay đổi cấu hình áp dụng cho trận mới và phải ghi phiên bản. Giá trị âm, không hữu hạn hoặc không phù hợp miền hợp lệ bị từ chối trước khi bắt đầu trận; không âm thầm chạy với cấu hình hỏng.

## 4. Camera và điều khiển

**Đề xuất để TV3 review:** camera góc nhìn thứ ba đặt cao, bám nhân vật local, giữ hướng cố định và không dùng chuột để xoay camera. WASD điều khiển trên mặt phẳng arena theo hướng camera cố định. Nhân vật có thể di chuyển và ngắm độc lập.

| Thao tác | Điều khiển đề xuất |
| --- | --- |
| Di chuyển | WASD |
| Ngắm | Di chuyển chuột để chỉ hướng trên mặt phẳng ngang qua vị trí đầu nòng |
| Bắn | Giữ chuột trái; server giới hạn từng phát theo cooldown |
| Nhảy | Space, chỉ khi grounded; giữ hành vi nền sau khi TV1/TV3 xác nhận |
| Ready | Nút trên lobby |
| Rời trận | Nút có xác nhận, tránh rời nhầm |

Hướng bắn theo hướng ngắm, không tự khóa vào đối thủ và không bắt buộc trùng hướng di chuyển. Đề xuất bản đầu chỉ ngắm ngang; không ngắm dọc bằng chuột. Vì nhảy làm thay đổi độ cao, tia có thể đi dưới/trên mục tiêu đang nhảy. TV3 cần kiểm tra cảm giác điều khiển và khả năng nhìn crosshair; nếu không phù hợp thì nhóm chọn cách ngắm khác trước khi chốt contract, không để từng người tự triển khai khác nhau.

## 5. Lobby và bắt đầu trận

1. Một server có một phòng, tối đa bốn người. Bản đầu kết nối bằng địa chỉ server; chưa bắt buộc mã phòng.
2. Người thứ năm bị từ chối với thông báo phòng đầy. Đang Playing không nhận người tham gia mới; reconnect đúng phiên cũ được xét riêng.
3. Người chơi bấm Ready. Server tự bắt đầu khi có ít nhất hai người, tất cả thành viên hiện diện đã Ready và tải/khởi tạo arena xong. Không cần gán một client làm host quyết định trận.
4. Khi nhận người mới trong Waiting, hủy các Ready cũ và yêu cầu xác nhận lại roster. Nếu có người rời hoặc load thất bại trước lúc Playing, chưa bắt đầu; cập nhật roster và yêu cầu Ready lại.
5. Server khóa roster và khởi tạo mỗi người một nhân vật, một vị trí spawn riêng, HP đầy và điểm 0 trước khi Playing. Không cho bắn trong thời gian khởi tạo.

Khởi đầu trận áp dụng bảo vệ spawn như hồi sinh. Đồng hồ trận vẫn chạy trong thời gian bảo vệ. Demo nghiệm thu capacity cần đủ bốn người và thử người thứ năm, dù luật cho phép bắt đầu với hai người.

## 6. Di chuyển và tương tác arena

- Chỉ người còn sống trong Playing và có kết nối hợp lệ mới được tạo input di chuyển mới. Bảo vệ spawn vẫn cho di chuyển.
- Server xử lý tốc độ, grounded jump, trọng lực và va chạm. Client sửa speed hoặc gửi nhiều lệnh hơn không được làm nhân vật đi nhanh hơn.
- Input không hợp lệ không được làm mới timeout. Đi chéo bị giới hạn về cùng tốc độ tối đa; input analog nhỏ vẫn có tốc độ tương ứng.
- Tường/vật cản chặn chuyển động và phát bắn. Không bắn xuyên tường hoặc xuyên người.
- Arena chính có ranh giới tránh rơi. Nếu vẫn ra khỏi vùng hợp lệ hoặc xuống dưới kill plane được cấu hình, xử lý như chết do môi trường và hồi sinh; không loại vĩnh viễn khỏi trận.
- Không có fall damage thông thường, hazard gây damage theo thời gian hoặc knockback trong bản này.

TV3 xác định hình học arena và kill plane; TV1 kiểm mô phỏng; TV2 xác nhận tác động gameplay. Không tự coi “đang ở trên không” là ra khỏi arena.

## 7. Súng và xác thực phát bắn

Một phát được nhận là hành động bắn khi người chơi còn sống, đang Playing, đã hết bảo vệ spawn, kết nối còn quyền điều khiển và cooldown đã hết. Server dùng thời gian của mình để xét cooldown; ở đúng mốc hết cooldown được bắn lại.

Server lấy đầu nòng từ nhân vật chính thức và kiểm hướng ngắm hữu hạn, khác zero, đúng miền hướng ngắm đã duyệt. Client không được chọn damage, HP đối phương, vị trí đầu nòng tùy ý hoặc kết quả hit. Request sai dữ liệu/trạng thái bị từ chối, không gây damage và không khởi động lại cooldown.

**Phát bắn hợp lệ nhưng bắn trượt vẫn tiêu thụ cooldown.** Giữ chuột không cho phép dồn các phát bị bỏ lỡ hoặc bù một loạt damage khi mạng trở lại. Mỗi phát được chấp nhận chỉ có một raycast và tối đa một mục tiêu nhận damage:

| Kết quả kiểm tia | Xử lý |
| --- | --- |
| Gặp đối thủ còn sống, không bảo vệ, trong tầm | Trừ D HP đúng một lần |
| Gặp tường/vật cản trước đối thủ | Dừng tia; không damage phía sau |
| Gặp đối thủ đang bảo vệ | Không damage; nhân vật đó vẫn chặn tia |
| Không gặp mục tiêu hoặc mục tiêu ngoài R | Bắn trượt |
| Nhiều collider thuộc cùng nhân vật | Chỉ tính một lần cho nhân vật đó |

Collider của người bắn bị loại khỏi truy vấn; collider gameplay của người chết bị tắt, không làm lá chắn. Trigger UI/spawn/kill plane không chặn tia; vật cản được đánh dấu chặn bắn thì có chặn. Không headshot, critical, damage suy giảm theo khoảng cách hoặc lag compensation trong bản đầu. Điểm va chạm đúng bằng R vẫn nằm trong tầm.

## 8. HP, chết và điểm

HP luôn trong [0,H]. Không tự hồi máu khi còn sống. Khi một hiệu ứng hợp lệ làm HP về 0, server xác nhận chết đúng một lần cho lần sống đó; phần damage dư bị bỏ.

Người chết không di chuyển chủ động, bắn hoặc chặn tia; có thể nhìn UI và đếm ngược hồi sinh. Không cần spectator tự do trong bản đầu. Mỗi lần hồi sinh tạo một lần sống mới; đạn/sự kiện/callback thuộc lần sống cũ không được gây damage hoặc phát sinh điểm cho lần sống mới.

| Nguyên nhân chết | Điểm người bắn | Điểm nạn nhân |
| --- | --- | --- |
| Phát bắn kết liễu hợp lệ của đối thủ | +1 đúng một lần | Không bị trừ |
| Rơi ra ngoài vùng / môi trường | Không ai được cộng | Không bị trừ |
| Rời trận / hết hạn reconnect | Không coi là kill | Không bị trừ; xử lý tư cách dự kết quả ở mục 12 |
| Callback trùng hoặc hit lên người đã chết | Không cộng | Không thay đổi |

Không cộng assist hoặc “người gây damage gần nhất” khi đối thủ chết do môi trường. Không có thao tác tự sát riêng. Bảng điểm có kills và deaths để quan sát; deaths không dùng phá hòa. Người bắn mất kết nối sau khi server đã chấp nhận phát bắn vẫn được ghi nhận kill hợp lệ vào đúng phiên của họ.

## 9. Hồi sinh và safe spawn

Sau Δ giây từ lúc chết, người chơi đủ điều kiện chuyển sang chờ spawn. Server chỉ cho hồi sinh nếu trận còn Playing và người chơi đã kết nối hoặc reconnect hợp lệ.

Một điểm spawn hợp lệ phải nằm trong arena, không giao tường/vật cản/nhân vật khác, có sàn phù hợp và cách mọi đối thủ còn sống ít nhất S theo khoảng cách ngang. Trong các điểm hợp lệ, chọn điểm có khoảng cách đến đối thủ gần nhất lớn nhất; nếu bằng nhau dùng thứ tự điểm spawn cố định. Trong cùng một bước, đặt chỗ spawn tuần tự để không tạo hai nhân vật chồng nhau.

Nếu tất cả điểm đều không hợp lệ, giữ RespawnPending và thử lại mỗi 0,5 giây; không bỏ qua kiểm tra va chạm để ép spawn. Không có số lần thử tối đa trong trận; nếu chờ quá 5 giây, hiển thị “Đang tìm vị trí hồi sinh” và ghi nhận để nhóm chỉnh arena. Trận kết thúc thì hủy việc thử lại.

Khi hồi sinh: HP về H, giữ điểm/deaths đã ghi, xóa trạng thái bắn đang giữ và bắt đầu bảo vệ P giây. Được bắn khi đồng thời hết bảo vệ và cooldown server cũ (nếu còn); không dùng hồi sinh để xóa cooldown đang tồn tại. Người chơi phải nhấn lại chuột sau hồi sinh. Không tạo damage/bắn trong bảo vệ; di chuyển không hủy bảo vệ.

## 10. Thứ tự xử lý và tình huống đồng thời

Đề xuất bản đầu dùng thứ tự server quyết định, không hứa hẹn trao đổi kill đồng thời:

1. Nhận input, gắn đúng người, kiểm quyền và trạng thái; không cho request tự chọn nhân vật khác.
2. Chỉ các bước có thời gian mô phỏng server t < T mới áp dụng gameplay. Ở bước t ≥ T, chốt kết quả trước phát bắn/respawn của bước đó.
3. Trong bước hợp lệ, server mô phỏng di chuyển và xử lý ra khỏi arena trước combat.
4. Các request bắn được xét theo thứ tự server tiếp nhận, dùng thứ tự phụ ổn định nếu cùng mốc; áp dụng HP/death/score ngay theo thứ tự đó.
5. Sau combat, xử lý respawn đến hạn và hết bảo vệ; nhân vật vừa respawn không bị hit từ request đã xử lý trước đó trong bước.
6. Phát trạng thái đã xác nhận cho client. Sau Finished/Aborted không tạo damage, score hay respawn mới.

Ví dụ: A và B cùng còn 25 HP, hai phát kết liễu đến trong cùng bước. Nếu server xử lý A trước thì B chết; request của B sau đó bị từ chối vì đã chết. Đây là tradeoff đơn giản của bản đầu, cần nhóm duyệt; nếu muốn mutual kill phải đổi luật batch và test trước khi code. Hit trong bước trước deadline được tính; hit tại/sau deadline không tính. Không dùng timestamp client để quyết định phát bắn xảy ra trước giờ hết trận.

## 11. Kết thúc trận và kết quả

Đề xuất bản đầu chỉ kết thúc bình thường theo thời gian, không có score limit hoặc sudden death.

- Khi hết T, chốt điểm và dừng gameplay. Người có điểm cao nhất và còn tư cách dự kết quả thắng.
- Hai hoặc nhiều người cùng điểm cao nhất: kết quả Draw giữa những người đó, hiển thị toàn bộ bảng điểm. Không dùng deaths, ping hoặc thời điểm đạt điểm để phá hòa.
- Tất cả 0 điểm: hòa. Không có người hợp lệ: Aborted, không winner.
- Người đã rời/hết hạn phiên không được tuyên bố thắng; vẫn hiển thị điểm cũ kèm trạng thái Left/Disconnected-expired.
- Khi số phiên còn đủ tư cách tham gia xuống dưới hai vì rời/hết hạn, hủy trận Aborted thay vì trao thắng tự động. Người đang trong thời hạn reconnect vẫn tính vào số này.
- Sau kết quả, quay lại lobby bằng thao tác UI. Trận mới reset HP/score/deaths/timer, có định danh trận mới và yêu cầu Ready lại.

## 12. Mất kết nối, reconnect và rời trận

### 12.1 Mất mạng tạm thời

Server giữ phiên trong G giây từ lúc phát hiện mất kết nối. Không nhận input mới từ kết nối đã mất; xóa ý định di chuyển/bắn còn giữ ngay khi phát hiện. Trọng lực/va chạm vẫn chạy, avatar đang sống còn ở arena và vẫn có thể bị bắn chết. Trước khi phát hiện disconnect, timeout input τ vẫn áp dụng.

Cooldown, thời lượng trận, bảo vệ spawn và thời gian chờ hồi sinh tiếp tục theo server; không pause vì mất mạng. Nếu người chơi chết lúc offline, vẫn ghi death/kill theo luật. Nếu timer respawn đến hạn khi offline, giữ RespawnPending, chưa tạo avatar sống mới. Reconnect hợp lệ sau đó được xét spawn ngay nếu đã hết Δ và có điểm an toàn; không reset timer.

### 12.2 Lấy lại phiên

Client phải chứng minh đúng phiên cũ bằng cơ chế TV1 cung cấp, không chỉ gửi player ID hoặc địa chỉ phòng. Tại mốc đã đủ G giây, phiên hết hạn; chỉ reconnect trước mốc đó được nhận. Kết nối cũ bị vô hiệu hóa khi phiên được gắn sang kết nối mới.

Server trả trạng thái hiện tại: HP, alive/dead/pending, score/deaths, timer/cooldown/bảo vệ còn lại và kết quả nếu đã kết thúc. Reconnect không hồi HP, thêm avatar, tạo kill hoặc cho bắn lại sớm. Người còn sống tiếp tục đúng trạng thái; người đang chết/chờ hồi sinh theo mục 9. Sau reconnect phải có input mới, không tự phát lại thao tác bắn cũ.

Nếu hết giờ trong lúc phiên vẫn còn trong grace, người đó vẫn được xét điểm như thành viên hợp lệ; reconnect chỉ xem kết quả, không tiếp tục combat. Sau terminal, không xét lại winner chỉ vì grace kết thúc muộn hơn.

### 12.3 Rời chủ động, hết hạn và server mất

Rời chủ động kết thúc tư cách tham gia ngay, không có grace. Hết hạn reconnect cũng loại phiên khỏi lượt hiện tại. Xóa avatar gameplay mà không tạo kill, giữ dòng lịch sử bảng điểm; không cho quay lại như thành viên mới trong cùng trận. Trận mới có thể join lại.

Server mất/khởi động lại: Aborted, client báo mất server; không tự suy ra winner và không yêu cầu khôi phục trận từ dữ liệu cũ. Đây là giới hạn bản đầu, khác việc một client rớt mạng.

## 13. UI tối thiểu để người chơi hiểu luật

Lobby hiển thị roster, trạng thái kết nối/Ready/load và lý do chưa bắt đầu. HUD hiển thị HP của mình, điểm, thời gian còn lại, crosshair và phản hồi trúng/không được bắn ở mức đơn giản. Khi chết hiển thị đếm ngược hoặc chờ spawn; khi được bảo vệ có dấu hiệu rõ ràng. Bảng kết quả hiển thị winner/draw/aborted, kills/deaths và người đã rời.

UI chỉ phản ánh dữ liệu server; không tự trừ HP hoặc quyết định kết quả. Thông báo “phát bắn bị từ chối” không cần hiện chi tiết kỹ thuật cho mọi lần giữ chuột trong cooldown; có thể dùng phản hồi súng chưa sẵn sàng.

## 14. Tình huống kiểm chứng sau khi duyệt

Các ca dưới đây là expected theo baseline đã chốt, **chưa chạy**. Mapping và biên cụ thể tại [TESTING](TESTING.md#baseline-v01--expected-đã-chốt).

| Ca | Expected |
| --- | --- |
| 2 người ready+loaded / chỉ 1 người / 1 người chưa ready | Chỉ trường hợp đầu bắt đầu |
| Phòng 4 người; người thứ 5 hoặc join khi Playing | Từ chối admission; vẫn phân biệt reconnect |
| Đi chéo, sửa speed client, input NaN hoặc spam lệnh | Không vượt tốc độ server; input lỗi không refresh timeout |
| Bắn tại 0 / 0,49 / 0,50 giây | Nếu đủ gate: nhận / từ chối / nhận |
| 4 hit 25 HP lên mục tiêu 100 HP không bảo vệ | 75 → 50 → 25 → 0; một death, một kill |
| Tường trước mục tiêu / mục tiêu đúng R / xa hơn R | Không hit / trong tầm / ngoài tầm |
| Một người có nhiều colliders hoặc request/effect trùng | Không damage/death/kill lặp |
| A và B bắn kết liễu cùng bước, A được xử lý trước | B chết; B không bắn sau chết trong bước |
| Ra kill plane sau khi từng bị đối thủ bắn | Environment death; không cộng last-damager kill |
| Respawn trước Δ / mọi điểm spawn unsafe | Chưa hồi sinh / giữ Pending và thử lại |
| Spawn bảo vệ; giữ nút bắn từ lần sống trước | Không bắn trong bảo vệ hoặc tự phát lại input cũ |
| Callback life cũ tới sau respawn | Không ảnh hưởng HP/death/score life mới |
| Hết 180 giây, hai người cùng điểm cao nhất | Draw; không xét deaths làm tie-break |
| Request tại/sau deadline; callback sau kết quả | Không tạo gameplay effect mới |
| Offline chết và hết timer; reconnect trong grace | Giữ death/score; xét safe spawn, không reset thời gian |
| Reconnect sai proof / đúng mốc G / connection cũ | Từ chối; không chiếm phiên hoặc avatar |
| Chỉ còn 1 phiên hợp lệ sau leave/expiry | Aborted, không thắng mặc định |
| Server mất khi trận đang diễn ra | Aborted; không client nào tự nhận thắng |

## 15. Bảng review tương ứng GD-01–09

GD-01–08 ACCEPTED gameplay qua xác nhận owner ngày 08/11/2026. GD-09 ACCEPTED baseline, OPEN technical matrix/budgets/lịch. Bảng dưới giữ phân công phối hợp; DECISIONS ghi outcome và giới hạn.

| GD | Nhóm cần review | Đề xuất trong bản này | Người phối hợp |
| --- | --- | --- | --- |
| GD-01 | Tuning và input | Bảng H/D/C/R/V/τ ở mục 3, không copy offline defaults | TV2 + TV1 + TV3 |
| GD-02 | Ammo/reload | Đạn vô hạn, không reload | TV2 + TV3 |
| GD-03 | Camera/aim/geometry | Camera cao cố định; mouse aim ngang; origin server; không xuyên mục tiêu/tường | TV3 chủ camera, TV2 combat, TV1 state |
| GD-04 | Respawn/safe spawn/protection | 3s, cách ≥5m, retry khi unsafe, bảo vệ 1s cấm bắn | TV2 + TV3 + TV1 |
| GD-05 | Kill credit/simultaneous | +1 lethal hit; không environment credit; server xử lý tuần tự | TV2 + TV1 |
| GD-06 | End/tie/order | 180s, không score limit; bằng điểm cao nhất thì hòa; deadline trước gameplay | TV2 + TV1 + TV3 |
| GD-07 | Start/join/ready | 2–4, tất cả ready+loaded, tự start, không mid-join | TV1 + TV2 + TV3 |
| GD-08 | Reconnect/expiry/server loss | Grace 15s; avatar vulnerable; offline không spawn; <2 phiên thì abort | TV1 + TV2 + TV3 |
| GD-09 | Baseline và môi trường | Duyệt hướng raycast/respawn; dùng project chung main; TV1 chốt toolchain/OS/transport/build và lịch riêng | Cả nhóm, TV1 chủ kỹ thuật |

### Mẫu ghi phản hồi lịch sử (chưa có chữ ký riêng)

| Người review | Mục/GD | Đồng ý / cần sửa / phản đối | Nội dung và lý do | Đề xuất thay thế |
| --- | --- | --- | --- | --- |
| Long / TV2 | … | … | … | … |
| TV1 | … | … | … | … |
| TV3 | … | … | … | … |

### Checklist bản thảo lịch sử — superseded bởi xác nhận owner

- [ ] Long review và chỉnh bản dự thảo trước khi gửi nhóm.
- [ ] TV1/TV2/TV3 thống nhất baseline và tất cả GD ảnh hưởng đến phần sắp triển khai.
- [ ] Chốt góc nhìn/cách ngắm bằng ví dụ hoặc mockup trước contract bắn.
- [ ] Review fairness của simultaneous hits, safe spawn và reconnect.
- [ ] Ghi actual decision, người duyệt, ngày, phiên bản và điểm còn mở trong DECISIONS/SRS.
- [ ] Cập nhật contract và expected test tương ứng; không đổi nghĩa ID lịch sử.
- [ ] Sau đó triển khai trên feature branch từ main có prerequisites, PR vào main; việc duyệt luật không đồng nghĩa code đã pass hoặc đã merge.

## 16. Nguồn đối chiếu và lịch sử bản thảo

Phần này giữ xuất xứ ngày 06/11; trạng thái hiện hành theo xác nhận owner ở đầu tệp và DECISIONS ngày 08/11. Các câu “đề xuất/chưa duyệt” phía dưới chỉ mô tả bản gốc trước xác nhận.

Bản thảo tổng hợp hướng phân công TV2 và agenda hiện hữu, đồng thời bổ sung các lựa chọn cụ thể để nhóm dễ review. Các giá trị số, camera/aim chi tiết, thứ tự xử lý, start tự động, respawn/protection, điều kiện abort và chính sách reconnect là **đề xuất của bản này**, không được trình bày như nội dung đã thống nhất từ trước.

Nguồn đã đọc trên nhánh TV2-001 main-based; các liên kết dưới dùng SHA đã xác minh trong phiên làm việc trước để giữ mốc đối chiếu:

- [SRS v0.4 và ma trận xung đột](https://github.com/tasdus-117/SE2026-T17/blob/4a8348b5a74241905cad0ba3a42e3a34364a1b71/Long_document/srs.md).
- [Agenda GD-01–09](https://github.com/tasdus-117/SE2026-T17/blob/4a8348b5a74241905cad0ba3a42e3a34364a1b71/docs/DECISIONS.md).
- [Hợp đồng logic CT-201–205](https://github.com/tasdus-117/SE2026-T17/blob/4a8348b5a74241905cad0ba3a42e3a34364a1b71/docs/PROTOCOL.md).
- [Hướng dẫn và phân công TV1–TV3](https://github.com/tasdus-117/SE2026-T17/blob/4a8348b5a74241905cad0ba3a42e3a34364a1b71/docs/SE2026-T17-Huong-dan-va-phan-cong-nhom.md).

Tài liệu không phải báo cáo kiểm thử, chứng nhận cân bằng hay quyết định toolchain. Khi tạo bản gốc 06/11 chưa có thay đổi GitHub hoặc tin nhắn gửi thành viên. Delivery 08/11 được ghi ở PROJECT_STATE; việc tạo PR không chứng nhận code đã triển khai hoặc merged.
