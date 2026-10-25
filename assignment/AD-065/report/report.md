# AD-065 · Quyền sở hữu dữ liệu và rollback

**Đồng Vũ Hoàng Long · 23001901 · SE2026-T17**  
GAME-3D-03 – Small Arena · Thành viên nhóm · 3TC · Ngày khảo sát: 25/10/2026

> Tôi chịu trách nhiệm về toàn bộ nội dung bài làm và đã khai báo việc sử dụng AI.

## 1. Phạm vi và căn cứ thực tế

Đây là lát cắt máy chủ có thẩm quyền do Codex tạo mới cho bài cá nhân: xử lý lệnh đấm, lưu HP, chống lặp và ghi sự kiện trong một giao dịch. Không phải toàn bộ trò chơi. SRS v0.2 tại `Long_document/srs.md:29–37,358–371` xác nhận MVP ngoại tuyến, bot 10 HP, đấm thường trừ 1 HP; phần mạng là hướng tương lai (`:577–629`). Các mức 764 tác vụ, p95 ≤ 4,5 giây, lưu vận hành 7 ngày và phản hồi yêu cầu thay đổi 12 giờ đến từ đề AD-065, không phải NFR ngoại tuyến.

Kho gốc `tasdus-117/SE2026-T17`, nhánh mặc định `main@b5054a6` chỉ có README. Đã fetch và đọc hai nhánh tham khảo: `tasdus117@1257ad7` (Godot/C#/EF/PostgreSQL), `feature/system-skeleton@db886dd` (Go/UDP/pgx). Mã của đồng đội không được sao chép, merge hay cherry-pick. Worktree riêng bảo toàn `AD-065-prompt.md` và `Long_document/` chưa được theo dõi. Bằng chứng SHA đầy đủ: [E1].

SRS đề xuất Unity/C# cho client và không bắt buộc backend. Chọn Node 25.9.0 + SQLite 3.51.3 cho thí nghiệm độc lập: sẵn có, không cần thư viện runtime, có giao dịch thật và database dùng một lần. Docker có thể dùng nhưng không cần cho adapter này. Đây là quyết định phạm vi; chưa xác minh tích hợp Unity hay tính tương thích PostgreSQL.

## 2. Đúng ba driver, theo thứ tự ưu tiên

| Ưu tiên | Driver và nguồn | Hệ quả kiến trúc và lý do |
|---|---|---|
| D1 | Độ trễ đồng thời: 764 tác vụ, p95 ≤ 4,5 s [đề AD-065] | API nhỏ, tải tách tiến trình, ghi cùng database; ưu tiên vì là thuộc tính chất lượng chính. Không suy ra năng lực sản xuất. |
| D2 | Tin cậy khi đổi schema và rollback [tình huống AD-065] | Thêm cột nullable; xác minh sau triển khai; phục hồi app cũ trên schema mở rộng. Tránh DOWN mất dữ liệu. |
| D3 | Nhất quán và quyền sở hữu [SRS FR-010/015/017; AD-065] | Match sở hữu luật; Store sở hữu dữ liệu; HP, định danh lệnh, sự kiện commit nguyên tử. Dữ liệu vận hành giữ 7 ngày; phản hồi thay đổi trong 12 giờ. |

## 3. Một kịch bản chất lượng Performance

**Nguồn:** 764 tác vụ client thử nghiệm. **Kích thích:** gửi `POST /v1/combat`, 50% lệnh mới và 50% retry. **Môi trường:** máy Windows cục bộ, app/database ổn định sau 764 yêu cầu khởi động. **Đối tượng:** HTTP → dịch vụ → SQLite file. **Phản ứng:** xác thực, áp dụng đúng một lần hoặc trả kết quả đã lưu. **Đáp ứng đo được:** p95 từ lúc gửi đến đọc xong JSON ≤ 4,5 giây trong cửa sổ 10 vòng/tác vụ, 7.640 mẫu, tính cả yêu cầu lỗi; yêu cầu tỷ lệ lỗi 0. Cách tính và kết quả thực tại trang 6. Đây là kịch bản duy nhất của báo cáo.

<!-- page -->
# 2 · Module View và chiều phụ thuộc

{{MODULE_DIAGRAM}}

*Hình 1. Mermaid Module View; ô xanh thể hiện quyền sở hữu lưu trữ và ranh giới giao dịch.*

Transport xác thực token, giới hạn body và ánh xạ ý định; không tính sát thương. `CombatService` điều phối qua giao diện cấu trúc UnitOfWork. `Match`, `PlayerId`, `Health` tập trung luật và che giấu giá trị bên trong. Store hiện thực giao diện, là chủ duy nhất của các bảng `matches`, `commands`, `events`; migration chỉ thay cấu trúc, retention chỉ gọi xóa sự kiện đã hết hạn.

Đồ thị phụ thuộc hướng từ transport vào ứng dụng, từ ứng dụng vào domain và cổng giao dịch. Domain không import HTTP/SQLite. Composition root ghép các adapter cụ thể; đường implements nét đứt phân biệt cổng với hiện thực. Có tám trách nhiệm được chỉ rõ, trong đó công cụ tải đứng ngoài luồng nghiệp vụ.

`application.mjs:11–28` mở đơn vị công việc; `store.mjs:16–31` thực hiện `BEGIN IMMEDIATE` → đọc trạng thái/định danh → ghi HP + kết quả lệnh + sự kiện → COMMIT. Sai một bước thì ROLLBACK. Không phát sự kiện ra ngoài trước commit. `onCommitted` là observer tùy chọn, có thể lặp và mất; không được dùng làm nguồn thưởng bắt buộc. Domain data và danh tính lệnh không chịu TTL 7 ngày. [E2, E3]

<!-- page -->
# 3 · Runtime View: triển khai, phát hiện lỗi, rollback

{{RUNTIME_DIAGRAM}}

*Hình 2. Mô phỏng cục bộ hai chế độ writer; App/Store được gộp để đọc được trên A4.*

**Bất biến:** cùng `(matchId, commandId)` chỉ gây sát thương một lần; HP nguyên trong [0,10]. Match kiểm tra chủ sở hữu và khoảng cách do server lưu; SQL hỗ trợ bằng khóa chính, UNIQUE, CHECK và FK. Lệnh mới khi HP = 0 bị 409; retry hợp lệ vẫn trả kết quả gốc, có thể khác HP hiện tại. [E2]

**Hợp đồng v1:** bearer token ánh xạ một actor thử nghiệm; body chỉ có `matchId`, `commandId`. Kết quả chứa version, hai ID, health và status. Lỗi 400/401/403/404/409/413/500; không nhận damage hoặc actor từ client. V2 chỉ thêm `events.detail` nullable. Test phát hiện phản hồi HP sai sau commit, dừng v2, chạy lại v1 trên cùng file; HP đi 10→9→8→7, dữ liệu v2 vẫn còn. Rollback app không đảo ngược sát thương đã commit. [E2, E4]

<!-- page -->
# 4 · ADR-065-01: một chủ ghi, một giao dịch

**Trạng thái:** chấp nhận cho thí nghiệm cá nhân; chưa là quyết định triển khai của nhóm. **Bối cảnh:** kho main chưa có game, SRS phân biệt MVP ngoại tuyến và mạng tương lai; tình huống AD-065 yêu cầu đổi schema có rollback, đồng thời đo 764 tác vụ. Khả năng vận hành của bài cá nhân có giới hạn. [E1]

## So sánh hai phương án có thể triển khai

| Tiêu chí | A · Monolith mô-đun + một DB | B · Combat service + event worker |
|---|---|---|
| Performance | Không hop mạng nội bộ; SQLite sync có thể chặn event loop | Có thêm mạng/queue; có thể tách tải worker nhưng phải đo |
| Nhất quán | Một commit cho HP, identity, event | Cần outbox, dedup consumer; nhất quán cuối cho projection |
| Sửa đổi | Domain/cổng tách adapter; phát hành chung | Worker đổi riêng; contract và migration phải phối hợp |
| Triển khai/rollback | Một app, schema cộng thêm giữ được writer cũ | Nhiều version producer/consumer cùng tồn tại; kiểm tra tương thích khó hơn |
| Độ phức tạp/chi phí | Một tiến trình, một file; phục hồi file cần thiết kế thêm | Broker, retry, giám sát độ trễ queue và nhiều dịch vụ |
| Năng lực nhóm | Phù hợp một lát cắt cá nhân có kiểm chứng | Chỉ hợp lý khi có nhu cầu phân tách vận hành và người phụ trách |

**Quyết định:** chọn A. Combat application service là bên duy nhất yêu cầu đổi state; Match sở hữu luật và Store sở hữu ghi dữ liệu. Không dùng giao dịch phân tán. Phần B là lựa chọn tương lai có thật về thiết kế, chưa được triển khai hay benchmark trong bài này.

**Lý do:** tính nguyên tử chống lặp và tính tương thích schema có thể kiểm tra trực tiếp trong một database; lợi ích phân phối chưa có số đo chứng minh. Node/SQLite giúp thực thi bài ngay, dù không thể thay thế đầy đủ Unity authority hoặc PostgreSQL của các nhánh khác. Cổng UnitOfWork cho phép đổi adapter nhưng cần suite tích hợp mới cho DB mới.

**Hệ quả:** tăng cohesion nhờ luật trong Match/Health; giảm coupling trực tiếp với SQL ở ứng dụng. Đổi lại, giao dịch SQLite đồng bộ giữ khóa ghi và chặn event loop. COMMIT với `synchronous=FULL` bảo vệ độ bền tốt hơn, nhưng tăng độ trễ đĩa. Bỏ ghi dedup để giảm p95 sẽ phá bất biến khi retry; chọn giữ nhất quán, đo độ trễ rồi mới tối ưu. [E2, E5]

**Chính sách triển khai:** expand bằng nullable detail; chỉ contract khi hết cửa sổ rollback được thống nhất và không còn writer cũ. Không mặc định cửa sổ này là 12 giờ. Con số 12 giờ chỉ nghĩa vụ phản hồi yêu cầu thay đổi; chưa có hệ thống ticket tự động đo nghĩa vụ đó.

**Kích hoạt xem xét lại:** hai lần chạy 5 phút đại diện liên tiếp ở 764 tác vụ có p95 >4,5 s hoặc lỗi >0,1%; event-loop lag p95 >50 ms trong 5 phút; yêu cầu nhiều writer độc lập trên cùng tập match; hoặc danh tính lệnh vượt 1 GB/database. Khi đó đo contention, cân nhắc PostgreSQL/shard theo match hay outbox trước khi tách service. Đây là ngưỡng đề xuất, không phải kết quả đã đo. [E6]

<!-- page -->
# 5 · Chất lượng mã và refactoring có dấu vết

## Baseline thực, không gán nợ mới cho đồng đội

Commit `fd4df4c4f13734eb812063e1b1861d4523d4a68b` lưu feature đấm thuần và ba test đặc tả trước khi sửa. `src/combat.mjs:1–6` nhận `health`, `attacker`, `owner` dưới dạng number/string. Mã này được tạo trong phiên AI cho bài cá nhân. Không phải phát hiện smell có sẵn trong mã của nhóm. Commit `1ff103387097911312fdf0666bf993ad384a0e98` lưu hiện thực sau refactor và kết quả kiểm thử. Tiền tố đường dẫn `src/` và `test/` tại trang này là `assignment/AD-065/implementation/`.

| Vấn đề và bằng chứng | Rủi ro / phạm vi ảnh hưởng | Bước xử lý và an toàn hồi quy |
|---|---|---|
| Q1 · Primitive obsession, baseline `combat.mjs:1–6` | HP và damage đều có thể là số; string chưa phân biệt danh tính. Luật dễ bị lặp ở caller; mất nghĩa miền. | Đã xử lý: `domain.mjs:5–36` dùng PlayerId, Health, Match; private field che giấu state. Giữ `strike` làm adapter hành vi. Ba test characterization giữ nguyên. |
| Q2 · Coupling lịch thực thi, `store.mjs:16–29` | SQL đồng bộ và HTTP dùng cùng event loop; thao tác đĩa chậm ảnh hưởng mọi request trong process, dù khác match. | Chưa xử lý: đo event-loop lag và khóa trước; chuyển adapter sang worker hoặc PostgreSQL async khi có căn cứ. Giữ suite atomicity + 764 retry + benchmark khi thay adapter. |
| Q3 · Giao diện observer, `transport.mjs:22–23` | Callback chạy sau mọi response thành công, kể cả replay; mất hoặc lặp thông báo. Ghép phần thưởng vào callback sẽ sai. | Chưa xử lý: contract ghi rõ best effort; nếu cần side effect bắt buộc, dùng outbox/consumer dedup. Test hiện tại chứng minh observer lỗi không hủy commit; cần test crash/replay consumer mới. |
| Q4 · Vòng đời dữ liệu, `migrations.mjs:8–10`; `retention.mjs:3–4` | Commands không hết hạn: đúng chống lặp nhưng tăng dung lượng, ảnh hưởng index và vận hành. | Chưa xử lý: đóng/archival match rồi từ chối lệnh muộn trước khi xóa danh tính. Test purge hiện có bảo đảm retry cũ không gây damage mới. |

**Cohesion:** arithmetic và điều kiện HP tập trung trong Health, quyền đánh trong Match; transport chỉ biết contract. **Information hiding:** private state không có setter công khai; ứng dụng chỉ nhận hành vi và kết quả. **Dependency direction:** domain không biết Store; UnitOfWork được truyền vào thay vì dịch vụ tự mở DB. Hợp đồng cấu trúc JavaScript chưa được kiểm tra kiểu tĩnh, là giới hạn thực tế chứ không được gọi là “đã chứng minh type safety”.

**Trước/sau:** cùng mọi HP sống 1..10 trả HP−1; sai owner vẫn 403 trước kiểm tra kết thúc; HP=0 vẫn 409; health hỏng vẫn lỗi đóng. Ba test ban đầu qua cả hai commit. Phạm vi chứng minh là các trường hợp đã đặc tả với ID hợp lệ, không phải mọi input tùy ý của helper cũ. API v1 giữ shape, version và error semantics trong suite tích hợp. [E2, E3]

<!-- page -->
# 6 · Xác minh và sổ nợ kỹ thuật

## Kết quả đã thực thi

`node --test test/*.test.mjs`: **10 pass, 0 fail, 0 skip**, gồm 3 characterization và 7 integration. Kiểm tra HP/owner/range/terminal, CHECK/FK, intent-only API, 764 retry cùng ID, fault giữa ghi HP và event, đóng/mở DB, migration v1→v2→writer v1, cutoff 7 ngày và replay sau purge. `node tools/check.mjs`: cú pháp và whitespace 13 module pass; Node không cần bước biên dịch riêng. Không chạy Unity, PostgreSQL, WAN, kiểm tra kiểu tĩnh hay soak dài. [E3]

Hai lỗi đầu được giữ trong `evidence/tests/initial-failures.md`: burst 764 kết nối cùng event loop gây ECONNREFUSED; tăng backlog riêng chưa giải quyết. Chuyển fixture server sang process riêng giữ nguyên 764 tác vụ và mọi assertion thì pass. Lỗi EPERM cleanup do DB v1 còn mở được sửa bằng đóng handle trước xóa temp. Không bỏ assertion hoặc đổi tải để che lỗi.

## Đo tải thực, có giới hạn

Ryzen 7 5800HS, 16 CPU logic, RAM 16.541.487.104 byte; Windows `10.0.26200`; Node `v25.9.0`; SQLite `3.51.3`, file WAL/FULL. Client và server là hai process trên cùng máy, HTTP loopback; cố định một owner và khoảng cách hợp lệ. **764 tác vụ closed-loop**, mỗi tác vụ một request đang chờ, không think time; 50% lệnh mới/50% retry; 5 lệnh mới/match. Warm-up 764 mẫu trong **876 ms**, 0 lỗi. [E5]

Đo lúc `2026-10-25T16:25:49.859Z` trong **4,299 s**, **7.640 mẫu**, **0 lỗi (0%)**, **1.776,95 request/s**; p50 **420,75 ms**, p95 **583,74 ms**, p99 **658,32 ms**. Sắp xếp độ trễ tăng dần, nearest-rank `sorted[ceil(p×N)−1]`; thời gian từ gửi đến đọc xong JSON, bao gồm mẫu lỗi nếu có. **Đạt mục tiêu p95 ≤4.500 ms trong thí nghiệm này**. Không chứng minh p95 trong sản xuất, tải mở, nhiều writer hoặc đủ dài để thấy saturation. Raw samples và command tái lập tại [E5].

## Nợ tồn tại và thứ tự xử lý

Điểm = tác động × cấp bách / công sức; mỗi thang 1–5. Đây là ước lượng AI có giải trình, không phải chấm điểm của sinh viên hay số đo chi phí. Có thể đổi thứ tự khi có số liệu mới.

| Nợ | Bằng chứng / tác động | I/U/E → điểm | Hành động / hồi quy |
|---|---|---|---|
| TD1 | Q2: writer đồng bộ chặn các request | 4/4/3 → 5,33 | Đo lag/lock; adapter async khi cần; chạy atomicity và tải. |
| TD2 | Q3: observer có thể mất/lặp | 4/3/3 → 4,00 | Outbox khi có effect bắt buộc; test crash/replay. |
| TD3 | Q4: commands tăng không giới hạn | 3/3/3 → 3,00 | Archival với từ chối lệnh muộn; test replay sau purge. |

TD1 đứng đầu vì đường ghi nằm trên mọi command, trực tiếp đe dọa D1; TD2 chưa gây sai kết quả miền vì observer không bắt buộc; TD3 cần dữ liệu dung lượng dài hạn. Q1 đã giải quyết, không ghi thành nợ còn mở. [E6]

<!-- page -->
# 7 · Review AI, traceability và kết luận

## Review đơn phiên, có phê bình

Prompt thật yêu cầu kiểm tra boundary, rollback, refactoring, retention và tải tại commit `1ff1033`; lưu tại `evidence/ai/design-review-prompt.md`. Output do chính Codex phiên này tạo, không có reviewer độc lập. Đề xuất 1: áp dụng TTL 7 ngày cho cả commands và events. **Bác bỏ phần commands** vì xóa identity làm retry cũ gây damage mới; test retention xác minh identity còn sau purge (`test/integration.test.mjs:135–148`). Đề xuất 2: p95 tốt đủ để nhân nhiều writer. **Bác bỏ suy luận**, sửa thành đo contention trước: Store dùng BEGIN IMMEDIATE sync; tải chỉ có một writer và loopback. [E6]

Giữ đề xuất một transaction và schema cộng thêm vì test fault/restart/rollback hỗ trợ. Chưa chắc chắn về physics/cooldown, WAN, rotation token, outbox, disk contention và PostgreSQL. Không có xác nhận kiểm tra trực tiếp của sinh viên trong phiên; toàn bộ mã và phân tích mới do AI hỗ trợ và được khai báo. Phụ lục chứa prompt gốc đầy đủ, prompt review và output thực.

## Ma trận truy vết

Tiền tố **I** = `assignment/AD-065/implementation/`; **E** = `assignment/AD-065/evidence/`. Mã mới/refactor ở commit `1ff103387097911312fdf0666bf993ad384a0e98`; baseline smell `fd4df4c4f13734eb812063e1b1861d4523d4a68b`.

| Yêu cầu → quyết định | Boundary/contract → mã chính xác | Test → kết quả / chứng cứ |
|---|---|---|
| D1: 764, p95≤4,5 s → một writer, đo thật | HTTP v1 → I`tools/load.mjs:16–58`; I`src/config.mjs:1` | 7.640 mẫu, p95=583,74 ms; E`performance/load.json` |
| D2: rollback schema → expand/giữ cột | Writer v1/v2 → I`src/migrations.mjs:1–20`; I`src/store.mjs:23–25` | I`test/integration.test.mjs:111–133` pass; E`rollback/result.md` |
| D3: chống lặp/HP → giao dịch nguyên tử | Match + UnitOfWork → I`src/application.mjs:11–28`; I`src/domain.mjs:17–35` | I`test/integration.test.mjs:40–80,95–109` pass; E`tests/final-tests.txt` |
| Vận hành: 7 ngày → purge event, giữ identity | Retention → I`src/retention.mjs:3–4` | I`test/integration.test.mjs:135–148` pass |
| Phản hồi thay đổi: 12 giờ → quy tắc vận hành | I`src/config.mjs:1`; I`CONTRACT.md` | Test assert cấu hình pass; thực thi quy trình ticket chưa xác minh |
| Primitive obsession → Health/PlayerId | I`src/combat.mjs:1–6`; I`src/domain.mjs:5–26` | Ba characterization không đổi, pass trước/sau; E`tests/` |

## Kết luận trong phạm vi bằng chứng

Lát cắt chạy được chứng minh tính nguyên tử và khả năng phục hồi app cũ với schema cộng thêm. Không đánh đồng bản mẫu này với máy chủ 3D hoàn chỉnh. Các nhu cầu chưa thực thi phải qua đo và review trước khi mở rộng; sinh viên cần tự đọc, hiểu và chịu trách nhiệm về nội dung theo tuyên bố trang đầu.

**Tài liệu truy chứng:** [E1] `evidence/repository-baseline.md`, `evidence/source-analysis.md` (SHA và nguồn SRS); [E2] `implementation/CONTRACT.md`, `src/` tại commit nêu trên; [E3] `evidence/tests/final-tests.txt`, `static.txt`, `initial-failures.md`; [E4] `evidence/rollback/result.md`; [E5] `evidence/performance/load.json`, `implementation/tools/load.mjs`; [E6] `evidence/ai/design-review-output.md`, ADR/sổ nợ trên trang 4–6. Tất cả dưới `assignment/AD-065/` trừ SRS địa phương đã ghi rõ. Không sử dụng trích dẫn học thuật bên ngoài chưa kiểm tra.
