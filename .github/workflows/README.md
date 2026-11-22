# Unity CI

Workflow [Unity CI](unity-ci.yml) chạy khi mở hoặc cập nhật PR vào `main` và khi
`main` nhận commit mới. Trên PR, GitHub Actions kiểm tra bản merge thử với
`main`. Job **Git diff** phát hiện lỗi khoảng trắng và dấu xung đột Git mới.
Hai job **Static quality (ubuntu-24.04)** / **Static quality (windows-2022)** kiểm
architecture, metadata/GUID, scene order, Input Actions và regression XML không cần license.
Job **Unity EditMode and PlayMode** chạy native tests và strict compilation/layout
trong cùng Editor container trên Windows. Phiên bản lấy từ
[ProjectVersion.txt](../../ProjectSettings/ProjectVersion.txt); image được cố định bằng digest trong mỗi lượt chạy.
Job **Windows Standalone Build** dùng Unity 6000.6.8f1 trên `ubuntu-latest`, image
`unityci/editor:ubuntu-6000.6.8f1-windows-mono-3` để cross-build Windows x64 Mono,
cache Library riêng Linux và upload ZIP/checksum + diagnostics giữ 3 ngày.
Máy Windows phát triển dùng wrapper native, không cần Docker Desktop.
Job chạy cho code/asset/tool/
workflow PR và mọi push main; PR chỉ sửa tài liệu được skip riêng build.
Job **Multiplayer Smoke Test** tải đúng ZIP/SHA từ build job, chạy Host + Client thật
trên Windows qua KCP, kiểm combat và abrupt disconnect rồi cleanup bằng Job Object/
watchdog; không cài Editor/Docker hoặc activate license. Smoke logs giữ 7 ngày.
**CI Quality Gate** chỉ đạt nếu tests/static thành công và build + smoke cần thiết
đều thành công; skip build/smoke chỉ hợp lệ khi classifier xác nhận docs-only.

## Thiết lập giấy phép một lần

1. Kích hoạt Unity Personal trong Unity Hub và lấy tệp `.ulf` theo
   [hướng dẫn GameCI](https://game.ci/docs/github/activation/#personal-license).
2. Trong repo GitHub, mở **Settings → Secrets and variables → Actions**.
   Thêm ba repository secrets.
   - `UNITY_LICENSE` chứa toàn bộ nội dung tệp `.ulf`.
   - `UNITY_EMAIL` và `UNITY_PASSWORD` chứa thông tin đăng nhập Unity dùng cho CI.
3. Mở tab **Actions** và chạy lại lượt CI bị lỗi do thiếu secrets. Kết quả và
   log của lượt chạy có trong **Checks** và artifact `unity-test-results`.

PR từ fork chạy **Git diff** và **Static quality** nhưng **CI Quality Gate** không đạt
vì Unity chưa được kiểm chứng bằng license; GitHub giữ kín secrets của repo. Để kiểm thử
Unity cho đóng góp từ fork, thành viên có quyền ghi cần review thay đổi rồi đưa
chúng lên một nhánh của repo và mở PR từ nhánh đó.

Kiến trúc gate, lệnh chạy cục bộ, kết quả thực tế và danh sách status checks đề xuất
ở [CI Phase 1–2](../../docs/ci/ci-pipeline.md). Sau khi các job xuất hiện trong Checks,
quản trị viên có thể yêu cầu **CI Quality Gate** qua quy tắc bảo vệ `main`. Xem
[cấu hình kiểm tra bắt buộc](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches#require-status-checks-before-merging).
