# Câu 1 — Specification và AI-assisted plan (15 điểm)

Nguyễn Chí Hoàng Tú — MSSV 23001947 — nhóm trưởng SE2026-T17. GAME-3D-03 — Small Arena: multiplayer 3D authoritative server. Biến thể VT-066. Repository: https://github.com/tasdus-117/SE2026-T17.

Đây là bản Câu 1 dùng làm nguồn cho PDF; evidence chi tiết và plan gốc sẽ đưa vào phụ lục. Trạng thái ngày 2026-11-14: đã khảo sát mã và cập nhật tài liệu, chưa tạo nhánh triển khai, sửa production code hoặc chạy test trong lượt này.

## 1. Specification

**Problem statement.** Khi host Start Match hợp lệ, người chơi cần thấy trạng thái tiến trình trong lúc client của mình tải `Arena_MVP` bất đồng bộ, kể cả khi UI lobby bị hủy. Retry và callback lặp phải giữ duy nhất một thao tác Start trong mỗi room/session; lỗi khởi động tải phải kết thúc an toàn, không tải lại trong cùng phiên và không làm client tự quyết định trạng thái trận.

| AC | Điều kiện đo được |
|---|---|
| AC1 — quyền và readiness | Chỉ host trong `Waiting`, có 2–4 thành viên và tất cả Ready được chấp nhận. Guest, unknown sender hoặc host chưa đủ điều kiện gây **0 lần gọi tải**. |
| AC2 — retry và phiên mới | Một Start được chấp nhận gọi boundary tải **đúng 1 lần**. Retry sát nhau, khi Loading và sau terminal không tăng bộ đếm trong cùng phiên. Phiên mới được reset theo lifecycle, đủ điều kiện thì Start được và có đúng 1 lần gọi riêng. |
| AC3 — tiến trình local | Overlay xuất hiện khi client bắt đầu chuyển sang `Arena_MVP`; snapshot Loading riêng lẻ chưa bật overlay. Overlay còn hiện sau khi RoomPanel bị hủy và trước mốc client-ready; được dọn khi chính client sẵn sàng, khi quan sát failure hoặc disconnect. Guest dùng lifecycle của guest. Slice này hiển thị mốc, chưa hiển thị phần trăm. |
| AC4 — completion và spawn | Chỉ callback arena hợp lệ khi thao tác server còn Loading chuyển sang Playing. Callback lặp không tạo terminal transition thứ hai; callback sau failure không chuyển sang Playing. Spawn bị chặn trước Playing; client-ready không tự mở gate server. |
| AC5 — failure và cô lập | Lỗi phát hiện được ở boundary khởi động tải tạo tối đa 1 terminal failure; retry cùng phiên không gọi tải lại. Phiên kết thúc an toàn theo lifecycle hiện có. Client dọn overlay, đọc được lỗi hoặc thông báo mất kết nối trên UI tồn tại/phục hồi. Một guest disconnect chỉ kết thúc overlay guest đó; các thành viên khác tiếp tục lifecycle server. |

**Invariant retry.** Với mỗi room/session: số lần chấp nhận Start ≤ 1, số lần gọi boundary khởi động tải ≤ 1, số terminal transition nghiệp vụ (thành công hoặc thất bại) ≤ 1. Start hợp lệ đi tới boundary có đúng 1 attempt; lỗi trước khi loader tạo side effect có thể tương ứng 0 lần thực sự tải. Đếm riêng attempt và actual load nếu quan sát được. Sau teardown vẫn không cho request/callback cũ mở lại thao tác của phiên đã đóng.

`LobbyStartRequest` tiếp tục rỗng, identity nằm ở room/session đang hoạt động. Snapshot được gửi lại theo contract hiện có; snapshot trùng không phải business notification trùng. Baseline chưa có event nghiệp vụ `MatchStartCompleted`; `ServerSceneLoaded` là callback hạ tầng. Không thêm event chỉ để đếm test; nếu patch có event terminal nghiệp vụ thì phải kiểm số lần phát ≤ 1.

**Non-functional constraints.**

1. Giữ server authoritative, host/readiness, capacity 4 và gate spawn. Giữ chiều tham chiếu assembly: Client → Networking ← Server; Domain/Protocol không thêm Unity/Mirror dependency.
2. Tái sử dụng async load của Mirror; không sửa vendor Mirror, thêm worker để tải scene hoặc chặn main thread bằng chờ đồng bộ.
3. Overlay chỉ thuộc một thao tác Start; cleanup listener/UI khi hoàn tất, lỗi hoặc disconnect và không mang trạng thái cũ sang phiên mới.
4. Kiểm 100 tác vụ độc lập đồng thời, mục tiêu p95 ≤ 3,0 giây từ submit đến terminal caller quan sát. Đây là mục tiêu chưa đo; nếu dùng harness chỉ kết luận trong boundary thật được chạy, công khai dependency giả lập.
5. Test chờ lifecycle/state có deadline, kiểm timing và shared state; không dùng sleep cố định làm oracle hoàn tất.

## 2. Baseline và bảo toàn checkout

Baseline cố định: **`d939568aae110668df5f0114541e4498593b6108`** — merge PR #9, `feat(tv2): add online character state and immutable match rules (TV2-002)`.

| Kiểm tra thực tế | Kết quả |
|---|---|
| `git branch --show-current` | `main` |
| `git rev-parse HEAD`, `git rev-parse origin/main` | Cùng full hash baseline |
| `git remote -v` | Fetch/push: `https://github.com/tasdus-117/SE2026-T17` |
| `git ls-remote origin refs/heads/main` | GitHub trả cùng full hash; lần sandbox đầu lỗi network, lần được cấp quyền thành công, exit 0 |
| `git worktree list` | Hiện có một worktree trên main |
| Source/test liên quan so với baseline | `git diff --name-only` cho Scripts, Tests, Mirror NetworkManager, Domain runner và ProjectVersion không có thay đổi |
| Editor | Project yêu cầu `6000.6.8f1 (237b6593792b)`; executable tương ứng đã cài và version khớp |

Chọn hash này vì HEAD, remote-tracking và main thực tế trên GitHub khớp nhau, có đầy đủ slice lobby và regression tests cần dùng. Đây là baseline có thể tái lập; bốn thay đổi settings local không thuộc baseline.

Giữ nguyên các file đang sửa: `Assets/_Project/Settings/UniversalRenderPipelineGlobalSettings.asset`, `ProjectSettings/ProjectAuditorSettings.asset`, `ProjectSettings/ProjectSettings.asset`, `ProjectSettings/ShaderGraphSettings.asset`; giữ cả thư mục tài liệu chưa tracked `docs/assignments/`. Khi bắt đầu triển khai sẽ tạo worktree sạch `.worktrees/bt3-vt-066`, nhánh `codex/bt3-vt-066` từ full hash trên, kiểm ignore và status trước khi copy evidence. Không stash/reset/checkout đè thay đổi hiện có và không commit lên main. Chưa thực hiện bước tạo nhánh ở Câu 1.

Evidence: `evidence/question1/repository-state.json`, `remote-main.txt`, `baseline-numbered-source.txt`. Script `capture-question1-evidence.py` lấy nội dung bằng `git show <baseline>:<path>`; số dòng dưới đây là dòng của baseline, không lấy từ diff patch tương lai.

## 3. Prompt, assumptions và call path

**Prompt và provenance.** Giữ nguyên output plan trước review tại `plan-v1.md`, review cũ tại `review-01-user-verbatim.md`, bản sau review cũ tại `plan-reviewed.md`. R1–R3 trong yêu cầu tiếp tục được chép nguyên văn vào `review-02-user-verbatim.md`. Bản ghi hiện có không chứa nguyên văn prompt lịch sử đã tạo `plan-v1`; không dựng lại rồi gọi là nguyên văn. Prompt kế hoạch đang áp dụng trong lượt này là yêu cầu thực tế của sinh viên, gồm đoạn:

> Hoàn thiện Câu 1 thành một mục sẵn đưa vào PDF:

Các yêu cầu trực tiếp đi kèm: 01 problem statement, 3–5 AC đo được, ≥2 constraints, invariant retry; xác minh Git/baseline; prompt/assumptions/call path/file; bảng review; plan sau review với test commands, giới hạn và giữ authority/host/readiness/capacity/spawn. R1–R3 là ràng buộc đầu vào của bản này, chưa phải review diff hay review test cases.

**Assumptions có evidence.** Mỗi phiên chỉ Start một lần (`LobbyRoom.cs:14,35–41`); request không có payload (`LobbyMessages.cs:24`); teardown và tạo phiên mới là ranh giới reset, không dùng connection ID làm identity xuyên phiên. Mirror đã async, không cần hệ thống job/queue mới. Host dùng scene load server chung; guest tải riêng. Chưa có reconnect token, host migration hoặc tự retry sau failure trong phạm vi này.

Trong bảng sau, đường dẫn tương đối thuộc `Assets/_Project/Scripts/`, trừ Mirror và Scenes được ghi riêng.

| Bước | Evidence tại baseline và ý nghĩa |
|---|---|
| UI → request | `Client/UI/LobbyRoomController.cs:27,79` nối StartButton → StartMatch; `Networking/ArenaNetworkManager.cs:93–95` gửi `LobbyStartRequest`. |
| Message → server rule | `Server/Sessions/ServerLobbyController.cs:42–43,65–69`; `LobbyRoom.cs:14,35–39` kiểm host/readiness và đổi Waiting → Loading trước gọi tải. |
| Server → Mirror async | `Networking/ArenaNetworkManager.cs:98–103` → `ServerChangeScene`; `Assets/Mirror/Core/NetworkManager.cs:818–838` đặt not-ready, hook server, pause message, `LoadSceneAsync` tại dòng 828 rồi gửi SceneMessage. |
| Guest/host scene hook | Mirror `NetworkManager.cs:861–889`: hook client trước tải; host thoát nhánh tải client tại 865, guest tải async tại 889. Phải kiểm thứ tự hook host thực tế khi triển khai, không giả định host tải hai lần. |
| Server complete | Mirror `NetworkManager.cs:1042` → `Networking/ArenaNetworkManager.cs:169–172` → `ServerLobbyController.cs:71–75` → `LobbyRoom.cs:41` → snapshot Playing. |
| Client complete → UI | Mirror gọi `OnClientSceneChanged` cho host tại 1045, guest tại 1076; override bridge tại `ArenaNetworkManager.cs:174–177`. Baseline chưa nối lifecycle này sang overlay bền qua scene; đó là patch dự kiến. |
| UI hiện tại và spawn | `Assets/_Project/Scenes/MainMenu.unity:2974,4708,4825,4838`: RoomPanel/canvas và lobby UI thuộc MainMenu. `Client/UI/LobbyRoomView.cs:90` chỉ có label Loading. `ServerLobbyController.cs:37`, `ArenaNetworkManager.cs:228–232` giữ gate spawn. |

Test baseline hỗ trợ khảo sát: `Assets/_Project/Tests/PlayMode/LobbyRoomManagementTests.cs:165–189` kiểm host Start → arena/spawn; `:281–295` khóa Start lặp ở room rules nhưng chưa đếm loader; `:117–128` kiểm capacity; `:192–218` kiểm spawn gate và phiên mới. `LobbyGovernanceAndScreenTests.cs:43–65` kiểm host/readiness; `:151–169` liên quan điều hướng scene. Đây là evidence đọc test, chưa phải kết quả chạy mới.

## 4. Review của sinh viên và quyết định

| ID | Đề xuất gốc | Evidence file:dòng | Quyết định của sinh viên và plan sau review |
|---|---|---|---|
| R1 — **sửa** | Overlay xuyên scene nhưng mốc server accept/client load/client complete còn gộp; thời điểm bật/tắt chưa chặt. | Mirror `NetworkManager.cs:828,861,865,889,1045,1076`; `MainMenu.unity:4708,4825,4838`. | Giới hạn trong một Start; bật khi chính client bắt đầu chuyển arena, không bật chỉ vì snapshot Loading. Guest hoàn tất theo guest. PlayMode phải thấy overlay sau RoomPanel bị hủy rồi thấy cleanup đúng lifecycle/failure/disconnect. Chọn mốc trạng thái, chưa dùng phần trăm. |
| R2 — **sửa** | Terminal lỗi và kết thúc phiên chưa phân biệt rõ guest disconnect, lỗi có thể phát hiện và UI thông báo sau cleanup. | `ServerLobbyController.cs:57–59,65–75`; `ArenaNetworkManager.cs:152–167,285–301`; `Networking/MatchSessionNavigation.cs:14–55`. | Failure ở boundary server terminal tối đa một lần, không load lại trong phiên; teardown an toàn, giữ thông báo trên UI đọc được. Guest disconnect chỉ kết thúc local guest; không fail toàn server. Kiểm cả terminal state và load attempts, callback sau failure và phiên mới. |
| R3 — **giữ** | Guard theo room/session, request Start rỗng, không thêm operation ID protocol. | `LobbyMessages.cs:24`; `LobbyRoom.cs:14,18–19,35–41`; snapshot tại `ServerLobbyController.cs:77–83`. | Giữ contract; thêm kiểm số lần tải cùng phiên và Start của phiên mới. Cho phép gửi lại snapshot theo contract; không cấm snapshot lặp để đạt chỉ số chống trùng. |

R1 và R2 là hai thay đổi plan do sinh viên quyết định. R3 là quyết định giữ, không tính là thay đổi. Chưa tính review plan này thay cho yêu cầu sinh viên trực tiếp sửa/xóa ≥2 test AI sinh ở Câu 2.

## 5. Plan sau review, file và verification

**Phạm vi patch nhỏ.** Bổ sung guard terminal success/failure, báo lifecycle local qua Networking và overlay trong Client; tận dụng async Mirror và navigation hiện có. Không mở rộng sang job service, database, REST API, reconnect hoặc host migration.

| File dự kiến | Lý do |
|---|---|
| `Assets/_Project/Scripts/Server/Sessions/LobbyRoom.cs` | Guard hoàn tất/failure theo phiên, không mở lại Start đã terminal. |
| `Assets/_Project/Scripts/Server/Sessions/ServerLobbyController.cs` | Bắt lỗi boundary gọi tải, terminal đúng một lần và giữ guest isolation. |
| `Assets/_Project/Scripts/Networking/ArenaNetworkManager.cs` | Lifecycle client, boundary tải/error và cleanup, giữ authority/gate spawn. |
| `Assets/_Project/Scripts/Networking/LobbyMessages.cs` (nếu cần) | Chỉ thêm trạng thái/data lỗi cần thiết; không đổi payload Start. Baseline chưa có Failed trong enum. |
| `Assets/_Project/Scripts/Client/UI/ArenaLoadingOverlay.cs` mới + `.meta` | Overlay độc lập RoomPanel, tồn tại xuyên scene đúng vòng đời thao tác. Owner/listener phải sống qua việc lobby bị hủy. |
| `Assets/_Project/Scripts/Client/UI/LobbyRoomController.cs`, `LobbyRoomView.cs` | Nối UI và khôi phục thông báo lỗi đọc được. |
| `Assets/_Project/Scripts/Networking/MatchSessionNavigation.cs` (chỉ nếu cần) | Tái sử dụng teardown; nếu thông báo bị xóa cùng manager, cần giữ lý do đến màn hình MainMenu mà không tạo phụ thuộc Networking → Client. |
| `Assets/_Project/Tests/PlayMode/LobbyStartProgressTests.cs` mới + `.meta`; fixture lobby nếu cần | Test-first retry/failure/UI lifecycle; đếm side effect tại boundary production. |
| `tools/Verify-Bt3Pairwise.py`, công cụ load khi cần; `docs/assignments/bt3-vt-066/` | Kiểm pairwise, dữ liệu tải và traceability/evidence/README/PDF. |

Không dự kiến đổi scenes/prefabs, Packages, asmdef, settings hoặc vendor Mirror. Nếu seam test cần mở rộng file, giải thích cụ thể trong diff; không đặt rule thay thế trong harness rồi dùng nó làm bằng chứng production.

**Trình tự và checkpoint.**

1. Tạo worktree sạch từ baseline, lưu status/environment; chạy baseline hai suite lobby bằng Unity đúng version, lưu command, SHA, exit code, XML/log và test IDs đã discover.
2. Câu 2: AI sinh ≥12 cases để sau khi sinh viên trực tiếp sửa/xóa ≥2 case vẫn còn ≥10. Lưu bản gốc, before/after và lý do. Chọn factors/constraints từ repo; script kiểm mọi cặp mức khả thi, bổ sung riêng retry sát nhau, failure và đúng biên.
3. Viết test hành vi trước patch, chạy RED bằng assertion đúng lý do. Compile/license/import/discovery error không phải RED. Đếm load attempt và terminal transition; test guest-ready riêng với server callback, không dùng guest giả lập chỉ nhận message để kết luận guest UI tải scene thật.
4. Tạo patch nhỏ, trình diff để sinh viên review scope, design, correctness (retry/failure/session), evidence/assertions trước chấp nhận. Giữ authority, host/readiness, capacity và gate spawn.
5. Chạy đúng filter RED để lấy GREEN, rồi regression. Overlay cần cửa sổ quan sát sau khi RoomPanel bị hủy, trước client-ready; dùng tín hiệu điều khiển/deadline, giữ lifecycle thật.
6. Câu 4: mutation đúng `>` → `>=` tại `ArenaNetworkManager.cs:203`, authenticated count 3/4/5. Bản sạch cho phép 3/4, từ chối 5; mutant từ chối cả 4. Chạy cùng test sạch → mutant → khôi phục, lưu diff/SHA/assertion, xác minh nhánh nộp không còn mutant. Thu coverage nếu hỗ trợ, map AC/risk; lặp ít nhất 20 lượt cho timing và order/shared-state/concurrency, báo số lượt và failures thực tế.
7. Khởi phát 100 thao tác độc lập bằng barrier; retry cùng identity thao tác, đếm riêng requests. Không đặt 100 người vào room 4 người. Đo submit → terminal caller quan sát; lưu raw data, lỗi/timeout, side effect trùng và p95 nearest-rank (mẫu thứ 95/100 sau sắp xếp).
8. Câu 5: traceability, AI Usage Log, README tái lập; gửi kết quả để sinh viên tự viết reflection 250–350 từ. Chỉ push/mở PR sau review và cho phép của sinh viên; xác minh link repo chính thức, không merge. PDF thực tế `BT3_23001947_VT-066.pdf`, ≤8 trang chính, phụ lục đánh dấu; chỉ chèn cam kết khi sinh viên xác nhận và render kiểm font tiếng Việt/bảng/link/phân trang.

**Lệnh dự kiến, chưa chạy.** Chạy PowerShell trong worktree sạch; tạo thư mục output trước. Chờ Unity process kết thúc rồi lấy exit code, không suy kết quả chỉ từ việc process được khởi chạy.

```powershell
$bt3Unity = 'C:/Program Files/Unity/Hub/Editor/6000.6.8f1/Editor/Unity.exe'
$bt3Project = (Get-Location).Path
New-Item -ItemType Directory -Force -Path 'TestResults/BT3','Logs' | Out-Null
$bt3Filter = 'SmallArena.Tests.PlayMode.LobbyRoomManagementTests;SmallArena.Tests.PlayMode.LobbyGovernanceAndScreenTests'
$bt3Args = @('-batchmode','-nographics',
  '-projectPath', ('"' + $bt3Project + '"'),
  '-runTests','-testPlatform','PlayMode','-testFilter',$bt3Filter,
  '-testResults', ('"' + "$bt3Project/TestResults/BT3/baseline.xml" + '"'),
  '-logFile', ('"' + "$bt3Project/Logs/BT3-baseline.log" + '"'))
$bt3Run = Start-Process -FilePath $bt3Unity -ArgumentList $bt3Args -WindowStyle Hidden -Wait -PassThru
$bt3Run.ExitCode
[xml]$bt3Xml = Get-Content -Raw 'TestResults/BT3/baseline.xml'
$bt3Cases = @($bt3Xml.SelectNodes('//test-case'))
if ($bt3Cases.Count -eq 0) { throw 'Không discover test-case; run không hợp lệ.' }
$bt3Xml.'test-run' | Select-Object result,total,passed,failed,skipped
```

RED/GREEN dùng cùng filter `SmallArena.Tests.PlayMode.LobbyStartProgressTests`, đổi output thành `red.xml`/`green.xml` và log tương ứng, luôn lưu command/filter thực tế. Kiểm XML có đúng test IDs mong đợi và failure assertion; không chỉ kiểm count >0. Regression chạy lại hai suite trên. Chưa tạo tên/lệnh harness giả định như thể đã tồn tại; khi có runner thật sẽ ghi lệnh tái lập vào README.

**Vì sao không dùng Domain runner cho lobby.** `tools/Run-DomainTests.ps1:2` mặc định Editor `6000.6.7f1`, khác project; `:9–10,32–40` chỉ compile Shared/Domain và danh sách cố định `AssemblyInfo`, `MatchStateTests`, `GameplayConfigurationTests`, `OnlineStateTests` trong EditMode. Script không import project Unity, không compile/discover lobby PlayMode hay chạy scene/network/UI. Đổi EditorRoot không thay đổi phạm vi đó.

**Kiểm môi trường khi chạy.** Editor version/executable đã khớp. License chưa kiểm bằng run mới; phải xem log licensing/activation, không suy từ Editor đã cài. Worktree mới cần import/resolve packages/compile thành công; lưu lỗi import/compiler và asmdef nếu có. XML phải chứa các suite/case dự kiến; thiếu XML/0 case/sai filter/compile error là run không hợp lệ. Không mở cùng project bằng hai Editor; giữ nguyên Editor ở checkout cũ. Chưa có kết luận baseline pass, RED/GREEN, coverage, mutation hoặc p95 tại thời điểm Câu 1.

**Giới hạn failure.** Mục tiêu kiểm là lỗi đồng bộ/exception từ boundary thật khởi động tải, hoặc preflight thất bại tường minh nếu bridge bổ sung nó. Mirror có nhánh chỉ log rồi return (`NetworkManager.cs:798–814`); try/catch đơn thuần không phát hiện những nhánh này. Khi code phải xác định tín hiệu failure thực sự và đưa vào test, không hứa bắt mọi lỗi native. Mirror pause message lúc loading (`:824–826,868–874`), nên không bảo đảm client luôn nhận thông điệp lỗi trước teardown; thông báo disconnect đọc được là fallback. Chưa chứng minh native load bị treo sau khởi động, crash, Internet packet loss hoặc reconnect. Navigation hiện có còn chờ `loadingSceneAsync` kết thúc (`MatchSessionNavigation.cs:49`); không coi nó là recovery cho load treo vô hạn.

**Áp dụng bài giảng.** Lecture 06 slides 9, 14, 17, 21: spec → plan → generate → verify, checkpoint sinh viên, review diff bốn lớp và branch/evidence. Lecture 07 slides 7, 16, 18, 21, 23: ưu tiên risk retry gây trùng, pairwise có constraints, coverage trong ngữ cảnh, kiểm flaky và mutation. Đã đọc đề VT-066 và trích text trực tiếp từ hai PPTX; `evidence/question1/lecture-text.json` lưu slide và hash. Đây là kế hoạch kiểm chứng, không thay thế evidence chạy ở Câu 3–4.
