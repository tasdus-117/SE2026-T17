# Chạy lại BT3 — 23001955 — VT-067

Xem [báo cáo](BT3_23001955_VT-067_Report.md) và [đặc tả trước bản vá](01-Spec-and-Plan.md).
Không sửa/xóa `AGENTS.md`, `PROJECT_CONTEXT.md`, Packages/README có sẵn; không
commit cache, DLL, XML, license hoặc token. Native Unity cần license hợp lệ.

## Domain, concurrency và load 40 task

Từ gốc repository, PowerShell trên Windows, Unity 6000.6.8f1 đã cài:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-VT067Checks.ps1 -Mode Green
```

Lệnh trên compile Domain không engine/Mirror và chạy toàn bộ EditMode (165 ca).
Chỉ chọn 24 ca VT-067, gồm TC12 40 task và dòng `VT067_LOAD`:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-DomainTests.ps1 -EditorRoot 'C:\Program Files\Unity\Hub\Editor\6000.6.8f1\Editor' -OutputDirectory TestResults/VT067/Only -TestPrefix SmallArena.Tests.EditMode.VT067
```

Đây là NUnit runner độc lập dùng cùng nguồn test, không giả là native Unity.
Red/mutation chỉ thay source copy trong TestResults rồi restore bằng SHA-256:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-VT067Checks.ps1 -Mode Red
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-VT067Checks.ps1 -Mode Mutation
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-VT067Checks.ps1 -Mode Green
```

Red/Mutation wrapper trả 0 khi phát hiện đúng lỗi mong đợi; XML `results.xml`
bên trong ghi failed và `restored.txt` xác nhận không còn mutant. Không dùng mã
thoát wrapper làm tỷ lệ test pass. TC03/04/06 phải giết mutant; TC05/12 còn có
thể tình cờ giữ max nếu scheduler cho revision lớn nhất chạy cuối.

## Native Unity Test Runner

Đóng Editor của project được kiểm chứng; có thể dùng bản sao đồng bộ toàn bộ
Assets/Packages/ProjectSettings để giữ Editor đang làm việc. Không mở cùng một
project bằng hai Editor. Từ gốc project đó:

```powershell
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.6.8f1\Editor\Unity.exe'
New-Item -ItemType Directory -Force TestResults/VT067,Logs | Out-Null
foreach ($platform in @('EditMode', 'PlayMode')) {
    $filter = if ($platform -eq 'EditMode') { ' -testFilter SmallArena.Tests.EditMode' } else { '' }
    $runArgs = '-batchmode -nographics -projectPath "' + $PWD.Path + '" -runTests -testPlatform ' + $platform + $filter + ' -testResults "' + $PWD.Path + '/TestResults/VT067/' + $platform + '.xml" -logFile "' + $PWD.Path + '/Logs/VT067-' + $platform + '.log"'
    $run = Start-Process -FilePath $unity -ArgumentList $runArgs -WindowStyle Hidden -Wait -PassThru
    if ($run.ExitCode -ne 0) { throw "$platform failed: exit $($run.ExitCode)" }
}
```

Không thêm `-quit` vào lệnh `-runTests` vì runner tự kết thúc sau khi ghi XML.
Trong script PowerShell phải chờ process thực sự thoát trước khi mở runner sau;
chỉ nhìn thấy XML chưa chứng minh Editor đã đóng. Native Windows FileShare/Replace
là nền tảng của TC17. Dùng `Start-Process -WindowStyle Hidden -Wait` nếu cần
điều phối rõ lifetime; mỗi lượt dùng đường dẫn XML/log riêng để không đọc nhầm.

Chỉ file integration/load 40 task trên disk: thêm
`-testFilter SmallArena.Tests.PlayMode.AutoSaveDraftFileTests` vào lệnh PlayMode.
Chỉ TC18: filter đầy đủ
`SmallArena.Tests.PlayMode.AutoSaveDraftFileTests.TC18_DurableLoad40AcrossIndependentBackends_P95AtMost1500ms`.
Editor: Window → General → Test Runner, chọn EditMode → namespace VT067 hoặc
PlayMode → AutoSaveDraftFileTests/AutoSaveDraftIntegrationTests → Run Selected.
Suite nhóm vẫn phải Run All trước khi kết luận zero regression.

## Xác nhận hoạt động sản phẩm

MainMenu → Multiplayer → Host Room, join client thứ hai, đổi Ready hoặc rời
phòng. Mỗi thay đổi được server capture tự động, không có nút Save. Draft mặc
định nằm dưới `Application.persistentDataPath/VT067/Drafts`, tên file là SHA-256
RoomId; header/schema/revision/roster nằm trong binary snapshot. Không sửa file
trực tiếp khi server đang ghi. Room rỗng không ghi bản payload rỗng; phiên mới
dùng RoomId mới và Ready vẫn false, không nhận quyền từ archive.

## Coverage có đo thật, phạm vi Domain VT-067

Coverlet 6.0.4 là tool phụ trong thư mục ignore, không thêm package vào Unity.
Hướng dẫn CLI gốc: [Coverlet GlobalTool](https://github.com/coverlet-coverage/coverlet/blob/master/Documentation/GlobalTool.md).
`Run-DomainTests.ps1` tạo portable PDB. Cài và chạy (cần Internet cho lần cài):

```powershell
$sdk = 'C:\Program Files\Unity\Hub\Editor\6000.6.8f1\Editor\Data\DotNetSdk\dotnet.exe'
& $sdk tool install coverlet.console --version 6.0.4 --tool-path TestResults/VT067/Coverlet
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-DomainTests.ps1 -EditorRoot 'C:\Program Files\Unity\Hub\Editor\6000.6.8f1\Editor' -OutputDirectory TestResults/VT067/CoverageInput -TestPrefix SmallArena.Tests.EditMode.VT067
$env:DOTNET_ROOT = 'C:\Program Files\Unity\Hub\Editor\6000.6.8f1\Editor\Data\NetCoreRuntime'
$inputPath = Join-Path $PWD 'TestResults/VT067/CoverageInput'
$runnerArgs = '"' + (Join-Path $inputPath 'NUnitDomainRunner.dll') + '" "' + (Join-Path $inputPath 'SmallArena.EditMode.Tests.dll') + '" "' + (Join-Path $inputPath 'coverage-tests.xml') + '" SmallArena.Tests.EditMode.VT067'
& TestResults/VT067/Coverlet/coverlet.exe (Join-Path $inputPath 'SmallArena.EditMode.Tests.dll') --target (Join-Path $env:DOTNET_ROOT 'dotnet.exe') --targetargs $runnerArgs --include '[SmallArena.Shared.Domain]SmallArena.Domain.AutoSaveDraftService*' --include '[SmallArena.Shared.Domain]SmallArena.Domain.LobbyDraft*' --include '[SmallArena.Shared.Domain]SmallArena.Domain.InMemoryDraftStorage*' --format cobertura --format json --output TestResults/VT067/Coverage/
```

Không compile lại assembly trong target Coverlet. Coverage chỉ của các type
VT-067 Domain được include, không phải toàn project hoặc file backend; không
dùng latency của lượt instrumented làm kết quả performance chính.

## Xuất PDF

Rà soát lời khai và Reflection trước khi nộp. Mở Markdown báo cáo trong trình
soạn thảo có Preview/Print, chọn khổ A4, in PDF tên `BT3_23001955_VT-067.pdf`.
Các khối page-break đánh dấu trang metadata và các phần. Báo cáo không tự ký tên
thay sinh viên; kết quả máy chỉ đại diện môi trường ghi trong evidence.
