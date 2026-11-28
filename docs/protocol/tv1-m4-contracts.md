# M4.0 — Room Directory/Code và Retained-Session Reconnect

> **DRAFT — đề xuất kỹ thuật để TV1/TV2/TV3 review, chưa được ký duyệt hoặc triển khai.** Tài liệu được viết trên `main` **`5fb5df93003ee72deece21c6d7e215e2615bc569`**, nhánh `feat/tv1-m4-contracts`, ngày 28/11/2026. Các tên DTO/interface/message mới bên dưới là hợp đồng đề xuất, không phải API C# đã tồn tại. Không dùng tài liệu này để đánh dấu M4 hoặc nghiệm thu M3 đã hoàn thành.

## 1. Phạm vi và đối chiếu M3

- Công nghệ giữ Unity 6000.6.8f1, Mirror/KCP, Windows x64 Mono và bảy asmdef: năm production + hai test. `SmallArena.Client.UI` là namespace trong Client.
- Domain/Protocol phải engine-free. Networking không tham chiếu Server/Client; Server và Client không tham chiếu nhau. TV1 nối qua hợp đồng/event/delegate, không biến NetworkPlayer thành controller tổng hợp.
- Giữ M3 **H10/D1/C1/R1.8**, buff D2, loot30s, score/rank, ArenaNetworkTransform, offline và rematch. Không rollout H100/D25/C0.5/R30, host migration, prediction, lag compensation hoặc Phase 3 CI trong M4.0.
- Đây là đặc tả Room Directory/Public Hub và reconnect; không tự chứng nhận Dedicated Server build, matchmaking toàn Internet hoặc NAT relay.

| Seam đã kiểm trong source | Hiện có | Nghĩa vụ mở rộng M4, chưa triển khai |
|---|---|---|
| [GameplayConfiguration](../../Assets/_Project/Scripts/Shared/Domain/GameplayConfiguration.cs) | `double reconnectGraceSeconds = 15`, property `ReconnectGraceSeconds` bất biến; không phải field float | Dùng snapshot config của phiên, không tạo timer 15s hardcode song song |
| [MatchViewContracts](../../Assets/_Project/Scripts/Shared/Domain/MatchViewContracts.cs) | Read-only health/damage/match interfaces; chưa có directory/reconnect view contract | Bổ sung read-model interface cùng nguyên tắc, UI không gọi trực tiếp Server |
| [ArenaNetworkManager](../../Assets/_Project/Scripts/Networking/ArenaNetworkManager.cs) | Disconnect hủy player, trả reservation và phát callback cleanup | Phân nhánh retained-disconnect trước destroy/base cleanup; không gọi cleanup terminal rồi mới cố giữ phiên |
| [MatchSessionRuntime](../../Assets/_Project/Scripts/Shared/Domain/MatchSessionRuntime.cs) | Phase `WaitingForPlayers`, `Countdown`, `InProgress`, `Ended`; connection binding chưa hỗ trợ rebind | Thêm connection-state/generation/rebind transaction, tách khỏi LifeState; không sửa tên phase M3 |
| [ArenaLaunchOptions](../../Assets/_Project/Scripts/Networking/ArenaLaunchOptions.cs) | `--arena-server`, `--arena-client`, `--arena-host`, `--arena-address`, `--arena-port` | Giữ nguyên semantics các flags thật này; `--server`/`--connect` không phải alias hiện có |
| [ArenaObstacles](../../Assets/_Project/Scripts/Networking/ArenaObstacles.cs) | SyncList mô tả collider đầy đủ; seed lấy từ GUID khi generate, chưa giữ lại seed runtime | Snapshot phải mang layout authoritative; không giả định chỉ gửi seed là tái dựng đúng |

Thuật ngữ `Ending` trong yêu cầu M4 chỉ **cửa sổ post-match của phase `Ended`**; không thêm enum phase hoặc chuyển ngược phase để reconnect. `RoomInstanceId` định danh một lần host phòng; `MatchSessionId` là phiên/round M3; `PlayerSessionId` định danh một lần admission của người chơi vào phòng; `ParticipantId` định danh participant trong round. Bốn định danh không được thay bằng `connectionId` hoặc RoomCode.

Nguồn đối chiếu: [protocol M3](protocol.md), [CT-201–205](tv2-004-contracts.md), [source audit](../testing/tv2-current-contract-audit.md). Các đoạn lịch sử trong nguồn không thay thế seam đã kiểm tại SHA đầu tài liệu.

## 2. Quy ước hợp đồng chung

- `MUST`/“phải” là điều kiện nghiệm thu draft; “hiện có” chỉ được dùng trong bảng source phía trên. Giá trị mặc định mới là lựa chọn của draft, cần ba vai trò review.
- DTO Domain là immutable value/read model; DTO wire và version thuộc Shared.Protocol; không chứa Unity/Mirror connection, Transform, Task UI hoặc delegate serialization.
- Interface `IRoomDirectoryService` thuộc Shared.Protocol, dùng DTO Domain và kiểu .NET thuần; adapter UDP/registry ở Networking, chính sách session/auth/lease ở Server, presenter ở Client.UI. Không thêm asmdef.
- Control messages dùng delivery reliable; movement vẫn Unreliable, state replication theo M3. Không hứa total order giữa hai kênh; không thêm application retry cho shot/pickup.
- Wire M4 có `ProtocolVersion = m4-draft-v1`; implementation phải chốt encoding/size và negotiation trước admission. Peer không tương thích bị từ chối trước cấp slot/token. Không âm thầm đổi payload M3 để client build cũ đọc nhầm.
- Tất cả mutation room/rebind/expiry phải serialize trên server hoặc atomic transaction tương đương; callback async directory quay về main thread trước khi chạm Unity/Mirror.
- UTC chỉ phục vụ listing; grace, cooldown và baseline deadlines dùng server monotonic time cùng miền `ServerNow` của M3. Không tin deadline do client gửi.

## Phần A — Dedicated Room Code và Public Multiplayer Hub

### CT-401 — Điều hướng, RoomCode và vòng đời đăng ký

Luồng sản phẩm: `MainMenu → MultiplayerHub → LobbyRoom → Arena_MVP`. Hub/LobbyRoom là **trạng thái UI**, không yêu cầu scene mới; giữ build order MainMenu0/Bootstrap1/Arena_MVP2. Nút Single Player giữ đường offline hiện tại.

1. Multiplayer mở Hub, chưa tạo connection/room tự động và không buộc người dùng nhập IP.
2. Hub có Public Room List, Refresh, auto-poll, Room Code input/Join by Code, Host Room và Back. `CharacterPreviewSlot` là vùng bố cục dành sẵn, không bao gồm model/customization hoặc gameplay trong M4.0.
3. Host Room tạo host, lấy KCP endpoint đang lắng nghe rồi claim RoomCode/register `IsPublic=true`. Chỉ sau bind transport + claim thành công mới công bố phòng và vào LobbyRoom; lỗi đăng ký phải báo rõ, dọn provisional host/lease, không hiện mã chưa hợp lệ.
4. LobbyRoom hiện RoomCode và Copy. Copy chỉ sao chép mã sáu ký tự, không token/endpoint. Register/Host click lặp không tạo hai room hoặc hai host.
5. Back từ Hub hủy query/poll, không để callback cũ đổi UI. Rời phòng/StopHost hủy registration, heartbeat và token; code được release. Host crash dựa lease expiry ở CT-402.

**Định dạng duy nhất:** sáu ký tự từ `ABCDEFGHJKLMNPQRSTUVWXYZ23456789` (32 ký tự; không `0/O/1/I`). Client normalize `Trim().ToUpperInvariant()` trước validation; không bỏ dấu/khoảng trắng bên trong, không truncate. Server kiểm lại độ dài/alphabet; chuỗi chứa ký tự Unicode tương tự ASCII bị từ chối. RoomCode là locator công khai, **không phải mật khẩu hoặc reconnect proof**.

Code do server/host sinh bằng nguồn ngẫu nhiên mật mã, không derive từ tên/IP/thời gian. Không có modulo bias khi chọn 32 ký tự. Claim gắn `(DirectoryScopeId, RoomInstanceId, RoomCode, LeaseId)`; mọi update/release phải khớp lease hiện hành. Collision thì sinh lại, tối đa32 lần cho một thao tác Host; hết budget trả lỗi DirectoryUnavailable và cleanup, không publish trùng mã.

**Miền uniqueness:** registry trung tâm phải có atomic compare-and-claim trên toàn namespace của nó. LAN độc lập không thể bảo đảm uniqueness toàn cục khi subnet bị partition. LAN adapter phải probe/claim, phát hiện hai RoomInstanceId dùng cùng mã và fail-closed khi resolve; chủ claim thua theo so sánh ordinal RoomInstanceId phải cấp mã mới rồi cập nhật LobbyRoom. Trong lúc conflict chưa hội tụ, không tự chọn một endpoint. Đây là giới hạn của LAN, không được quảng cáo global uniqueness; nếu yêu cầu tuyệt đối trên nhiều subnet, phải dùng central registry. RoomInstanceId trong handshake ngăn mã cũ tái sử dụng đưa client vào nhầm phòng.

### CT-402 — Directory, listing, lease và hai mode

`RoomListingInfo` thuộc Domain; validation trước khi vào view hoặc wire:

| Field | Kiểu/giới hạn và ý nghĩa |
|---|---|
| `RoomCode` | string ASCII chuẩn hóa, đúng CT-401 |
| `RoomName` | string plain text, 1–64 UTF-8 bytes sau trim; không control characters |
| `HostDisplayName` | string plain text, 1–32 UTF-8 bytes; không phải identity/auth |
| `CurrentPlayers` | int0–4, số slot đang chiếm; **gồm reserved slot trong reconnect grace**, không gồm provisional/rejected connections |
| `MaxPlayers` | int, luôn4; listing sai giá trị bị loại |
| `Phase` | `RoomListingPhase.Lobby` hoặc `InMatch`; Loading/WaitingForPlayers/Countdown/InProgress/Ended đều map InMatch đến khi thật sự về lobby |
| `IsPublic` | bool, mặc định true; false không vào Public Room List, vẫn có thể resolve code nếu admission cho phép |
| `LastHeartbeatUtcMs` | long UTC milliseconds không âm do authority phát; chỉ hiển thị/chẩn đoán, không dùng làm gameplay clock hoặc freshness proof |

Envelope listing còn có `ProtocolVersion`, `DirectoryScopeId`, `RoomInstanceId`, `LeaseId`, monotonic `ListingRevision`; endpoint nằm trong adapter/resolution, không bắt UI nhập IP. Không đưa ReconnectToken hoặc PlayerSessionId vào listing công khai. Cùng room chỉ áp dụng revision mới hơn; lifecycle mới dùng RoomInstanceId mới, không hồi sinh cache từ heartbeat cũ.

Hợp đồng logic của `IRoomDirectoryService` (signature C# cụ thể sẽ được chốt khi implementation):

| Operation | Input → output / nghĩa vụ |
|---|---|
| `ListPublicRooms` | Scope + cancellation → immutable listing snapshot; chỉ public, còn lease, version hợp lệ; sort RoomCode ordinal |
| `ResolveRoom` | Normalized code → `RoomJoinResolution`; không tự mở KCP connection |
| `RegisterRoom` | Host listing + endpoint + RoomInstanceId → atomic claim/lease hoặc failure |
| `HeartbeatRoom` | Lease + revision + occupancy/phase → refresh nếu đúng owner; delayed revision không rollback |
| `UnregisterRoom` | Lease → release idempotent; lease cũ không xóa room mới cùng code |

**Mode1 — LanDiscovery, mặc định:** discovery UDP broadcast/request/response trên subnet + đường loopback explicit cho localhost. Host trả discovery response; không cần server ngoài, tài khoản hoặc registry cài riêng. Adapter phải hỗ trợ nhiều cửa sổ và nhiều room trên cùng máy: không giả định broadcast quay lại sender hoặc mọi socket chia sẻ discovery port đều nhận packet. Query có RequestId/nonce và response version; chỉ nhận response thuộc query còn hạn. Discovery port và KCP game port tách biệt, cấu hình thống nhất cho nhóm; port cụ thể là cấu hình triển khai phải ghi trong evidence, không thay mặc định KCP7777 của M3. Nhiều host cùng máy phải bind game endpoint khác nhau thành công trước publish.

**Mode2 — CentralRegistry:** registry endpoint cấu hình explicit qua phần mở rộng `ArenaLaunchOptions`/`config/`, dùng cùng interface/DTO/UI boundary. HTTP(S) hoặc UDP adapter phải giữ semantics lease/atomic claim; HTTP lỗi/timeout không được diễn giải thành RoomNotFound. Internet phải dùng HTTPS cho registry và bảo vệ credential theo CT-406; registry không tự cung cấp NAT traversal/relay. Endpoint được quảng bá phải reachable với topology đã kiểm. Không tự fallback mode khi query lỗi, không gộp namespace LAN và registry rồi resolve mơ hồ.

Defaults directory draft: auto-poll5 s khi Hub đang mở, manual refresh debounce1 s, mỗi service instance chỉ một list query đang chạy; query timeout2 s. Host heartbeat2 s, lease TTL6 s; quyết định expiry bằng monotonic time tại registry hoặc local receipt tại LAN observer, không bằng chênh UTC giữa hai máy. Heartbeat và TTL không thay reconnect grace15 s. Hết lease phải bỏ listing; room host còn sống phải claim lại trước quảng bá. Client chỉ thấy trạng thái lỗi/stale rõ ràng, không cache room thành available vô thời hạn.

Response được giới hạn64 listing/trang và16 KiB cho một trang logical; UDP datagram tối đa1200 bytes, pagination/fragmentation có bounds/request identity trước khi parse. Không reflection deserialize kiểu do peer chọn, không execute dữ liệu listing. Client/server rate-limit request, cap inflight peers và hủy query khi rời Hub; các budget được ghi trong cấu hình/test, không phát phản hồi khuếch đại không giới hạn tới địa chỉ tùy ý.

### CT-403 — Resolve và join bằng list/code

`RoomJoinResolution` thuộc Domain:

| Field | Quy tắc |
|---|---|
| `Success` | bool; true chỉ là resolve thành công, chưa phải admission thành công |
| `FailureReason` | Nullable enum; null khi Success=true; khi false bắt buộc một trong InvalidCodeFormat, RoomNotFound, RoomFull, MatchAlreadyInProgress, DirectoryUnavailable |
| `ResolvedEndpoint` | Nullable engine-free value `(Transport=Kcp, Address, Port)`; chỉ có khi true; address không trắng, port1–65535, không command/path thực thi |

Response envelope giữ RoomInstanceId/DirectoryScopeId/version để connection handshake xác nhận phòng đích. Endpoint là dữ liệu nội bộ của controller/adapter, không thêm bước bắt người dùng nhập IP. Khi false không trả endpoint cũ. Cả click listing và Join by Code gọi cùng pipeline:

`normalize → local validate → directory resolve → connect endpoint → server xác nhận room instance/version/admission → LobbyRoom`.

Thứ tự lỗi directory: code sai → InvalidCodeFormat; query không hoàn tất/conflict → DirectoryUnavailable; không có lease hợp lệ → RoomNotFound; InMatch → MatchAlreadyInProgress; Lobby đã chiếm4 slot → RoomFull; còn lại resolve thành công. Khi phase/full cùng đúng, Phase được ưu tiên cho resolve; server vẫn giữ thông báo room-full chuẩn nếu admission chạm cap thực tế.

List snapshot có thể cũ: server admission là quyết định cuối, kiểm lại phase/cap/room instance, không tin CurrentPlayers phía client. Nếu phòng đóng giữa resolve và connect, báo không vào được và giữ Hub; không âm thầm join mã vừa tái cấp cho phòng khác. Reconnect hợp lệ dùng CT-406 riêng, không bị loại bởi fresh-join filter InMatch/RoomFull; không chiếm slot thứ5.

### CT-404 — Giữ governance M2 và CLI

- Fresh join chỉ ở Lobby; tổng occupied slots ≤4, kể cả reconnect reservations. Slot tối đa0–3, host badge do server xác nhận, client không gửi quyền host hoặc slot authoritative.
- Peer thứ5 nhận modal **Cannot join: Room is full (4/4)**, graceful disconnect, không spawn/ready/ảnh hưởng bốn người cũ. Provisional socket cho reject/reconnect không phải slot mới; phải có timeout/budget để không DoS.
- Chỉ host Start khi ít nhất2 thành viên hiện tại **connected + baseline-ready** và tất cả occupied slots Ready. Reconnecting slot luôn Not Ready, làm Start khóa; server kiểm lại, không chỉ disable nút UI.
- Ready/Start idempotent và server-owned, loading/spawn grace10 s cùng input gates M3 giữ nguyên trừ thay đổi retained Countdown được nêu rõ CT-405. New join không vượt sealed match roster.
- Giữ nguyên các CLI flags thật trong bảng source; raw endpoint entry là đường automation/debug, không là giao diện Hub mặc định. Không tự thêm alias `--server`/`--connect` hoặc thay semantics parser hiện có.
- Local four-process tooling có flag/build symbol riêng, không phải job CI Phase3; future compatibility test phải chạy lại entry/harness hiện hữu. Registry options mới phải explicit và không thay mặc định CLI cũ.

## Phần B — Retained-Session Reconnect

### CT-405 — Grace, state retention và cleanup

Grace lấy `GameplayConfiguration.ReconnectGraceSeconds`, mặc định **15,0 s** (double). Gọi `tLost` là lúc server quan sát transport loss đầu tiên; `ReconnectDeadline = tLost + grace`. Chỉ nhận resume khi **serverNow < ReconnectDeadline**, đúng tại deadline phải expiry. Callback loss/retry/duplicate ACK không gia hạn deadline; client mất mạng trước server phát hiện không được tự backdate hoặc tạo thêm grace.

| Sự kiện | Hành vi authoritative |
|---|---|
| Unexpected disconnect của avatar đã bind trong Countdown/InProgress/Ended-postmatch | Thu hồi control ngay; giữ PlayerSessionId, participant/slot, LifeId, HP, stats/stocks, pose và deadlines hiện tại; chuyển connection-state sang GraceRetained |
| Explicit Leave/Cancel, invalid version, kick, StopHost/StopServer | Cleanup terminal ngay; không giữ grace hoặc auto-resume |
| Unspawned peer khi WaitingForPlayers/initial loading | Tiếp tục spawn deadline10 s và minimum-roster policy M3; không lấy reconnect để tránh timeout spawn |
| Hết grace | Revoke token/binding, release slot/reservation/owned identity đúng một lần, publish disconnected; không thêm kill/death hoặc reset score giả |
| Match kết thúc trong grace | Giữ result/rank đã chốt; không hồi sinh hoặc chạy combat/loot sau Ended; grace không ngăn post-match return |
| Host mất/thoát process | Không có host migration; room hết lease/được unregister, mọi reconnect thất bại, client về Hub |

`ConnectionState` mới (`Connected`, `GraceRetained`, `Rebinding`, `AwaitingBaselineAck`, `Expired`) tách khỏi `ParticipantLifeState`; không dùng `Disconnected` terminal M3 để ghi đè Alive/Respawning còn được giữ. `MatchParticipantRuntime.ConnectionId` hiện read-only và lookup theo connection chưa rebind được: implementation phải bổ sung transaction/record rõ ràng, không dùng reflection hoặc một bảng song song bỏ qua Domain.

Revoke phải clear axes/accepted-receipt freshness/jump/vertical/fire/aim và pending commands trước detach ownership. Server giữ avatar visible/damageable theo luật phase/protection; không cấp invulnerability vì disconnect. Khi InProgress, gravity/environment vẫn authoritative, không còn horizontal intent. Điều này yêu cầu tách **quyền nhận input** khỏi **quyền chịu damage** vốn đang dùng chung CanAct ở M3; TV2 phải review để không biến người mất mạng thành bất tử.

Death trong grace vẫn ghi một Death/kill hợp lệ, HP0 và respawn deadline như bình thường; tới deadline chưa bind-ready thì ở Respawning, không force spawn. Khi resume hợp lệ, nếu deadline đã qua và match vẫn InProgress, dùng safe-spawn path M3, tăng LifeId đúng một lần, HP10/base1; nếu chưa tới hạn, giữ nguyên deadline. Rebind khi còn Alive **không** tăng LifeId, refill HP, reset cooldown hoặc cấp protection mới.

**Ngoại lệ buff có chủ đích để giữ luật loot M3:** transport disconnect của carrier vẫn mất D2 về D1 và bắt đầu timer loot30s đúng một lần tại tLost; reconnect không lấy lại buff hoặc hồi timer. Không drop loot ở chân; score/life không đổi chỉ vì mất buff.

Trong Countdown, reserved avatar đã spawn còn grace được tính eligible để không abort vì callback disconnect tức thời; countdown tiếp tục, không restart. Nếu grace hết trước FIGHT làm eligible còn dưới2, abort về Lobby không winner. Đây là mở rộng rõ ràng so với connected-only count M3; Waiting chưa spawn không được hưởng ngoại lệ. Trong InProgress, grace không tự trao last-survivor victory; finite-life xét life/stock thật, timeout vẫn chạy. Sau expiry dùng policy M3 tương ứng phase, không ép unlimited match dừng chỉ còn một người.

Room tự về Lobby đúng lịch dù còn grace: reserved slot được giữ đến deadline gốc, Ready=false; score/feed/round state reset theo rematch M3, token không cho phục hồi round đã đóng. Resume lúc này trả **Lobby baseline mới**, không old-match baseline; Start khóa cho đến khi reservation rebind xong hoặc hết hạn. Teardown phòng hủy toàn bộ grace/token/timers, không giữ hồ sơ sang room lifecycle mới.

### CT-406 — Token, danh tính và atomic rebind

Sau admission lobby/match thành công, server cấp **một credential riêng cho mỗi PlayerSessionId**, không phải một token chung cả phòng. Token opaque từ CSPRNG, ít nhất32 random bytes, Base64URL không padding; client giữ trong RAM của phiên, không listing/clipboard/log/PlayerPrefs/Git. Server lưu verifier và so sánh constant-time; mọi reject/log phải redact credential.

`ResumeSessionRequest` có `ProtocolVersion`, `RoomCode`, `RoomInstanceId`, `PlayerSessionId`, `ReconnectToken`, `RequestId`; **không** nhận HP/score/slot/pose/LifeId authoritative từ client. RoomCode/PlayerSessionId không phải proof. Token bind vào room instance + player session, không thể dùng lại trên room tái tạo/cross-player/cross-room. Server restart không giữ phiên RAM; credential cũ invalid.

Server xử lý theo thứ tự, trước cấp quyền/spawn:

1. Kiểm framing/version, độ dài token/IDs và rate limit; resolve exact room/player record.
2. Kiểm token, record chưa revoked, trạng thái đang grace/rebind và deadline chưa hết. Client vẫn Connected không được một socket mới lấy quyền chỉ nhờ có cùng display name.
3. Claim rebind atomic: một record chỉ có một connection instance thắng. Request thứ hai trong lúc claim active không cấp owner thứ hai; retry cùng RequestId chỉ trả cùng kết quả an toàn, không tăng grace/reset life.
4. Tăng `BindingGeneration` monotonic, revoke mọi binding cũ **trước** attach connection mới; disconnect callback từ instance cũ không xóa binding mới dù numeric connectionId được tái sử dụng.
5. Attach avatar/runtime record hiện tại, gate mọi gameplay, rồi chạy baseline barrier CT-407; không gọi AddPlayer path tạo participant/score mới.
6. Khi baseline ACK hợp lệ, commit Connected và trả commit notice idempotent theo RequestId/SnapshotId. Token giữ nguyên trong cùng PlayerSessionId; không rotate giữa handshake để tránh mất ACK/commit notice làm client mất credential. Token bị revoke khi explicit leave, grace expiry, kick hoặc room teardown; admission mới luôn sinh credential mới. Duplicate ACK không commit lại hoặc mở một grace mới.

Rebind attempt có timeout5 s nhưng không vượt deadline grace; timeout/connection loss trước commit bỏ claim và đóng socket mới, giữ record trong thời gian còn lại. Sau commit, loss mới mở một grace mới từ tLost mới. Reject sai proof/unknown session trả lỗi chung `ResumeDenied`; record hết hạn có thể trả `ResumeExpired` sau xác thực credential, không oracle lộ người chơi khác. Các mã khác: `ResumeBusy`, `ProtocolMismatch`, `BaselineTimeout`, `RoomClosed`; UI không chứa token hoặc stack trace.

**Transport security:** opaque random token không tự mã hóa KCP. Không tuyên bố chống hijack bởi người nghe lén nếu credential chạy trên KCP không bảo vệ. Mode LAN chỉ được dùng trong mạng demo tin cậy với giới hạn này được ghi rõ. Internet rollout bị chặn cho đến khi TV1/TV2 duyệt và kiểm secure credential exchange **và** bảo vệ session/binding chống replay; HTTPS cho directory riêng không đủ bảo vệ gameplay KCP. Không cài crypto tự chế hoặc lưu proof plaintext vào báo cáo.

### CT-407 — Full baseline và server input barrier

`BaselineStateSnapshot` là logical DTO trong Protocol, đọc từ transaction state authoritative; không tạo gameplay store thứ hai. Envelope bắt buộc: `ProtocolVersion`, `RoomInstanceId`, `MatchSessionId` (hoặc lobby epoch), `PlayerSessionId`, `ParticipantId`, `BindingGeneration`, `SnapshotId`, `StateRevision`, `ServerSampleTime`, `ConfigurationRevision`.

Payload đầy đủ gồm:

- MatchPhase đúng enum M3, clock/deadlines/countdown/postmatch/winner/ranks; server monotonic sample để project clock, không restart timer local.
- Mọi participant: slot/name, connection-state/life-state, LifeId, HP/max, Kills/Deaths/actual DamageDealt/rank, stocks, buff/base damage, cooldown/protection/respawn deadlines. Token của người khác không nằm trong snapshot.
- Pose authoritative/identity mapping đủ để rebind camera/HUD, giữ ServerToClient transform; owner không ghi official pose.
- Loot map identities/anchor/collected state và carrier/respawn state để không tự sinh thêm hộp; client không tự chạy timer spawn.
- Obstacle layout revision/hash, **ObstacleSeed** và full collider descriptors của server; M4 generator phải giữ seed đã dùng. Seed không thay descriptors: baseline import từ M3 không có seed được đánh dấu null, vẫn cần full descriptors/hash, không fabricate seed hoặc chạy lại RNG ở client.
- Room slots/Host/Ready/RoomCode để Lobby return trong lúc reconnect cũng tạo baseline nhất quán.

Nếu chia snapshot thành chunks, phải có total length/count/hash/timeout, cap256 KiB tổng và64 chunks; thiếu/sai/duplicate-conflicting chunk không apply một phần. Client chỉ ACK sau complete snapshot, scene + obstacle/loot identities tương ứng đã sẵn sàng và presenters được bind đúng context. Không ACK chỉ vì nhận SyncVar HP hoặc OnStartLocalPlayer.

Client gửi reliable `BaselineAppliedAck(SnapshotId, StateRevision, BindingGeneration, MatchSessionId)` qua control handler authenticated có thể hoạt động trước gameplay unlock. Server kiểm **exact new connection instance**, record/claim/deadline/context và snapshot đã gửi; fake/old/duplicate ACK không cấp quyền. State mới phát sinh trong lúc transfer được reliable-buffer theo revision sau snapshot, không rollback HP/score đã tiến; overflow/mismatch yêu cầu baseline mới và vẫn khóa input. ACK không có nghĩa server tin state client tính lại.

Invariant: `CanAcceptInput = Connected && BaselineAcknowledged && CurrentBinding && CurrentLife && Phase==InProgress && Alive`, cộng protection/cooldown cho attack và luật riêng cho pickup. Movement/legacy attack/ShotRequestGate/loot đều phải dùng gate này trên server; chỉ khóa UI không đủ. Cần ACK trong5 s và trước grace deadline; terminal/round change ưu tiên hơn ACK, snapshot cũ không unlock match mới.

**Fencing cùng LifeId:** M4 ingress phải mang/kiểm binding generation cho movement, shot, legacy attack và pickup trước kiểm sequence. `LifeId` chỉ tăng khi respawn; không tăng giả để che reconnect. Sequence/jump/shot watermarks không rollback trong binding cũ; binding mới chỉ bắt đầu counters mới sau khi epoch cũ bị fence và baseline ACK commit. Packet đang bay từ connection/generation cũ bị bỏ kể cả sequence cao hơn. Đây là wire/gate mở rộng M4 cần version negotiation, không phải bảo đảm đã có của payload M3.

Pure client tiếp tục tắt CharacterController; rebind không tự teleport/predict. Server dùng đường ArenaNetworkTransform hiện hữu nếu cần correction, clear interpolation đúng cách nhưng không reset delta-compression history giữa stream. Presentation Context mới sau baseline, revision tăng trong context, gỡ listener cũ trước bind để không nhận feedback life/session cũ.

### CT-408 — UX reconnect và input reset

- Khi local mất connection, ngay lập tức clear held WASD/jump/fire/aim/interaction, invalidate sink/provider, giữ input-system initialization hoạt động; mở overlay **Reconnecting… X.Xs** và Cancel/Back. Không consume gameplay vào queue để phát lại sau resume.
- Countdown dựa server deadline/sample gần nhất; trước resume response chỉ là ước lượng từ config, không tự cho mình thêm15 s hoặc tuyên bố server còn giữ slot. Connection timeout lúc discovery không đồng nghĩa đã có grace server.
- Retry reconnect chỉ thuộc control workflow, tối đa một inflight attempt, backoff1/2/4 s rồi4 s, dừng khi hết deadline/Cancel/RoomClosed. Không retry shot/pickup hoặc reset grace mỗi attempt.
- Token không được đưa vào argv/process logs. Cancel gửi explicit leave khi kết nối được; nếu không gửi được, local xóa credential/đóng UI, server tự expiry đúng hạn.
- Sau ACK commit: close overlay, bind local camera/HUD theo identity/context mới; phase Countdown/Ended/Respawning vẫn khóa action. Held fire phải release rồi repress; không auto-fire, jump hoặc pickup vì phím đã giữ trước disconnect.
- Grace hết hoặc room đóng: clear credential, HUD/feedback/context/camera cũ, trở lại Hub kèm lỗi thân thiện; Single Player và Host/Join mới vẫn dùng được, không singleton/identity mồ côi.
- Peer khác thấy trạng thái Reconnecting/reserved slot; không hiển thị death giả chỉ vì transport mất. Khi actor thực sự chết, dùng death/feed/score authoritative như M3.

## Phần C — Ownership, AC và review

### 3. Phân công triển khai và dependency

| Chủ trì | Phạm vi / boundary | Bàn giao để role khác nối |
|---|---|---|
| TV1 @xzviett | Directory abstraction/adapters, code generator/claim/lease, CLI config; reconnect record/token/rebind/generation/barrier; session teardown | DTO/read-model/control result, endpoint resolution, server snapshots và error codes; không viết UI authority |
| TV3 @tasdus-117 | Hub/list/refresh/code/copy/CharacterPreviewSlot, reconnect overlay/cancel, camera/HUD rebind và release gate | View qua engine-free interfaces cùng hướng MatchViewContracts; visual/input evidence trên build cùng SHA |
| TV2 @HoangLong205 | Retained-life/damageability/cooldown/score/stock rules, loot exception và expiry/win oracle; phối hợp multi-client verification | Expected độc lập cho death trong grace/respawn/protection/finite-life/end; không tự rollout profile súng |

Các packet #20/#24/#40/#41 và same-build QA #26/#27 vẫn giữ scope M3 của chúng; draft M4 không đóng/reopen issue hoặc đổi expected M3. Có thể review/viết test design M4 song song; implementation cần chốt các thay đổi count/damage/rebind với TV2 và UX với TV3. Internet security, LAN conflict convergence và multi-host localhost là release gates riêng, không suy từ unit tests.

### 4. Acceptance criteria và kế hoạch Red/Green

| AC / Test đề xuất | Loại | Red phải phát hiện / Green quan sát được |
|---|---|---|
| AC401 / CodeFormatAndNormalize | EditMode | Length/alphabet sai bị nhận → reject; trim/lowercase hợp lệ → đúng6 ASCII; không truncate |
| AC402 / ClaimCollisionAndRelease | EditMode + adapter | Hai claim cùng mã đều thắng/old lease xóa room mới → đúng một owner, bounded retry, teardown idempotent, expired code có lifecycle mới |
| AC403 / DirectoryFilteringAndFreshness | EditMode fake clock | Private/expired/old revision lọt list, UTC skew đổi expiry → public valid-only, exact occupancy0–4, revision không rollback |
| AC404 / ResolveFailureMatrix | EditMode | Nhầm not-found với timeout, endpoint cũ khi failure → đúng five reasons/null invariants và phase/cap precedence |
| AC405 / HubJoinByListAndCode | PlayMode + real processes | Raw IP bắt buộc/menu bypass → MainMenu/Hub/code/list/Host/copy đi đúng Lobby, Single Player không đổi |
| AC406 / FourSlotsAndFifthReject | PlayMode | Directory snapshot vượt server cap/Ready policy → giữ4 slots, modal chuẩn, host-only≥2/all-ready, reserved slot không bị chiếm |
| AC407 / GraceBoundaryAndDuplicateLoss | EditMode explicit clock | Callback lặp gia hạn/reset stats → proof eligibility **tLost+14.999 hợp lệ, +15.000/+15.001 hết hạn** khi grace15; full resume còn yêu cầu ACK trước deadline, giữ HP/life/stats/deadlines |
| AC408 / WrongProofAndConcurrentRebind | EditMode + PlayMode | Token sai/cross-room/connectionId tái dùng/dual resume giành avatar → không mutation, đúng một new owner, old callback không xóa binding mới |
| AC409 / OldEpochTraffic | PlayMode serialized Mirror ingress | Same-life old generation + high sequence gây move/attack/pickup sau restore → reject trước watermark; fresh new generation sau ACK hoạt động |
| AC410 / CompleteBaselineAckBarrier | PlayMode | HP hook/partial snapshot/fake ACK tự mở input → gate đóng; đúng snapshot/connection/generation mới commit; revision mới không rollback |
| AC411 / DisconnectDeathAndRespawn | Domain + PlayMode | Disconnect làm invulnerable/refill HP/reset cooldown → body chịu damage đúng luật, death credit một lần; pending respawn đủ điều kiện mới LifeId+1; loot mất buff/timer30s một lần |
| AC412 / CountdownEndLobbyExpiry | Domain + PlayMode | Grace giữ match bất tử/restart clock/revive Ended → count policy đúng, timer/result frozen đúng phase, về Lobby giữ reservation đến hạn, Ready=false |
| AC413 / ReconnectUXAndRelease | PlayMode + TV3 cửa sổ thật | Hold/fire/aim cũ hoặc callback HUD cũ tái phát → clear/rebind, release/repress, Cancel/expiry về Hub, không leak |
| AC414 / LanAndRegistryParity | Integration nhiều process | Chỉ chạy central hoặc một localhost room → cùng contract chạy LAN loopback/subnet, nhiều room cùng máy, collision fail-closed; central atomic claim/TTL có evidence riêng |
| AC415 / TeardownAndCliCompatibility | PlayMode + local harness | StopHost/leave tạo orphan/token reuse/CLI bị đổi → sạch owned refs/slot/lease, flags --arena-* và ba round tooling cũ vẫn hoạt động |

AC407 dùng clock truyền vào, không sleep để chạm biên; mọi deadline tính từ tLost+grace. Tests concurrency phải dùng barrier điều phối hai request, không dựa scheduler “thường đến trước”. Directory tests dùng response reorder/cancellation/clock fake, không gọi production selector để tạo expected. PlayMode kiểm server và client replica tách object, serialized Commands, baseline ACK thật; không chỉ reflection cờ owner hoặc gọi damage thẳng thay ingress.

Quy trình triển khai sau review: thêm regression thất bại trước fix → giữ raw Red → patch tối thiểu → focused Green → full EditMode/PlayMode/strict7 + scaffold/XML gates không hạ floors → rebuild đồng bộ → real multi-process và visual QA. Mỗi evidence ghi exact SHA/build tree hash/config/topology/commands/exit/counts. LAN, central registry, impairment và localhost tách trạng thái; không chạy thì NOT TESTED/BLOCKED. CI vẫn Phase1+Phase2; các harness acceptance là công cụ local.

### 5. Checklist trước khi ký và sổ review

- [ ] TV1 xác nhận scope uniqueness LAN/registry, collision/reuse lease, localhost multi-host và budgets; không tuyên bố NAT/Internet tự có.
- [ ] TV1 + TV2 chốt token transport security, same-life generation fencing, counter reset, deadline comparator và token revocation/ACK-loss recovery.
- [ ] TV2 xác nhận damageability khi grace, countdown minimum, finite-life eligibility, death/respawn và exception buff30s không phá M3.
- [ ] TV3 xác nhận Hub/Room UI boundary, CharacterPreviewSlot, error/cancel/reconnect clock, baseline-ready và no stale held input/HUD.
- [ ] Cả nhóm review schema/version/size, descriptor baseline, expiry/Lobby-return và replay tests; chọn exact implementation scope/PR trước viết runtime.
- [ ] Không lấy chữ ký nháp, CI xanh hoặc output AI thay human sign-off; sau thay đổi contract cần review lại phần ảnh hưởng.

| Reviewer | Phạm vi | Reviewed SHA / ngày / quyết định | Trạng thái |
|---|---|---|---|
| @xzviett — TV1 | CT-401–408, authority/directory/reconnect/clock/wire/build boundaries | Chưa có; reviewer tự điền exact SHA khi review | PENDING |
| @HoangLong205 — TV2 | Life/cooldown/HP/score/loot/finite-life, replay và oracle | Chưa có; reviewer tự điền exact SHA khi review | PENDING |
| @tasdus-117 — TV3 | Hub/layout/input/overlay/HUD baseline và visual acceptance | Chưa có; reviewer tự điền exact SHA khi review | PENDING |

### 6. Kết quả M4.0 tài liệu

Chỉ author draft Markdown và link từ protocol index; không có DTO/runtime/UI/test implementation mới. Scaffold/link/whitespace validation ghi trong bàn giao nhiệm vụ, không phải Red/Green hoặc proof reconnect. Tất cả AC trên là **PLANNED / NOT EXECUTED**; không ký hoàn thành M4 từ tài liệu này.
