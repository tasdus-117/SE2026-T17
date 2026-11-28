# Unity CI

Workflow [Unity CI](unity-ci.yml) chạy khi mở hoặc cập nhật PR vào `main` và khi
`main` nhận commit mới. Trên PR, GitHub Actions kiểm tra bản merge thử với
`main`. Job **Git diff** phát hiện lỗi khoảng trắng và dấu xung đột Git mới.
Hai job **Static quality (ubuntu-24.04)** / **Static quality (windows-2022)** kiểm
architecture, metadata/GUID, scene order, Input Actions và regression XML không cần license.
Job **Unity EditMode and PlayMode** (`unity`) trên **windows-2022** chạy native
EditMode/PlayMode, XML gates và strict compilation/configuration/layout, rồi build
production **Windows x64 Mono** khi các gate đã đạt. Tests, static gates trong
Editor và build tái sử dụng cùng Windows Unity image pin digest cùng checkout/Library;
không có job Ubuntu cross-build riêng. Phiên bản lấy từ
[ProjectVersion.txt](../../ProjectSettings/ProjectVersion.txt), hiện là 6000.6.8f1.
Hai XML steps chạy `if: always()`: minimum passed 261/408, project passed 261/406,
zero project skips; vendor skips phải qua allowlist/reason/budget. Test artifacts
giữ 7 ngày; player ZIP/SHA256/source commit và build diagnostics giữ 3 ngày.
Máy Windows phát triển dùng `tools/Build-WindowsStandalone.ps1` gọi Editor native,
không cần Docker Desktop.

Bốn job ID là `diff`, `static` (hai runner), `unity`, `quality`. Với PR chỉ thay đổi
đường dẫn khớp `docs/*` hoặc `*.md`, classifier skip toàn job `unity`, gồm tests và
Windows build; **Git diff**, **Static quality** và **CI Quality Gate** vẫn chạy.
Code/asset/tool/workflow PR và mọi push `main` yêu cầu job `unity` thành công.
**CI Quality Gate** luôn đòi diff/static thành công và chỉ chấp nhận Unity skipped
khi classifier xác nhận docs-only. CI giữ **Phase 1 + Phase 2**, không chạy harness
nhiều process hoặc tự phát hành sản phẩm.

## Thiết lập giấy phép một lần

1. Kích hoạt Unity Personal trong Unity Hub và lấy tệp `.ulf` theo
   [hướng dẫn GameCI](https://game.ci/docs/github/activation/#personal-license).
2. Trong repo GitHub, mở **Settings → Secrets and variables → Actions**.
   Thêm ba repository secrets.
   - `UNITY_LICENSE` chứa toàn bộ nội dung tệp `.ulf`.
   - `UNITY_EMAIL` và `UNITY_PASSWORD` chứa thông tin đăng nhập Unity dùng cho CI.
3. Mở tab **Actions** và chạy lại lượt CI bị lỗi do thiếu secrets. Kết quả và
   log của lượt chạy có trong **Checks** và artifact `unity-test-results`.

PR code từ fork chạy **Git diff** và **Static quality** nhưng **CI Quality Gate** không đạt
vì Unity bị skip khi verification được yêu cầu; GitHub giữ kín secrets của repo. PR
docs-only vẫn theo chính sách skip ở trên. Để kiểm thử
Unity cho đóng góp từ fork, thành viên có quyền ghi cần review thay đổi rồi đưa
chúng lên một nhánh của repo và mở PR từ nhánh đó.

Kiến trúc gate, lệnh chạy cục bộ, kết quả thực tế và danh sách status checks đề xuất
ở [CI Phase 1–2](../../docs/ci/ci-pipeline.md). Sau khi các job xuất hiện trong Checks,
quản trị viên có thể yêu cầu **CI Quality Gate** qua quy tắc bảo vệ `main`. Xem
[cấu hình kiểm tra bắt buộc](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches#require-status-checks-before-merging).
