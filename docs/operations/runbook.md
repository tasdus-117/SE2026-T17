# Chạy, build và CI

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

Chưa có script thực thi trong bộ khung này. Khi viết script phải trả mã lỗi phù hợp để CI phát hiện thất bại.

## Pipeline CI dự kiến

Checkout → chuẩn bị Unity và dependency → kiểm thử EditMode/PlayMode phù hợp → build server → build client → thu log, kết quả và bản build.

Chỉ thêm workflow YAML khi chốt phiên bản Unity, nền tảng, runner và phương án giấy phép cho môi trường CI. Không lưu giấy phép hoặc credentials trong Git. Có thể cache Library trong CI nhưng không commit nó vào repository.

## Git

Commit code, tài nguyên, file .meta, Packages, ProjectSettings, tài liệu và mẫu cấu hình. Không commit Library, Temp, log, binary build và bí mật. Di chuyển/đổi tên asset bằng Unity Editor khi có thể để giữ liên kết .meta. Dùng Git LFS cho tài nguyên nhị phân lớn nếu repository thực sự cần.
