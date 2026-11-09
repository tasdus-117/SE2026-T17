# Chuẩn 10 HP, loot mạng và va chạm người chơi

## Hành vi và kiến trúc

- Cùng chuẩn **10 HP / damage 1**, cooldown 0,5 giây; weapon nâng vĩnh viễn lên
  **damage 2** đến hết trận. HP không hồi khi nhặt. UI local dùng panel 400×130,
  thanh HP nguyên 0–10, dòng HP và damage; overhead dùng cùng prefab cho Bot
  offline và đối thủ mạng. Single Player giữ việc hiển thị HP Bot theo loop cũ,
  Multiplayer hiển thị HP của local owner. Không thêm AI gây damage cho player offline.
- `NetworkPlayer.CmdRequestPickup(uint netId)` có requiresAuthority. Server tra
  registry, kiểm tra item/player còn spawn, health còn Playing, chưa có weapon
  và khoảng cách tâm **≤1,5 m**. `NetworkLootItem` đặt SyncVar IsCollected trước
  mọi side effect, tắt collider, tăng SyncVar damage rồi NetworkServer.Destroy.
  Callback Command chạy tuần tự trên main thread, không có yield trong commit.
  Request tiếp theo bị từ chối bởi flag hoặc registry đã xóa. Người đã damage 2
  không thể làm mất item khác; damage không tăng vượt 2.
- “Đến trước” là request server xử lý trước, không phải đồng hồ/phím phía client.
  Client chỉ chọn item gần nhất và gửi ID bằng E; không tự hide, destroy hay buff.
  E trước LMB trong cùng frame gửi hai Command Reliable theo thứ tự, như offline.
- `ArenaLootSpawner` trên root MainMenu/Bootstrap spawn đúng một hộp vàng tại
  **(-3;0,3;0)** khi server vào Arena. Prefab đăng ký vào spawnPrefabs của cả hai
  entry. Flag giữ item đã dùng không respawn bởi callback scene lặp và reset khi
  server dừng. Item Layer 9, trigger/kinematic; không chặn thân player. Bob/rotate
  áp lên render child, không đổi anchor/collider hoặc khoảng cách server.
- Matrix bật đúng cặp **Player–Player**. Server dùng CharacterController active,
  không thêm Rigidbody vào player; proxy client vẫn tắt physics và theo
  NetworkTransform ServerToClient. `ServerPlayerSeparation` chạy FixedUpdate sau
  movement, actor có netId nhỏ nhất xử lý từng cặp theo thứ tự ổn định, tối đa
  tám lượt. Khoảng cách capsule theo radius/height/center world; correction qua
  CharacterController.Move nên không xuyên tường. Nếu một nửa bị tường chặn,
  phần còn lại trả sang body kia. Bias xuống nhỏ giữ isGrounded cho Space.
  Không có singleton solver hoặc client viết official transform.
- Giữ bảy asmdef; Networking/Server/Client không tham chiếu vòng. Domain/Protocol
  vẫn thuần C#. Server/Simulation giữ collision, Networking giữ loot/combat state,
  Client/UI giữ view/presenter qua IHealthSource. Offline Player gỡ thêm solver;
  Health, pickup, Victory/replay offline giữ semantics cũ. Không tạo Gameplay.asmdef.

## Kiểm tra thủ công hai client

1. Chạy hai bản từ MainMenu: Host Room / Join IP, cả hai Ready, host Start.
   Xác nhận mỗi bên HP 10/10, damage 1 và overhead HP đối thủ cùng thang 10.
2. Tìm hộp vàng tại (-3;0,3;0), màu/kích thước như Single Player, có bob/rotate.
   Đi sát hộp (dưới 1 m) rồi nhấn **E**. Hộp biến mất ở cả hai; chỉ người nhặt
   thấy damage 2. Không tự biến mất trước xác nhận server khi thêm độ trễ.
3. Cả hai đứng gần hộp trong trận mới, cùng nhấn E. Chính xác một người lên 2,
   người còn lại giữ 1. Thử đảo người/độ trễ; không ưu tiên cố định host hoặc ID.
   Nhấn tiếp/giữ E không cộng buff lần nữa. Ngoài 1,5 m không nhặt được.
4. Người damage 2 đứng phía -Z của đối thủ cách khoảng 1 m, LMB: HP đối thủ
   giảm 10→8. Trận mới, unarmed làm 10→9. HP clamp 0, kết quả Win/Lose cũ hoạt động.
5. Đưa hai người về gần tâm arena: A phía -Z, B phía +Z. A giữ W, B giữ S;
   capsule dừng/chèn đẩy ở tiếp xúc, không đi xuyên nhau. Nhả một bên rồi bên
   kia tiếp tục đi để thử push; ép người đứng yên vào tường, không xuyên người/tường.
   Thử Space khi đang chạm người khác: grounded jump vẫn hoạt động.
6. Escape/Return Main Menu, tạo/join trận mới: loot trở lại đúng một hộp,
   damage reset 1 và HP reset 10. Không còn identity, collider hoặc singleton cũ.
7. Single Player: cùng HUD HP/damage/slider, overhead Bot, hộp vàng E buff 1→2,
   WASD/Space/LMB/J, Victory/replay như cũ. Visual animation cần kiểm tra trong
   Editor/player có hiển thị; batchmode không dựng render child.

## Kết quả thực thi ngày 08/11/2026

Unity **6000.6.8f1**, native Test Runner trong bản sao có cache; không dừng Editor
của người dùng. Hai runner cuối cùng trả mã 0.

| Kiểm chứng | Kết quả |
| --- | --- |
| Full PlayMode | **264 ca: 262 đạt, 0 lỗi, 2 bỏ qua package có sẵn** |
| PlayMode nhóm | **260/260 đạt**, không bỏ qua |
| LootAndCollisionTests mới | **13/13 đạt** |
| Combat/HUD trước đó | **13/13 đạt** với chuẩn mới 10/1 |
| Baseline trước nhiệm vụ | Giữ đủ 251 ca, cập nhật ba assertion cấu hình theo policy mới |
| EditMode | **34/34 đạt**, 0 lỗi/bỏ qua |
| Assembly | **7/7 đạt**, 0 lỗi/cảnh báo mã nhóm; Mirror weaving native thành công |
| Layout | **4.017 assertion / 18 YAML đạt** |
| Scaffold/GUID | **1.474 cặp metadata**, bảy asmdef không vòng; MainMenu 0 / Bootstrap 1 / Arena_MVP 2 |

Hai skip là package Input System, “Unstable due to 1252825”. Vendor/Mirror vẫn
có cảnh báo analyzer sẵn có; không che cảnh báo hoặc khẳng định vendor zero-warning.

Mười ba ca mới gồm metric 10→9, E/buff/punch 10→8, không predict trước server,
range/ID lạ/boundary 1,5 m, cả hai thứ tự race trong cùng pump server, authority
giả, player chết/đã buff, đối đầu trên server và native proxies, grounded jump,
spawn trùng tâm và pressure vào tường. Race dùng Command đã serialize với
hash/index lấy từ weaver; uint dùng đúng varuint của Mirror, không giả ownership.
Fixture chung tạo world trong thân test sau InputTestFixture.SetUp. Một native
NetworkClient và connection ảo không thay thế hai engine/KCP; checklist trên
và animation chưa được ghi là đã chạy qua hai cửa sổ trong nhiệm vụ này.

Bằng chứng cục bộ được ignore: `TestResults/LootParity/PlayMode-final.xml`,
`EditMode.xml`, `Focused-final.xml`, `tested-source-hashes.json`; log
`Logs/LootParity-PlayMode-final.log`, `Logs/LootParity-EditMode.log`;
layout/compile tại `TestResults/ArenaLayout/`. Không cộng các lượt chẩn đoán
vào tổng số ca đạt. Tool standalone lobby đếm riêng avatar và loot để không
nhầm identity item thành player; công cụ chưa build/chạy lại trong nhiệm vụ này.
