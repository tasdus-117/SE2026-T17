# Melee và cửa sổ nửa màn hình

## Thay đổi và nguyên nhân

Query cũ là OverlapSphere bán kính 0,6 m, tâm cách trước 0,8 m. Vùng giao với
capsule thu hẹp nhanh ở biên tầm đánh khi đối thủ lệch ngang; mask mạng chỉ
gồm Player nên Bot layer 8 bị bỏ qua. Ngoài ra CharacterController Player có center=(0;0;0): root nằm giữa capsule,
không ở chân. Offset Y=1 cũ đặt query gần đỉnh đầu; query capsule thu hẹp ở
đầu chỏm/skin width có thể làm hụt trường hợp lệch góc/tầm xa. Không dùng ray
từ camera để xác nhận hit.
Đây là nguyên nhân hình học/mask đọc được từ mã, không phải kết luận đã đo RTT
hoặc ghi hình tái hiện lỗi trên hai máy.

MeleeHitGeometry dùng chung cho simulation mạng và adapter offline: tâm
`TransformPoint(CharacterController.center) + rotation * (0;0,25;0,5)`, radius 1,2 m, cone 140° (±70°), giới hạn tâm theo mặt
phẳng XZ 1,8 m. Query dùng collider thật, kể cả CharacterController; cone/range
kiểm tra tâm bounds nên không nhạy với lệch chiều cao nhỏ của capsule.
Focused diagnostic tại 1,8 m/60°/height +0,25 cho thấy range/cone đạt nhưng
OverlapSphere nhỏ không trả CharacterController dù bounds có radius 0,5.
Vì vậy query thu ứng viên với radius +0,5 m, sau đó narrowphase kiểm tra đúng
sphere radius 1,2 với segment/radius capsule hiện hành. Collider khác dùng
ClosestPoint (bỏ mesh không convex). Candidate margin không mở reach/cone,
không tự biến mọi collider trong query lớn thành hit.
Unity cũng ghi nhận query capsule có thể thu nhỏ và bỏ sót ở biên trong
[Character Controller reference](https://docs.unity3d.com/6000.0/Documentation/Manual/class-CharacterController.html).
Điều này phù hợp với diagnostic của project; chưa đo riêng hệ số shrink của engine.
Mở rộng sphere không cho đánh sau lưng hoặc xuyên Environment; server vẫn
bỏ self, trigger, collider trùng và mục tiêu không CanAct. Multiplayer chỉ
nhận NetworkHealth của instance server, mask Player | Bot. Bot mạng phải dùng
NetworkIdentity/NetworkHealth; không bật Health offline trên client để giả
đồng bộ. Không tự thêm Bot AI/spawner vào phòng multiplayer.

Cooldown server chính xác **1,0 giây** theo NetworkTime.localTime. Request trong
cooldown bị bỏ và không thay deadline; đòn hụt vẫn tiêu tốn cooldown. Client chỉ
gửi ý định owner, bỏ cạnh nhấn sớm để giảm traffic; không tính hit/damage và
không tạo hàng đợi tự đánh khi hết cooldown. Server vẫn chặn raw Command spam.
Offline cooldown prefab/default cùng 1,0 giây; HP 10 và damage 1→2 giữ nguyên.
Không có rewind/lag compensation hoặc client hit prediction.

F11 dùng Display.main.systemWidth/systemHeight: Windowed đặt width/2, height/2;
FullScreenWindow đặt native width/height. Kích thước được chặn tối thiểu một
pixel cho môi trường không có display. Giữ request cuối vì SetResolution áp
dụng bất đồng bộ; cursor không được sửa. Không dùng Screen.width/height hiện
tại làm native metrics và không thêm minimum window lớn hơn nửa màn hình.

Giữ bảy asmdef và chiều tham chiếu cũ. Geometry nằm Networking để Server và
Client đọc hợp đồng/tuning mà không thêm Networking → Server/Client. Client
chỉ dùng hằng cooldown; quyết định hit chỉ chạy trong NetworkCombat server
hoặc CombatController offline khi không có NetworkIdentity.

## Kiểm tra thủ công

1. Mở MainMenu, nhấn F11 vào fullscreen, nhấn lần nữa để về Windowed. Màn
   1920×1080 phải cho cửa sổ nội dung 960×540; hai cửa sổ có thể xếp cạnh nhau.
   Windows có thể thêm viền/title bar ngoài kích thước nội dung yêu cầu.
2. Nhấn F11 lại: native resolution, FullScreenWindow. Thử ở Bootstrap và Arena;
   giữ F11 không lặp. Cursor vẫn đúng trạng thái menu/pause/gameplay trước đó.
3. Host/Join hai bản, cả hai Ready và host Start. Đưa người đánh phía -Z của
   đối thủ, cách 1,2 m và lệch 30°. LMB giảm HP 10→9. Thử lệch ±60°, khoảng
   1,0–1,8 m, di chuyển nhẹ ngang và nhảy để kiểm tra cảm giác thực tế.
4. Click nhanh trong giây đầu: không có damage thêm; sau hơn một giây nhấn
   mới giảm thêm một HP. Giữ LMB không tự lặp. Nhặt E vẫn tăng damage lên 2.
5. Quay vị trí để mục tiêu ở sau lưng/ngoài cone, xa hơn reach hoặc sau tường:
   không gây damage. Click UI/Pause không đánh. Đổi vai trò host/client.
6. Single Player: tiếp cận Bot cùng tư thế, thử LMB/J mỗi hơn một giây,
   weapon E, mười đòn unarmed, Victory và replay như trước.

## Kiểm chứng

Unity 6000.6.8f1, native runner trong bản sao có cache; hai runner cuối mã 0.

| Kiểm chứng | Kết quả |
| --- | --- |
| Full PlayMode | **279 ca: 277 đạt, 0 lỗi, 2 skip package có sẵn** |
| MeleeReliabilityAndDisplayTests | **8/8 đạt** |
| EditMode | **34/34 đạt**, 0 lỗi/bỏ qua |
| Assembly nhóm | **7/7**, 0 cảnh báo/lỗi Roslyn strict, native import/weaving đạt |
| Layout/scaffold | **4.291 assertion/18 YAML**, **1.478 cặp metadata/GUID**, graph không vòng |

Giữ baseline 271 ca trước nhiệm vụ. Test cũ chờ qua cooldown mới thay cho
0,5 giây; không bỏ assertion hoặc hạ mức kiểm tra. Hằng 1 giây được kiểm tra
trực tiếp; raw Command trước deadline bị từ chối, sau deadline +0,01 giây gây
hit mới. Test F11 bơm phím qua lifecycle Update và xác nhận mode/resolution request.
Test Bot mạng dùng NetworkHealth server-only để kiểm tra layer/hit authority;
không tuyên bố đã thêm AI hay replication prefab Bot vào sản phẩm.
Vendor/Mirror vẫn có cảnh báo analyzer có sẵn, không khẳng định toàn bộ Unity
zero-warning. Lần chẩn đoán đầu có thiếu namespace test đã sửa; không dùng log
lỗi đó làm kết quả kiểm chứng.
Bằng chứng cục bộ: TestResults/MeleeDisplay/PlayMode-capsule.xml, EditMode-capsule.xml,
summary.json, tested-source-hashes.json, Compilation-final/results.json; Logs/MeleeDisplay-PlayMode-capsule.log,
Logs/MeleeDisplay-EditMode-capsule.log. Giữ 32 file cấu hình/README và 190 hash
nguồn/asset/meta khớp bản sao native. Test resolution
xác nhận request truyền cho Screen.SetResolution; nographics không chứng minh
kích thước cửa sổ vật lý. Harness mạng là một native client và peer ảo trong
engine, không thay thế checklist KCP/hình ảnh hai cửa sổ ở trên.
