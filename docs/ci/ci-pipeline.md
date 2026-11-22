# CI Phase 1 — Kiểm thử và kiểm tra kiến trúc bắt buộc

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
  \-- CI Quality Gate: mọi job phải có result == success
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
chấp nhận `success` của Git diff, toàn bộ static matrix và Unity; `skipped`, `failure`,
`cancelled` không đạt. Có thể chọn thêm các check chi tiết sau để review trực quan:

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
