# Chạy, build và CI

## Chạy mẫu offline và kiểm tra thực tế

Dùng Unity 6000.6.8f1 (khớp ProjectVersion.txt) mở gốc repository; chạy MainMenu và chọn Single Player. Build order: MainMenu 0, Bootstrap 1, Arena_MVP 2. Mirror 96.0.1 giữ nguyên ở Assets/Mirror; giữ Packages/ProjectSettings của project.

Từ PowerShell ở gốc repository:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Verify-Scaffold.ps1 -CompileProbes
$editorRoot = 'C:/Program Files/Unity/Hub/Editor/6000.6.8f1/Editor'
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-DomainTests.ps1 -EditorRoot $editorRoot
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Verify-ArenaLayout.ps1 -EditorRoot $editorRoot
& "$editorRoot/Unity.exe" -batchmode -nographics -projectPath (Get-Location).Path -runTests -testPlatform PlayMode -testResults "$PWD/TestResults/TV13Verified/PlayMode.xml" -logFile "$PWD/Logs/TV13Verified-PlayMode.log" | Out-Null
& "$editorRoot/Unity.exe" -batchmode -nographics -projectPath (Get-Location).Path -runTests -testPlatform EditMode -testFilter SmallArena.Tests.EditMode -testResults "$PWD/TestResults/TV13Verified/EditMode.xml" -logFile "$PWD/Logs/TV13Verified-EditMode.log" | Out-Null
```

Đóng project đang mở trước khi chạy batch cùng thư mục, hoặc dùng bản sao kiểm chứng; không ép tắt editor chứa thay đổi chưa lưu. Cần license Editor hợp lệ trong Hub; nếu gặp mã 198, kiểm tra lại license trước khi chạy. Roslyn chỉ kiểm tra biên dịch, không thay Unity import/Mirror weaving hoặc physics native. Kết quả M1 hiện hành ở [tv1.3-results.md](../testing/tv1.3-results.md), lịch sử di trú ở [migration-results.md](../testing/migration-results.md); dữ liệu thô trong TestResults/Logs được Git bỏ qua.

Các mục scripts/CI bên dưới vẫn là kế hoạch. Script cục bộ không phải workflow CI hay bằng chứng build/network nhiều tiến trình.

## Cấu hình

- `config/development/`: mẫu dùng trên máy nhóm.
- `config/test/`: mẫu cho kiểm thử và CI.
- `config/production/`: mẫu cho máy chủ triển khai.

Chưa có loader cấu hình nên các thư mục này chưa được game tự đọc. Khi triển khai phải xác định đường dẫn, thứ tự ưu tiên, kiểm tra giá trị và cách truyền tham số. Không coi file trong repository là bí mật. Đặt bí mật trong biến môi trường hoặc kho secrets của hệ thống CI.

Settings trong Assets là Unity asset; config bên ngoài là tham số triển khai. Không lưu trùng một giá trị ở hai nơi mà không quy định nguồn có quyền ưu tiên.

## Script dự kiến

- `scripts/build/`: gọi Unity để build client/server.
- `scripts/run/`: khởi động server và các client thử, truyền đường dẫn log/cấu hình.
- `scripts/ci/`: kiểm tra đầu vào, thu gom kết quả và chuẩn bị artifact.
- `Assets/_Project/Scripts/Editor/Build/`: code Editor thực hiện cấu hình build bên trong Unity.

Chưa có script build/chạy server trong các thư mục `scripts/` nêu trên; các script kiểm tra cục bộ hiện có ở `tools/`. Khi thêm script build/chạy phải trả mã lỗi phù hợp để CI phát hiện thất bại.

## Pipeline CI dự kiến

Checkout → chuẩn bị Unity và dependency → kiểm thử EditMode/PlayMode phù hợp → build server → build client → thu log, kết quả và bản build.

Chỉ thêm workflow YAML khi chốt phiên bản Unity, nền tảng, runner và phương án giấy phép cho môi trường CI. Không lưu giấy phép hoặc credentials trong Git. Có thể cache Library trong CI nhưng không commit nó vào repository.

## Git

Commit code, tài nguyên, file .meta, Packages, ProjectSettings, tài liệu và mẫu cấu hình. Không commit Library, Temp, log, binary build và bí mật. Di chuyển/đổi tên asset bằng Unity Editor khi có thể để giữ liên kết .meta. Dùng Git LFS cho tài nguyên nhị phân lớn nếu repository thực sự cần.
