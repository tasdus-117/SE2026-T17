# VT-067 — đặc tả và quyết định trước khi viết bản vá

Ngày: 14/11/2026. Trần Xuân Việt — 23001955 — SE2026-T17 — GAME-3D-03.
Baseline: `d939568aae110668df5f0114541e4498593b6108`.
Nhánh: `xzviett/personal-assign-3`. Repo: https://github.com/tasdus-117/SE2026-T17.

## Kiểm toán baseline

HEAD bằng ref `origin/main` đang lưu cục bộ; local `main` còn cũ ở `adedfa1`.
Không fetch/merge hoặc tuyên bố đã kiểm tra trạng thái GitHub mới hơn ref này.
PR #10 chứa M1/M2 của TV1; #7 (`46e707c`) thêm vật cản arena và assertion layout;
#9 (`d939568`) thêm GameplayConfiguration, OnlineCharacterState, OnlineMatch và
test Domain; #8 (`695f584`) thêm Unity CI Windows. Vì vậy có hai PR tính năng
TV2/TV3 và một PR CI, không gộp ba thay đổi thành hai PR.
Domain runner baseline vừa chạy: **141 đạt, 0 lỗi, 0 skip**; con số 34 là lịch sử
trước khi merge TV2. PlayMode 279/277/0/2 trong context là lịch sử trước merge,
không tự coi đó là kết quả native sau merge.

## Hai lựa chọn

| | A — timestamp và ghi rời rạc | B — revision và transaction theo khóa |
|---|---|---|
| Freshness | DateTime.UtcNow, last-write-wins | long Revision > revision đã commit |
| Đồng thời | Check rồi await/write có cửa sổ race | Khóa transaction bao trọn read/check/stage/publish |
| Persistence | PlayerPrefs hoặc ghi đè trực tiếp | Ghi file tạm, flush, replace nguyên tử; đọc lại khi mở transaction |
| Test | Phụ thuộc đồng hồ/timing và Unity | Domain engine-free; Task/TCS điều khiển thứ tự; public contract |
| Phù hợp repo | PlayerPrefs thuộc Unity; không phải storage server | EditMode chỉ tham chiếu Domain; Server đã quản lý Publish lobby |

**Chọn B:** lõi AutoSaveDraftService/DTO/transaction contract trong
SmallArena.Shared.Domain, không Unity/Mirror; adapter capture và atomic file
storage ở Server/Sessions. UI/Networking không có quyền phục hồi Ready từ draft.
Không thêm asmdef thứ tám. Transaction in-memory dùng semaphore theo room key;
file dùng lease độc quyền theo khóa và replace cùng thư mục, bao gồm nhiều
service/backend instance dùng chung thư mục. Capture của Unity/Mirror diễn ra
trên main thread; chỉ DTO bất biến và System.IO chạy trên worker.

### Sửa kế hoạch AI ngây thơ bằng bằng chứng

1. Không tạo Core/Gameplay/Client.UI asmdef theo ví dụ đề: `rg --files -g *.asmdef`
   chứng minh bảy assembly là Shared.Domain, Shared.Protocol, Networking, Server,
   Client và hai test assembly. EditMode asmdef chỉ reference Domain.
2. Không ghi PlayerPrefs/đọc Application.persistentDataPath trong 40 worker:
   ServerLobbyController đang giữ state Mirror/main thread. Chụp DTO và đường dẫn
   trước khi chạy background I/O; tránh gọi Unity API từ service engine-free.
3. Không dùng clock làm version hoặc Sleep để ép thứ tự: nhiều burst cùng clock
   resolution/clock thay đổi không biểu diễn thứ tự chỉnh sửa. TCS/barrier quyết
   định save cũ hoàn tất sau save mới; revision là hợp đồng, Stopwatch chỉ đo latency.
4. Không mở room 40 player: LobbyRoom.Capacity=4 và host/all-ready guard M2 phải
   giữ nguyên. Load là 40 task lưu tranh chấp, không đổi sức chứa phòng.
5. Không coi Domain config TV2 100 HP/25 damage là tuning runtime melee 10/1:
   GameplayConfiguration và NetworkCombat là hai slice hiện chưa tích hợp. Không
   chỉnh combat để thực hiện bài autosave; không coi bản nháp là nguồn authority.

## Đặc tả

**Vấn đề:** sau mỗi thay đổi hợp lệ của roster/Ready/pha lobby, server tự lưu
snapshot nháp bất biến. Completion đảo thứ tự, revision trùng, payload sai hoặc
lỗi ghi file không được làm mất bản đã commit mới hơn.

- **AC1:** sau thay đổi lobby, capture tự động tạo revision tăng theo session và
  lưu DTO; Read trả đúng snapshot, không cần người dùng nhấn Save.
- **AC2:** theo từng RoomId, chỉ bản hợp lệ với revision lớn hơn được commit;
  bản cũ/trùng bị từ chối, không đổi payload đã lưu. Revision trùng first-commit
  wins; retry cùng revision không ghi lại (không hứa winner theo scheduler).
- **AC3:** null/empty payload, schema không hỗ trợ, revision ≤0, player/slot sai
  bị từ chối; lỗi I/O/cancel trước publish giữ nguyên snapshot trước, cho retry.
- **AC4:** 40 task cạnh tranh cùng khóa vẫn kết thúc ở revision hợp lệ lớn nhất;
  file có thể mở lại, nhiều backend instance không phá monotonicity. Draft không
  tự khôi phục quyền host, ownership hoặc Ready của connection đang sống.
- **NFC1:** concurrency safety; 40 task đồng thời, p95 ≤1.500 ms. Đo p50/p95/p99/max
  bằng Stopwatch, nearest-rank; ghi rõ in-memory hay disk, không suy thành 40 client.
- **NFC2:** bảy asmdef không vòng; Domain không Unity/Mirror; test điều khiển lịch
  bằng tín hiệu, không assert field private hoặc winner theo thứ tự ThreadPool.

**Invariant:** commit được phép iff incoming.IsValid() và
incoming.Revision > current.Revision (chưa có current xem revision=0).
Validation/read/compare/write/publish nằm trong cùng transaction theo RoomId.
Ngoại lệ I/O không phải commit; atomic storage không được công bố partial write.
RoomId phân biệt hoa/thường, tối đa 64 ký tự; schema=1; 1–4 member bất biến,
ID player không trắng/trùng, slot 0–3 không trùng, tối đa một host; phase hợp lệ.
Mỗi lần Open dùng RoomId mới; revision thuộc stream đó, không dùng số connection
hoặc thời gian làm revision. Lobby rỗng không tạo một draft hợp lệ mới.

## Cam kết kiểm thử trước bản vá

Tạo DTO/contract tối thiểu và service chưa có validation/freshness guard, chạy
NUnit thật để lưu Red XML/log trước khi thêm hai guard. Sau Green: bỏ đúng
stale guard trong bản sao compile, chạy cùng test không sửa assertion, ghi
mutant kill và khôi phục bản sao bằng hash. Production không giữ mutant.
Test quan sát Save result, Read payload/revision, file sau reopen và lỗi công khai;
không reflection/kiểm tra semaphore private.

Phạm vi E2E: integration lobby Publish → autosave và file round-trip/failure;
không cần bấm camera/đấm bot để chứng minh atomicity, nhưng full PlayMode vẫn
regression các tính năng nhóm. Báo cáo cuối bổ sung bảng TC, log và metrics thật.
