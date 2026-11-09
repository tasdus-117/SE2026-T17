# Kết quả kiểm chứng TV1.1

Unity 6000.6.7f1, Mirror 96.0.1; nhánh hiện tại qua kiểm tra Git cuối: `feature/tv1-m1-server-connect`. [Thiết kế và phạm vi](../architecture/tv1.1-network-lifecycle.md).

## Thay đổi

- Thêm ArenaNetworkManager và NetworkPlayer trong Networking: server chọn/giữ marker, tạo Player, gán connection ID, đăng ký bằng AddPlayerForConnection và dọn khi disconnect.
- Thêm identity/bridge vào Player prefab tại Characters, giữ GUID; Bootstrap gán prefab, KCP, RoundRobin, auto-create và giới hạn bốn connection.
- Arena thêm bốn marker `(±6; 1; ±6)`. Player offline gỡ hai component mạng bằng override; OfflineMatchScope tắt actor/coordinator/HUD mẫu khi tải arena trong phiên mạng.
- Input action map, locomotion và combat chỉ đọc ý định của local owner; trường hợp không có identity giữ hành vi offline. Camera bind/unbind theo sự kiện local và xử lý enable muộn.
- Thêm NetworkLifecycleTests và OfflinePlayerFixture; cập nhật ba fixture offline để gỡ identity, giữ nguyên các assertion gameplay. PlayMode asmdef thêm Mirror; không thêm assembly hoặc tham chiếu Client↔Server.
- Cập nhật verifier scaffold/layout và tài liệu bàn giao. Các package/settings đã có thay đổi trước nhiệm vụ được giữ nguyên; không tự stage, commit hoặc push tính năng này.

## Kết quả cuối

| Kiểm tra | Kết quả | Bằng chứng |
| --- | --- | --- |
| Biên dịch Roslyn nghiêm ngặt | 7/7 assembly nhóm đạt, cảnh báo mã nhóm là lỗi; thêm hai dependency Mirror đạt | TestResults/TV11/Compilation/results.json |
| Unity native import/compilation và Mirror weaving | Đạt; không lỗi biên dịch hoặc tham chiếu/script thiếu của mã nhóm | Logs/TV11-PlayMode.log; Logs/TV11-EditMode.log |
| PlayMode đầy đủ | **186 ca: 184 đạt, 0 lỗi, 2 package bỏ qua** | TestResults/TV11/PlayMode.xml |
| NetworkLifecycleTests | **8/8 đạt**; tổng ca nhóm tăng từ 174 lên 182, tất cả đạt | Cùng PlayMode.xml |
| EditMode, toàn bộ namespace của nhóm | **34/34 đạt**, không lỗi/bỏ qua | TestResults/TV11/EditMode.xml |
| Saved layout | **2.746 assertion đạt**, 12 YAML | TestResults/ArenaLayout/results.json |
| Scaffold | Bảy asmdef không vòng; **1.432** cặp metadata/GUID; ba scene đúng index 0/1/2 | TestResults/TV11/scaffold.txt |
| Đối chiếu snapshot chạy native | **144 tệp** script/test/scene/prefab và metadata khớp SHA-256 với workspace | TestResults/TV11/tested-source-hashes.json |

Hai ca Input System bỏ qua vẫn là WindowsInput_MouseMovements_AreDetected và WindowsInput_RemoteDesktopMouseMovements_AreDetected, cùng lý do package ghi nhận “Unstable due to 1252825” như baseline. Native import vẫn có cảnh báo analyzer từ mã Mirror/vendor; không sửa vendor để che cảnh báo. Không phát hiện cảnh báo C# của mã nhóm trong biên dịch nghiêm ngặt. Cả hai lần chạy native cuối hoàn tất thành công.

## Assertion tích hợp mới

1. Host handshake thật của Mirror tạo local owner với isLocalPlayer = true; connection khác không có quyền điều khiển local. Sự kiện local spawn/despawn đúng một lần, action map và camera bind/clear đúng; camera enable muộn lấy lại owner.
2. Bốn connection được đăng ký/spawn thật vào bốn vị trí khác nhau; ConnectionId và NetworkServer.spawned đúng. Disconnect xóa identity, owned set và registry; gọi lại an toàn, reconnect sử dụng marker vừa giải phóng.
3. Duplicate add không tạo actor dư; marker đầy từ chối Player thứ năm.
4. Server không hoạt động không tạo Player; disconnect không có Player hoặc null an toàn.
5. Không có marker thì không spawn tại tọa độ prefab/client.
6. Remote prefab không nhận WASD/Space/J/E, không di chuyển/đổi yaw, không consume nguồn combat và không được camera theo dõi.
7. Arena đăng ký đủ bốn marker; Player offline không có identity, input và camera vẫn hoạt động.
8. Host tải arena qua lifecycle scene của Mirror, tắt các actor/HUD offline và bind camera/input vào Player mạng được spawn.

## Môi trường và giới hạn

Đã có Unity Editor và các chỉnh sửa package/settings trong workspace khi bắt đầu. Chạy native trong bản sao `TestResults/TV11/ValidationProject` có cache package riêng; không dừng Editor của người dùng. Lần thử đầu bộc lộ vấn đề cách ly fixture sau đổi scene và một helper pickup dùng đường dẫn prefab nội suy chưa gỡ identity. Đã sửa cleanup/fixture, không xóa hoặc nới assertion gameplay; kết quả ở bảng là lần chạy hoàn chỉnh sau sửa. Log lần thử trước được giữ riêng trong Logs.

Host dùng hàng đợi LocalConnection thật; transport fixture bỏ qua socket cho connection giả lập phía server. Chưa kiểm chứng nhiều tiến trình qua UDP/KCP, dedicated build, reconnect có token, movement replication hoặc combat mạng. TV1.1 hoàn tất trong phạm vi lifecycle/spawn/gate; baseline sẵn sàng cho TV1.2.

Diff của nhiệm vụ gồm **16 tệp hiện có được sửa và 12 tệp mới** (năm script C#, năm `.meta`, hai tài liệu), tổng 28 tệp; ngữ cảnh cục bộ không nằm trong tập này. Danh sách thay đổi của nhiệm vụ và Git status đầy đủ: `TestResults/TV11/task-files.json`, `TestResults/TV11/git-status.txt`. Các thay đổi package/settings có sẵn được ghi trong Git status nhưng không tính vào diff của nhiệm vụ. Kiểm tra whitespace và liên kết Markdown nội bộ đạt. Báo cáo chạy và snapshot được Git bỏ qua. `AGENTS.md` và `PROJECT_CONTEXT.md` tiếp tục giữ cục bộ.
