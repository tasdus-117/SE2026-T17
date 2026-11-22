# Vũ khí: hồi sinh sau khi người mang chết và chọn vị trí công bằng

Ngày triển khai: 20/11/2026. Nhánh `feature/tv1-m3-match-lifecycle`, nền `a07db32` đã có M3.1 và bản sửa teleport TH2. Unity 6000.6.8f1, Mirror 96.0.1.

## Hợp đồng vòng đời

`AvailableOnMap → CarriedByPlayer → RespawningAfterDeath → AvailableOnMap`.

- E vẫn gửi Command của avatar sở hữu; server kiểm tra phase, mạng sống, range, identity còn đăng ký và damage hiện tại. `NetworkLootItem` đặt cờ collection trước khi trao quyền mang vũ khí. Coordinator chỉ nhận một carrier thuộc phiên đang chạy. Sau khi cấp buff 1 → 2, map identity bị Destroy và thông báo đến observers; hồ sơ carrier sống độc lập với identity đã bị xóa.
- Nhặt vũ khí **không khởi động timer**. Người mang có thể giữ đến cuối trận mà không sinh thêm hộp. Khi carrier chết hoặc disconnect, coordinator trả damage về base nếu avatar còn tồn tại, bỏ đăng ký OnDeath và bắt đầu đầy đủ **30 giây** từ timestamp mất carrier. Không rơi hộp tại chân. Callback chết/disconnect trùng hoặc mất người không mang không khởi động lại timer.
- `GameplayConfiguration.WeaponRespawnDelaySeconds` bất biến, mặc định 30, được thêm cuối constructor để giữ tương thích các tham số trước. Giá trị phải hữu hạn và dương. Có thể cung cấp snapshot qua `ServerMatchLifecycleController.Configure` trước khi Host Start; mọi luật khác giữ nguyên (10 HP, 1/2 DMG, cooldown 1 s).
- Hết timer chưa đồng nghĩa đã có vật phẩm. Domain giữ trạng thái Respawning/ready đến khi server thực sự Spawn thành công. Không có điểm an toàn thì retry mỗi 0,5 giây, không fallback vào vật cản hoặc ra ngoài sàn. Khi phase Ended, hoặc deadline đã hết trước frame tick phase tiếp theo, hủy lease/timer và map item. Stop/Disable dọn callback và tham chiếu, không đưa trạng thái sang phiên mới.
- Bản gameplay hiện hành lấy loot đầu tiên ở Countdown sau khi các avatar đã spawn, theo cùng thuật toán an toàn/công bằng; chỉ nhặt được khi InProgress. Debug Bootstrap trực tiếp không có session vẫn giữ đường legacy. Single Player giữ cơ chế offline cũ.

## Tách Domain, Networking và Server

`LootRespawnTracker`, `LootPoint`, `FairLootSpawnSelector` là C# thuần trong Shared.Domain, không Unity/Mirror/clock hệ thống. Tracker nhận delta hữu hạn từ server. Selector nhận các candidate đã hợp lệ và snapshot tọa độ X/Z người Alive.

`ServerLootRespawnController` giữ lease theo ParticipantId và connection instance, nghe Health.OnDeath và disconnect của manager. Nhờ kiểm tra connection instance, callback cũ không thể làm mất lease của người khác có cùng numeric connection ID. Timer có mốc `respawnNotBefore` tuyệt đối để không bị rút ngắn bởi delta của frame trước khi chết hoặc sai số cộng trừ số thực.

`ArenaLootSpawner.ServerSpawnOverride` chỉ là điểm chuyển quyền dựng loot sang Server, không tham chiếu assembly Server. `NetworkLootItem` có claim callback server riêng cho map item được quản lý; pickup race guard cũ và các fixture/unmanaged item vẫn hoạt động. `ServerMatchLifecycleController.ArenaFloor` chỉ cung cấp sàn đã xác thực từ TH2. Không thêm logic combat vào NetworkPlayer.

Bảy asmdef không đổi: Domain/Protocol thuần C#, Networking dùng Mirror/Shared, Server và Client không tham chiếu nhau, hai assembly test cô lập. TV1 giữ mạng/server/tích hợp; TV2 giữ luật/config/combat; TV3 giữ input/UI/camera/arena. Không tạo assembly thứ tám hoặc sửa Mirror.

## Vị trí công bằng và kiểm tra vật lý

Mỗi lần thử dùng seed server và **24 candidate phân tầng 6×4**, có jitter trong mỗi ô. Biên candidate cách bounds sàn ít nhất 1 m. Với mỗi candidate:

1. Raycast xuống, chỉ chấp nhận đúng `ArenaFloor` (không nhận nóc cover hoặc platform ngoài bản đồ), normal gần hướng lên.
2. CheckBox clearance với nửa cạnh 0,9 m, đáy cách sàn 0,04 m, trên Environment/Player và bỏ trigger; không chồng cover, tường hoặc capsule player.
3. Loại điểm nằm cách người Alive dưới 2,5 m theo X/Z; không tính người chết, bị loại hoặc đã disconnect.
4. Chọn điểm có score lớn nhất:

   `score(p) = min_u ((p.x-u.x)^2 + (p.z-u.z)^2)`.

Đây là **maximin khoảng cách gần nhất**, không phải khoảng cách trung bình có thể bỏ qua người đứng sát hộp. Chỉ xoay thứ tự xét bằng seed để phân xử score bằng nhau, không cộng noise làm một điểm kém hơn thắng. Với một người, chọn điểm xa nhất; không có người Alive, chọn một candidate an toàn bằng tie seed; không có candidate thì trả không có lựa chọn.

Spawn anchor ở mặt sàn +0,3 m, phù hợp prefab hộp vàng scale 0,6. Chuyển động nổi/xoay là render-only của prefab hiện hữu. Đây là tối ưu trên tập candidate hiện tại, không khẳng định tìm cực đại toàn bộ mặt phẳng liên tục hoặc cân bằng mọi cấu trúc chiến thuật của bản đồ.

## Danh sách tệp

- Mới: `Assets/_Project/Scripts/Shared/Domain/LootRespawnTracker.cs` và metadata; `Assets/_Project/Scripts/Server/Combat/ServerLootRespawnController.cs` và metadata.
- Sửa: `GameplayConfiguration.cs`, `Networking/NetworkLootItem.cs`, `Networking/ArenaLootSpawner.cs`, `Server/Sessions/ServerMatchLifecycleController.cs`.
- Prefab: thêm coordinator vào `Assets/_Project/Prefabs/Network/ArenaNetworkRoot.prefab`, giữ GUID prefab và các component cũ. Không sửa scene, Player prefab, input actions hoặc tuning TH2.
- Test mới + metadata: `Tests/EditMode/FairLootSpawnTests.cs`, `Tests/PlayMode/WeaponRespawnAndObstaclesTests.cs`; cập nhật `tools/Run-DomainTests.ps1` để chạy các ca Domain mới.
- Tài liệu: tệp này và `PROJECT_CONTEXT.md` cục bộ, bằng tiếng Việt. Context/AGENTS không stage.

## Kiểm chứng

25 ca EditMode mới kiểm tra: giữ lâu không timer, đúng carrier mới kích hoạt, đủ 30 s, duplicate callback, hủy phiên, pickup chỉ có một người thắng, chu kỳ kế tiếp, dữ liệu sai, maximin thay vì average, zero/single player, tie seed và fallback rỗng. Bộ validation config tự thêm năm ca số không hợp lệ cho tham số mới. Runner Domain độc lập đã chạy **205/205 đạt**; bước này không thay native Unity.

Native PlayMode chạy trên **Arena_MVP thật**, qua MainMenu → Host/Ready/scene load với guest ảo. Các ca gồm:

| Ca | Assertion chính |
|---|---|
| Carrier bị đối phương đánh chết | Nhặt bằng Command, buff 2; đòn đánh guest qua serialized Command, HP 0; avatar hồi sinh buff 1; **đợi đủ 30 s thời gian server thật**, không hộp trước hạn; hộp mới hiện ở observer |
| 20 chu kỳ | 20 identity khác nhau, vị trí mới trong bounds/trên đúng sàn/không chồng vật cản; score bằng score lớn nhất trong các candidate hợp lệ và đủ khoảng cách; dùng delay 0,03 s có cấu hình cho test, không thay mặc định runtime |
| Non-carrier/Host teardown | Người không mang rời không thay đổi hộp/timer; Host Stop hủy lease |
| Carrier guest disconnect | Giữ Host đang chạy, mất đúng carrier kích hoạt timer và hộp mới an toàn |
| Match Ended | Hủy timer 30 s đang chờ, không còn map loot hoặc respawn muộn |
| Arena bị chặn | Timer ready vẫn giữ Respawning khi không có candidate; bỏ blocker thì retry Spawn thành công |

Kết quả đã chạy thật bằng native Unity 6000.6.8f1:

| Bộ kiểm chứng | Kết quả |
|---|---|
| PlayMode nhóm | **337/337 đạt, 0 lỗi, 0 skip** |
| PlayMode tính package | **341 tổng, 339 đạt, 0 lỗi, 2 skip vendor** |
| EditMode | **205/205 đạt, 0 lỗi, 0 skip** |
| Ca mới | **25/25 Domain**, **6/6 native loot**; là tập con của tổng, không cộng trùng |
| Compilation | **7/7 asmdef**, không lỗi compiler/Weaver hoặc cảnh báo compiler mã nhóm trong log cuối |
| Scaffold/layout | **1.504 cặp metadata/GUID**, không vòng; **4.305 assertion / 18 YAML**, build order đúng |

Hai skip vẫn là mouse Windows/RDP của package Input System, lý do `Unstable due to 1252825`; không ghi chúng là pass. Cả hai native runner thoát **0**; PlayMode chạy 176,68 s, bao gồm ca đợi 30 s thật và 20 chu kỳ respawn. **242 tệp nguồn/asset và metadata** khớp hash với bản sao native cuối. Toàn bộ package/settings/URP giữ nguyên byte, không dừng Editor người dùng hoặc sửa vendor.

Bằng chứng: `TestResults/WeaponRespawn/{PlayMode.xml,EditMode.xml,summary.json,tested-source-hashes.json,Compilation/results.json,scaffold.txt,layout.txt}`; log `Logs/WeaponRespawn-{PlayMode,EditMode}.log`. Lần focused đầu **5/5 đạt** trước khi thêm ca arena bị chặn. Báo cáo chạy/cache/binary bị ignore, không đưa vào Git. Không stage/commit/push; context/AGENTS tiếp tục bị ignore.

Native harness dùng Host engine thật và connection/observer guest ảo; không tuyên bố đã kiểm chứng hai tiến trình KCP/LAN hoặc mọi latency. Các assertion fairness dùng snapshot người chơi tại thời điểm server chọn điểm; người chơi vẫn có thể chạy đến hộp sau đó.

## Chạy lại và kiểm tra hai cửa sổ

Trong Unity Test Runner: EditMode → Run All, PlayMode → Run All. Lọc `SmallArena.Tests.EditMode.FairLootSpawnTests` hoặc `SmallArena.Tests.PlayMode.WeaponRespawnAndObstaclesTests` để chạy riêng. CLI Windows dùng Unity 6000.6.8f1, `-batchmode -nographics -projectPath <project> -runTests -testPlatform PlayMode -testFilter SmallArena.Tests.PlayMode.WeaponRespawnAndObstaclesTests -testResults <xml> -logFile <log>`; không thêm `-quit`. Dùng `Start-Process -WindowStyle Hidden -PassThru` và `WaitForExit()` trong script; nếu Editor gốc đang mở, chạy bản sao riêng đã đồng bộ sạch.

1. Build lại standalone, chạy cùng bản với Editor/server. Host và Join `127.0.0.1`, cả hai Ready rồi Start. Giữ nguyên 10 HP, cooldown LMB 1 s.
2. Dùng E nhặt hộp vàng, HUD damage lên 2. Đợi hơn 30 s khi vẫn sống: không sinh thêm hộp và damage không tự hết hạn.
3. Cho đối phương giết carrier. Damage về 1, không rơi hộp ở chân; avatar hồi sinh theo TH2 với 10 HP. Từ lúc chết, trước 30 s không có hộp; sau 30 s xuất hiện đúng một hộp trên sàn, tránh cover và cách xa vị trí người Alive lúc chọn.
4. Nhặt hộp mới và lặp lại; thử để carrier guest thoát, Host vẫn chạy và hộp hồi sinh sau 30 s. Thử guest không mang thoát: hộp hiện tại giữ nguyên.
5. Kết thúc trận/Leave Match trước deadline: không xuất hiện hộp muộn, tạo phòng mới được. Kiểm tra lại Single Player và năm lần hồi sinh TH2 để xác nhận parity.
