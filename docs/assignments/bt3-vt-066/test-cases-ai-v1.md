# Câu 2 — chiến lược và test cases AI sinh, trước review

Ngày 2026-11-14. Sinh viên Nguyễn Chí Hoàng Tú, 23001947. Baseline `d939568aae110668df5f0114541e4498593b6108`. AC1–AC5/invariant/R1–R3 giữ nguyên `question-1.md`. Đây là thiết kế chưa chạy, chưa được sinh viên sửa/xóa cases. Không ghi draft này thành bảng cuối đã duyệt.

## Chiến lược theo risk

Ưu tiên P0: retry gây tải/terminal trùng, failure mở lại thao tác, sai quyền/spawn. P1: overlay và guest lifecycle, phục hồi UI, phiên mới/capacity. P2: kiểm phụ trợ. P0/P1 không thay thế mức Unit/Integration/E2E.

- **Unit:** rule `LobbyRoom` và guard terminal/session. Chạy nhanh, phủ partitions/pairwise, đo chuyển trạng thái trước/sau. Vì assembly Server có Unity/Mirror dependency, Unit này vẫn có thể đặt trong PlayMode test assembly; không gọi Domain runner để chứng minh lobby.
- **Integration/API:** API là `LobbyStartRequest`, `LobbyReadyRequest`, `LobbySnapshot`, Mirror scene callback và coordinator→load boundary; dùng M1LoopbackTransport có kiểm số request dispatch. Fixture fake transport, production server/coordinator/rules thật. Bộ đếm boundary, actual side effect, terminal tách rời. Virtual guest nhận snapshot không chứng minh guest scene/UI.
- **E2E:** host dùng UI thật, Start→Mirror async→Arena_MVP→cleanup overlay/spawn. Guest scene/UI cần một client-only runtime hoặc process riêng; chưa được thay bằng virtual guest. Test local scene lifecycle có điều khiển chỉ là Integration UI, không tự gọi là multi-process E2E.
- **Không cần:** REST/HTTP status, database transaction, payment/email queue tests vì slice không có các dependency đó. Không thử 100 người trong một phòng 4 người. Không mở rộng reconnect/host migration/soak Internet; giới hạn được báo riêng. Snapshot lặp được phép, không assert mỗi snapshot chỉ gửi một lần.

## Oracle và đo lường

- **B (boundary attempts):** tăng ngay trước coordinator gọi boundary bắt đầu tải, kể cả call ném lỗi.
- **L (actual loader side effects):** số lần thực sự khởi động scene loader ở từng process. Wrapper fake chỉ chứng minh L của dependency fake; E2E thật mới chứng minh Mirror load thực. Với healthy host session mong B=1,L_server=1; host không tải scene lần hai như guest. Mỗi guest có loader riêng, không cộng tải guest vào counter chống retry server.
- **S/F (terminal transitions):** số cạnh Loading→Playing / Loading→Failed của thao tác; S+F≤1. Close→Offline sau teardown không là terminal nghiệp vụ thứ hai. Lặp snapshot/callback không tự tăng S/F.
- Rejected Start: B=L=S=F=0, room không bị request đó thay đổi. Accepted healthy: B=L=S=1,F=0. Injected startup exception trước loader: B=1,L=S=0,F=1; retry không đổi các counter; sau teardown room Offline vẫn giữ evidence kết quả failure của thao tác.
- Overlay: ít nhất ba điểm quan sát: trước client scene-start không có; sau RoomPanel bị hủy và trước client-ready còn hiện; sau client-ready/failure/disconnect không còn. Không dùng server-ready làm oracle guest, không dùng fixed sleep làm chứng cứ.
- Failure signal phải được xác định từ production bridge khi triển khai. Exception injection vào seam được coordinator thật gọi chứng minh handling của seam, không chứng minh mọi native exception. Mirror log rồi return không bị try/catch phát hiện; T14 kiểm failure detection riêng.

## Bảng AI sinh — 18 cases (chưa review)

| ID | AC/risk | Level | Precondition/input | Expected result | Priority |
|---|---|---|---|---|---|
| T01 | AC1/AC2; tương tác quyền × roster × gửi lặp | Unit rules; Integration cho request admitted | Chạy 12 dòng PW01–PW12; mỗi dòng room mới Waiting; readiness đúng roster. Với unknown dùng ID không có trong room ở rule boundary, không giả vờ có guest thực. | Accept iff host + two_ready/four_ready; rejection giữ Waiting. Ở integration admitted rows, healthy loader theo B/L/S của matrix. Unknown rows chứng minh rule rejection; không claim unknown E2E. | P0 |
| T02 | AC2; retry sát nhau gây load trùng | Integration/API Mirror | Host+guest Ready; enqueue 2 Start cùng connection trước pump. Giữ Loading qua seam có tín hiệu, kiểm cả hai requests đã dispatch nếu dùng seam. | Trong cùng phiên B=1; healthy completion L=1,S=1,F=0. Không để message thứ hai bị mất/pause mà gọi đó là chứng minh dispatch retry; dùng T03 kiểm giai đoạn tiếp theo. | P0 |
| T03 | AC2; retry khi Loading và sau Playing | Integration/API | Start hợp lệ, boundary có kiểm soát giữ Loading; dispatch retry tại Loading; hoàn tất; dispatch tiếp sau Playing. | Tổng requests=3, B=1,L=1,S=1,F=0; room cuối Playing. Nếu Mirror pause dispatch, test coordinator seam riêng và ghi rõ, cộng integration sau load. | P0 |
| T04 | AC4; callback complete lặp | Unit + Integration callback | Start hợp lệ; callback arena hợp lệ hai lần; callback scene không liên quan một lần. | Loading→Playing đúng 1 lần; B=L=1,S=1,F=0; gate spawn không mở trước Playing. Snapshot resend được phép. | P0 |
| T05 | AC5/AC2; failure rồi retry tạo load/terminal trùng | Integration production coordinator + injected boundary | Cùng phiên hợp lệ; boundary thật được coordinator gọi ném startup exception trước scene side effect; retry cùng sender, lặp failure notification nếu có, rồi callback arena đến sau failure. | B=1,L=0,S=0,F=1; retry không load lại, callback muộn không Playing/spawn. Trước teardown terminal Failed, sau teardown Offline; lưu cả hai mốc và error/disconnect UI riêng T10. | P0 |
| T06 | AC1; guest Ready cố Start (negative) | Integration Mirror message | Hai thành viên Ready; guest thực của transport gửi Start. | Rejected; Waiting; B=L=S=F=0, host vẫn có thể Start hợp lệ sau đó. | P0 |
| T07 | AC1; thiếu số lượng/readiness (negative) | Unit + Integration Mirror | Host một mình Ready; hoặc 2 thành viên còn 1 chưa Ready; host gửi Start. | Waiting, B=L=S=F=0; readiness/roster không bị Start sửa; sau bổ sung đủ điều kiện host Start được. | P0 |
| T08 | AC3/R1; UI lobby bị hủy giữa tải | Integration local UI scene + host E2E | MainMenu/RoomPanel thật; Start hợp lệ, scene start thật. Tạo cửa sổ quan sát lifecycle có tín hiệu để không bỏ lỡ khoảng trước ready. | Overlay 0 trước local scene-start; còn sống sau RoomPanel destruction và trước local-ready; dọn sau local-ready; không dùng Loading snapshot để bật, không phần trăm. B=L_server=S=1 ở host E2E. | P1 |
| T09 | AC3/R1; guest hoàn tất theo host quá sớm | Integration client-only lifecycle; guest E2E khi đủ runtime | Client-only guest đã bắt đầu đổi scene, chưa local-ready; server/host complete trước; sau đó guest ready. | Overlay guest vẫn hiện sau host complete/RoomPanel destroyed, chỉ dọn khi chính guest ready. Virtual snapshot guest không đủ; ghi local lifecycle test hay process guest UI thật. | P1 |
| T10 | AC5/R2; cleanup làm mất thông báo | Integration UI + E2E failure/disconnect nếu có runtime | Overlay đang hoạt động; client quan sát startup failure hoặc disconnect; navigation về MainMenu và manager cũ có thể bị hủy. | Overlay/listener dọn; màn hình MainMenu/UI phục hồi có thông báo đọc được, không chỉ giữ string trong manager đã destroy. Session mới không mang lỗi/overlay cũ. | P1 |
| T11 | AC5/R2; guest disconnect làm fail cả phiên | Integration server + client-local isolation | Host+2 guest; Start; một guest disconnect khi tải; guest còn lại và server tiếp tục. Test local overlay của guest rời riêng nếu dùng virtual transport. | Chỉ overlay guest rời kết thúc; server không chuyển Failed do leave, thành viên khác tiếp tục; B_server=1, healthy server S=1,F=0. Virtual transport không chứng minh overlay process guest còn lại. | P0 |
| T12 | AC2/AC4; reset phiên và callback cũ | Unit + Integration session lifecycle | Phiên A thành công hoặc failure rồi teardown; mở B Waiting; callback arena cũ đến khi B chưa Start; sau đó B đủ Ready và Start. | Callback khi Waiting không terminal/spawn; B Start bình thường, B_boundary=1,S_B=1. Không tuyên bố phân biệt callback cùng tên scene từ A sau khi B đã Loading nếu chưa có correlation lifecycle thật. | P0 |
| T13 | AC1/NFC capacity; mutation >→>= | Integration thật OnServerConnect authenticated count | Fixture tạo total authenticated count gồm connection mới lần lượt 3,4,5, mỗi count trạng thái độc lập; dispatch contract connect thật. | Count 3/4 được admitted; 5 rejected, room≤4. Cùng test sạch/mutant/khôi phục; mutant >= phải fail assertion tại 4. Đây là capacity regression, không thay test retry. | P1 |
| T14 | AC5; nhánh log-return bị hiểu nhầm là exception | Integration load bridge characterization | Trước patch đọc/characterize inactive server và Mirror invalid scene/log-return. Sau patch kiểm preflight scene không khả dụng hoặc failure signal tường minh ở boundary thật nếu seam hỗ trợ. | Không coi return/log là success; kiểm loại tín hiệu thật: exception, false/error result hoặc preflight fail. No-start L=0. B/F theo vị trí guard được ghi rõ; nếu không đi qua accepted Start thì không claim T05/AC5 đầy đủ. | P0 |
| T15 | AC4; bypass spawn trước Playing | Integration Mirror AddPlayer/callback | Room Waiting, Loading, Failed; gửi/dispatch add-player theo contract; rồi Playing hợp lệ. | Không có player gameplay trước Playing, callback sau failure không mở gate; Playing hợp lệ mới cho phép một player/connection, không nhân đôi. | P0 |
| T16 | AC3/NFC; shared state/listener cũ | Integration UI lifecycle lặp | Hoàn tất/teardown nhiều session liên tiếp; subscribe/unsubscribe; chạy thứ tự suite thay đổi nếu runner hỗ trợ. | Không overlay/listener singleton rò, không dùng manager cũ, không lỗi cũ trong session mới; ghi số lượt thật. Lặp timing T08 và order/shared-state T12/T16 ≥20 lượt ở Câu 4. | P1 |
| T17 | AC2; retry sau hoàn tất | Unit | Host đã Start thành công; gọi TryStart(host) lần nữa. | TryStart trả false. | P2 |
| T18 | AC3; label tải hiển thị | UI | Bấm Start hợp lệ; chờ cố định 2 giây. | Tìm thấy text “Loading arena...” trong RoomPanel. | P2 |

T01 là **một case tham số hóa 12 rows**, không tính là 12 case độc lập để thổi số lượng. Bảng có 18 ID; nếu sinh viên xóa T17/T18 thì còn 16 ID. T06/T07 là negative rõ ràng; T05 là failure-path kiểm cả terminal và side effects.

## Pairwise: phạm vi và constraints

Factors/levels:

1. Requester: host / guest / unknown (unknown = ID không có trong room tại rule boundary).
2. Roster: solo_ready (host Ready); two_partial (host Ready, guest chưa Ready); two_ready; four_ready.
3. Delivery: single (1 request); burst (2 requests sát nhau); after_result (1 request rồi lặp sau kết quả request đầu).

Mỗi row bắt đầu Waiting, healthy loader. `after_result` là sau từ chối nếu request đầu bị từ chối, hoặc sau terminal Playing nếu được chấp nhận. Không bắt tổ hợp bị từ chối phải đi qua Loading/Playing. Roster là compound factor count/readiness có chủ ý; chưa tuyên bố pairwise riêng cho mọi biến thành viên/readiness độc lập.

Excluded: guest × solo_ready với cả 3 delivery levels (3 full rows), vì guest đã admitted phải tồn tại cạnh host. Unknown × solo_ready hợp lệ tại TryStart(int) vì unknown là ID không thuộc room, không phải guest đã join. Unknown rows giới hạn Unit guard; không dựng network state bất khả thi để gọi là Mirror E2E.

Trên 36 full combinations có 33 feasible rows; universe feasible pairs là **32**, gồm requester×roster=11, requester×delivery=9, roster×delivery=12. Greedy chọn 12 rows, verifier dùng tập cặp từ toàn bộ feasible Cartesian rows, đối chiếu matrix và lưu witness row cho từng cặp vào `pairwise-ai-v1.json`. Xem matrix ở `pairwise-ai-v1.md`.

Retry trong Loading, startup failure, callback lặp/sau failure, guest disconnect, overlay guest và mutation boundary không thuộc healthy Waiting matrix; đã có T02–T05, T08–T14 bổ sung. Pairwise không được dùng để loại các high-risk cases này.

## Ứng viên sinh viên trực tiếp sửa/xóa

**T17 — nội dung gốc:** Host đã Start thành công; gọi TryStart(host) lần nữa. Expected: TryStart trả false. Priority P2, Unit, AC2.

Lý do yếu/trùng: lặp phần guard đã có trong T01 after_result và T03; chỉ kiểm bool, không đo load boundary/actual load/terminal nên một coordinator bug vẫn có thể lọt. Gợi ý xóa vì đã có test mạnh, hoặc sinh viên viết lại oracle/level/inputs có giá trị riêng.

**T18 — nội dung gốc:** Bấm Start hợp lệ; chờ cố định 2 giây. Expected: tìm thấy text “Loading arena...” trong RoomPanel. Priority P2, UI, AC3.

Lý do yếu: RoomPanel bị hủy khi scene change; text ở panel không phải overlay xuyên scene; sleep 2 giây có thể bỏ lỡ operation và flaky; không kiểm guest lifecycle/cleanup. Gợi ý xóa vì T08/T09 mạnh hơn, hoặc sinh viên sửa thành cửa sổ lifecycle có assertion phù hợp.

Giữ nguyên file này làm AI-before. Chỉ tạo bảng reviewed và before/after khi sinh viên gửi quyết định trực tiếp ít nhất 2 ID. Khi đó kiểm lại ≥10 IDs và rerun verifier; nếu sửa T01/factors/constraints phải tái tạo feasible universe/matrix, không chỉ giữ báo cáo cũ.
