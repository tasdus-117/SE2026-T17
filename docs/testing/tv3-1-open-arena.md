# TV3.1 — Arena mở và vật cản ngẫu nhiên

Arena_MVP giữ sàn 16 × 16 m, mặt sàn y=0; bỏ bốn tường bao và hai khối cover lưu cố định.
Mỗi lần tải cảnh offline hoặc bắt đầu trận online, ArenaObstacles chọn 3–6 vật cản
Cube/Sphere/Capsule, bảo đảm ít nhất hai dạng. Kích thước và vị trí thay đổi theo
nguồn ngẫu nhiên mới cho mỗi trận. Generator dùng System.Random riêng, không thay
đổi UnityEngine.Random của gameplay khác.

Server gửi toàn bộ danh sách mô tả collider qua Mirror SyncList trên scene
NetworkIdentity. Client dựng primitive từ chính danh sách đó khi spawn cảnh;
host dùng cùng dữ liệu. Không cần chạy lại RNG trên client. Danh sách không đổi
trong trận và được gửi đầy đủ khi observer nhận scene identity lần đầu.

Vị trí được xét theo AABB của collider, không chỉ tâm đối tượng. Vật cản chừa
0,35 m với mép sàn, collider của vật cản khác và hình chiếu collider tại cả bảy
vị trí: Player/Bot/Pickup offline và bốn NetworkStartPosition. Marker online dùng
collider của playerPrefab trên server. Collider offline bị tắt trong multiplayer
vẫn được tính từ dữ liệu hình học; không dùng bounds rỗng của collider bị tắt.
Pickup offline và hòm online hiện có cùng kích thước 0,6 m và vị trí (-3; 0,3; 0).
Sphere dùng scale đều; Capsule giữ tiết diện tròn và scale chiều cao theo primitive
cao 2 m. AABB được xét bảo thủ trên mặt phẳng XZ, cả khi spawn đứng trên mặt sàn.
Nếu cấu hình sàn/vùng cấm không còn đủ chỗ, generator báo lỗi thay vì trả thiếu số lượng.

FallDamage trên Player và Bot gọi TakeDamage bằng toàn bộ HP còn lại khi tâm
nhân vật thấp hơn y=-5. Trong multiplayer, chỉ instance server được phép làm
điều này; NetworkHealth đồng bộ HP và Defeat qua các hook HUD hiện có. Offline
GameManager theo dõi Health của người chơi, phát MatchState.Defeat, dừng input,
movement/combat và dùng banner DEFEAT với nút Play Again hiện có. Replay tải lại
cảnh, khôi phục HP và sinh bố cục mới.

## Chạy lại kiểm thử

Chạy từ checkout riêng, không dùng cùng project đang mở trong Unity Editor:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-Tv31Unity.ps1 -Phase EditMode
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-Tv31Unity.ps1 -Phase PlayMode
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Verify-ArenaLayout.ps1 -EditorRoot 'C:/Program Files/Unity/Hub/Editor/6000.6.8f1/Editor'
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-Tv31Unity.ps1 -Phase Build
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-Tv31StandaloneSmoke.ps1 -Faller client
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-Tv31StandaloneSmoke.ps1 -Faller host
```

ArenaLayoutTests kiểm tra 1.000 seed theo 10 nhóm, tạo collider Unity thật để
đối chiếu kích thước, mặt sàn, vùng spawn, giao nhau và độ đa dạng của layout.
Các kiểm thử khác xác nhận rơi offline, replay, sát thương server đúng một lần
và replica tự rơi không được quyết định sát thương. Test camera dùng cover kiểm
soát được thay cho giả định phải có tường ở mép nam.

Build xác minh chỉ thêm harness tạm, dùng define SMALL_ARENA_M2_SMOKE và dọn các
file được tạo sau build; harness không có trong build sản phẩm thông thường.
Hai tiến trình standalone thực hiện MainMenu → Host/Join → Ready → Start qua KCP.
Chúng ghi danh sách layout và bounds/type collider thật để script so sánh chính
xác. Sau khi cả hai đã ghi layout, server đưa nhân vật được chọn ra ngoài mép
sàn tại (9; 1; 0), để gravity thật kéo xuống ngưỡng. Cả hai tiến trình phải thấy
HP nạn nhân bằng 0, Defeat, và HUD DEFEAT/VICTORY của người chơi local đúng vai.
Đây là phép thử physics/replication tự động; không thay cho kiểm tra hình ảnh bằng mắt.

Log, NUnit XML và JSON của từng tiến trình nằm trong TestResults/TV31 (Git ignore).
Kết quả chạy thực tế và các hạn chế được ghi trong mô tả PR.

## K?t qu? local ng?y 15/11/2026

Unity Editor 6000.6.8f1, Windows; ch?y trong worktree ri?ng t? commit c?c b?
52fe2fe, checkout main g?c v? ba file ch?a commit ???c gi? nguy?n.

| Ki?m tra | ??t | L?i | B? qua |
| --- | ---: | ---: | ---: |
| Unity EditMode | 142 | 0 | 0 |
| Unity PlayMode (g?m 1.000 seed, offline/replay v? Mirror fall) | 287 | 0 | 2 |
| Hai ti?n tr?nh Host/Join, client r?i | 1 | 0 | 0 |
| Hai ti?n tr?nh Host/Join, host r?i | 1 | 0 | 0 |

Hai ca b? qua thu?c dependency: IntegrationTests.WindowsInput_MouseMovements_AreDetected
v? WindowsInput_RemoteDesktopMouseMovements_AreDetected, c?ng l? do c? s?n
?Unstable due to 1252825?. C?c test d? ?n ??u ??t. Domain runner ??c l?p ??t
142 ca v? ph?t hi?n 13/13 mutation; kh?ng c?ng l?p c?c ca n?y v?o t?ng Unity.
Saved-layout verifier ??t 4.215 assertion. Standalone build Succeeded, 0 l?i;
15 c?nh b?o analyzer t? m? Mirror/dependency c? s?n.

- `19e1382be4d04b669299ca023dd4b356`: host r?i; 4 v?t c?n; layout v? collider hai ph?a gi?ng nhau; HP server = 0; HUD n?n nh?n DEFEAT, ng??i c?n l?i VICTORY; 0 l?i runtime.
- `a0e78705d9bd4ee5ae13615ea4fda57c`: client r?i; 3 v?t c?n; layout v? collider hai ph?a gi?ng nhau; HP server = 0; HUD n?n nh?n DEFEAT, ng??i c?n l?i VICTORY; 0 l?i runtime.

Ch?a ki?m ch?ng b?ng m?t ch?t l??ng h?nh ?nh/material v? tr?i nghi?m ?i?u khi?n
qua m?ng Internet c? ?? tr?/m?t g?i. Hai ti?n tr?nh ch?y localhost, headless;
harness ??a nh?n v?t ra m?p r?i d?ng gravity th?t, kh?ng gi? l?p vi?c ng??i d?ng
b?m ph?m ?i h?t qu?ng ???ng. Ch?a ch?y ca standalone dedicated server v? client
v?o mu?n; c?c ca n?y kh?ng n?m trong hai k?ch b?n Host/Join n?u tr?n. K?t qu? CI
v? Copilot review cho commit PR m?i nh?t ???c c?p nh?t tr?c ti?p tr?n PR.
