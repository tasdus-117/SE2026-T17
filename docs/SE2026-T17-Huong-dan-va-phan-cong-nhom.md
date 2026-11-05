# SE2026-T17 — Hướng dẫn khởi động và phân công nhóm

**Đề tài:** Small Arena — game 3D nhiều người chơi, server quyết định trạng thái chính thức.
**Quy mô nhóm:** 3 thành viên.
**Repo:** https://github.com/tasdus-117/SE2026-T17
**Định hướng triển khai:** Unity + Mirror.
**Trạng thái khởi đầu:** Đã có bộ khung thư mục và tài liệu trên nhánh main sau PR #2; chưa có gameplay hay Unity project hoàn chỉnh.

Tài liệu này là kế hoạch làm việc đề xuất để nhóm thống nhất trước khi triển khai. Thay tên thành viên và chốt lịch thực tế trong buổi họp đầu tiên.

## 1. Mục tiêu chung

Xây dựng một trận đấu 3D tối đa 4 người trong một arena. Client gửi thao tác; server kiểm tra và quyết định vị trí hợp lệ, sát thương, máu, trạng thái sống/chết và điểm số.

Các phần cần bàn giao cuối dự án:

- Lobby: vào phòng, hiển thị người chơi, ready và bắt đầu trận.
- Một arena có ranh giới, vật cản và điểm xuất hiện.
- Di chuyển và chiến đấu được server kiểm soát.
- Xác thực đòn đánh tại server.
- Kết nối lại đúng phiên khi mất mạng tạm thời.
- Mô phỏng độ trễ, jitter và mất gói để kiểm thử.
- Tài liệu giao thức, kết quả kiểm thử tải/mạng, ca thử gian lận.
- Quy trình CI tạo bản client và server khi đã chốt môi trường build.

**Mốc ưu tiên đầu tiên:** Một server và hai client kết nối được, mỗi người điều khiển đúng nhân vật của mình và nhìn thấy người kia di chuyển.

## 2. Phạm vi bản đầu cần thống nhất

| Nội dung | Đề xuất |
|---|---|
| Engine và mạng | Unity + Mirror; tất cả dùng cùng phiên bản cụ thể |
| Client ban đầu | Windows |
| Server ban đầu | Chạy riêng trên máy nhóm; chốt hệ điều hành đích trước khi viết pipeline |
| Quy mô | Một phòng, tối đa 4 người; chưa cần nhiều phòng đồng thời |
| Góc nhìn | Nhóm chọn một góc nhìn trong buổi đầu và giữ ổn định cho bản đầu |
| Di chuyển | Đi/chạy và xoay hướng; nhảy là phần bổ sung nếu còn thời gian |
| Vũ khí | Một loại súng, bắn bằng raycast |
| Luật trận | Đấu tự do, có máu, chết, hồi sinh và điểm hạ đối thủ |
| Reconnect | Giữ phiên trong khoảng thời gian nhóm chốt; bản đầu không yêu cầu phục hồi sau khi server khởi động lại |
| Tài nguyên | Dùng capsule và hình khối đơn giản trước |

Raycast là phép kiểm tra một tia để xác định vật bị bắn trúng. Server thực hiện hoặc quyết định kết quả kiểm tra này.

Chưa đưa vào bản đầu: tài khoản, database, cửa hàng, nhiều loại súng, nhiều bản đồ và hạ tầng nhiều dịch vụ. Nếu giảng viên có yêu cầu khác, nhóm điều chỉnh phạm vi và tài liệu trước khi làm.

## 3. Danh sách thành viên và trách nhiệm

| Mã | Điền tên | Vai trò chính |
|---|---|---|
| TV1 | … | Mạng, server và điều phối tích hợp |
| TV2 | … | Gameplay, luật trận và combat |
| TV3 | … | Client, giao diện, camera và arena |

Mỗi người chịu trách nhiệm cả triển khai, kiểm thử và tài liệu của phần mình. TV1 điều phối tích hợp, không có nghĩa phải sửa thay tất cả lỗi của nhóm. Có thể đổi người phụ trách theo năng lực nhưng cần chốt rõ trước khi bắt đầu.

### TV1 — Mạng, server và tích hợp

**Công việc:**

- Khởi tạo Unity project thật, cài Mirror và ghi lại phiên bản.
- Xây vòng đời kết nối client/server và spawn nhân vật.
- Gắn kết nối với đúng nhân vật; chặn điều khiển nhân vật của người khác.
- Tiếp nhận input, kiểm tra đầu vào và xử lý di chuyển chính thức tại server.
- Đồng bộ trạng thái đến client.
- Quản lý danh sách người chơi, ready, bắt đầu trận ở phía server.
- Quản lý mất kết nối, giữ phiên và xác thực reconnect.
- Chủ trì cấu hình build/CI sau khi cả nhóm chốt nền tảng; TV2 và TV3 hỗ trợ kiểm chứng.

**Thư mục chính:**

```text
Assets/_Project/Scripts/Networking/
Assets/_Project/Scripts/Server/Simulation/
Assets/_Project/Scripts/Server/Sessions/
Assets/_Project/Scripts/Server/Validation/
Assets/_Project/Scripts/Client/Networking/
```

**Bàn giao:** Kết nối được, đúng quyền điều khiển, di chuyển do server quyết định, lobby phía server và reconnect hoạt động; có hướng dẫn chạy và log để chẩn đoán lỗi.

### TV2 — Gameplay và combat

**Công việc:**

- Định nghĩa dữ liệu nhân vật, trạng thái trận và quy tắc gameplay dùng chung, phối hợp với TV1.
- Trong mốc đầu: xây quy tắc tốc độ, trạng thái nhân vật và kiểm tra input để ghép vào mô phỏng của TV1.
- Xây quy tắc vũ khí: cooldown, tầm bắn, sát thương và đạn nếu bản đầu có sử dụng.
- Xác thực phát bắn tại server: còn sống, đúng cooldown, nguồn/hướng bắn hợp lệ, vật cản và mục tiêu.
- Xử lý máu, chết, hồi sinh và điểm số.
- Viết kiểm thử cho các quy tắc có thể kiểm tra độc lập.
- Chuẩn bị ca thử gian lận combat và ghi kết quả.

**Thư mục chính:**

```text
Assets/_Project/Scripts/Server/Combat/
Assets/_Project/Scripts/Shared/Domain/
Assets/_Project/Settings/
Assets/_Project/Tests/EditMode/
```

**Bàn giao:** Server quyết định trúng đích và sát thương; luật trận hoạt động; có kiểm thử và bằng chứng từ chối yêu cầu combat sai luật.

### TV3 — Client, giao diện và arena

**Công việc:**

- Đọc input, điều khiển camera và gửi ý định qua API đã thống nhất với TV1.
- Tạo arena tối giản với sàn, tường, vật cản và điểm spawn.
- Làm màn hình kết nối, lobby, danh sách người chơi và nút ready.
- Hiển thị HUD: máu, điểm, trạng thái trận và thông báo kết nối.
- Thể hiện animation/hiệu ứng theo dữ liệu được xác nhận.
- Phụ trách lắp ráp scene và các prefab giao diện.
- Chuẩn bị cách mở nhiều client thử; hỗ trợ ghi hình và ghi nhận lỗi tích hợp.

**Thư mục chính:**

```text
Assets/_Project/Scripts/Client/Input/
Assets/_Project/Scripts/Client/Presentation/
Assets/_Project/Scripts/Client/UI/
Assets/_Project/Scenes/
Assets/_Project/Prefabs/UI/
Assets/_Project/Art/
Assets/_Project/Audio/
```

**Bàn giao:** Client thao tác được, camera đúng nhân vật, arena chơi được, UI phản ánh đúng trạng thái server và có thông báo lỗi kết nối dễ hiểu.

## 4. Buổi khởi động chung

Thực hiện theo thứ tự để tránh ba người tự tạo ba project khác nhau:

1. Điền tên TV1–TV3, thống nhất thời gian làm và lịch họp tích hợp.
2. Chốt phiên bản Unity, Mirror, góc nhìn và nền tảng build.
3. TV1 tạo Unity project thật từ bộ khung đã có. Không tạo một repo Git lồng bên trong repo hiện tại.
4. Cài Mirror theo phiên bản đã chọn; commit cấu hình Packages, ProjectSettings và các file .meta tương ứng.
5. TV3 tạo Bootstrap, Lobby, Arena và Player prefab tối thiểu; TV1 phối hợp cấu hình thành phần mạng.
6. TV2 chuẩn bị quy tắc nhân vật, dữ liệu input và các giới hạn ban đầu.
7. Hai thành viên còn lại clone/cập nhật repo và mở bằng đúng Unity version.
8. Thử build client/server tối thiểu để phát hiện sớm lỗi môi trường.

**Điều kiện hoàn thành:** Cả ba máy mở được cùng project, không thiếu package/tham chiếu tài nguyên; có cách chạy thử được ghi trong README.

## 5. Lộ trình bàn giao

Các mốc dưới đây là thứ tự triển khai, chưa phải cam kết ngày hoàn thành. Nhóm điền deadline sau khi biết thời hạn môn học và năng lực thực tế.

| Mốc | Nội dung | Phụ trách chính | Điều kiện hoàn thành | Deadline |
|---|---|---|---|---|
| M0 | Môi trường và Unity project | TV1, cả nhóm kiểm tra | Cả ba máy mở được; có bản chạy tối thiểu | … |
| M1 | Kết nối và di chuyển | TV1; TV2 + TV3 phối hợp | 1 server, 2 client, đúng quyền điều khiển và đồng bộ | … |
| M2 | Lobby 4 người | TV1 + TV3 | Đủ 4 người, ready, vào cùng trận; từ chối người thứ 5 | … |
| M3 | Combat và luật trận | TV2; TV1 + TV3 tích hợp | Bắn, máu, chết, hồi sinh, điểm do server quyết định | … |
| M4 | Reconnect | TV1 + TV3 | Lấy lại đúng phiên trong hạn; từ chối token sai | … |
| M5 | Kiểm thử tải, mạng, gian lận | Cả nhóm | Có kịch bản, số liệu, kết quả và lỗi đã xử lý | … |
| M6 | Build, CI và bàn giao | Cả nhóm; TV1 điều phối | Tạo được bản client/server, có tài liệu và pipeline phù hợp | … |

### Công việc giao ngay ở mốc M1

| Thành viên | Nhiệm vụ | Bằng chứng bàn giao |
|---|---|---|
| TV1 | Kết nối, spawn, nhận input và mô phỏng chuyển động tại server | Hai client kết nối; log nhận diện người chơi; vị trí do server cập nhật |
| TV2 | Dữ liệu/quy tắc nhân vật và giới hạn input | Quy tắc rõ ràng; kiểm tra giá trị ngoài phạm vi và trạng thái không được di chuyển |
| TV3 | Arena tối giản, camera, input, UI kết nối | Camera theo đúng nhân vật; thao tác được; nhập địa chỉ server và thấy trạng thái kết nối |

**Kiểm tra chung trước khi chốt M1:**

- Hai client đều nhìn thấy nhau di chuyển.
- Client chỉ điều khiển được nhân vật thuộc kết nối của mình.
- Sửa tốc độ cục bộ không làm tốc độ chính thức trên server vượt luật.
- Người chơi rời đi không làm server hoặc client còn lại lỗi.
- Có hướng dẫn tái hiện từ một bản clone mới.

## 6. Hợp đồng phối hợp giữa ba phần

Trước khi viết các phần phụ thuộc nhau, nhóm thống nhất tên dữ liệu, kiểu dữ liệu, đơn vị và bên có quyền quyết định trong docs/protocol.

| Dữ liệu/hành động | Bên đề xuất/phát sinh | Bên quyết định |
|---|---|---|
| Hướng di chuyển, hướng nhìn, yêu cầu bắn | Client | Server kiểm tra trước khi áp dụng |
| Vị trí hợp lệ, máu, cooldown, sống/chết, điểm | Server | Server |
| Camera, bố cục UI, hiệu ứng hiển thị | Client | Client hiển thị theo trạng thái liên quan |
| Ready và yêu cầu vào trận | Client gửi yêu cầu | Server quản lý trạng thái phòng |
| Yêu cầu lấy lại phiên | Client gửi thông tin phiên | Server xác thực và chấp nhận/từ chối |

Luồng chính:

```text
Input client → Dữ liệu giao thức → Lớp mạng → Server kiểm tra
→ Mô phỏng/combat → Trạng thái chính thức → Đồng bộ → Client hiển thị
```

Quy tắc tích hợp:

- TV1 và TV2 cùng chốt hợp đồng input và kết quả combat trước khi ghép code.
- TV3 nhận dữ liệu qua API/sự kiện đã thống nhất, không tự trừ máu hoặc quyết định điểm.
- Ai thay đổi hợp đồng dùng chung phải báo hai người còn lại và cập nhật tài liệu trong cùng PR.
- NetworkPlayer là cầu nối; không dồn UI, camera, combat và reconnect vào cùng một lớp.
- Thư mục chỉ tổ chức mã; khi viết assembly definition cần cấu hình phụ thuộc đúng, tránh tham chiếu vòng.

## 7. Quy trình Git

### Nguyên tắc

- main là nhánh tích hợp ổn định.
- Mỗi nhiệm vụ dùng một nhánh riêng, bắt đầu từ main mới nhất.
- Mỗi PR tập trung vào một mục tiêu có thể kiểm tra.
- Một thành viên khác review trước khi merge.
- Không force-push vào main và không đưa bí mật lên Git.

### Ví dụ nhánh

```text
feature/server-connect
feature/player-movement
feature/lobby-ui
feature/server-combat
feature/reconnect
```

### Bắt đầu nhiệm vụ

Chỉ chuyển nhánh khi đã commit hoặc lưu an toàn thay đổi đang làm; nếu main có thay đổi cục bộ, xử lý trước thay vì bỏ đi.

```bash
git switch main
git pull --ff-only origin main
git switch -c feature/server-connect
```

Sau khi hoàn thành, stage các file liên quan đến nhiệm vụ, kiểm tra rồi commit và push. Ví dụ:

```bash
git status
git add <duong-dan-file-lien-quan>
git diff --cached --check
git diff --cached
git commit -m "feat: add server connection flow"
git push -u origin feature/server-connect
```

Thay tên nhánh và đường dẫn bằng nhiệm vụ thực tế. Sau đó tạo PR vào main trên GitHub.

### Nội dung PR tối thiểu

- Vấn đề/tính năng được giải quyết.
- Thay đổi chính.
- Cách chạy thử cụ thể và kết quả thực tế.
- Phụ thuộc hoặc giới hạn còn lại.
- Ảnh/video nếu thay đổi giao diện hoặc gameplay cần quan sát.

Nếu main thay đổi trong lúc làm, tích hợp main mới vào nhánh tính năng theo quy ước nhóm và chạy lại kiểm tra liên quan. Khi hai người cùng sửa giao thức hoặc prefab, phối hợp trước khi merge.

## 8. Tránh xung đột scene và prefab

| Tài nguyên | Điều phối chính |
|---|---|
| Bootstrap và cấu hình kết nối | TV1 |
| Lobby, Arena và prefab UI | TV3 |
| Player prefab | TV3 lắp ráp, TV1 duyệt thành phần mạng, TV2 duyệt thành phần combat |
| Cấu hình vũ khí và luật trận | TV2 |
| Domain và Protocol dùng chung | Người thay đổi trao đổi với cả nhóm |

- Không để hai người sửa cùng một scene/prefab cùng lúc nếu chưa thống nhất.
- Người viết component có thể gửi PR code trước, sau đó phối hợp gắn vào prefab.
- Đổi tên/di chuyển tài nguyên qua Unity Editor khi có thể; giữ file .meta đi kèm.
- Không commit Library, Temp, log hoặc bản build; kiểm tra .gitignore.
- Khi đổi package hoặc ProjectSettings, nêu lý do trong PR và báo nhóm.

## 9. Kiểm thử và tiêu chí hoàn thành

Mỗi nhiệm vụ được coi là hoàn thành khi:

- Đạt điều kiện của nhiệm vụ và đã ghép vào luồng liên quan.
- Không còn lỗi Console liên quan đến thay đổi mới.
- Có bước kiểm tra và kết quả thực tế, không chỉ ghi “đã test”.
- Nếu thay đổi giao thức/quy tắc/cách chạy thì cập nhật tài liệu tương ứng.
- PR đã được review và merge.

### Kiểm thử theo người phụ trách

| Thành viên | Nhóm kiểm thử chính |
|---|---|
| TV1 | Quyền điều khiển, giới hạn input, kết nối, lobby, reconnect và ổn định server |
| TV2 | Cooldown, sát thương, vật cản, chết/hồi sinh, điểm và yêu cầu combat sai luật |
| TV3 | Input/camera/UI, hiển thị từ hai client trở lên và thông báo lỗi |
| Cả nhóm | Trận đủ 4 người, mô phỏng mạng xấu, build client/server và demo cuối |

Ví dụ ca thử cần có: người thứ 5 vào phòng; bắn quá nhanh; bắn khi đã chết; bắn xuyên tường; gửi input ngoài phạm vi; lấy lại phiên bằng token sai; mất mạng rồi quay lại.

Ở kiểm thử độ trễ, có thể bắt đầu với các mức RTT 0/50/100/200 ms, sau đó thêm jitter và loss. Đây là điều kiện thử đề xuất, không tự động là tiêu chí đạt. Ghi rõ công cụ đang áp dụng độ trễ một chiều hay khứ hồi và đo điều kiện thực tế.

Kiểm thử tải ban đầu tập trung vào một trận đủ 4 người. Chỉ kiểm tra nhiều trận đồng thời khi kiến trúc thực sự hỗ trợ. Ghi phiên bản build, phần cứng, CPU/RAM, lưu lượng và thời gian xử lý tick.

## 10. Nhịp phối hợp

- Mỗi buổi làm việc cập nhật ngắn: đã xong gì, đang làm gì, đang vướng gì.
- Báo ngay khi thay đổi ảnh hưởng API, dữ liệu mạng hoặc tài nguyên người khác đang dùng.
- Sau mỗi mốc, cả nhóm chạy lại một trận và giữ bản có thể demo.
- Nếu bị chặn bởi phần khác, thống nhất dữ liệu giả/API tạm để tiếp tục việc độc lập, sau đó thay bằng tích hợp thật.
- Không để toàn bộ việc ghép code đến cuối dự án.

### Mẫu giao một nhiệm vụ

```text
Tên nhiệm vụ:
Người phụ trách:
Mục tiêu:
Phần phụ thuộc:
Phạm vi thay đổi:
Điều kiện hoàn thành:
Cách kiểm tra:
Deadline:
Nhánh / PR:
```

### Nội dung cần chốt trong nhóm trước khi bắt đầu

- [ ] Điền tên ba thành viên vào TV1, TV2, TV3.
- [ ] Chốt Unity/Mirror version, góc nhìn và nền tảng build.
- [ ] Điền deadline cho M0–M6 theo lịch môn học.
- [ ] Thống nhất hợp đồng dữ liệu cho mốc kết nối/di chuyển.
- [ ] Xác nhận cả ba máy mở được project.
- [ ] Bắt đầu ba nhiệm vụ của M1 và đặt lịch demo chung.
