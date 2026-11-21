# Audit F1 #15, F2 #16, F3 #17 — Đối chiếu HEAD và kiểm chứng hành vi

Ngày thực hiện: 21/11/2026. Repository `tasdus-117/SE2026-T17`, nhánh `feature/tv1-m3-match-lifecycle`. Baseline trước sửa: **`ae90f0705c6b29fc2f3e851077e3f5a35bc64634`**, đã có M3.1/M3.2. Ba issue được viết từ static audit cũ tại `9ee7ea715b6fc5d07156411aac543a481585cee8`; không mặc nhiên coi phát hiện cũ vẫn đúng tại HEAD.

`gh issue list --state open --limit 30` không chạy được vì `gh` chưa cài/không có trong PATH và các vị trí cài chuẩn. Đã đọc danh sách issue mở và fetch riêng toàn bộ body #15/#16/#17 qua GitHub connector read API. Snapshot cục bộ ở `TestResults/AuditF123/issues.json`. Không cài CLI, đổi authentication, đăng comment hoặc đóng issue.

## Phát hiện và quyết định

| Issue / tiêu đề chính xác | Trạng thái trước sửa tại HEAD | Xử lý |
|---|---|---|
| [#15](https://github.com/tasdus-117/SE2026-T17/issues/15) — `[Audit F1][Cao][Cần runtime] Xác minh fall sớm khi chưa đủ avatar spawn có làm mất kết quả trận` | Không tái hiện trong hosted room hiện hành. M3.1 đã bind Health vào session và dùng InProgress/Alive gate chung. F1 host test đạt trước sửa; test replica engine riêng sau khi sửa fixture cũng đạt trên production HEAD chưa sửa. | Giữ gate đúng hiện hữu; thêm delayed AddPlayer + early-fall regression kiểm HP, result, score, SyncVar replica và UI. Không sửa FallDamage hoặc luật ranking để tạo một bản sửa không cần thiết. |
| [#16](https://github.com/tasdus-117/SE2026-T17/issues/16) — `[Audit F2][Cao] Giới hạn thời gian chờ avatar để một connection không khóa combat cả trận` | M3.1 đã có SpawnGraceSeconds/timeout, nhưng BindSpawn vẫn nhận request đến đúng/sau deadline nếu Tick chưa chạy; startup cho một người còn lại chơi. NetworkCombat còn kiểm toàn bộ transport connections thay vì roster của bound session. | Deadline kiểm cả trước Instantiate lẫn Domain bind; cấu hình grace 10 s trên prefab; bỏ global scan khỏi bound path. Sau timeout/mid-load disconnect: còn ≥2 avatar hợp lệ mới Countdown/InProgress; dưới 2 abort không winner/score rồi về phòng còn sống. |
| [#17](https://github.com/tasdus-117/SE2026-T17/issues/17) — `[Audit F3][Trung bình] Khóa thực thi gameplay offline khi readiness chưa đạt` | Tái hiện thật: Initializing vẫn WASD/jump, LMB làm Bot 10→9, E thu pickup. GameManager chặn state transition nhưng actor không đọc quyền thực thi. | GameManager đẩy quyền Playing qua contract Domain tới movement/combat, mặc định khóa. Input tiếp tục khởi tạo; probe kiểm technical readiness riêng. Pickup trực tiếp cũng bị chặn qua CanInteract. |

## Một spawn barrier authoritative cho F1/F2

- Session khóa roster trước đổi scene; các participant đã được giữ chỗ cùng lúc nên cùng lấy mốc `LockRoster + SpawnGraceSeconds`. Mỗi người chưa spawn bị kiểm và loại riêng; người đã spawn không bị timeout này. Mặc định **10 s**, chỉnh bằng `spawnGraceSeconds` trong ArenaNetworkRoot hoặc snapshot `GameplayConfiguration` trước BeginSession. Test dùng grace ngắn để chạy nhanh.
- `CanBindSpawn(connectionId, now)` kiểm roster đã khóa, Waiting, đúng connection còn sống/chưa spawn và **now < deadline**. Server gọi trước tạo object/reserve marker; `BindSpawn` kiểm lại. Đúng tại deadline cũng bị từ chối, không phụ thuộc thứ tự AddPlayer/Update.
- Khi đến deadline, Domain xóa active binding của những người chưa spawn, giữ record Disconnected để truy vết. Server yêu cầu Mirror disconnect; callback hiện hữu dọn lobby membership/owned identities/spawn allocation. Không tự dựng lại transport hoặc giữ identity mồ côi.
- Sau loại peer: **≥2 connected/spawned** mới tiến Countdown → InProgress. Disconnect trong Countdown làm thiếu người cũng abort trước FIGHT. **<2** chuyển `OnlineMatch.Aborted`, không winner, không kill/death/damage giả; room mode trở về MainMenu/Lobby ngay khi scene transition hiện tại hoàn tất. Giữ Host/connection/slot còn lại, Ready=false và có thể đón người mới. Quy tắc này áp dụng startup; không đổi luật disconnect hoặc ranking bốn tầng của trận đã InProgress.
- Bound combat tin `NetworkHealth.CanAct` → session gate, không đếm mọi connection của transport. Scan legacy chỉ còn ở path unbound/debug; đường MainMenu hosted-room luôn bind lifecycle. Peer bị từ chối đã bị server đặt unauthenticated và không thể khóa trận đang chơi.
- Fall/environment đi qua cùng Health runtime gate; trước InProgress không mất HP hoặc chốt kết quả dựa trên số avatar tạm thời. `NetworkMatchRules` legacy bỏ qua lifecycle-bound health như trước. Khi FIGHT bắt đầu, avatar còn nằm dưới FallHeight chịu lethal damage bình thường, ghi đúng một environmental Death rồi respawn.
- Giữ `disconnectInactiveConnections = false`: hạn spawn là chính sách match roster rõ ràng, không phụ thuộc heartbeat timeout có thể đá nhầm client tải scene. Test đọc prefab thật và xác nhận cấu hình này trong cả normal load/stall/disconnect fixture.

Không sửa offline fall damage, NetworkHealth, NetworkMatchRules, cooldown 1 s, HP 10, buff 1→2, loot/respawn Maximin 30 s, ArenaNetworkTransform teleport, clock, ranking M3.2 hoặc networking authority.

## Readiness offline tách khỏi Mirror

`IMatchGameplayParticipant.SetGameplayAllowed(bool)` là contract C# thuần trong Domain. GameManager cấp quyền cho component trên player khi **Playing và manager active**, khóa ở Configure/Awake/Initializing, terminal state và OnDisable. Quyền được cập nhật trước thông báo state cho observer.

PlayerController dừng Move/gravity/jump, reset vertical velocity khi khóa, vẫn consume/drop jump intent. CombatController consume/drop attack/interact intent rồi kiểm permission; `CanPunch`/`CanInteract` đều false khi khóa. WeaponPickup.TryCollect vốn kiểm CanInteract nên direct-call cũng không vượt gate. Không cần thêm authority vào pickup/UI hoặc truy cập Server từ Client.

**Không dùng permission để tắt input hoặc readiness:** Input Actions vẫn enabled, IsInputReady/IsMovementReady vẫn đo khả năng khởi tạo. `OfflineMatchReadinessProbe` trước đây dùng CanInteract; nếu giữ nguyên sẽ tạo vòng “chưa Playing → không tương tác → không ready → không Playing”. Nay dùng `IsCombatReady` (component active, chưa EndMatch), độc lập permission. Full scene readiness/HUD vẫn bắt buộc như trước.

Actor mặc định không có quyền gameplay. Fixture mechanics tách rời GameManager cấp quyền tường minh cho các unit component; test readiness dùng prefab với permission mặc định khóa. Victory/Defeat vẫn dùng EndMatch hiện hữu; reload tạo actor/input/match mới. Không dùng singleton/static gate hoặc giữ subscription mới.

## Bằng chứng Red và đối chiếu HEAD

Chưa sửa production khi chạy lượt Red đầu trong bản sao Unity riêng:

- Domain: **241 total, 237 passed, 4 failed**. Hai ca deadline 2.000/2.001 vẫn được nhận; một ca dưới hai người đi Countdown; một ca disconnect trong Countdown vẫn Playing. Trước deadline 1.999 vẫn được nhận đúng.
- Native focused Red: **45 total, 37 passed, 8 failed**. F1 host đã đạt; F2 hai ca thiếu người không về Lobby; F3 năm assertion hành vi thất bại. Một thất bại còn lại do test tưởng peer vừa reject vẫn nằm trong connections, nhưng harness tự ACK và server đã disconnect; đã sửa precondition của test, không đổi server để khớp giả định sai.
- Bổ sung F1 native replica: fixture đầu dùng DestroyPlayerForConnection rồi cố spawn lại cùng connection mà bỏ qua reservation của manager, nên timeout (artifact `Head-F1-Fixture-initial.xml`). Đã sửa fixture trì hoãn **lần spawn đầu**, không hủy avatar để giả lập loading; production HEAD vẫn giữ nguyên trong validation copy. Chạy lại **2/2 đạt** gồm native replica và rejected-peer control.
- Sau patch: Domain **241/241 đạt**; native focused **56/56 đạt**, gồm audit, MatchInitialization, lobby integration và VictoryFlow/replay. Assertion không bị bỏ qua hoặc nới lỏng để tạo pass.

### Các ca regression chính

| Nhóm | Ca / assertion quan sát được |
|---|---|
| F1 hosted room | Delayed AddPlayer + host Y=-6: HP=10/result Playing/deaths=0/UI result ẩn khi Waiting; khi guest spawn, InProgress → đúng một environmental Death → HP10/life2, không winner giả. |
| F1 native client | Client health và match-state là object khác server; SyncVars HP10/Playing/Waiting đúng, sau release thấy HP0 rồi HP10/life2 và một Death; UI không Victory/Defeat sớm. |
| F2 normal load | Guest tải chậm trong grace vào marker khác; sau Countdown đòn đánh thật làm HP10→9. |
| F2 deadline | 1.999 nhận; 2.000/2.001 từ chối dù Tick chưa chạy. |
| F2 timeout, đủ người | Host + guest hợp lệ + guest không AddPlayer: peer chậm bị loại, còn hai người vào InProgress/đánh được, không tặng score; late callback không tạo object. |
| F2 timeout, thiếu người | Chỉ Host còn lại: Aborted, winner rỗng, điểm 0, về Lobby với Ready=false, không orphan; guest mới dùng lại slot. |
| F2 mid-load disconnect | Thiếu người trước FIGHT: abort/room reset, late AddPlayer cũ bị từ chối; Domain cũng kiểm disconnect trong Countdown. |
| F3 blocked initialization | Prefab thật, probe Fail: vị trí đầy đủ XYZ bất biến khi W+D+Space; LMB không trừ HP; E/direct pickup không thu; InputReady/MovementReady vẫn true. |
| F3 retry/owner lifecycle | Repair probe rồi TryStartMatch: không replay J/E đã bị drop, fresh WASD/J/E hoạt động; disable manager khóa action nhưng input vẫn ready. |

Test file mới `AuditSpawnLifecycleTests.cs` chứa hai fixture (7 test). `MatchInitializationTests.cs` nâng ca InitialState thành kiểm hành vi thật và thêm 4 ca. `MatchLifecycleDomainTests.cs` thêm 5 ca (gồm parameterized deadline) và cập nhật policy dưới hai người; clock regression M3.2 dùng đủ hai participant để không kiểm clock trên session đã abort. Tổng tăng **5 EditMode và 11 PlayMode**, không cộng trùng focused vào full.

## Cổng nghiệm thu cuối

Full native cuối ngày **21/11/2026**, Unity **6000.6.8f1**; cả hai invocation trả **exit 0**, XML gate hiện hữu đạt:

| Gate | Kết quả thực chạy |
|---|---|
| EditMode | **241/241 passed**, 0 failed, 0 skipped |
| PlayMode project | **355/355 passed**, 0 failed, 0 skipped |
| PlayMode gồm package | **359 total: 357 passed, 0 failed, 2 skipped vendor** |
| Native focused | **56/56 passed**, 0 failed/skipped |
| Domain runner riêng | **241/241 passed**, 0 failed/skipped |
| Strict compilation | **7/7 asmdef**, 0 project compiler warnings/errors; native compiler/weaver diagnostic count 0 |
| Scaffold | Graph không vòng; **1.510 cặp metadata**, GUID hợp lệ; MainMenu 0 / Bootstrap 1 / Arena_MVP 2 |
| Layout | **4.536 assertions / 18 YAML** đạt |
| Tính toàn vẹn | **254** file source/asset/meta khớp hash bản native, **48** file package/settings giữ nguyên hash |

Hai skip thuộc Input System Windows/RDP mouse, lý do `Unstable due to 1252825`, không phải test nhóm và không tính skip thành pass. Không có test nhóm bị skip hoặc ignore để đạt gate.

Các báo cáo/log ở `TestResults/AuditF123/{editmode-results.xml,playmode-results.xml,Green-Focused.xml,Red-PlayMode.xml,Red-Domain.xml,Head-F1-Replica.xml,summary.json,Compilation,scaffold.txt,layout.txt,tested-source-hashes.json,preserved.json}` và `Logs/AuditF123-*` (ignored). Không đưa cache/binary/secrets vào Git. Raw Red/fixture-failure vẫn được giữ, không ghi đè thành green.

Chạy native theo [README](../../README.md#5-kiểm-thử-và-biên-dịch), dùng Unity 6000.6.8f1, bản sao `TestResults/TV11/ValidationProject` đã đồng bộ nguồn, không chạy hai Editor trên cùng project. Không dùng `-quit` cùng `-runTests`. Filter cho lượt focused:

```text
-testPlatform PlayMode -testFilter "SmallArena.Tests.PlayMode.AuditSpawnLifecycleTests;SmallArena.Tests.PlayMode.AuditFallReplicaTests;SmallArena.Tests.PlayMode.MatchInitializationTests"
-testPlatform EditMode -testFilter SmallArena.Tests.EditMode.MatchLifecycleDomainTests
```

Đây là argument cho Unity.exe, không phải lệnh shell độc lập. Full regression và compiler/scaffold/layout gate theo README vẫn bắt buộc.

## Danh sách file thay đổi

| Phần | File |
|---|---|
| Domain | `Scripts/Shared/Domain/MatchSessionRuntime.cs`, `MatchViewContracts.cs` |
| Server | `Scripts/Server/Sessions/ServerMatchLifecycleController.cs`, `Scripts/Server/Simulation/GameManager.cs`, `OfflineMatchReadinessProbe.cs`, `Scripts/Server/Combat/CombatController.cs` |
| Client / Networking | `Scripts/Client/Presentation/PlayerController.cs`, `Scripts/Networking/NetworkCombat.cs` |
| Prefab | `Prefabs/Network/ArenaNetworkRoot.prefab`: chỉ thêm grace 10 s, giữ inactive-disconnect=false và GUID |
| EditMode | `Tests/EditMode/MatchLifecycleDomainTests.cs`, `MatchScoringAndRankingTests.cs` |
| PlayMode | Thêm `Tests/PlayMode/AuditSpawnLifecycleTests.cs` + `.meta`; sửa `MatchInitializationTests.cs`, `NetworkMatchLobbyIntegrationTests.cs`, `NetworkGameplayFixture.cs`, `OfflinePlayerFixture.cs` |
| Tài liệu | `README.md`, báo cáo này; `PROJECT_CONTEXT.md` tiếng Việt cục bộ/ignored |

Các đường dẫn Scripts/Prefabs/Tests đều dưới `Assets/_Project/`. Không thêm assembly/package, không đổi scene hoặc ProjectSettings, không sửa vendor. Giữ bảy asmdef (năm production + hai test); Client.UI là namespace, Domain/Protocol engine-free; Server và Client không tham chiếu nhau.

## Kiểm tra thủ công

1. Build lại cùng mã. Hai cửa sổ Host/Join → Ready/Start bình thường, countdown xong mới di chuyển/đánh/nhặt; Tab/feed/respawn/loot và rematch vẫn như M3.2.
2. Dùng client kiểm thử/debugger trì hoãn Ready/AddPlayer sau nhận đổi scene. Trong grace 10 s, avatar khác không mất HP do fall và không xuất hiện kết quả sớm. Cho client hoàn tất trong grace: cả hai chơi bình thường.
3. Với hai connection, giữ một connection không AddPlayer quá grace: server loại peer đó và đưa người còn lại về room, Ready=false, không winner/score giả. Kết nối lại kiểm slot tái sử dụng.
4. Với ba connection, hai avatar hoàn tất, một peer trì hoãn: hết grace chỉ peer chậm bị đá; hai người còn lại chơi/đánh được. Thử disconnect tự nguyện khi còn loading hoặc Countdown.
5. Offline: trước Play, làm thiếu HUD reference của readiness provider. Khi báo startup blocked, WASD/Space/LMB/E không thay vị trí/HP/pickup; input vẫn được khởi tạo. Khôi phục wiring và vào Play mới: gameplay, Victory/Defeat/Play Again hoạt động. Regression cũng kiểm retry TryStartMatch trực tiếp.

Harness native dùng client engine thật và connection/observer ảo trong cùng tiến trình. Chưa chạy nhiều executable KCP/LAN hoặc đo latency/load trong nhiệm vụ này; không suy thành chứng nhận mọi trường hợp multiplayer/dedicated server.

## Comment đề xuất, chưa đăng lên GitHub

### #15 — F1

Đã đối chiếu issue với HEAD hậu M3.1/M3.2: không tái hiện early-fall/premature-result trong hosted room. Health runtime đã dùng spawn/phase/life gate chung, còn legacy result resolver bỏ qua bound actors. Thêm regression delayed AddPlayer + early fall cho Host và native client replica: Waiting giữ HP10/Playing, không result UI; sau FIGHT ghi đúng một environmental Death và respawn HP10/life2, không winner giả. Giữ nguyên offline fall và ranking M3.2. Full native: EditMode 241/241, PlayMode nhóm 355/355, 0 failed; 7/7 assemblies sạch compiler. Bằng chứng: `docs/testing/audit-f1-f2-f3.md`.

### #16 — F2

Thống nhất bound combat với sealed session roster, kiểm spawn deadline cả trước tạo avatar và trong Domain BindSpawn (đúng/sau deadline bị từ chối). Prefab có spawn grace 10 s; timeout loại riêng peer chưa spawn. Còn ≥2 spawned participants thì bắt đầu bình thường; dưới 2 hoặc disconnect làm thiếu người trước FIGHT thì abort không score/winner và về phòng giữ connection/slot/Ready=false. Regression bao gồm normal load, boundary trước Tick, timeout hai nhánh, mid-load disconnect và slot reuse. `disconnectInactiveConnections=false` được giữ và kiểm thử. Full native: EditMode 241/241, PlayMode nhóm 355/355, 0 failed; bằng chứng: `docs/testing/audit-f1-f2-f3.md`.

### #17 — F3

GameManager nay cấp quyền offline gameplay theo Playing/active qua contract Domain; actor mặc định khóa. Movement/jump, attack và cả direct pickup bị từ chối khi Initializing/readiness Fail; input vẫn khởi tạo. Probe dùng technical IsCombatReady thay CanInteract để tránh vòng readiness. MatchInitializationTests dùng prefab thật và kiểm XYZ/HP/pickup, drop press cũ, retry thành công và disable-owner; suite offline Victory/Defeat/replay giữ pass. Full native: EditMode 241/241, PlayMode nhóm 355/355, 0 failed; bằng chứng: `docs/testing/audit-f1-f2-f3.md`.
