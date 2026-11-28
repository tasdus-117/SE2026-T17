# Chạy, build và CI

## Chạy mẫu offline và kiểm tra thực tế

Dùng Unity 6000.6.8f1 (khớp ProjectVersion.txt) mở gốc repository; chạy MainMenu và chọn Single Player. Build order: MainMenu 0, Bootstrap 1, Arena_MVP 2. Mirror 96.0.1 giữ nguyên ở Assets/Mirror; giữ Packages/ProjectSettings của project.

Từ PowerShell ở gốc repository:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Verify-Scaffold.ps1 -CompileProbes
$editorRoot = 'C:/Program Files/Unity/Hub/Editor/6000.6.8f1/Editor'
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-DomainTests.ps1 -EditorRoot $editorRoot
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Verify-ArenaLayout.ps1 -EditorRoot $editorRoot
& "$editorRoot/Unity.exe" -batchmode -nographics -projectPath (Get-Location).Path -runTests -testPlatform PlayMode -testResults "$PWD/TestResults/M2/PlayMode.xml" -logFile "$PWD/Logs/M2-PlayMode.log" | Out-Null
& "$editorRoot/Unity.exe" -batchmode -nographics -projectPath (Get-Location).Path -runTests -testPlatform EditMode -testFilter SmallArena.Tests.EditMode -testResults "$PWD/TestResults/M2/EditMode.xml" -logFile "$PWD/Logs/M2-EditMode.log" | Out-Null
```

Đóng project đang mở trước khi chạy batch cùng thư mục, hoặc dùng bản sao kiểm chứng; không ép tắt editor chứa thay đổi chưa lưu. Cần license Editor hợp lệ trong Hub; nếu gặp mã 198, kiểm tra lại license trước khi chạy. Roslyn chỉ kiểm tra biên dịch, không thay Unity import/Mirror weaving hoặc physics native. Kết quả M1 hiện hành ở [tv1.3-results.md](../testing/tv1.3-results.md), lịch sử di trú ở [migration-results.md](../testing/migration-results.md); dữ liệu thô trong TestResults/Logs được Git bỏ qua.

Multiplayer hiện vào [phòng hosted M2](../architecture/m2-lobby-rooms.md) ngay từ MainMenu: Host Room hoặc nhập IP rồi Join Room, mọi người Ready và host Start. Cùng máy dùng localhost; máy khác dùng IP của host và cho phép UDP cổng cấu hình (mặc định 7777). [Kết quả M2](../testing/m2-lobby-results.md) gồm test native và hai standalone process KCP. Công cụ Run-LobbyStandaloneSmoke.ps1 chỉ dùng với build instrument theo hướng dẫn báo cáo, không phải điều kiện chạy PlayMode.

Xem [Unity CI](../../.github/workflows/README.md) để thiết lập license và xem kết quả kiểm thử trên GitHub.

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

## CI và build

[Workflow CI](../../.github/workflows/unity-ci.yml) có Git diff, Static quality (Ubuntu/Windows), Unity EditMode and PlayMode và CI Quality Gate. Job Unity trên `windows-2022` chạy native tests, XML gates và strict compilation/configuration/layout; khi đạt, gọi `SmallArena.Tools.SmallArenaBuildPipeline.BuildWindowsStandalone` để build production Windows x64 Mono, dùng lại Windows Editor image pin digest và Library đã import. Output/BuildReport được kiểm trước khi đóng ZIP kèm SHA256/source commit; player/build diagnostics giữ 3 ngày, test artifacts giữ 7 ngày. Không có job Ubuntu cross-build riêng hoặc Phase 3 smoke trong CI.

PR chỉ đổi đường dẫn khớp `docs/*` hoặc `*.md` skip cả tests và build trong job Unity, nhưng vẫn chạy diff/static/quality. Code/asset/tool/workflow PR và mọi push `main` phải qua Unity verification; fork PR cần Unity không thể đạt gate nếu thiếu lượt kiểm chứng có license. XML giữ project pass floors 261 EditMode/406 PlayMode và không cho project skip.

Build cục bộ bằng `powershell -NoProfile -ExecutionPolicy Bypass -File tools/Build-WindowsStandalone.ps1`; wrapper gọi cùng Editor entry point, không cần Docker Desktop. Dùng `-ValidateOnly` để chỉ kiểm preflight, không coi đó là build đã chạy. Xem [hướng dẫn build/CI](../ci/ci-pipeline.md) để biết điều kiện Editor, output và commands. Build này là player Windows; không chứng nhận Dedicated Server build, tự phát hành hoặc nghiệm thu mạng nhiều process.

## Git

Commit code, tài nguyên, file .meta, Packages, ProjectSettings, tài liệu và mẫu cấu hình. Không commit Library, Temp, log, binary build và bí mật. Di chuyển/đổi tên asset bằng Unity Editor khi có thể để giữ liên kết .meta. Dùng Git LFS cho tài nguyên nhị phân lớn nếu repository thực sự cần.
