# CI — Quality gates, Windows build và multiplayer smoke

Phase 2 bổ sung Windows player build (mục 7–8); Phase 3 thêm multiplayer smoke (mục 9).
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
  +-- Unity EditMode and PlayMode (windows-2022, nhánh nội bộ)
        resolve/pull Editor image -> cố định digest
        GameCI native PlayMode + EditMode -> XML validations luôn chạy
        cùng image + checkout/Library đã import:
          Editor/scaffold -> strict 7-asmdef compile -> physics/URP -> arena/prefab/UI
        upload kết quả luôn chạy
  +-- Windows Standalone Build (ubuntu-latest cross-build Mono, nhánh nội bộ)
        code/asset/tool/workflow PR hoặc mọi push main -> Library cache -> build
        verify output + BuildReport -> ZIP/SHA256/source commit -> artifact 3 ngày
        PR chỉ sửa tài liệu được bỏ qua riêng job build
  +-- Multiplayer Smoke Test (windows-latest, needs Windows Standalone Build)
        download cùng run/SHA -> checksum -> 2 process KCP -> combat + crash client
        Job Object/watchdog/finally cleanup -> logs/results giữ 7 ngày
  \-- CI Quality Gate: tests/static phải success; build + smoke phải success khi được yêu cầu
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
chấp nhận `success` của Git diff, toàn bộ static matrix và Unity. Windows build cũng
phải `success` khi classifier yêu cầu; chỉ được `skipped` nếu PR chỉ sửa tài liệu.
`failure`/`cancelled`, thiếu classifier hoặc skip build của PR code đều không đạt.
Có thể chọn thêm các check chi tiết sau để review trực quan:

1. `Git diff`
2. `Static quality (ubuntu-24.04)`
3. `Static quality (windows-2022)`
4. `Unity EditMode and PlayMode`
5. `CI Quality Gate`

Sau lượt chạy thật đầu tiên, chọn đúng tên hiển thị trong Settings → Branches/Rulesets,
yêu cầu PR + review, resolve conversations, nhánh cập nhật với main và các checks trên.
Không coi một job Unity bị skip là đủ điều kiện merge. Với fork, thành viên review
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

- Job **`windows-standalone-build` / Windows Standalone Build** chạy trên
  `ubuntu-latest`, song song với Unity tests sau job Git diff; timeout **60 phút**.
  Dùng GameCI unity-builder SHA `4423ee75828dff56037d1d1cfdac6b68e3a6ba00`, Unity
  **6000.6.8f1**, `StandaloneWindows64`, production buildMethod và `versioning: None`.
  Image chính thức: `unityci/editor:ubuntu-6000.6.8f1-windows-mono-3`.
  Linux Editor cross-build Windows Mono; không cần Windows Docker daemon hoặc Docker Desktop trên máy lập trình.
  Action có sẵn `-quit -batchmode -nographics`; không dùng `manualExit` hay GPU mode.
- Dùng đúng ba secrets hiện hữu `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`.
  Không in giá trị, không upload license/environment dump. Report log chỉ ghi bước
  build sau khi Editor đã khởi động; lỗi license/import trước entry point xem Actions
  log của GameCI. Full local Editor log không được in tự động ra terminal.
- Cache **chỉ `Library/`** bằng actions/cache SHA pin. Key bao gồm OS/architecture,
  Unity version, hash `Packages/manifest.json`, `Packages/packages-lock.json`,
  `ProjectSettings/**` và commit SHA. Restore prefix cùng config hash cho phép reuse
  cache khi chỉ source/assets thay đổi; Unity vẫn kiểm dependency/import hiện hành.
  Prefix `windows-mono-cross-v2` và runner OS tách cache Linux khỏi cache Windows cũ.
  Không cache Builds, ZIP, Logs, Secrets hoặc xem cache hit là build đã đạt.
- Classifier mặc định yêu cầu build cho mọi path ngoài allowlist `docs/**`,
  `README.md`, `.github/workflows/README.md`. Git diff dùng `--no-renames` để asset/code
  bị chuyển vào docs vẫn tính là xóa code, không bị nhận nhầm thành docs-only.
  Mọi **push main luôn build**, kể cả tài liệu. PR fork không được cấp secrets và
  code PR fork không vượt gate; quy trình review/nhánh nội bộ giữ như Phase 1.
- `CI Quality Gate` giữ kiểm tra O(1), không chạy lại tests/build. Nó chờ build cần thiết
  hoàn tất; chỉ chấp nhận skip khi classifier nói docs-only, không có default-green
  khi thiếu output hoặc job lỗi. Nên required **CI Quality Gate**; không cần required
  riêng Windows Standalone Build vì docs-only PR hợp lệ có job bị skip.

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
Job build dùng Bash `zip`, `unzip -tq` và `sha256sum --check` trước upload để kiểm
archive/checksum thực sự, không chỉ tạo file checksum. Job smoke tương lai chạy exe
trên `windows-latest`, không gọi GameCI builder/Docker trên Windows.
Phase 3 bên dưới thêm CLI opt-in vào cùng production player; không chèn smoke define hoặc đổi scene/prefab.

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

## 8. Sửa incident Windows Docker — Ubuntu cross-build Mono

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

### Đưa fix Phase 2 lên PR và xác nhận artifact

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
không chỉ chép exe. Chỉ chuyển sang Phase 3 sau khi job Ubuntu và artifact của đúng
commit được xác nhận thực tế; YAML/local compile không thay bằng chứng Actions.

## 9. Phase 3 — Multiplayer smoke nhiều process Mirror/KCP

Baseline `83fdb7b` (main đã merge PR #33), nhánh `feat/ci-phase3-multiplayer-smoke`.
Giữ 7 asmdef, Domain/Protocol engine-free; không sửa luật movement/combat/health,
constants, các test Unity, prefab/scene hay production settings. Thêm một MonoBehaviour
automation trong Networking và tooling ngoài Assets. NetworkPlayer vẫn là bridge mỏng.

### CLI opt-in và marker

`MultiplayerSmokeAutomation` chỉ được tạo khi có **--auto-host** hoặc **--auto-join**.
Không có role flag thì không tạo object, đăng ký handler, sửa transport, frame rate
hoặc khởi động session. Không dùng `SMALL_ARENA_M2_SMOKE` và không chép script
instrumented vào Client khi build. Hai process dùng cùng player của Phase 2.

| Flag | Hành vi khi đã bật auto-role |
|---|---|
| `--auto-host` | Gọi production `ArenaNetworkManager.HostRoom()`. |
| `--auto-join 127.0.0.1` | Gọi production `JoinRoom(address)`, không tạo player ở client. |
| `--port <1..65535>` | Đặt KCP qua PortTransport trước khi mở connection; mặc định 7777, runner luôn chọn port động riêng. |
| `--auto-ready` | Gửi RequestReady qua lobby protocol, chờ synchronized Ready acknowledgement. |
| `--auto-start` | Chỉ Host yêu cầu Start khi đủ expected players và server-published CanStart; server vẫn kiểm preconditions. |
| `--expected-players N` | Mặc định 2, giới hạn room 2–4; combat-duel yêu cầu đúng 2. |
| `--smoke-action combat-duel` | Server chọn cặp capsule positions có floor/clearance, teleport bằng ArenaNetworkTransform; remote owner gửi movement/attack Commands thật. |
| `--smoke-action hold` | Giữ match cho runner kill Client; Host xác nhận disconnect/cleanup và clock/control còn hoạt động. |
| `--max-runtime-seconds N` | Deadline process 5–300 giây; hết giờ phát FAILURE và exit 1. Runner đặt deadline ngắn hơn watchdog ngoài 5 giây. |

Role mặc định dùng **MainMenu lobby root → Arena_MVP**, đúng workflow GUI hiện hành.
Bootstrap vẫn trong build settings index 1 và thuộc entry path legacy `--arena-*`;
không giả vờ load Bootstrap trong room flow hoặc dùng legacy flags để bỏ qua Lobby.

Các marker single-line `[SMOKE_EVENT]` gồm GAME_ENTRY, HOST_STARTED, CLIENT_CONNECTED,
LOBBY_READY, AVATARS_SPAWNED, MATCH_COUNTDOWN, MATCH_IN_PROGRESS, MOVEMENT_VERIFIED,
COMBAT_VERIFIED, PEER_DISCONNECTED, SERVER_RESILIENT, CLEAN_SHUTDOWN và FAILURE.
Marker ghi cả Unity log và Console; Unity có thể chuyển Console vào cùng player log,
nên parser chấp nhận bản lặp. Parser kiểm tên chính xác và thứ tự Countdown → InProgress,
không đếm duplicate thành assertion/pass mới.

Combat setup chỉ chạy trên Server và chỉ khi explicit combat-duel: chọn hai capsule
được environment-clearance kiểm chứng, không để client gửi pose. Sau đó movement
được kiểm **displacement ngang > 0,08 m**, không dùng gravity làm bằng chứng movement.
Remote Client gọi `NetworkMovement.SubmitInput` và `NetworkPlayer.RequestAttack`;
Command authority, lifecycle, cooldown, overlap/range/obstacle checks và damage code
production vẫn nguyên. Server và Client phải cùng thấy victim **10 → 9 HP**, attacker
**10 HP**, scoreboard **DamageDealt = 1**. Ack riêng của automation chỉ xác nhận trạng
thái đã được Server kiểm độc lập, không cấp damage hoặc ghi HP từ client.

### Runner, watchdog và kết quả

```powershell
# Sau khi build mới bằng Phase 2, hoặc giải nén artifact đúng commit vào Builds/WindowsStandalone.
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Test-InvokeMultiplayerSmoke.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Invoke-MultiplayerSmoke.ps1
# Tùy chọn:
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Invoke-MultiplayerSmoke.ps1 -TimeoutSeconds 90 -Player Builds/WindowsStandalone/SmallArena.exe
```

- **Combat:** Host bắt đầu KCP, rồi Client join, hai bên Ready, Countdown/InProgress,
  hai avatars, authoritative movement/damage và replica/score nhất quán. Reliable
  completion/ack cho phép client đóng connection sạch; Host chờ peer được cleanup
  rồi shutdown. Cả hai process phải exit **0**.
- **Disconnect:** Hai process vào InProgress, runner kill **chỉ Client mình đã tạo**.
  Host phải nhận KCP timeout/disconnect tự nhiên (không gọi giả OnServerDisconnect),
  connection/identity/proxy biến mất, participant Disconnected và còn đúng một avatar.
  Clock tiếp tục tick, Host vẫn CanAct và exit **0**. Client exit khác 0 chỉ hợp lệ
  trong nhánh này và khi runner đã chủ động kill.
- Mỗi scenario có `try/finally`, đồng hồ monotonic và watchdog **90 giây** mặc định.
  Helper Windows 10+/Server 2016+ dùng atomic job-list process creation + suspended
  startup, `KILL_ON_JOB_CLOSE`, Timer độc lập gọi TerminateJobObject. Khi runner bị
  dừng đột ngột, handle job không được child kế thừa; không có cửa sổ child chạy
  ngoài job trước assignment. Dispose chờ job ActiveProcesses=0, rồi runner xác nhận
  workers kết thúc và bind độc quyền lại UDP port. Chỉ ghi `result.json: Passed` **sau**
  cleanup thành công. Không enumerate/kill player hoặc Editor của người dùng.
  Cơ chế theo [Job Objects](https://learn.microsoft.com/en-us/windows/win32/procthread/job-objects)
  và [PROC_THREAD_ATTRIBUTE_JOB_LIST](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute).
- Mỗi lượt có thư mục UUID mới dưới `TestResults/MultiplayerSmoke/Combat/` hoặc
  `Disconnect/`; gồm host/client `.log`, `.stdout.log`, `.stderr.log`, `.pid` và
  `result.json` khi đạt. Không nhận marker/report cũ. Exception/fatal/FAILURE,
  marker thiếu hoặc sai thứ tự, exit code sai, watchdog/cleanup/port lỗi đều nonzero.

### CI và điều kiện merge

Job **multiplayer-smoke-test / Multiplayer Smoke Test**, `windows-latest`, chỉ needs
windows-standalone-build và chạy khi build success. Checkout dùng source cùng run,
download artifact `windows-standalone-${github.sha}`, kiểm SHA256 và source-commit,
giải nén rồi gọi runner PowerShell. **Không cài Unity Editor, Docker hoặc activate
license** trong job smoke. Job timeout 8 phút bao ngoài hai watchdog scenario.
Logs/results upload `if: always()`, artifact `multiplayer-smoke-${github.sha}`, giữ
**7 ngày**. Không thêm secrets vào job và không commit binary/evidence.

CI Quality Gate nhận thêm SMOKE_RESULT: code/asset/main build cần thiết thì build
và smoke đều phải success; docs-only thì cả hai phải skipped. Giữ classifier và
EditMode/PlayMode gates, không dùng continue-on-error hoặc bỏ tests để có xanh.
Windows Docker của job Phase 1 unity vẫn là vấn đề riêng đã ghi ở mục 8; không
được coi smoke success là native test job success. Cấu hình/giữ ngưỡng kiểm thử không thay đổi.

### Bằng chứng thực thi cục bộ

Đã chạy thật player Windows từ `Builds/WindowsStandalone/SmallArena.exe`; source
CLI hook trùng SHA256 với snapshot dùng build. Build Unity **6000.6.8f1 / Mono2x**
thành công, **0 errors / 0 warnings**, tổng **113.896.092 bytes**. Không dùng player
instrumented hoặc report cũ để vượt smoke.

| Kiểm chứng cuối | Kết quả |
|---|---|
| Strict Roslyn project compile | **7/7** asmdef, **0 project warnings/errors**; Mirror compile riêng, Domain engine-free. |
| Parser/exit/timeout/order + native watchdog tests | **25/25 pass**, Windows PowerShell **5.1.26100.10068**; có child thực bị kill exit 124 và Job disposal không để child sống. |
| Combat, UDP **62367** | **45,23s**, Host exit **0**, Client exit **0**, marker HP **9**, damage **1**, movement ngang authoritative/replicated đạt; không exception/fatal. |
| Abrupt disconnect, UDP **53289** | **25,98s**, Host exit **0**, Client exit **-1** do runner kill; orphan **0**, participant Disconnected, clock ticking/control allowed, clean host shutdown. |
| Cleanup cả hai scenario | **0 leftover workers**, Job ActiveProcesses về 0, bind độc quyền lại từng UDP port đạt; runner process exit **0**. |
| Scaffold và workflow | **1.528** metadata pairs/GUIDs, 7-asmdef acyclic graph; YAML/PowerShell parse đạt; **8/8** aggregate build/smoke/test/skip cases đúng. |

Raw proof cuối: `TestResults/MultiplayerSmoke/Combat/eb2f44e48b434cf1982c24f62702edaf/`
và `TestResults/MultiplayerSmoke/Disconnect/2d72f60a53ea4a8a8385658eb409e86c/`.
Lượt đầu cũng đạt cả hai case, nhưng bảng trên dùng lượt cuối đã kiểm ngang và
atomic job assignment. Log/stdout/stderr/PID/result.json và build/compile artifacts
được ignore. Không commit exe, DLL, log, report hoặc context cá nhân.

Chưa chạy workflow trên GitHub, PowerShell 7/Ubuntu matrix hoặc smoke trên artifact
cross-build Ubuntu trong nhiệm vụ này; mới kiểm YAML/PowerShell và thực thi thật local
Windows. Không phát hành hoặc tự commit/push. Job Phase 1 Windows Docker vẫn là
giới hạn riêng; không dùng smoke pass để bỏ gate đó.

Native suites lịch sử 261 EditMode / 408 PlayMode pass (406 project, 2 vendor skip)
không được cộng vào smoke counts hoặc gọi là native suites mới đã chạy trong Phase 3.

## 10. Gia cố bộ nhớ Ubuntu cho Windows cross-build — exit 137

Baseline `bcafcd8cc682ffd3c3c3fe00b515d31862e16b7e` (merge PR #34), nhánh
`fix/ci-ubuntu-oom-137`, ngày 23/11/2026. Incident xảy ra sau khi license được chấp
nhận, trước khi build hoàn tất; job smoke bị skip vì phụ thuộc build. Exit **137**
biểu thị SIGKILL; thiếu RAM/swap hoặc shared memory là hướng điều tra, nhưng phải
đối chiếu kernel log để xác nhận OOM, không kết luận chỉ từ exit code.

Chỉ sửa job `windows-standalone-build`; giữ nguyên các job khác, dependency graph,
docs-only skip, test thresholds, Mono backend, Editor build helper và wrapper Windows.
Không thêm Dockerfile hay yêu cầu Docker Desktop trên máy lập trình viên.

| Thay đổi | Cấu hình và lý do |
|---|---|
| Swap của runner | Tạo thêm **8 GiB** tại `/swapfile-smallarena-unity-ci` trước GameCI; `chmod 600`, `mkswap`, `swapon`. Giữ swap có sẵn. Kiểm còn đủ 8 GiB cộng **4 GiB** dự phòng trên `/`; thiếu dung lượng hoặc trùng đường dẫn thì fail rõ ràng, không ghi đè. |
| Giới hạn RAM container | `dockerMemoryLimit: 12g`, thay mặc định khoảng 95% RAM host, chừa RAM cho runner/Docker trên máy khoảng 16 GiB. Không vô hiệu hóa OOM killer. |
| Shared memory | `dockerShmSize: 2g` tạo `--shm-size=2g`; không dùng input không được hỗ trợ `dockerRunOptions`. Đây là giới hạn tmpfs, không phải RAM được cấp thêm. |
| Unity jobs | `customParameters: -job-worker-count 2` giảm song song của Unity job workers trong import/build; không khẳng định giới hạn mọi tiến trình importer/shader riêng biệt. |
| Library cache | Tách `actions/cache/restore` và `actions/cache/save`, cùng SHA pin và nguyên key/prefix Linux/Mono/Unity/config/commit. Log exact hit và matched key. Prefix hit vẫn là cache đã restore dù `cache-hit` không phải `true`. Chỉ save khi Unity build success và chưa exact-hit; không lưu Library bị ngắt giữa chừng bởi SIGKILL. |
| Diagnostics | `if: failure()` in RAM/swap/disk, tối đa 80 dòng kernel có `killed process/oom/out of memory` nếu được phép, và 80 dòng cuối của BuildReport/build log. Log chưa được tạo trước executeMethod hoặc kernel bị chặn đều có thông báo riêng. Không dump environment, ULF hay license credentials. |

Input được đối chiếu trực tiếp với [action.yml của GameCI tại SHA đang pin](https://github.com/game-ci/unity-builder/blob/4423ee75828dff56037d1d1cfdac6b68e3a6ba00/action.yml)
và [cách tạo Docker flags](https://github.com/game-ci/unity-builder/blob/4423ee75828dff56037d1d1cfdac6b68e3a6ba00/src/model/docker.ts).
Theo [Docker memory/swap constraints](https://docs.docker.com/engine/containers/resource_constraints/),
khi đặt `--memory` mà không đặt `--memory-swap`, container có thể dùng swap đến mức
tương ứng giới hạn memory nếu host có swap. Vì vậy swap runner có thể giúp container
vượt đỉnh RAM tạm thời; không bảo đảm mọi build sẽ tránh OOM hoặc tránh timeout.
Swap chỉ áp dụng VM CI dùng một lần, không chạy trên máy Windows của lập trình viên.

### Kiểm chứng cục bộ của bản sửa

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Test-AssertUnityTestResults.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Test-WindowsStandaloneBuild.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Test-InvokeMultiplayerSmoke.ps1
git diff --check
```

| Kiểm tra mới thực thi | Kết quả |
|---|---|
| XML validator, Windows PowerShell 5.1 | **34/34 pass**, 0 failed |
| Windows build validator, Windows PowerShell 5.1 | **30/30 pass**, 0 failed |
| Smoke parser/exit/watchdog/native child cleanup | **25/25 pass**, 0 failed |
| YAML + contract theo metadata GameCI SHA pin | Đạt; inputs hợp lệ, cache key giữ nguyên, các job khác không đổi |
| `bash -n` trên các Bash blocks của job | **4/4** blocks riêng biệt đạt (6 tệp kiểm tra gồm 2 bản sao dùng cho mock) |
| Bash checks có mock, không chạy sudo/swap thật | **7/7** đạt: swap success, thiếu disk, path tồn tại, swapon lỗi, kernel OOM/không OOM/bị từ chối; diagnostics chịu được report/log chưa tồn tại |

Bằng chứng tạm tại `TestResults/CI-OOM137/` bị ignore. Đây là kiểm tra workflow và
validator; không phải lượt chạy Unity build, compilation hay EditMode/PlayMode mới.
Chưa cấp swap, chạy Docker Unity Ubuntu hoặc chạy GitHub Actions trong nhiệm vụ này.
Không dùng số liệu native suites lịch sử làm kết quả kiểm thử mới.

### Push và xác nhận artifact-to-smoke trên GitHub

Các lệnh dưới đây dành cho người review thực thi; chỉ stage hai tệp được sửa, không
stage `AGENTS.md`, `PROJECT_CONTEXT.md`, logs, license hoặc binaries:

```powershell
git switch fix/ci-ubuntu-oom-137
git diff --check
git diff -- .github/workflows/unity-ci.yml docs/ci/ci-pipeline.md
git add -- .github/workflows/unity-ci.yml docs/ci/ci-pipeline.md
git commit -m "fix(ci): reduce Unity cross-build memory pressure and diagnose SIGKILL"
git push -u origin fix/ci-ubuntu-oom-137
# Tạo PR nếu nhánh chưa có PR; workflow chạy theo trigger pull_request hiện có.
gh pr create --base main --head fix/ci-ubuntu-oom-137 --title "Fix Unity cross-build exit 137" --body "Add runner swap, bounded Unity workers/container memory, safe Library caching and failure diagnostics."
gh run list --branch fix/ci-ubuntu-oom-137 --workflow unity-ci.yml --limit 5
# Thay RUN_ID bằng ID của run chứa commit vừa push:
gh run watch RUN_ID --exit-status
gh run view RUN_ID --json headSha,jobs
```

Trong logs: swap tăng thêm 8 GiB, cache matched key/exact hit rõ ràng, Docker nhận
`--shm-size=2g`/`--memory=12g`, Unity nhận `-job-worker-count 2`, không còn exit 137.
Xác nhận **windows-standalone-build success**, ZIP chứa `SmallArena.exe` và
`SmallArena_Data`, BuildReport success/0 errors, SHA256/source commit hợp lệ. Sau đó
**multiplayer-smoke-test success** với cả Combat và Disconnect, exit 0 và cleanup
không còn process/port bị giữ. Build artifact giữ 3 ngày, smoke logs giữ 7 ngày.
Nếu thất bại, xem failure diagnostics và `gh run view RUN_ID --log-failed` trước khi
thay đổi thêm; không bỏ quality gate. Vấn đề Docker Windows của job Phase 1 vẫn
độc lập với bản sửa này và có thể ảnh hưởng aggregate gate.
