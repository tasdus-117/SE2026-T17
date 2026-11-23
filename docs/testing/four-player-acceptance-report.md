# Báo cáo TV1 #18 / #25 / #27 — nghiệm thu bốn tiến trình

## Baseline và phạm vi bằng chứng

- Repository: tasdus-117/SE2026-T17; nhánh `fix/tv1-issues-18-25-27`.
- Commit nguồn sản phẩm cố định: **8fba1c127f650615faec4f14b34b16da7552c858**.
  Bao gồm PR #36 revert Phase 3 CI và PR #37 raycast/feedback/HUD.
- Unity **6000.6.8f1**, StandaloneWindows64, Mono2x, Mirror/KCP, bảy asmdef.
- Tooling mới đang ở working tree, chưa commit. Không gọi toàn bộ tooling mới là đã
  nằm trong SHA baseline. Build/run manifests ghi riêng SHA baseline, SHA256 từng
  nguồn instrumentation/runner và hash gói player; nguồn runner được chụp trong run.
  Cần commit/review và nghiệm thu lại SHA tích hợp cuối trước ký #27.
- Không sửa gameplay production, scene/prefab/asmdef, backend hoặc stripping settings.
  Instrumentation chỉ được copy vào validation project trong TestResults với build
  symbol `SMALL_ARENA_FOUR_PLAYER_HARNESS`; không flag CLI thì không tạo automation.
- CI giữ Phase 1 + Phase 2: chỉ thêm hai MinProjectPassedCount; không thêm job hoặc
  required check đa tiến trình. Không merge PR, đóng issue hay gửi comment từ tác nhân.

## Dependency readiness (kiểm tra live trong nhiệm vụ)

| Dependency | Bằng chứng hiện tại | Trạng thái bàn giao |
|---|---|---|
| #18 XML gate | 55 regression passed; project skip bị reject; vendor policy và project floor độc lập | READY FOR HUMAN REVIEW; issue vẫn OPEN |
| #19 scaffold (TV3) | Code đã bỏ Assets.meta gốc; positive mới đạt 1.530 metadata pairs, graph/GUID/build order hợp lệ | BLOCKED: issue OPEN, chưa có bàn giao/review negative-fixture mới của owner trong đợt này |
| PR #13 movement (TV2) | OPEN/DRAFT, head861866f; chưa merge vào baseline8fba1c1 | BLOCKED: không tích hợp thay TV2 hoặc dùng bằng chứng nhánh cũ ký baseline này |
| #24 oracle (TV2) | OPEN, 0 comments, chưa có oracle hoàn chỉnh bàn giao | PENDING_TV2_ORACLE (#24); BLOCKED cho sign-off #27 |
| #25 harness | Source, parser/watchdog tests và build instrumented có trong nhánh | LOCAL PASS: 3/3 rounds + teardown, exit0; READY FOR HUMAN REVIEW; issue vẫn OPEN, oracle còn pending |
| #26 visual/input QA (TV3) | OPEN, chưa có bằng chứng 1280×720/1920×1080 cùng SHA | BLOCKED / NOT TESTED |

**#27 tổng thể: BLOCKED**, kể cả khi lượt localhost thành công, do các dependency
và human review ở trên chưa hoàn tất. Không coi các checkbox issue là tự hoàn thành.

## Kiểm chứng mới thực thi

| Gate | Kết quả / exit |
|---|---|
| tools/Test-AssertUnityTestResults.ps1 | **55 passed, 0 failed**, exit0; 34 fixtures cũ được giữ, negative CLI exits đúng1 |
| Strict Roslyn | **7/7 project asmdefs**, 0 project warning/error, exit0 |
| Scaffold/GUID | **1.530** metadata pairs, không GUID trùng/orphan, graph không vòng; scene order0/1/2 đúng; exit0 |
| Native EditMode | **277 passed / 277 total**, 0 failed/inconclusive/skipped, exit0 |
| Native PlayMode | **414 passed / 416 total**, 0 failed/inconclusive, 2 vendor skips, exit0 |
| Hardened XML gate | EditMode floors261/261; PlayMode floors408/406; nhận 412 project +2 vendor passed và2 vendor skipped; exit0 |
| Harness parser/watchdog | **56 passed, 0 failed**, Windows PowerShell5.1, exit0; không dùng fixtures thay bằng chứng player thật |
| Windows Mono instrumented build | **Succeeded**, 0 errors / 0 warnings, 113.898.727 bytes, process exit0; identity ở dưới |
| Localhost 4-process /3 rounds | **3/3 completed**, thêm partial match kiểm teardown; runner exit0, bốn player exit0, cleanup=true |

Hai skip vẫn là IntegrationTests.WindowsInput_MouseMovements_AreDetected và
WindowsInput_RemoteDesktopMouseMovements_AreDetected, Assembly Unity.InputSystem.IntegrationTests.dll,
reason `Unstable due to 1252825`. Không whitelist namespace IntegrationTests hoặc
che project skip. Native XML/logs mới ở `TestResults/TV1Acceptance/Native/`, compile
ở `Compilation/`, gate copies ở `ValidatedReports/`, scaffold.log và regression logs
cùng thư mục. Kết quả Domain runner/lượt lịch sử không được cộng vào bảng native.

## Identity và kết quả localhost cuối

| Thuộc tính | Giá trị đã kiểm chứng |
|---|---|
| Production commit | `8fba1c127f650615faec4f14b34b16da7552c858` |
| Instrumentation/tooling SHA256 | `50d228450aa84486c3c2718292545fec6a6d0f689b798ff28f3d8ff99f12e86c` |
| SmallArena.exe SHA256 | `8060790b33cf3ca520e289bf961094e2ed5473c7fad5d6148d8489e96055d27b` |
| Toàn bộ player directory SHA256 | `9f7c7477dcbd016b6cb299a6e96e6d39a8b258eda0fefc5190bc03f720e59bdb` |
| Run UUID | `4d4cb053fd56416fa97fd3ca8186260e` |
| Topology | 1 Host + 3 Client, KCP127.0.0.1:59128 |
| PID theo role0/1/2/3 | 7992 / 21912 / 14136 / 18184 |
| Thời gian monotonic | 390,447 giây |
| Hội tụ | 23 checkpoints: 4 Countdown, 16 InProgress, 3 Ended |
| Clock spread | Max0,183411s; mean0,074739s; ngưỡng0,75s |
| Cleanup | Bốn player exit0, Job rỗng, không process con còn sống; UDP bind lại thành công |

Ba round Ended đều quan sát cùng standings trên bốn process: Host **5 kills,
0 deaths, 50 actual damage**, victim5 deaths; rank/winner khớp comparator độc lập.
Mỗi round kiểm death/respawn, buff resetbase1, HP10 và life tăng; chờ **30 giây thật**
sau carrier death rồi nhặt item xuất hiện lại. Tự động về Lobby giữ connection/slot,
Ready=false, score/feed/avatar cũ được xóa; bắt đầu round kế tiếp bằng Ready/Start.
Sau ba round hoàn chỉnh, partial match thứ tư kiểm Client3 Leave, ba peer còn lại
nhận Disconnected/không orphan avatar và Host clock tiếp tục; Host Leave đưa tất cả
về MainMenu với0 identities,0 room members và network inactive.

Bằng chứng thô (gitignored):

- `TestResults/FourPlayerHarness/Runs/4d4cb053fd56416fa97fd3ca8186260e/summary.json`:
  result/rounds/cleanup, CLI từng PID, snapshots hội tụ, exit codes.
- Cùng thư mục: player logs, stdout/stderr, events, JSONL segments, command files,
  `runner-source/`, `build-source/` và manifests; SHA256 source lưu trữ khớp file hiện tại.
- `TestResults/TV1Acceptance/final-metrics.json`, `four-player-run.log`,
  `xml-regression.log`, `harness-regression.log`, `source-audit.json`.
- Build: `TestResults/TV11/ValidationProject/Builds/WindowsStandalone/`,
  `four-player-build.json`, `build-report.json`; log `Logs/FourPlayerBuild.log`
  trong validation project. Toàn bộ player-tree hash cần thiết vì exe bootstrap
  có thể giữ nguyên trong khi assembly/data đổi.

Đối chiếu validation Assets/Packages/ProjectSettings với git archive baseline:
**0 semantic differences** ngoài source instrumentation được liệt kê. Repo chính
không thay production Assets/Packages/ProjectSettings. Cold build trước đó có15
vendor warnings (Mirror analyzers/Mono.Cecil obsoletion); build incremental cuối
báo0 warnings. Không suppress warnings hoặc gọi cache là đã sửa vendor warnings.

### Các lượt phát triển thất bại được giữ nguyên

Không dùng các lượt sau để ký pass; mọi lượt có cleanup=true. Lỗi nằm ở transport
artifact/runner instrumentation, không sửa gameplay để vượt qua.

| Run UUID | Completed rounds | Lỗi quan sát / sửa tooling |
|---|---:|---|
| 02072628c70c43deb5ddb97c6172a962 | 0 | Missing snapshot trong cửa sổ thay file; bỏ snapshot mutable |
| c30ab835618e4277a7114976b245c070 | 0 | PowerShell File.Replace với backup path không hợp lệ |
| 9f43e51dbd054bde839191c59312872e | 0 | Sharing violation khi đọc snapshot mutable |
| 8e85bbfddd244dca974fa31b466b72d8 | 0 | Sample thiếu khiến slot fixture sai; yêu cầu full roster/hội tụ trước khi chọn slot |
| 90458435a44f4dc6af0748e3c389a5ad | 0 | Command file biến mất khi replace; chuyển sang immutable sequence files |
| 4632196315434fbd80808458160e3f45 | 2 | IOException Win32 IO112 khi ghi stream thứ ba; giảm5Hz, rotate250records, fail-fast Infrastructure |

Lượt IO112 cũ bị gắn Product bởi wrapper thiếu marker; đây là lỗi ghi artifact
infrastructure. Probe sau lỗi cho thấy37,603GiB trống, nên **không kết luận ổ đĩa
thật đã đầy** hoặc nguyên nhân quota cụ thể. Không xóa cache/file người dùng để
che lỗi; bổ sung disk preflight, bounded segments và diagnostics. Lượt cuối hoàn
thành trên protocol sửa; snapshots cũ/thiếu/JSONL lỗi không được chuyển thành pass.

## Thiết kế local harness và provisional expectations

Topology **1 Host executable + 3 Client executable độc lập**, cùng player directory,
KCP IPv4 **127.0.0.1**, port tự chọn hoặc -Port. Tất cả đi từ MainMenu → Multiplayer
→ Host/Join buttons → bốn slot → Ready/Start → Arena_MVP. Không dùng --arena-* hoặc
Bootstrap/debug entry; Host giữ local Mirror connection thông thường, ba peer dùng KCP.

Các assertions dưới đây dựa trên code/config tại baseline, gắn nhãn
**PENDING_TV2_ORACLE (#24)**, không thay tài liệu oracle của TV2:

| Quan sát | Expected độc lập / nguồn quy tắc |
|---|---|
| Spawn/phase | Bốn avatar; Countdown cấm CanAct; sau InProgress có thể điều khiển (MatchSessionRuntime / NetworkHealth) |
| HP/damage/cooldown | HP10, base1/buff2, melee1s; Host nhận5 kills bằng50 damage thực trên victim10HP mỗi life (ServerMatchLifecycleController, NetworkCombat, config) |
| Pickup/death | RequestPickup của client có range hợp lệ; chỉ buff owner, map item biến mất; chết resetbase1, Respawning rồi HP10/life tăng |
| Loot delay | Không item mới trước30s từ timestamp elimination server; sau30s có item để nhặt lại (ServerLootRespawnController) |
| Hội tụ | Cùng session/phase, HP/life/buff/score/winner giống nhau; clock spread ≤0,75s sau hội tụ, cho sai số sample0,2s và snapshot/interpolation; không yêu cầu mọi frame đồng thời |
| Ranking | Kills giảm, deaths tăng, damage giảm, slot tăng; expected tính bằng PowerShell độc lập, không gọi ComputeFinalStandings production |
| End/lobby | Host Victory, các peer Defeat, input freeze; tự về Lobby, cùng connection/slot, Ready false, score/feed/avatar cũ không còn |
| Teardown | Sau3 round hoàn chỉnh mở thêm một partial match: một client Leave, ba peer còn chạy, rồi Host Leave → tất cả về MainMenu/0 identities |

Host fixture setup chỉ teleport qua ArenaNetworkTransform trên server đến vị trí
có capsule clearance trong sàn thật, để bố trí melee/pickup. Không set HP, damage,
clock, score, phase hoặc timer. RequestAttack/RequestPickup vẫn qua Command và
validation production. Local PlayerController sampling được thay bằng automation
trong build/flag riêng; đây không phải nghiệm thu phím vật lý, LMB aim, focus loss,
cursor hay PR #13 revoke/restore edge case. Các phần đó vẫn chờ #26/PR #13.

Control sequence files bất biến, snapshot append-only JSONL **5Hz**, rotate mỗi **250 records** (~0,5MiB); PID/run/commit/tooling
identity validation và monotonic timeout. Chỉ snapshot hoàn chỉnh được đọc; cache
giữ sample hợp lệ qua read gap ngắn, không nhận sequence cũ, fail khi stale>10s.
Windows Job Object atomic membership + KILL_ON_JOB_CLOSE/watchdog bảo vệ cả khi
runner chết; finally kiểm worker exit, Job empty và bind UDP độc quyền trước Passed.

## Commands

Các lệnh thực thi native dùng Unity.exe `-batchmode -nographics -projectPath <repo>`
`-runTests -testPlatform EditMode/PlayMode -testResults <Native/*.xml> -logFile <Native/*.log>`.
Private launcher tại TestResults/TV1Acceptance/Native/Run.ps1 chỉ điều khiển process,
timeout20 phút và ghi exit. Có thể tái hiện bằng wrapper đã tracked:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Test-AssertUnityTestResults.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Test-InvokeFourPlayerHarness.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Compile-UnityAssemblies.ps1 -OutputDirectory TestResults/TV1Acceptance/Compilation
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Verify-Scaffold.ps1 -SkipEditorCheck
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-Tv31Unity.ps1 -Phase EditMode
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-Tv31Unity.ps1 -Phase PlayMode
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Assert-UnityTestResults.ps1 -XmlPath TestResults/TV31/editmode.xml -MinPassedCount 261 -MinProjectPassedCount 261
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Assert-UnityTestResults.ps1 -XmlPath TestResults/TV31/playmode.xml -MinPassedCount 408 -MinProjectPassedCount 406
# Dùng mặc định để tạo validation project mới; lượt local dùng cache riêng đã đóng:
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Build-FourPlayerHarness.ps1 -ValidationProject TestResults/TV11/ValidationProject
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Invoke-FourPlayerHarness.ps1 -Player TestResults/TV11/ValidationProject/Builds/WindowsStandalone/SmallArena.exe -Rounds 3
```

Nếu sandbox chặn licensing IPC hoặc child process, chạy lại đúng lệnh sau khi được
cấp quyền. Player build không cần license khi chạy; Editor native/build cần license.
Không đưa Build/TestResults/Logs/ULF hoặc PROJECT_CONTEXT.md vào Git. Bản build
instrumented không phát hành thay bản sản phẩm.

## Phân tách phạm vi

| Phạm vi | Trạng thái / giới hạn |
|---|---|
| Localhost automated | PASSED: run4d4cb053fd56416fa97fd3ca8186260e, 3 rounds + partial teardown; không bao gồm visual/input QA |
| LAN | NOT TESTED: không có topology nhiều máy; controller/file channel hiện dành cho một máy Windows |
| Latency/packet loss | NOT TESTED: không có impairment proxy/netem; KCP localhost không chứng nhận mạng xấu |
| Visual/input #26 | BLOCKED / NOT TESTED: headless không chứng minh layout/cursor/focus/physical bindings ở hai độ phân giải |
| Movement PR #13 | BLOCKED: chưa tích hợp head của TV2 vào baseline; không nhận test nhánh đó làm test main |
| Official oracle #24 | BLOCKED: chưa bàn giao; provisional checks phải được TV2 review |
| GitHub CI | NOT TESTED trong đợt local; không thêm smoke job để thay việc này |

## Inventory thay đổi

Sửa: tools/Assert-UnityTestResults.ps1, tools/Test-AssertUnityTestResults.ps1,
.github/workflows/unity-ci.yml (chỉ hai project floors), docs/ci/ci-pipeline.md.
Thêm: tools/Invoke-FourPlayerHarness.ps1, tools/FourPlayerHarnessChecks.ps1,
tools/FourPlayerProcessJob.cs, tools/Test-InvokeFourPlayerHarness.ps1,
tools/Build-FourPlayerHarness.ps1, tools/FourPlayerBuild.cs,
tools/FourPlayerAutomation.cs, tools/FourPlayerHarnessBridge.cs và báo cáo này.
PROJECT_CONTEXT.md cập nhật tiếng Việt nhưng tiếp tục ignore. Không thêm asmdef,
production runtime hook hoặc asset/meta vào Assets của repo chính.
