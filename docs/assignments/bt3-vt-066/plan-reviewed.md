# VT-066 — plan sau review 01

Sinh viên: Nguyễn Chí Hoàng Tú — 23001947 — SE2026-T17 — GAME-3D-03.
Ngày: 2026-11-14 (Asia/Saigon).
Repository: https://github.com/tasdus-117/SE2026-T17
Baseline cố định: `d939568aae110668df5f0114541e4498593b6108`.
Trạng thái: cập nhật tài liệu theo review; chưa triển khai hoặc chạy test.

Nguồn: [plan gốc](plan-v1.md), [review nguyên văn](review-01-user-verbatim.md).
Review này là review plan; chưa phải review test cases, review diff, cho phép push/PR, reflection hoặc xác nhận cam kết.

## 1. Quyết định review và thay đổi trước/sau

| ID | Trước review (plan-v1) | Sau review của sinh viên | Evidence baseline đã đọc lại | Lý do / kiểm chứng dự kiến |
| --- | --- | --- | --- | --- |
| R1 — sửa | Overlay tồn tại xuyên scene; AC3 còn gộp server chấp nhận, client đang tải, client xong/chờ server; thời điểm bắt đầu/kết thúc chưa đủ chặt. | Overlay chỉ thuộc một thao tác Start và chỉ xuất hiện khi client thực sự bắt đầu chuyển sang Arena_MVP. Snapshot Loading đơn lẻ không bật overlay. Dọn khi lifecycle scene của chính client sẵn sàng, client quan sát lỗi hoặc disconnect. Guest không hoàn tất theo callback server/host. Chỉ hiện mốc như “Đang tải arena…”. | `Assets/Mirror/Core/NetworkManager.cs:828` (server async), `:861` (client change hook), `:865` (host không tải client lần hai), `:889` (guest async), `:1045` (host client complete), `:1076` (guest client complete); `Assets/_Project/Scenes/MainMenu.unity:4708`, `:4825`, `:4838` (canvas và lobby UI); `Assets/_Project/Scripts/Networking/ArenaNetworkManager.cs:174–177` (client complete bridge). | PlayMode phải quan sát overlay còn sống sau khi RoomPanel bị hủy và trước client-ready signal, rồi xác minh cleanup. Callback host/guest kiểm riêng; test không chỉ assert label trên lobby. |
| R2 — sửa | Đề xuất terminal lỗi và kết thúc phiên an toàn; chưa phân tách rõ server load failure với một guest disconnect, chưa định nghĩa thông báo sau cleanup. | Lỗi server phát hiện được ở boundary bắt đầu tải chuyển terminal lỗi tối đa một lần; cùng phiên không retry load. Client đã quan sát lỗi hoặc disconnect phải dọn overlay và đọc được thông báo trên UI còn tồn tại/được phục hồi. Guest disconnect chỉ dọn local guest; server và các thành viên khác tiếp tục lifecycle của họ. Không tự mặc định mọi lỗi đều dừng toàn bộ phiên. | `Assets/_Project/Scripts/Server/Sessions/ServerLobbyController.cs:65–75` (Loading trước load, complete callback); `:57–59` (leave xóa thành viên); `Assets/_Project/Scripts/Networking/ArenaNetworkManager.cs:152–167` (disconnect/stop client), `:285–301` (server disconnect cleanup); `Assets/Mirror/Core/NetworkManager.cs:824–828`, `:868–874` (pause message khi load). | Inject lỗi tại boundary production được gọi thật, gửi retry, assert trạng thái và số load attempts; lặp complete không biến Failed thành Playing. Kiểm thông báo bằng UI có thể đọc được, không chỉ biến string. Kiểm một guest rời không làm fail phiên khác. Không hứa lỗi server luôn được client nhận khi message bị pause. |
| R3 — giữ, bổ sung test | Giữ request rỗng, guard theo room/session. | Giữ `LobbyStartRequest` rỗng; không thêm operation ID protocol. Chứng minh retry cùng phiên chỉ gọi load một lần và phiên mới đủ điều kiện Start được. | `Assets/_Project/Scripts/Networking/LobbyMessages.cs:24`; `Assets/_Project/Scripts/Server/Sessions/LobbyRoom.cs:14`, `:18–19`, `:35–41`. | Có test session reset và completion lặp/cũ trong phạm vi lifecycle thực tế; không dùng numeric connection ID làm identity xuyên phiên. |
| Runner — giữ | Dùng Unity PlayMode runner, cảnh báo Domain runner. | Kiểm license, import, compile và discovery thực tế; chỉ công nhận RED khi test đã discover và fail đúng assertion. | `ProjectSettings/ProjectVersion.txt:1–2`; `tools/Run-DomainTests.ps1:2`, `:9–10`, `:34–40`. | Domain runner chỉ compile Shared/Domain và danh sách test EditMode cố định; không compile/discover PlayMode lobby. Không thay Unity bằng kết quả Domain. |

Các dòng trên là evidence đọc mã, chưa phải evidence PlayMode runtime. Lecture 06 được áp dụng qua spec → plan → generate → verify và review diff theo scope/design/correctness/evidence. Lecture 07 được áp dụng qua risk, pairwise, boundary, mutation, coverage và flaky tests.

## 2. Specification đã cập nhật

### Problem statement

Khi host Start hợp lệ, Mirror tải arena bất đồng bộ; người chơi cần thấy trạng thái local trong đúng khoảng client của họ chuyển scene, kể cả khi lobby UI bị hủy. Retry, lỗi bắt đầu tải và callback lặp phải bảo vệ một thao tác Start duy nhất trong mỗi room/session, giữ quyền quyết định và spawn tại server.

### Acceptance criteria

| AC | Kết quả cần đo |
| --- | --- |
| AC1 — authorization/readiness | Chỉ host trong Waiting, 2–4 thành viên và tất cả Ready được chấp nhận. Guest, unknown sender và host không đủ điều kiện tạo 0 load attempts. |
| AC2 — retry/session | Start được chấp nhận gọi boundary bắt đầu tải đúng 1 lần. Retry sát nhau, trong Loading và sau terminal không tăng số lần gọi trong cùng phiên. Phiên mới reset đúng và Start được khi đủ điều kiện. |
| AC3 — overlay local | Snapshot Loading chưa đủ để hiện overlay. Client bắt đầu chuyển sang Arena_MVP mới hiện trạng thái theo mốc. Overlay sống qua việc RoomPanel bị hủy, dọn khi scene của chính client sẵn sàng, hoặc khi client quan sát lỗi/disconnect. Không hiện phần trăm. Guest dựa lifecycle guest. |
| AC4 — completion/spawn | Chỉ completion arena hợp lệ của thao tác đang Loading chuyển server sang Playing; callback lặp không tạo terminal transition thứ hai; callback sau failure không mở gate spawn. Local client-ready không tự quyết định Playing của server. |
| AC5 — failure/isolation | Lỗi server phát hiện được tại boundary bắt đầu tải chuyển terminal Failed tối đa 1 lần; retry cùng phiên không load lại. Client quan sát lỗi/disconnect dọn overlay và có thông báo ở UI đọc được. Guest disconnect không tự chuyển thao tác server thành Failed và không dọn overlay của client khác. |

### Invariant và contract

- Mỗi room/session chỉ chấp nhận một Start; đã chấp nhận thì tối đa một lần gọi boundary khởi động tải và tối đa một terminal transition (thành công hoặc thất bại).
- Failure trước khi scene loader tạo side effect: có thể có 1 attempt nhưng 0 lần thực sự bắt đầu tải. Ghi riêng attempt và actual load nếu test quan sát được; không đánh đồng hai số đếm.
- Snapshot có thể gửi lại để đồng bộ; không tính snapshot lặp là business notification trùng. `ServerSceneLoaded` là callback hạ tầng. Không thêm business event chỉ để có counter test.
- Guest disconnect là sự kiện local/roster, không phải lỗi load chung của server. Host dừng phiên vẫn theo lifecycle host/server hiện có.

### Non-functional constraints

1. Giữ authority server, capacity 4, readiness/host và gate spawn; Client không tham chiếu Server, Networking không tham chiếu Client/Server, Domain/Protocol không phụ thuộc Unity/Mirror.
2. Dùng Mirror async hiện có; không thêm worker, không block main thread, không sửa vendor Mirror.
3. Overlay thuộc một thao tác; không tồn tại như UI idle của phiên, không nhân đôi listener/UI hoặc mang thông báo cũ sang phiên mới.
4. Tải 100 tác vụ độc lập đồng thời, p95 ≤ 3,0 giây. Nếu harness thì chỉ kết luận về production boundary thực sự chạy và công khai dependency fake.
5. Test chờ tín hiệu/trạng thái với deadline; kiểm timing và shared-state/concurrency, không lấy sleep cố định làm oracle.

## 3. Scope lỗi và giới hạn

Mục tiêu triển khai và kiểm: lỗi đồng bộ/exception hoặc kết quả thất bại tường minh mà bridge thật có thể báo khi yêu cầu bắt đầu tải arena; retry sau lỗi; callback lặp; lỗi/disconnect mà client thực sự quan sát. Seam inject lỗi phải đặt ngay boundary được coordinator production gọi, không viết harness thay thế rule.

Chưa chứng minh: mọi lỗi xảy ra sâu trong native scene loading sau khi đã bắt đầu, tiến trình treo vô hạn, crash/kill process, packet loss trên Internet, reconnect xuyên phiên và host migration. Không tự suy ra các lỗi này từ một injected exception.

Mirror pause xử lý message trong loading: cần kiểm trình tự thực thi và phân phối lỗi. Khi sửa code phải chỉ rõ thời điểm nào gửi/nhận lỗi được, client nào đã chuyển scene; client chưa quan sát lỗi không được báo là đã nhận. Mọi teardown phục vụ recovery phải giữ được thông báo cho màn hình có thể đọc và không biến guest disconnect thành stop toàn bộ server. Không dự kiến tự động retry thao tác đã terminal lỗi.

## 4. Implementation và verification plan

1. Tạo worktree sạch `.worktrees/bt3-vt-066`, nhánh `codex/bt3-vt-066` từ baseline sau khi kiểm ignore và quyền ghi Git/worktree. Giữ nguyên 4 Unity settings đang sửa và các editor đang mở. Đưa các tài liệu evidence hiện tại sang nhánh làm việc; không commit trên main.
2. Ghi baseline/environment, Unity version, license/import/compile/discovery và kết quả PlayMode hai suite lobby. XML phải có test-case thật; exit code riêng. Không coi log cũ là kết quả mới.
3. AI sinh ≥12 cases có ID, AC/risk, level, input, expected, priority. Giữ bản AI trước review. Sinh viên trực tiếp sửa/xóa ≥2 test yếu/trùng; lưu before/after và phản hồi, kiểm lại ≥10 cases.
4. Pairwise: requester {host, guest, unknown}; điều kiện {một người Ready, nhiều người chưa đủ Ready, tất cả Ready}; retry timing {sát nhau, Loading, terminal}. Constraints loại tổ hợp không thể đi tới Loading/terminal khi Start bị từ chối; script lập tập cặp khả thi và xác nhận matrix bao phủ. Bổ sung high-risk cases cho failure, lifecycle guest, boundary capacity, phiên mới và callback lặp. Kiểm lại sau chỉnh test.
5. Viết test hành vi công khai trước patch, chạy filter nhỏ để RED đúng assertion. Test hiện có bảo vệ guard là characterization/regression, không gọi đó là RED tính năng mới. Resolve compile/license/discovery trước khi lấy RED.
6. Patch nhỏ: guard terminal lỗi/complete; bridge lifecycle client và boundary lỗi; overlay Client tồn tại xuyên scene nhưng chỉ trong thao tác; thông báo lỗi sau cleanup; giữ session guard và request rỗng. Trình diff cho sinh viên review trước chấp nhận patch theo scope/design/correctness/evidence.
7. GREEN bằng đúng filter RED rồi regression lobby. Test overlay có cửa sổ quan sát có kiểm soát bằng tín hiệu/fixture nếu cần: sau RoomPanel destroyed, trước client-ready, overlay vẫn tồn tại. Không dùng callback server làm oracle guest-ready. Fixture xử lý singleton Mirror và scene/static teardown.
8. Mutation tạm đúng `>` → `>=` tại capacity guard `ArenaNetworkManager.cs:203`. Kiểm authenticated connection count 3/4/5. Lưu sạch → mutant → khôi phục, diff/commit tạm, lệnh, XML/assertion; nhánh nộp không chứa mutation.
9. Coverage line/branch nếu công cụ sẵn có; ghi scope/limitations và map AC/risk. Lặp ≥20 lượt cho timing và order/shared-state/concurrency, báo số lượt thực sự và failures; không lấy rerun che flaky.
10. Barrier phát 100 thao tác độc lập, retry giữ identity local của thao tác (không thêm protocol ID). Không chạy 100 client trong một room 4 người. Đo từ caller submit đến terminal caller quan sát, không dừng stopwatch ở thời điểm queue/accept. Lưu raw data, operations/requests, errors/timeouts, duplicate effects; p95 nearest-rank. Harness phải chạy rule production và khai báo fake scene/network dependencies; không suy ra p95 native Unity từ harness.
11. Traceability, AI Usage Log, README commands cho test/mutation/load; sinh viên tự viết reflection 250–350 từ. Chỉ push/mở PR sau quyền rõ ràng của sinh viên; không merge. Chỉ đưa cam kết vào trang đầu sau sinh viên xác nhận. PDF đúng tên, ≤8 trang chính, phụ lục đánh dấu rõ, render kiểm font Việt/bảng/link/số trang.

## 5. Files dự kiến

- `Assets/_Project/Scripts/Server/Sessions/LobbyRoom.cs`: state terminal và session guard.
- `Assets/_Project/Scripts/Server/Sessions/ServerLobbyController.cs`: điều phối, load failure, complete guard, giữ guest leave isolation.
- `Assets/_Project/Scripts/Networking/ArenaNetworkManager.cs`: lifecycle client, boundary load/error, cleanup; không thay quyền server.
- `Assets/_Project/Scripts/Networking/LobbyMessages.cs`: chỉ bổ sung error state/data nếu cần truyền lỗi; giữ Start request rỗng.
- `Assets/_Project/Scripts/Client/UI/ArenaLoadingOverlay.cs` mới + `.meta`: UI local qua scene, scope một operation, cleanup.
- `Assets/_Project/Scripts/Client/UI/LobbyRoomController.cs`, `LobbyRoomView.cs`: lỗi đọc được trên màn hình còn tồn tại/phục hồi và tránh label sai lifecycle.
- `Assets/_Project/Tests/PlayMode/LobbyStartProgressTests.cs` mới + `.meta`; tái sử dụng fixture trong hai suite lobby, sửa fixture chỉ khi cần.
- `tools/Verify-Bt3Pairwise.py` mới; harness/load runner mới nếu cần và có tên/lệnh cụ thể khi triển khai.
- `docs/assignments/bt3-vt-066/`: prompts, plans, review, cases, traceability, README, evidence manifests/raw data phù hợp, reflection và PDF.

Không dự kiến sửa scenes/prefabs, asmdef, Packages, Unity settings hoặc vendor Mirror. Nếu seam runtime/harness yêu cầu thêm file/đổi scope phải giải thích trong diff; không dựa Domain runner để chứng minh lobby.

## 6. Lệnh dự kiến và evidence

Chạy trong worktree, chưa chạy ở lần cập nhật plan:

```powershell
$unity = 'C:/Program Files/Unity/Hub/Editor/6000.6.8f1/Editor/Unity.exe'
& $unity -batchmode -nographics -projectPath $PWD.Path `
  -runTests -testPlatform PlayMode `
  -testFilter 'SmallArena.Tests.PlayMode.LobbyRoomManagementTests;SmallArena.Tests.PlayMode.LobbyGovernanceAndScreenTests' `
  -testResults "$PWD/TestResults/BT3/baseline.xml" `
  -logFile "$PWD/Logs/BT3-baseline.log"
```

RED/GREEN dự kiến filter `SmallArena.Tests.PlayMode.LobbyStartProgressTests` hoặc từng test khi cần; giữ cùng filter giữa hai lần, lưu command đầy đủ. Regression giữ hai suites trên. Mỗi lần ghi timestamp, commit/source diff, Unity version, exit code, test IDs/assertions, XML/log path. Mutant phải chạy cùng filter phù hợp ở sạch/mutant/khôi phục.

## 7. Trạng thái bằng chứng

- Đã đọc lại source locations ở bảng R1/R2/Runner; chưa có PlayMode chứng minh overlay, error UI hoặc retry load count.
- Baseline hash và remote main đã kiểm ở khảo sát đầu tiên; lần cập nhật này chưa fetch/chuyển nhánh.
- Đã lưu review nguyên văn và giữ plan-v1. R1/R2 là hai quyết định sửa plan thực sự của sinh viên; R3 là quyết định giữ.
- Chưa có test-case review của sinh viên, RED/GREEN, coverage, mutation, flaky/load results, reflection, xác nhận cam kết hoặc GitHub branch/PR mới.
