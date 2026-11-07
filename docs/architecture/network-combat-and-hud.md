# Combat và HUD mạng

Giữ bảy assembly hiện hành, không thêm Gameplay.asmdef thứ tám. NetworkPlayer
chỉ giữ lifecycle và bridge Command; NetworkCombat/NetworkHealth/NetworkMatchRules
thuộc Networking. Client giữ input, HUD và billboard. Server/Simulation chỉ đọc
trạng thái CanAct để chặn di chuyển sau khi thua/thắng. Không có tham chiếu
Networking → Client/Server hoặc Client → Server.

## Luồng dữ liệu

LMB (J tương thích) → PlayerInputHandler → NetworkCombatInput local owner →
NetworkPlayer.RequestAttack → CmdPerformAttack(Vector3), requiresAuthority →
NetworkCombat kiểm tra server → NetworkHealth.TakeDamage → SyncVar HP/kết quả →
hook client → HUD, overhead và modal kết quả.

Client không gửi damage, mục tiêu, vị trí hitbox, cooldown hay thời gian mô phỏng.
Server dùng đồng hồ riêng, cooldown 0,5 giây, sphere offset (0;1;0,8), bán kính
0,6, chỉ Layer 7 Player; bỏ self, trigger, mục tiêu đã kết thúc, collider trùng
và hit xuyên Environment Layer 6. Aim phải hữu hạn, chuẩn hóa và hướng +Z như
invariant D-01; không dùng aim để mở rộng range hoặc đổi góc đánh. Không thêm
projectile vì mẫu single-player hiện có là melee.

Nếu còn connection đã xác thực nhưng chưa có avatar, server bỏ request đánh;
đây là hàng rào khi các client tải Arena xong khác thời điểm, tránh quyết định
người sống cuối khi một thành viên phòng còn đang tải. Disconnect trả slot và
gỡ hàng rào; client phải nhấn một đòn mới, không có hàng đợi đòn bị bỏ.

NetworkHealth có max/current mặc định 100; mỗi đòn unarmed 10 HP, nên vẫn mười
đòn như Bot offline 10 HP/đòn 1 HP. Máu chỉ đổi trên instance server, clamp 0,
không nhận amount ≤ 0 hoặc sát thương sau kết quả cuối. OnDeath server phát một
lần; last-survivor rule đánh dấu nạn nhân Defeat và người sống cuối Victory khi
có ít nhất hai player đã spawn. Các kết quả được SyncVar xuống từng client;
HUD chỉ gọi ShowMatchResult theo dữ liệu này, không tự quyết định thắng/thua.
Không dùng luồng MatchStateMachine offline làm authority của trận mạng.

## Input và UI

LMB là binding chính; J được giữ để tương thích single-player và regression cũ.
Nhấn giữ không lặp đòn; nhấn trong cooldown bị bỏ. Chỉ owner có gameplay input
enabled. Pause/result khóa map và xóa ý định đang chờ. Kiểm tra pointer ở lúc
consume intent, tránh truy vấn UI từ callback Input System trước khi UI cập nhật.
Ngoài IsPointerOverGameObject, raycast UI tại vị trí chuột hiện tại chặn click
đúng frame và tránh attack bị lọt khi hover cache còn cũ.

NetworkHUD.prefab là root độc lập trong Arena, sorting 20 dưới pause menu 30;
chỉ hiện khi có local NetworkPlayer. Slider chỉ đọc và text HP theo hook, bind
cả snapshot ban đầu và lần đổi HP, unsubscribe khi despawn/disable. Panel HUD
có raycast target để click vào thanh HP không tạo attack. OpponentHealthBar
world-space chỉ tạo trên client cho đối thủ, không cho owner hoặc server-only;
LateUpdate quay theo Camera.main. Các phần đồ họa overhead không bắt pointer.

Player prefab thêm NetworkHealth, NetworkCombat, NetworkCombatInput và
OpponentHealthPresenter. Player instance offline cùng OfflinePlayerFixture gỡ
đủ các component này; offline Health/combat/pickup/HUD giữ riêng. CombatController
offline trả về với mọi NetworkIdentity, kể cả owner/host, để không áp dụng damage
local hoặc tiêu thụ nhầm intent mạng.

## Kiểm tra hai người

1. Hai bản chạy từ MainMenu: Host Room và Join IP, cả hai Ready; host Start.
2. Kiểm tra local HUD HP: 100/100, đối thủ có thanh overhead, không thấy HUD Bot.
3. Dùng WASD để đứng cách nhau khoảng 1 m. Người đánh đứng về phía -Z của nạn
   nhân, vì hướng đánh vẫn cố định +Z. LMB làm HP đối thủ giảm 10 trên cả HUD
   của nạn nhân và overhead của người đánh; người đánh không mất HP do self-hit.
4. Click nhanh hơn 0,5 giây hoặc giữ LMB: không tăng tốc đánh. Ra ngoài 2 m hoặc
   đứng cách nhau bởi tường: không trừ HP. Đổi vị trí để người kia thử đánh.
5. Escape, click Resume/Leave hoặc click panel HP: không có đòn đánh lọt qua UI.
6. Đánh đủ mười đòn hợp lệ: nạn nhân thấy DEFEAT, người sống cuối thấy VICTORY;
   không còn di chuyển/đánh. Return to Main Menu dọn phiên như luồng trước.
7. Single Player: thử cả LMB và J, WASD/Space/E, buff 1→2, Victory/replay cũ.

## Kết quả thực thi ngày 08/11/2026

Unity **6000.6.8f1**, Test Runner native trong bản sao
`TestResults/TV11/ValidationProject`, không dừng Editor của người dùng.

| Kiểm chứng | Kết quả |
| --- | --- |
| Full PlayMode | **251 ca: 249 đạt, 0 lỗi, 2 bỏ qua package** |
| PlayMode của nhóm | **247/247 đạt**, không bỏ qua |
| NetworkedCombatAndHUDTests mới | **13/13 đạt** |
| Baseline trước combat | Giữ đủ **238 ca**, gồm M1, M2, offline và menu |
| EditMode | **34/34 đạt**, không lỗi/bỏ qua |
| Roslyn nghiêm ngặt | **7/7 asmdef đạt**, 0 cảnh báo/lỗi mã nhóm |
| Saved layout | **3.847 assertion / 15 YAML đạt** |
| Scaffold | **1.465 cặp metadata/GUID**, graph không vòng, build order đúng |

Hai ca bỏ qua là package Input System có sẵn, lý do “Unstable due to 1252825”;
không phải ca combat bị bỏ. Unity import/Mirror weaving thành công; cảnh báo
analyzer vendor/Mirror có sẵn vẫn được ghi, không cam kết vendor không có cảnh báo.

Mười ba ca mới kiểm tra LMB/cooldown/giữ nút, Command từ connection khác làm HP
victim giảm trên server và native client/HUD, cấm sửa HP từ client proxy, aim
không hợp lệ/range, tường, self/collider trùng, giả ownership, death clamp và
kết quả mỗi bên, UI raycast cùng việc nhận click mới khi rời UI, overhead bind
và refresh sau disable/enable, thành viên chưa spawn. Không giả ownership hoặc
gọi thẳng damage receiver để thay cho kiểm tra Command.

Fixture tạo thế giới mạng trong thân từng UnityTest sau InputTestFixture.SetUp;
UnitySetUp tạo world quá sớm sẽ khiến device/action state bị reset và làm các
ca mouse thất bại giả. Các workaround input chẩn đoán đã bỏ. Một NetworkClient
native có proxy riêng và một connection ảo gửi message đã serialize qua transport
loopback; không coi đây là kiểm chứng KCP hai tiến trình cho combat. Các bước
hai người ở trên là checklist thủ công, chưa ghi là đã thực thi trong nhiệm vụ này.

Bằng chứng cục bộ được Git ignore:
`TestResults/NetworkCombat/PlayMode-verified.xml`, `EditMode.xml`,
`tested-source-hashes.json`; log `Logs/NetworkCombat-PlayMode-verified.log` và
`Logs/NetworkCombat-EditMode.log`; báo cáo layout/compile trong
`TestResults/ArenaLayout/`. Không cộng các lần chẩn đoán trước đó vào số ca đạt.

Chưa triển khai lag compensation, projectile, pickup mạng, respawn hoặc điểm số;
chúng không được suy ra là hoàn thành từ phạm vi melee/HP/HUD này.
