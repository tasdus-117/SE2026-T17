# Unity CI

Workflow [Unity CI](unity-ci.yml) chạy khi mở hoặc cập nhật PR vào `main` và khi
`main` nhận commit mới. Trên PR, GitHub Actions kiểm tra bản merge thử với
`main`. Job **Git diff** phát hiện lỗi khoảng trắng và dấu xung đột Git mới.
Job **Unity EditMode and PlayMode** chạy trên runner Windows do GitHub quản lý,
dùng đúng phiên bản Editor trong [ProjectVersion.txt](../../ProjectSettings/ProjectVersion.txt).

## Thiết lập giấy phép một lần

1. Kích hoạt Unity Personal trong Unity Hub và lấy tệp `.ulf` theo
   [hướng dẫn GameCI](https://game.ci/docs/github/activation/#personal-license).
2. Trong repo GitHub, mở **Settings → Secrets and variables → Actions**.
   Thêm ba repository secrets.
   - `UNITY_LICENSE` chứa toàn bộ nội dung tệp `.ulf`.
   - `UNITY_EMAIL` và `UNITY_PASSWORD` chứa thông tin đăng nhập Unity dùng cho CI.
3. Mở tab **Actions** và chạy lại lượt CI bị lỗi do thiếu secrets. Kết quả và
   log của lượt chạy có trong **Checks** và artifact `unity-test-results`.

PR từ fork chỉ chạy **Git diff** vì GitHub giữ kín secrets của repo. Để kiểm thử
Unity cho đóng góp từ fork, thành viên có quyền ghi cần review thay đổi rồi đưa
chúng lên một nhánh của repo và mở PR từ nhánh đó.

Sau khi hai job đã chạy thành công, quản trị viên có thể yêu cầu chúng qua
quy tắc bảo vệ `main`. Xem
[cấu hình kiểm tra bắt buộc](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches#require-status-checks-before-merging).
