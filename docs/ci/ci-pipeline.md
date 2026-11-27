# CI — Quality gates và Windows Standalone

Phase 2 bổ sung Windows player build; hướng dẫn và bằng chứng ở mục 7 bên dưới.
Các số liệu Phase 1 trong mục 1–6 là kết quả lịch sử, không phải lượt chạy native mới.

Ngày kiểm chứng cục bộ: **23/11/2026**. Baseline: `49ab265` (merge PR #31),
nhánh `feat/ci-phase1-hardening`, Unity **6000.6.8f1**.
Phạm vi chỉ gồm workflow, công cụ kiểm chứng và tài liệu; không sửa runtime C#,
scene, prefab, Input Actions, ProjectSettings hay suite Unity.

## 1. Kết quả kiểm toán HEAD

| Thành phần | Phát hiện | Thay đổi |
|---|---|---|
| `Assert-UnityTestResults.ps1` | Cho phép `Skipped:Ignored`, `total > 0` nhưng `passed = 0`; có thể xanh khi toàn bộ test bị skip. Không có ngưỡng pass. | Kiểm tra XML, counters, từng test-case và lỗi setup/teardown; `MinPassedCount >= 1`. |
| `unity-ci.yml` | Chỉ Git diff và native tests; XML phụ thuộc outcome của step tests; thiếu strict compiler/static gates. | Thêm static matrix, các bước compiler/configuration/layout rõ ràng, hai XML steps `if: always()`, gate tổng hợp. |
| `Verify-Scaffold.ps1` | Đòi `Assets.meta` đã bị ignore; clone sạch thất bại. Luôn cần Editor Windows ngay cả khi chỉ đọc file. | Chỉ kiểm metadata bên trong Assets; `SkipEditorCheck` dành cho kiểm tra tĩnh không license. Mọi assertion khác được giữ. |
| Compiler và các verifier | Mặc định Editor 6000.6.7f1; compiler/package resolver dùng DLL Editor, package hoặc Library, không phải công cụ độc lập hoàn toàn. | Resolver đọc ProjectVersion, hỗ trợ `UNITY_PATH` của GameCI, xác nhận phiên bản. Các gate cần package import chạy sau native tests. |
| `Verify-Input.ps1` | Còn kỳ vọng 8 keyboard bindings; cấm cả tên `Health` dù input chỉ đọc `NetworkHealth` để gate lifecycle. | Kiểm đúng 9 bindings gồm LMB/J; vẫn cấm physics, damage và global lookup. |
| `Verify-ArenaConfiguration.ps1` / C# helper | Không nhận GUID viết hoa của `Assets/Mirror/LICENSE.meta`; collision policy cũ cấm Player–Player. | GUID nhận hex không phân biệt hoa/thường; kiểm Player–Player bật theo M2. Không đổi asset hay test runtime để chiều verifier. |
| `Verify-ArenaLayout.ps1` | Đã kiểm đầy đủ prefab/scene nhưng compile 7 asmdef thêm lần nữa. | `SkipCompilation` chỉ bỏ compile lặp; CI có compiler gate riêng, layout assertions vẫn chạy đủ. |

Các script fail bằng terminating exception hoặc kiểm native exit code rồi throw;
không có `continue-on-error`. `Run-DomainTests.ps1` dùng cùng resolver, vẫn chạy
suite Domain thật bằng host .NET, không được gọi kết quả này là native EditMode.

`Run-Tv31Unity.ps1`, `Run-LobbyLoadingUnity.ps1` và ba `*StandaloneSmoke.ps1`
là công cụ tích hợp thủ công: cần Editor có license hoặc build instrumented riêng,
kiểm exit code/timeout/report và có bước teardown. Hai wrapper Unity lịch sử còn
kiểm XML đơn giản; **không dùng chúng làm gate CI**. `Import-TmpResources.ps1`
là công cụ ghi asset, không phải verifier và không được chạy trong pipeline này.

## 2. Luồng CI

```text
PR -> main / push main
  +-- Git diff
  +-- Static quality (ubuntu-24.04)
  |     XML regression (pwsh 7), scaffold, Input Actions
  +-- Static quality (windows-2022)
  |     XML regression (pwsh 7 + Windows PowerShell 5.1), scaffold, Input Actions
  +-- Unity EditMode and PlayMode (windows-2022, nhánh nội bộ, khi classifier yêu cầu)
        Library cache Windows -> resolve/pull Editor image -> cố định digest
        GameCI native PlayMode + EditMode -> XML validations luôn chạy
        cùng image + checkout/Library đã import:
          Editor/scaffold -> strict 7-asmdef compile -> physics/URP -> arena/prefab/UI
        khi các gate trên đạt: Windows x64 Mono build trong cùng image/Library
        verify output + BuildReport -> ZIP/SHA256/source commit -> artifact 3 ngày
        upload test/build diagnostics luôn chạy
        PR chỉ sửa docs/** hoặc *.md được bỏ qua toàn job unity
  \-- CI Quality Gate: diff/static phải success; unity phải success khi được yêu cầu
```

GameCI chạy Editor **trong Docker**, không cài vào Hub trên runner host. Vì vậy
không gọi compiler bằng một đường dẫn Hub giả trên host: workflow khởi chạy các
container kiểm chứng với **cùng image digest**, mount cùng checkout, không truyền
Unity credentials vào các container kiểm tra tĩnh. `Invoke-UnityCIGate.ps1` xác định
Editor từ `UNITY_PATH`; DLL package/PackageCache đến từ lần import native trên chính
checkout đó. Không tải Library từ máy người phát triển. Nếu image/package import
không tồn tại, pipeline thất bại với chẩn đoán thay vì bỏ qua gate.

Action bên ngoài giữ SHA pin hiện hữu. Cách chọn image/path được đối chiếu với mã
[GameCI Docker runner](https://github.com/game-ci/unity-test-runner/blob/fa6ced25861c16ef56187828c43f76d00df43a23/src/model/docker.ts),
[image tag](https://github.com/game-ci/unity-test-runner/blob/fa6ced25861c16ef56187828c43f76d00df43a23/src/model/image-tag.ts) và
[Windows runner](https://github.com/game-ci/unity-test-runner/blob/fa6ced25861c16ef56187828c43f76d00df43a23/dist/platforms/windows/run_tests.ps1).

Quyền token chỉ `contents: read`; checkout không lưu credentials. Ba secrets hiện
hữu chỉ cấp cho bước kiểm tra có/không và GameCI. Không in giá trị, dump environment
hay upload tệp license. Artifact `unity-test-results` chứa XML/log tests và báo cáo
compiler/layout/configuration, giữ **7 ngày**; không upload toàn bộ workspace.

## 3. Hợp đồng XML và ngưỡng baseline

- Mặc định đòi **cả** `editmode-results.xml` và `playmode-results.xml`.
- File phải tồn tại, có nội dung, đúng NUnit 3 `test-run`; XML hỏng/DTD bị từ chối.
- Counts phải là số nguyên không âm; `passed >= MinPassedCount`, mặc định **1**.
  Tổng/từng leaf phải khớp; thiếu test-case, hidden failed/error/inconclusive,
  failure setup/teardown hoặc trạng thái lạ đều thất bại.
- Chấp nhận root Unity `Skipped:Ignored` chỉ khi thật sự có đủ test **Passed**.
  Skips không được cộng vào pass. Không áp dụng mức minimum bằng 0.
- CI đặt minimum **261 EditMode**, **408 PlayMode**. Baseline hiện hữu có 406 project
  PlayMode pass, 2 vendor pass và 2 vendor skip; đây là ngưỡng tối thiểu toàn report,
  không phải chứng minh từng tên test luôn được phát hiện. Tool in riêng project pass
  count và artifact giữ leaf names để review thay đổi discovery/skip.

Hai vendor skip hiện biết thuộc Input System:
`IntegrationTests.WindowsInput_MouseMovements_AreDetected` và
`IntegrationTests.WindowsInput_RemoteDesktopMouseMovements_AreDetected`, lý do
`Unstable due to 1252825`. Không sửa, xóa hoặc đổi chúng thành pass. Khi baseline
tăng, review report và tăng minimum cùng PR; không giảm chỉ để làm CI xanh.

Compiler dùng `/warnaserror+` cho **cả bảy** asmdef dự án. Không thêm suppression
hay thay warning policy. Mirror vendor vẫn giữ warning trong log theo chính sách
cũ. Hai host YAML có ngoại lệ **có sẵn** `/nowarn:1701` cho YamlDotNet .NET 7 đi kèm
Editor khi host dùng .NET 8; ngoại lệ này không áp dụng cho compiler mã dự án và
không được mở rộng trong Phase 1. Saved-file/Roslyn checks không thay Mirror Weaver
hoặc native Test Runner.

## 4. Chạy lại cục bộ

Từ gốc repo; Windows PowerShell 5.1 hoặc PowerShell 7. Nếu ExecutionPolicy chặn,
khởi chạy từng script bằng `powershell -NoProfile -ExecutionPolicy Bypass -File ...`
(hoặc `pwsh`); không cần Pester hay cài module PowerShell.

Không cần Editor/license, dùng được cả Windows/Ubuntu:

```powershell
$ErrorActionPreference = 'Stop'
& ./tools/Test-AssertUnityTestResults.ps1
if ($LASTEXITCODE -ne 0) { throw 'XML regression failed.' }
& ./tools/Verify-Scaffold.ps1 -SkipEditorCheck
& ./tools/Verify-Input.ps1 -SkipCompilation
git diff --check
if ($LASTEXITCODE -ne 0) { throw 'Invalid patch.' }
```

Có Unity đúng ProjectVersion và đã import package trong checkout:

```powershell
$ErrorActionPreference = 'Stop'
$editor = 'C:\Program Files\Unity\Hub\Editor\6000.6.8f1\Editor'
& ./tools/Verify-Scaffold.ps1 -EditorRoot $editor -CompileProbes
& ./tools/Verify-ArenaConfiguration.ps1 -EditorRoot $editor
& ./tools/Verify-ArenaLayout.ps1 -EditorRoot $editor -SkipCompilation
& ./tools/Run-DomainTests.ps1 -EditorRoot $editor
# CompileProbes đã chạy Compile-UnityAssemblies; nếu cần chạy riêng:
# & ./tools/Compile-UnityAssemblies.ps1 -EditorRoot $editor
```

Chạy native suites theo [CLI ở README](../../README.md#unity-test-runner-và-cli),
đóng Editor hoặc dùng bản sao riêng; **không thêm `-quit` vào `-runTests`**.
Xóa XML cũ trước mỗi lượt native chạy cục bộ để tránh dùng lại báo cáo cũ. Với báo
cáo mới trong `TestResults/Local`, kiểm ngưỡng bằng:

```powershell
& ./tools/Assert-UnityTestResults.ps1 -ResultsRoot TestResults/Local -TestPlatform editmode -MinPassedCount 261
& ./tools/Assert-UnityTestResults.ps1 -ResultsRoot TestResults/Local -TestPlatform playmode -MinPassedCount 408
```

CI dùng checkout mới, không restore cache XML. Các lệnh compiler không cần license
nhưng cần SDK/DLL của Editor và package đã import; Unity native tests vẫn cần license.
Không dùng file context cá nhân, Editor đang mở hay artifact của máy khác để vượt gate.

## 5. Bằng chứng cục bộ và giới hạn

| Kiểm chứng | Kết quả |
|---|---|
| Reproduce all-skipped trên validator HEAD | Exit **0** sai; cùng XML sau sửa exit **1** đúng. |
| Regression XML CLI/process | **34/34 pass, 0 failed**, Windows PowerShell **5.1.26100.10068**: valid/minimum/vendor skips, failure/all-skipped/zero/inconclusive, missing/empty/corrupt, counter/leaf mismatch, setup failure/error, DTD. |
| Clone sạch baseline + overlay tools, không Library/Assets.meta/context cá nhân | Scaffold **7 asmdef**, **1.526** metadata pairs, unique GUIDs/build scenes/packages đạt; Input **40** assertions đạt. |
| Strict project compile | **7/7** đạt, **0** project warning/error; Mirror compile riêng; không suy thành native import. |
| Domain engine-boundary compiler probes | Không có engine reference → từ chối `UnityEngine` với CS0246; positive control có reference → compile đạt. |
| Fault injection trên bản sao riêng | Thiếu `.meta`, scene GUID sai, Domain bật engine đều exit **1** đúng diagnostic; `#warning` của project bị nâng thành **error CS1030**, exit **1**. Tất cả fault đã phục hồi, bản sao Assets/ProjectSettings khớp HEAD. |
| Saved arena configuration | **5.245** assertions, **1.024** collision entries và symmetry; probe PlayMode compile đạt. |
| Saved arena/prefab/UI layout | **4.627** assertions, **18** YAML assets đạt. |
| Domain runner mới thực thi | **261 pass / 0 failed / 0 skipped / 0 inconclusive**. |
| XML native baseline được validator mới đọc lại | EditMode **261/261 pass**; PlayMode **408 pass / 0 failed / 2 skipped**, **406** project pass; báo cáo native ngày **22/11/2026**, không phải native run mới của Phase 1. |
| Cú pháp và patch | Toàn bộ script tools parse trên PowerShell 5.1; YAML workflow parse đạt, hai XML steps `always()`, gate tổng hợp đủ dependencies, không có `continue-on-error`; `git diff --check` đạt. |

Raw local artifacts nằm trong `TestResults/CIPhase1/`, `TestResults/Compilation/`,
`TestResults/ArenaConfiguration/`, `TestResults/ArenaLayout/`, `TestResults/Domain/`
và `Logs/ScaffoldVerification/`, đều bị ignore. Không commit XML/log/cache.
Chưa chạy workflow trên GitHub trong nhiệm vụ này; không tự push, sửa Secrets hay
branch protection. Máy cục bộ chỉ có Windows PowerShell 5.1, chưa chạy PowerShell 7 /
Ubuntu; matrix CI chịu trách nhiệm xác minh hai môi trường đó. Docker daemon cục bộ
không hoạt động, vì vậy chưa chạy đường dẫn container mới trên máy này. Native Unity
full suites không được chạy lại; chỉ kiểm report native cũ và thực thi mới Domain/static.

## 6. Đề xuất bảo vệ `main`

**Required check tối thiểu: `CI Quality Gate`.** Job này dùng `if: always()` và chỉ
chấp nhận `success` của Git diff và toàn bộ static matrix. Job `unity` bao gồm tests,
XML, strict compilation và production build phải `success` khi classifier yêu cầu;
chỉ được `skipped` nếu PR chỉ sửa tài liệu. `failure`/`cancelled`, thiếu classifier
hoặc skip Unity của PR code đều không đạt.
Có thể chọn thêm các check chi tiết sau để review trực quan:

1. `Git diff`
2. `Static quality (ubuntu-24.04)`
3. `Static quality (windows-2022)`
4. `Unity EditMode and PlayMode`
5. `CI Quality Gate`

Sau lượt chạy thật đầu tiên, chọn đúng tên hiển thị trong Settings → Branches/Rulesets,
yêu cầu PR + review, resolve conversations, nhánh cập nhật với main và các checks trên.
Không coi Unity bị skip trên PR code là đủ điều kiện merge. Với fork, thành viên review
rồi đưa commit lên nhánh nội bộ để có license và mở PR; không dùng `pull_request_target`
để chạy mã fork với secrets. Đây là **đề xuất**, không phải cấu hình remote đã áp dụng.

## 7. Phase 2 — Windows Standalone Build

### Baseline và phạm vi

Nhánh `feat/ci-phase2-standalone-build` ban đầu ở `49ab265`, chưa chứa Phase 1.
Đã fast-forward nhánh hiện tại đến commit Phase 1 có sẵn **`53e4741`**, không tạo
merge commit, không sửa main và không tự push. Không coi Phase 1 đã merge vào main
chỉ dựa vào mô tả nhiệm vụ. Sau đó chỉ thêm Editor builder, verifier/wrapper/tests,
workflow và tài liệu. Kiến trúc giữ **7 asmdef**; helper ở thư mục Editor dùng assembly
Editor do Unity tạo, không thêm asmdef hay tham chiếu vào runtime.

`tools/Build-LobbyStandalone.cs` hiện hữu là builder instrumented cho smoke tests:
hardcode scenes, đổi backend/stripping và thêm `SMALL_ARENA_M2_SMOKE`. Production
builder mới không gọi helper đó, không chèn smoke code/defines và không đổi
PlayerSettings, scenes hoặc gameplay. Backend hiện hành được giữ và ghi trong report.

### Entry point và chạy cục bộ

`SmallArena.Tools.SmallArenaBuildPipeline.BuildWindowsStandalone` nằm tại
`Assets/_Project/Scripts/Editor/Build/SmallArenaBuildPipeline.cs` (có `.meta`). Method:

1. Lấy **enabled scenes từ `EditorBuildSettings.scenes`**; bắt buộc đúng thứ tự
   **MainMenu 0 → Bootstrap 1 → Arena_MVP 2**. Kiểm scene asset tồn tại/import được
   và GUID khớp. Repository không có `LobbyRoom.unity`; lobby dùng UI hiện hành.
2. Xác nhận Windows Build Support; dọn **chỉ** thư mục output cố định
   `Builds/WindowsStandalone/`, rồi gọi `BuildPipeline.BuildPlayer` với
   `StandaloneWindows64`, `BuildOptions.StrictMode`, output `SmallArena.exe`.
3. Ghi `build-report.json` (Unity version, target, backend, scenes, result, errors,
   warnings, bytes) và `Logs/WindowsStandalone/build.log` từ BuildReport messages.
4. Chỉ thành công khi `BuildResult.Succeeded`, `totalErrors == 0` và size > 0;
   exception/failure gọi exit 1 trong batchmode. Không tắt hoặc che warning.

Từ root repo, dùng Windows PowerShell 5.1 hoặc PowerShell 7:

```powershell
# Chỉ kiểm scenes, Editor version, Windows Build Support và entry point; không build.
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Build-WindowsStandalone.ps1 -ValidateOnly

# Đóng Editor của project trước khi chạy. Có thể thêm -EditorRoot <Editor directory>.
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Build-WindowsStandalone.ps1

# Hoặc dùng một bản sao riêng đã đồng bộ source để giữ Editor chính đang mở.
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Build-WindowsStandalone.ps1 -ProjectPath C:/path/to/validation-project

# Saved-scene/output checks và regression synthetic, không cần Unity license.
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Assert-WindowsStandaloneBuild.ps1 -ScenesOnly
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Test-WindowsStandaloneBuild.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Assert-WindowsStandaloneBuild.ps1
```

Wrapper khởi chạy đúng Editor với `-quit -batchmode -nographics`,
`-buildTarget StandaloneWindows64` và `-executeMethod`; full Editor log ở
`Logs/WindowsStandalone/editor.log`. Wrapper từ chối project đang có Unity lock,
xóa report/log cũ trước lượt build thật, kiểm process exit code rồi kiểm output;
không dùng thành công của bản build trước khi một lượt mới thất bại. `ValidateOnly`
không kiểm chứng license/native build và không được dùng làm CI build step.

`Assert-WindowsStandaloneBuild.ps1` yêu cầu exe không rỗng, DOS/MZ + PE signature,
machine AMD64, PE32+ optional header hợp lệ; `UnityPlayer.dll` không rỗng;
`SmallArena_Data` có `globalgamemanagers` và built scene `level0`/`data.unity3d`;
BuildReport đúng Unity version/target/scenes, backend Mono2x, `Succeeded`, errors 0, bytes > 0 và
build log có nội dung/không có project compiler warning/error. Đây là kiểm cấu trúc,
không chứng minh exe đã chạy hoặc KCP nhiều process đã hoạt động.

### CI, cache và gate

- Production build nằm trong job **`unity` / Unity EditMode and PlayMode** trên
  **windows-2022**, sau tests, hai XML floors và scaffold/compiler/configuration/layout
  gates. Gate trước đó fail thì không build/package/upload player. Timeout chung
  vẫn **60 phút**. Không còn job `windows-standalone-build` hay Linux cross-build.
- Builder SHA pin hiện hữu nhận cùng **`steps.image.outputs.image` digest** Windows
  Unity **6000.6.8f1** đã pull cho tests, `StandaloneWindows64`, production buildMethod
  và `versioning: None`. Windows script gọi Unity trực tiếp với
  `-quit -batchmode -nographics -executeMethod SmallArena.Tools.SmallArenaBuildPipeline.BuildWindowsStandalone`.
  Custom parameters ghi `Logs/WindowsStandalone/editor.log`, giữ worker count 2 và
  GC time slice 3. Image tên windows-il2cpp chứa module Windows; backend sản phẩm
  vẫn **Mono2x**, không đổi PlayerSettings theo tên image.
- Dùng đúng ba secrets hiện hữu `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`.
  Builder quản lý activation/return license cho container build riêng; không giả
  định license của container test còn hiệu lực sau tests. Không in secrets hay upload
  license/activation log/environment dump. Lỗi khởi động xem Actions log của GameCI;
  build Editor log ghi riêng vào artifact, không tự in ra terminal.
- Cache chỉ **Library/** với prefix **windows-native-tests-v1-**, key OS/arch/Unity,
  hash manifest/lock/ProjectSettings và SHA; restore prefix cùng cấu hình. Build
  mount cùng checkout/Library ấm vừa được tests import. Không restore cache Linux,
  không cache XML/build/log/secret và không xem cache hit là pass.
- Classifier yêu cầu job `unity` cho mọi path ngoài allowlist `docs/**`, `*.md` ở
  mọi cấp. `--no-renames` bảo vệ trường hợp di chuyển code vào docs. PR docs-only
  vẫn có Git diff, static matrix, CI Quality Gate; mọi push main chạy đầy đủ.
  Fork PR code không có license secrets vẫn không vượt gate.
- `CI Quality Gate` chờ `[diff, static, unity]`: diff/static phải success; unity
  phải success nếu `BUILD_REQUIRED=true`, chỉ được skipped nếu false. Thiếu classifier,
  failure/cancelled hoặc skip trên PR code bị từ chối. Required check vẫn là
  **CI Quality Gate**; không còn check riêng Windows Standalone Build.

### Artifact cho Phase 3

Sau build và output verification thành công, CI tạo artifact
**`windows-standalone-${github.sha}`**, giữ **3 ngày**:

```text
SmallArena-Windows-x64.zip
SHA256SUMS.txt
source-commit.txt
```

Giải nén ZIP vào một thư mục riêng sẽ có `SmallArena.exe`, `SmallArena_Data/`,
`UnityPlayer.dll`, các dependency build thực tế khác và `build-report.json` ngay
ở root. ZIP chứa trọn player output, không chỉ exe; upload dùng compression-level 0
để không nén lại ZIP. Phase 3 tải artifact **cùng workflow run/commit**, kiểm SHA256
và source commit rồi giải nén, chạy exe cùng data/DLL để điều phối Mirror/KCP.
PowerShell/.NET tạo ZIP toàn bộ output, đọc lại từng entry và so kích thước/SHA256
với file nguồn; sau đó ghi/đọc lại SHA256SUMS.txt để kiểm archive checksum.
Không cần zip/unzip/sha256sum Linux. Job smoke tương lai chạy exe
trên `windows-latest`, không gọi GameCI builder/Docker trên Windows.
Phase 2 chưa thêm tự động host/join hoặc instrumented smoke entry vào player.

Artifact chẩn đoán **`windows-standalone-build-logs-${github.sha}`** luôn được thử
upload, giữ **3 ngày**, chứa build log/report nếu đã được tạo. Khi license/Editor
không khởi động được, artifact có thể thiếu; job vẫn thất bại, không xem thiếu log
là thành công. `.gitignore` hiện hữu đã ignore root `Builds/`, `Logs/`, `Artifacts/`
và `TestResults/`; không thêm exe, DLL output, ZIP hoặc log vào Git.

### Kiểm chứng Phase 2 trước sửa cross-build (commit b134d1e)

- Scene preflight và wrapper `ValidateOnly` đạt trên Unity 6000.6.8f1 có Windows support.
- **21/21** regression cases đạt trên Windows PowerShell **5.1.26100.10068**:
  valid scenes/player layout; sai order/disabled/missing scene/GUID; missing/empty/bad
  exe; thiếu DLL/data/report/log; report hỏng/failed/errors/version/scenes sai và
  compiler warning và Unity analyzer warning. Fixture là dữ liệu synthetic, không phải player chạy thật.
- Strict Roslyn **7/7** project asmdefs đạt; Editor helper compile riêng với Unity
  6000.6.8f1 APIs, **0 warning/error**, không suppress. Scaffold **1.527** metadata pairs,
  GUID/build settings/packages và graph 7 asmdef đạt. PowerShell scripts và embedded
  workflow steps parse; YAML/needs/retention/path classification và ignore rules đạt.
- Native build **đã thành công**, process exit **0**, trên checkout riêng
  `TestResults/CIPhase2/ValidationProject`, dùng lại Library cache để giữ project
  chính đang có lock. `StandaloneWindows64`, backend **Mono2x**, Unity **6000.6.8f1**,
  **113.882.093 bytes**, `Succeeded`, **0 errors**, exe **667.136 bytes**; cả ba scene
  đúng order. Verifier chạy lại trên player thật và đạt.
- BuildReport có **15 warning từ `Assets/Mirror/`**, mã analyzer UAC0007/UAC0009/
  UAC1001/UAC1009/UAC1010. Chúng được giữ nguyên trong log, không suppress hay sửa vendor.
  **0 project compiler/analyzer diagnostics**; không mô tả thành zero warnings toàn build.
- Lượt Editor đầu bị `OutOfMemoryException`, exit **-2147483645**; probe helper cũng
  phát hiện kiểu count Unity API là `int`, đã sửa trước xác minh cuối. Retry tuần tự
  dùng source cuối compile đạt và tạo player thành công. Không dùng report từ lượt lỗi.
- Chạy chính block packaging trong YAML trên output thật: ZIP **44.652.528 bytes**,
  **203 entries**, có exe/runtime/data/report; SHA256 khớp file checksum. Marker local
  ghi `53e4741-local-phase2-uncommitted`, không giả là artifact đã commit/chạy GitHub.
  CI sẽ ghi `${github.sha}` thật. Raw proof nằm trong `TestResults/CIPhase2/` và output/log
  của validation copy, đều ignore. Đã kiểm **7/7** tình huống gate tổng hợp: required
  success, docs-only skip được nhận; required skip/failure/cancelled, mất classifier,
  Unity skipped đều bị từ chối.
- Classifier chạy bằng Git Bash với **7/7** ca: docs-only, asset, code, tool, workflow,
  asset chuyển vào docs và push main chỉ sửa README; kết quả build/skip đúng chính sách.
- Không chạy lại native EditMode/PlayMode trong Phase 2. Baseline lịch sử vẫn
  **261 EditMode pass / 408 PlayMode pass (406 project) / 2 vendor skips**, không gán
  số đó cho lượt chạy mới. GitHub Windows-latest/container/cache/artifact handoff
  chưa thực thi trong nhiệm vụ cục bộ này.

## 8. Lịch sử sửa incident Windows Docker — Ubuntu cross-build Mono

Mục này ghi phương án cũ; workflow hiện hành đã chuyển production build sang job
`unity` Windows theo mục 7 và phần Direction1 cuối tài liệu. Các lệnh PR/cross-build
dưới đây là lịch sử, không phải cấu hình CI hiện hành.

Baseline sửa lỗi: `b134d1e`, nhánh `feat/ci-phase2-standalone-build`, ngày 23/11/2026.
Job build cũ chạy GameCI trên Windows và không kết nối được Docker daemon
`npipe:////./pipe/docker_engine`. Job này chuyển sang **ubuntu-latest** với Docker
Linux có sẵn trên runner; không thêm Dockerfile hoặc yêu cầu Docker Desktop trên
máy Windows phát triển. Builder vẫn tạo **Windows x64 player**, không phải Linux player.

### Backend và tính tương thích

- `ProjectSettings/ProjectSettings.asset` chỉ ghi `scriptingBackend: Android: 1`;
  **không có dòng Standalone override**. Không được mô tả nhầm là file chứa
  `Standalone: 0`. Standalone dùng mặc định Mono; BuildReport Windows đã thực thi
  ở mục 7 xác nhận `Backend: Mono2x` với chính cấu hình này.
- Editor helper thêm **read-only guard** `GetScriptingBackend(NamedBuildTarget.Standalone)
  == ScriptingImplementation.Mono2x`. Nếu config đổi sang IL2CPP, build thất bại rõ;
  helper không gọi SetScriptingBackend hoặc sửa PlayerSettings để vượt gate.
- Job dùng image chính thức **`unityci/editor:ubuntu-6000.6.8f1-windows-mono-3`**,
  `targetPlatform: StandaloneWindows64`, cùng buildMethod/scene order/output path.
  [GameCI v4 image selection](https://github.com/game-ci/unity-builder/blob/4423ee75828dff56037d1d1cfdac6b68e3a6ba00/src/model/image-tag.ts)
  cũng ánh xạ Windows target trên Linux sang windows-mono. Chỉ định customImage
  giúp review dễ thấy rõ host/target/backend, không đổi production backend.
- `tools/Build-WindowsStandalone.ps1` giữ **nguyên từng byte** và vẫn khởi chạy
  Unity native trên Windows. C# helper dùng Path.Combine/Application.dataPath và
  Unity Editor APIs chung cho cả hai host; vẫn bắt đúng MainMenu → Bootstrap → Arena_MVP,
  BuildResult.Succeeded và errors 0. Không sửa runtime, scenes, asmdef hoặc ProjectSettings.

### Xác minh và handoff

- Cache chỉ Library, prefix **windows-mono-cross-v2**, key OS/arch/Unity + manifest,
  packages-lock, ProjectSettings hash + SHA; không restore cache Windows cũ vào Linux.
- Post-build vẫn dùng verifier PowerShell portable chạy `pwsh` của Ubuntu. Kiểm DOS/MZ,
  e_lfanew trong bounds, PE signature, machine AMD64, PE32+ optional header; không nhận
  file chỉ có hai byte MZ hoặc exe x86. Data phải có globalgamemanagers và built scene,
  BuildReport phải ghi Mono2x. Test giả lập kiểm cả thiếu/corrupt header và sai backend.
- Bash package toàn bộ output, `unzip -tq` kiểm ZIP, `sha256sum --check` kiểm checksum
  ngay trên runner. Artifact `windows-standalone-${github.sha}` và diagnostics vẫn
  **3 ngày**. Phase 3 `multiplayer-smoke-test` sẽ chạy trên **windows-latest**: tải artifact
  cùng run/SHA, kiểm checksum, giải nén và chạy exe; không build/Docker trên Windows.
- Classifier, docs-only PR skip, job IDs, dependencies và `CI Quality Gate` **giữ nguyên**.
  Đã so sánh YAML các job diff/static/unity/quality với HEAD, không đổi nội dung.

Kết quả cục bộ của lượt sửa: **30/30** validator regressions đạt trên Windows PowerShell
5.1; verifier mới đọc được player Windows thật đã build trước đó và xác nhận PE32+/Mono,
built scenes/report/log. Strict Roslyn **7/7** project assemblies và Editor helper mới
đều **0 project warning/error**. YAML, Bash syntax, PowerShell syntax và workflow
contracts/cache/artifact retention đạt. Không gán kết quả local Windows trước đó thành
bằng chứng Ubuntu cross-build đã chạy. Máy này không có công cụ zip trong Git Bash;
chỉ kiểm syntax block Bash mới, chưa thực thi nó hoặc Unity image Linux cục bộ.

**Giới hạn riêng Phase 1:** job `unity` hiện vẫn dùng `windows-2022` và lệnh Docker
Windows. Nhiệm vụ này chỉ sửa windows-standalone-build; job test đó có thể độc lập
chặn CI Quality Gate và cần migration riêng nếu runner không có daemon. Không bỏ
test/giảm ngưỡng hoặc đổi gate để che tình trạng này. Chưa bắt đầu Phase 3.

### Đưa fix lên PR và xác nhận trước Phase 3

Các lệnh dưới đây dành cho người review thực hiện; agent không tự commit/push:

```powershell
git switch feat/ci-phase2-standalone-build
git diff --check
git add .github/workflows/unity-ci.yml Assets/_Project/Scripts/Editor/Build/SmallArenaBuildPipeline.cs tools/Assert-WindowsStandaloneBuild.ps1 tools/Test-WindowsStandaloneBuild.ps1 docs/ci/ci-pipeline.md .github/workflows/README.md
git commit -m "fix(ci): cross-build Windows Mono player on Ubuntu"
git push origin feat/ci-phase2-standalone-build
gh run list --branch feat/ci-phase2-standalone-build --workflow unity-ci.yml --limit 5
```

Push cùng branch cập nhật PR đang mở. Trong run mới, xác nhận **Windows Standalone
Build** chạy Ubuntu/image windows-mono, build step và output verification đều success,
BuildReport ghi StandaloneWindows64/Mono2x/Succeeded/errors 0, rồi có artifact ZIP và
diagnostics. Có thể xem riêng job bằng `gh run view <RUN_ID> --json jobs`.

Tải artifact player đúng run từ Actions (hoặc `gh run download <RUN_ID> --name
<ARTIFACT_NAME> --dir Artifacts/Phase2Check`), sau đó kiểm trên Windows:

```powershell
$root = 'Artifacts/Phase2Check'
$zip = Join-Path $root 'SmallArena-Windows-x64.zip'
$expected = ((Get-Content (Join-Path $root 'SHA256SUMS.txt') -Raw).Trim() -split '\s+')[0]
if ((Get-FileHash $zip -Algorithm SHA256).Hash -ne $expected) { throw 'Checksum mismatch.' }
Get-Content (Join-Path $root 'source-commit.txt')
Expand-Archive -LiteralPath $zip -DestinationPath (Join-Path $root 'Player')
Get-Item (Join-Path $root 'Player/SmallArena.exe')
Get-Content (Join-Path $root 'Player/build-report.json')
```

Giữ toàn bộ Player directory, gồm SmallArena_Data/UnityPlayer.dll và dependencies;
không chỉ chép exe. CI hiện giới hạn Phase 1 + Phase 2. Harness bốn tiến trình ở
mục 9 là công cụ cục bộ, không phải job Phase 3 hay required CI check mới.

## 9. TV1 #18 — skip policy và nghiệm thu cục bộ #25/#27

Baseline nguồn sản phẩm: `8fba1c127f650615faec4f14b34b16da7552c858`, Unity6000.6.8f1.
Không suy kết quả hiện hành từ các bảng lịch sử phía trên. Báo cáo đợt này:
[four-player-acceptance-report.md](../testing/four-player-acceptance-report.md).

`Assert-UnityTestResults.ps1` giữ kiểm XML/counters/DTD/error/setup/leaf reconciliation,
thêm project attribution qua fullname/classname `SmallArena.Tests.*`, ancestor Assembly
`SmallArena.Tests.*` và hai tên asmdef thực tế `SmallArena.EditMode.Tests` /
`SmallArena.PlayMode.Tests`. Project skip bị từ chối dù vendor policy cho phép namespace
của leaf. Project pass floor độc lập, không lấy vendor pass bù vào.

Vendor skip phải thuộc prefix **UnityEngine. / Unity. / UnityEditor.** theo fullname,
classname hoặc tên Assembly; cần reason/message không rỗng và tối đa **10** skip.
Hai ca Windows/RDP Input System có namespace IntegrationTests được nhận qua Assembly
`Unity.InputSystem.IntegrationTests.dll`, reason `Unstable due to 1252825`; không mở rộng
prefix thành `IntegrationTests.` hoặc bỏ kiểm tra reason để nhận chúng.

| Tham số | Mặc định / hành vi |
|---|---|
| `MinPassedCount` | 1, không chấp nhận 0; toàn skip luôn fail |
| `MinProjectPassedCount` | 0 khi gọi cục bộ; CI EditMode **261**, PlayMode **406** |
| `AllowedVendorSkipPrefixes` | `UnityEngine.`, `Unity.`, `UnityEditor.` |
| `MaxVendorSkippedCount` | 10, có thể đặt 0 |
| `RequireSkipReason` | `$true`; chỉ tắt khi caller chủ động truyền `$false`, CI giữ bật |
| `XmlPath` | Tương thích invocation một XML; không cần ResultsRoot/TestPlatform |

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Test-AssertUnityTestResults.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Assert-UnityTestResults.ps1 -XmlPath TestResults/TV1Acceptance/Native/EditMode.xml -MinPassedCount 261 -MinProjectPassedCount 261
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Assert-UnityTestResults.ps1 -XmlPath TestResults/TV1Acceptance/Native/PlayMode.xml -MinPassedCount 408 -MinProjectPassedCount 406
```

Regression hiện có **55/55 passed**, process exit **0**, negative cases trả đúng **1**;
giữ 34 fixture gốc và thêm namespace/assembly attribution, skip allowlist/reason/budget,
project floor, custom policy và XmlPath compatibility. Workflow chỉ thêm hai project
floors vào hai XML steps `if: always()`, không đổi job khác hay hạ aggregate gate.

Harness cục bộ có explicit build symbol `SMALL_ARENA_FOUR_PLAYER_HARNESS` và CLI flag
`--four-player-harness`. Source nằm ngoài Assets, chỉ copy vào bản validation dưới
TestResults để build; sản phẩm thông thường không chứa instrumentation. Không đặt
PlayerSettings backend/stripping hoặc sửa gameplay để làm pass. Chạy native tests và
build cần license; chạy bốn player đã build không cần Editor/license.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Test-InvokeFourPlayerHarness.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Build-FourPlayerHarness.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Invoke-FourPlayerHarness.ps1 -Rounds 3
# Port tự chọn mặc định; có thể truyền -Port 7777. Không nối harness vào GitHub Actions.
```

Mỗi run có UUID riêng, source manifests, build identity và process logs trong
`TestResults/FourPlayerHarness/`. Job Object chỉ quản lý bốn process do runner tạo
và descendants, có watchdog độc lập; finally chờ job trống và bind độc quyền lại UDP
port trước khi nhận Passed. Protocol điều khiển dùng sequence files bất biến; snapshot
dùng append-only JSONL5Hz, rotate mỗi250records, chỉ đọc record hoàn chỉnh và fail nếu ngừng cập nhật quá 10s.
Không cho phép skip/missing snapshots biến thành dữ liệu 0 hoặc chứng nhận hội tụ.

Kết quả localhost/headless không chứng nhận LAN, impairment, hình ảnh hay hoàn thành
PR #13 / oracle #24 / QA #26. Không tự đóng issue hoặc merge; #27 chỉ được ký sau
khi dependency và bằng chứng cùng SHA được các owner review.

Kết quả mới của đợt này: regression harness **56/56**, native EditMode277 pass,
PlayMode414 pass/2 vendor skip, instrumented Mono build0 errors/0 warnings.
Localhost bốn player thật hoàn thành **3/3 rounds + client/host teardown**, exit0,
run `4d4cb053fd56416fa97fd3ca8186260e`; max clock spread0,183411s trên23 checkpoints.
Production baseline8fba1c1 cùng tooling overlay chưa commit được ghi hash riêng.
Chi tiết identity/source hashes, failed development runs và các blocker trong báo cáo
liên kết trên; #27 vẫn BLOCKED, không ký thay owner từ kết quả localhost này.

### PR #38 — khôi phục Docker và cô lập cache build

Job `unity` trên `windows-2022` kiểm tra/start service `docker` nếu cần, rồi poll
`docker info` mỗi 2 giây trong tối đa 60 giây trước khi pull Editor image. Nếu chưa
sẵn sàng, job in service/version và các Application/System events liên quan Docker,
rồi fail; không bỏ qua XML hoặc compilation gates.

Cache `Library` của Ubuntu cross-build chuyển sang prefix `windows-mono-cross-v3-`
ở cả key và restore-keys để không dùng lại snapshot v2 nghi vấn. Đây là phép cô lập
cache, chưa chứng minh cache v2 hỏng hoặc exit137 là OOM. Không thêm swap/memory limits
của PR #35. Khi lỗi, diagnostic đọc memory/swap host, cgroup counters còn tồn tại,
Docker status/exit/OOMKilled/error và kernel OOM messages. GameCI dùng `--rm`, nên
container/cgroup có thể đã bị xóa; thiếu counters không chứng minh không có OOM.

CI vẫn Phase1+Phase2, giữ project floors261/406 và toàn bộ verification gates.
Sau khi người dùng review/commit/push PR #38, kiểm readiness step, cache v3 hit/miss,
tests/XML và build thực trên GitHub; local syntax/regression không thay bằng chứng CI.

### Ổn định tài nguyên và giảm thời gian CI — Direction1

Nhánh `fix/ci-stability-and-speed`: giữ concurrency theo workflow + số PR/ref,
`cancel-in-progress`, docs-only classifier và cache **windows-native-tests-v1-**.
Không dùng paths-ignore ở trigger nên các check vẫn xuất hiện trên PR tài liệu.

Sau Direction2 vẫn gặp exit137, production Windows x64 Mono build được chuyển
vào job `unity` trên **windows-2022** sau tất cả test/XML/strict compilation gates.
Builder tái sử dụng chính Windows image digest đã pull và Library ấm trong cùng
checkout; action quản lý activation/return license cho container build riêng.
Không còn Linux Windows cross-build, nên bỏ hẳn đường chạy Linux SIGKILL này cùng
swap/drop-page-cache/cgroup steps. Điều đó không đảm bảo Windows không thể thiếu
RAM và chưa chứng minh giảm thời gian CI một nửa; cần so sánh run thực trên GitHub.

Giữ preflight MainMenu → Bootstrap → Arena_MVP; verifier kiểm PE32+/AMD64, runtime
DLL/data/scenes, BuildReport StandaloneWindows64/Mono2x/Succeeded/errors0, build log
và project diagnostics. PowerShell/.NET ZIP kiểm từng entry và SHA256; artifact
player/build diagnostics vẫn **3 ngày**, test artifacts **7 ngày**. Logs luôn được
thử upload kể cả build lỗi; không upload secrets/activation logs.

Aggregate chỉ chờ diff/static/unity; với code PR hoặc push main, success của unity
nay bao gồm production build/verification/package/upload. PR docs-only chỉ nhận
unity skipped có classifier hợp lệ. Job name **CI Quality Gate** giữ nguyên;
không còn job/check riêng Windows Standalone Build.

CI vẫn chỉ **Phase1 + Phase2**, không thêm smoke/harness. Project pass floors
**261/406**, zero project skips, XML `always()` và strict compilation giữ nguyên.
Local syntax/logic/package regression không thay bằng chứng native Windows Docker
build/test/license trên Actions; không gán kết quả lịch sử thành lượt chạy mới.
