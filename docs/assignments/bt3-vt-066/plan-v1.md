<!-- Bản lưu nguyên nội dung phản hồi plan đầu tiên của assistant, trước review 01. Không cập nhật bản này theo review; xem plan-reviewed.md. -->

Baseline đề xuất là **`d939568aae110668df5f0114541e4498593b6108`**, trên `main` của repo chính thức. Tôi đã khảo sát đề VT-066, hai bài giảng và các file bạn chỉ định. **Chưa sửa file, tạo nhánh, chuyển checkout hay chạy test.** Dưới đây là spec và plan ban đầu để bạn review.

**1. Baseline và môi trường thực tế**

| Kiểm tra | Kết quả |
|---|---|
| Nhánh checkout | `main`, khác thông tin ban đầu `codex/unity-ci` |
| HEAD | `d939568aae110668df5f0114541e4498593b6108` |
| `origin/main` cục bộ | Cùng hash trên |
| `git ls-remote origin refs/heads/main` | GitHub xác nhận cùng hash trên |
| Remote | `https://github.com/tasdus-117/SE2026-T17` |
| Worktree hiện có | Một worktree, trên `main` |
| Unity của project | `6000.6.8f1`, revision `237b6593792b` |
| Unity đã cài | Có `6000.6.8f1`; có tiến trình Unity đang chạy |
| Hướng dẫn agent | Không tìm thấy `AGENTS.md` trong repo và các thư mục cha đã kiểm tra |

Bốn file đang được Git báo thay đổi:

- `Assets/_Project/Settings/UniversalRenderPipelineGlobalSettings.asset`
- `ProjectSettings/ProjectAuditorSettings.asset`
- `ProjectSettings/ProjectSettings.asset`
- `ProjectSettings/ShaderGraphSettings.asset`

Tôi chọn baseline này vì **HEAD, remote-tracking ref và main thực tế trên GitHub khớp nhau**, đồng thời chứa đầy đủ luồng lobby cần kiểm chứng.

Sau review, tôi sẽ tạo worktree sạch trong thư mục con của workspace, dự kiến `.worktrees/bt3-vt-066`, với nhánh `codex/bt3-vt-066`. Tôi sẽ giữ nguyên checkout hiện tại và các thay đổi settings, kiểm tra cấu hình ignore trước khi tạo worktree. Không dừng các editor đang mở.

**2. Luồng hiện có và khoảng trống**

Đường đi đã lần theo:

`StartButton` → `LobbyRoomController.StartMatch()` → `ArenaNetworkManager.RequestRoomStart()` → `LobbyStartRequest` → `ServerLobbyController.StartMatch()` → `LobbyRoom.TryStart()` → snapshot `Loading` → `LoadRoomArena()` → Mirror `ServerChangeScene()` → `SceneManager.LoadSceneAsync()` → `OnServerSceneChanged()` → `FinishLoading()` → snapshot `Playing`.

Các bằng chứng chính:

| Hiện trạng | Evidence |
|---|---|
| Nút Start gửi request qua networking bridge | `LobbyRoomController.cs:79`, `ArenaNetworkManager.cs:93–95` |
| Server kiểm tra host, ít nhất hai người, mọi người Ready | `LobbyRoom.cs:14`, `35–39` |
| Start được chấp nhận chuyển ngay sang `Loading`; Start tiếp theo bị guard chặn | `LobbyRoom.cs:14`, `35–39` |
| Chỉ request được chấp nhận mới gọi tải arena | `ServerLobbyController.cs:65–70` |
| Mirror đã tải bất đồng bộ | `Assets/Mirror/Core/NetworkManager.cs:824–838` |
| UI đã có “Loading arena...” | `LobbyRoomView.cs:90` |
| Callback server tải xong đổi sang `Playing` | `ServerLobbyController.cs:71–75` |
| Gate spawn chỉ mở khi room `Playing` | `ServerLobbyController.cs:37`, `ArenaNetworkManager.cs:228–232` |

**Khoảng trống cần patch/test:**

- Chưa có tiến trình phân biệt server chấp nhận Start, client bắt đầu tải scene và client tải xong.
- `LobbyRoomController` và `LobbyRoomView` nằm trên `MainMenuCanvas` (`MainMenu.unity:4708`, `4825`, `4838`). Không thấy cơ chế giữ lobby canvas qua scene; network root mới được giữ bằng `DontDestroyOnLoad` (`ArenaNetworkManager.cs:61–64`). Vì vậy chỉ sửa label lobby chưa chứng minh được hiển thị xuyên chuyển scene.
- Mirror tạm ngừng xử lý message trong lúc tải ở cả server/client (`NetworkManager.cs:824–826`, `868–874`). Tiến trình trong lúc này nên lấy từ lifecycle local; không dựa vào snapshot mạng liên tục.
- Test hiện có kiểm tra khóa Start lặp ở room rules (`LobbyRoomManagementTests.cs:281–295`), nhưng chưa đếm số lần bắt đầu scene load qua boundary thật.
- Chưa thấy failure handling cho lỗi bắt đầu tải tại `ServerLobbyController.StartMatch()`.
- `FinishLoading()` không trả kết quả chuyển trạng thái; callback arena lặp vẫn có thể gọi `Publish()` lại. **Snapshot lặp không đồng nghĩa với thông báo nghiệp vụ trùng.**

Không cần thêm worker/background thread cho Unity scene loading.

**3. Specification đề xuất**

**Problem statement:** Khi host bắt đầu trận hợp lệ, người chơi cần quan sát được tiến trình tải arena bất đồng bộ, kể cả lúc UI MainMenu bị hủy. Retry và callback lặp phải giữ một thao tác Start duy nhất trong phiên, tránh khởi động tải arena hoặc kết thúc nghiệp vụ nhiều lần.

| AC | Điều kiện đo được |
|---|---|
| **AC1 — Quyền và điều kiện Start** | Chỉ host trong room `Waiting`, có 2–4 thành viên và tất cả Ready được chấp nhận. Guest, người không thuộc room và host chưa đủ điều kiện tạo **0 lần tải**. |
| **AC2 — Retry** | Với một Start đã được chấp nhận trong cùng phiên, gửi lại ngay sát nhau, trong lúc tải hoặc sau hoàn tất vẫn có **đúng 1 lần khởi động tải arena**. |
| **AC3 — Tiến trình quan sát được** | Client có trạng thái theo mốc thật: server đã chấp nhận, client đang tải arena, client đã tải xong/đang chờ server nếu cần. Hiển thị tải tồn tại qua scene change và được dọn khi hoàn tất, lỗi hoặc disconnect. Không hiển thị phần trăm trong slice đề xuất. |
| **AC4 — Hoàn tất và spawn** | Chỉ callback arena hợp lệ của phiên đang hoạt động mới hoàn tất thao tác. Callback lặp không tạo thêm chuyển trạng thái kết thúc. Spawn vẫn bị chặn trước `Playing`. Client tải xong không được tự quyết định room đã `Playing`. |
| **AC5 — Thất bại và phiên mới** | Khi boundary khởi động tải báo lỗi, thao tác kết thúc lỗi, không spawn và retry không gọi tải lần hai. Disconnect dọn tiến trình local. Phiên mới bắt đầu sạch và có thể Start một thao tác mới. |

**Invariant chính:** Với một room/session có Start được chấp nhận, số lần gọi boundary khởi động tải arena bằng một; số lần chuyển trạng thái kết thúc nghiệp vụ không quá một.

Repo chưa có event nghiệp vụ `MatchStartCompleted` riêng. Tôi đề xuất **không thêm event chỉ để đếm test**: kiểm tra chuyển trạng thái và side effect thật. Snapshot được phép gửi lại để đồng bộ client; `ServerSceneLoaded` là callback hạ tầng, không tự được xem là thông báo nghiệp vụ.

**Non-functional constraints:**

- Giữ server authoritative, luật host/readiness, capacity bốn người và gate spawn. Giữ ranh giới Client → Networking ← Server; Domain/Protocol không thêm Unity/Mirror.
- Dùng luồng async Mirror hiện có, không sửa vendor Mirror, không chặn main thread bằng chờ đồng bộ.
- Thử **100 thao tác độc lập đồng thời**, mục tiêu p95 ≤ 3,0 giây; báo riêng số thao tác và request retry. Nếu dùng harness, kết luận chỉ áp dụng boundary được đo.
- Test chờ tín hiệu/trạng thái có timeout, không dùng sleep cố định làm bằng chứng hoàn tất.

**Giả định cần review:** Mỗi phiên hiện chỉ có một lần Start; request Start hiện rỗng (`LobbyMessages.cs:24`). Tôi đề xuất tận dụng identity của phiên và guard server, chưa thêm operation ID từ client. Không mở rộng sang reconnect token, host migration hoặc tự restart sau lỗi.

**4. Plan sau khi bạn review**

1. **Lưu đầu vào và quyết định.** Lưu nguyên văn prompt, plan này thành `plan-v1`, assumptions, khảo sát và phản hồi của bạn. Tạo `plan-reviewed` với ít nhất hai quyết định sửa/bác bỏ do bạn review, kèm evidence; không ghi nhận thay bạn.
2. **Tạo worktree/nhánh sạch và baseline evidence.** Chạy các test lobby hiện có bằng Unity đúng version; lưu hash, lệnh, môi trường, exit code, log/XML và test được discover.
3. **AI sinh test cases để bạn chỉnh trực tiếp.** Tạo bản trước review có ít nhất 12 case; đánh dấu case yếu/trùng để bạn sửa hoặc xóa ít nhất hai. Lưu before/after và lý do của bạn.
4. **Pairwise có kiểm chứng.** Factors dự kiến: requester `{host, guest, unknown}`, điều kiện `{một người Ready, nhiều người chưa đủ Ready, tất cả Ready}`, thời điểm gửi lại `{ngay sát, Loading, terminal}`. Ghi constraints cho tổ hợp không thể tới Loading/terminal khi Start bị từ chối; script kiểm mọi cặp mức khả thi. Bổ sung case lỗi, stale callback, retry sát nhau và đúng biên.
5. **Test-first.** Dùng API công khai/lifecycle message để có RED do thiếu hành vi dự kiến, không lấy compile error làm RED. Sau đó tạo patch nhỏ, trình diff cho bạn review theo scope, design, correctness và evidence của Lecture 06.
6. **GREEN và đánh giá chất lượng.** Chạy lại đúng filter RED, regression lobby, mutation, coverage nếu công cụ hỗ trợ; chạy lặp ít nhất 20 lượt cho timing và shared-state/concurrency.
7. **Tải 100 tác vụ.** Barrier khởi phát 100 phiên/thao tác độc lập chạy logic production tại boundary đã chọn; retry giữ cùng identity. Đo đến terminal caller quan sát được, ghi raw CSV/JSON, lỗi/timeout, số side effect trùng và p95 nearest-rank — mẫu thứ 95 trong 100 mẫu đã sắp xếp.
8. **Bàn giao.** Traceability, README tái lập, AI Usage Log, khung reflection để bạn tự viết 250–350 từ. Sau review diff và quyền push/PR của bạn mới xuất bản GitHub. Khi có reflection và xác nhận cam kết, tạo PDF đúng tên, tối đa tám trang chính; render kiểm tiếng Việt, bảng, link và phân trang.

Phân tầng test dự kiến: unit cho state/retry; Integration/API qua Mirror message và scene boundary; ít E2E cho Host/Ready/Start và tiến trình xuyên scene. Không cần REST API hay database/queue tests vì slice không có các dependency này.

**5. File dự kiến thay đổi**

Các đường dẫn dưới đây tính từ worktree mới.

| File/nhóm file | Mục đích |
|---|---|
| `Assets/_Project/Scripts/Server/Sessions/LobbyRoom.cs` | Chuyển trạng thái hoàn tất/lỗi có guard, bảo vệ phiên |
| `.../Server/Sessions/ServerLobbyController.cs` | Điều phối tải, xử lý lỗi boundary, tránh hoàn tất lặp |
| `.../Networking/ArenaNetworkManager.cs` | Cung cấp lifecycle local cho tiến trình; reset theo phiên |
| `.../Networking/LobbyMessages.cs` | Chỉ đổi nếu cần truyền trạng thái lỗi/tiến trình server |
| `.../Client/UI/ArenaLoadingOverlay.cs` **mới** và `.meta` | Hiển thị tiến trình tồn tại xuyên scene; cleanup |
| `.../Client/UI/LobbyRoomController.cs`, `LobbyRoomView.cs` | Nối trạng thái và tránh trình bày mâu thuẫn |
| `Assets/_Project/Tests/PlayMode/LobbyStartProgressTests.cs` **mới** và `.meta` | Test-first, contract Mirror, retry, callback, UI lifecycle |
| Hai bộ test lobby hiện có | Tái sử dụng fixture; chỉ sửa khi có lý do |
| `tools/Verify-Bt3Pairwise.py`, công cụ harness/tải | Kiểm ma trận và xuất dữ liệu tái lập |
| `docs/assignments/bt3-vt-066/` | Prompt, plan trước/sau review, cases, evidence, README, reflection và PDF |

Ưu tiên overlay tự tạo trong assembly Client để tránh chỉnh scene/prefab. Nếu cần seam để inject lỗi, tôi sẽ trình rõ trong diff; không đưa test dependency vào production.

Lệnh Unity dự kiến, chạy tại worktree:

```powershell
$unity = 'C:/Program Files/Unity/Hub/Editor/6000.6.8f1/Editor/Unity.exe'
& $unity -batchmode -nographics -projectPath $PWD.Path `
  -runTests -testPlatform PlayMode `
  -testFilter 'SmallArena.Tests.PlayMode.LobbyRoomManagementTests;SmallArena.Tests.PlayMode.LobbyGovernanceAndScreenTests' `
  -testResults "$PWD/TestResults/BT3/baseline.xml" `
  -logFile "$PWD/Logs/BT3-baseline.log"
```

Test mới dùng filter riêng, giữ nguyên giữa RED và GREEN; kiểm cả XML và exit code.

Mutation đề xuất có sẵn trong production: `ArenaNetworkManager.cs:203`, số connection đã authenticated **`> LobbyLimits.Capacity`**. Kiểm dưới/tại/trên biên là 3/4/5; mutant `>=` sẽ từ chối connection thứ tư. Chạy sạch → mutant trên nhánh tạm → khôi phục, lưu assertion và diff. Đây là regression capacity trong cùng networking slice.

**6. Các điểm bạn cần review**

| Điểm review | Evidence và đề xuất |
|---|---|
| **R1 — Cách hiển thị tiến trình** | Mirror đã async (`NetworkManager.cs:828`) nhưng UI lobby thuộc MainMenu. Đề xuất overlay local tồn tại xuyên scene, dùng mốc lifecycle, không thêm worker hoặc chỉ đổi label. Bạn có muốn sửa phạm vi này? |
| **R2 — Hành vi sau lỗi tải** | Hiện room vào `Loading` trước `LoadRoomArena()` và chưa có recovery (`ServerLobbyController.cs:67–69`). Đề xuất terminal lỗi và kết thúc phiên an toàn; retry không tải lại. Phiên mới mới được Start lại. Bạn muốn điều chỉnh chính sách này thế nào? |
| **R3 — Identity của retry** | `LobbyStartRequest` rỗng và mỗi phiên chỉ Start một lần. Đề xuất giữ contract này, dùng guard theo phiên, chưa thêm operation ID vào protocol. Bạn có yêu cầu identity tường minh không? |

Một rủi ro tooling đã xác minh: `Run-DomainTests.ps1:2` mặc định Unity `6000.6.7f1`, và dòng 34–39 chỉ liệt kê ba file EditMode cố định. **Runner này không chạy test lobby mới.** Tôi sẽ dùng Unity PlayMode runner làm bằng chứng chính; license/import/discovery vẫn cần được kiểm thực tế sau review.

Bạn hãy phản hồi **ít nhất hai quyết định sửa hoặc bác bỏ** ở R1–R3, hoặc phần khác của plan. Tôi sẽ lưu nguyên phản hồi làm evidence review trước khi triển khai.
