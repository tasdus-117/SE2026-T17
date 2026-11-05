> TV2 PROPOSAL / source archive, selectively adapted from 61aebf38ce40f9ad131405c202d7b82729ecbbcf; không thay tài liệu dùng chung của main. Workflow TV2 hiện hành (owner 2026-11-05 realign): baseline latest fetched origin/main có prerequisites; feature/<TASK-ID>-<description>, PR vào main; long23001901 chỉ giữ lịch sử. Các nhánh độc lập là siblings, task phụ thuộc thường chờ prerequisite merged; stacking ngoại lệ phải ghi dependency. Không merge/direct push main, không sửa nhánh nguồn. Quyết định này chỉ workflow owner, không phê duyệt gameplay nhóm.

# Hợp đồng logic cho movement và combat TV2

[Shared protocol](protocol/protocol.md) là canonical contract chung. CT-201–205 ở đây là đề xuất TV2 cần TV1/TV3 review; chưa thay shared wire/API. N MovementInput là read-only feature evidence, chưa được nhập vào main base.

2026-11-02. PROPOSED, chưa code/wire/API Mirror. [SRS](../Long_document/srs.md) v0.4 sở hữu luật/GD; [TASKS](TASKS.md) TV2-004 sở hữu việc review; [ARCHITECTURE](ARCHITECTURE.md) sở hữu ownership. MSG-01–13 cũ ở phần lịch sử, không dùng như contract song song. Tên CT-201–205 dưới đây là contract mới, chưa định kiểu dữ liệu/channel/serialization.

## TV2-001 — đối chiếu contract đã có

Kiểm ngày 2026-11-05. CT-201–205 vẫn là logical draft TV2; “chưa code” phía trên nói về các CT mới trong personal tree, không phủ nhận movement wire đã có tại N = 4a7568f3698c5a8f2a533afff3a04368314076c5. Full source refs/paths trong [SRS matrix](../Long_document/srs.md#tv2-001--đối-chiếu-nguồn-ngày-2026-11-05). Đây là mapping/gaps để TV2-004 review, không định nghĩa wire schema mới.

| CT | Implementation quan sát tại N hoặc M | Gap cần review / gate |
| --- | --- | --- |
| CT-201 | N Shared/Protocol/MovementInput.cs: uint Sequence, float MoveX/MoveZ world axes, uint JumpSequence; NetworkMovement.CmdSubmitMovement Unreliable/requiresAuthority, sender interval 1/60 s; server validator reject nonfinite, clamp magnitude≤1, uint serial comparison; ServerPlayerMovement.FixedUpdate dùng server delta, 5 m/s và timeout 0.25 s | Sender throttle không chứng minh ingress chống flood. Cần TV1+TV2 ghi serial acceptance window/reset theo connection/life, rate budget, state eligibility, V/τ và clamp/reject policy được duyệt GD-01/08/09; TV3 xác nhận world/camera aim, jump GD-03 |
| CT-202 | M Shared/Protocol/PlayerIntentContracts.cs chỉ ICombatInputSource local punch/interact; CombatController.Update sphere/local Time.time | Không có raycast shot wire được xác minh. TV2-004 với TV1/TV3 chốt actor từ ownership, life/match context, aim/origin, optional shot id/retry; GD-02/03. Không reuse MovementInput Sequence làm combat identity tự động |
| CT-203 | M Health.OnHealthChanged/OnDeath local events | Không network combat outcome/rejection/score authority được xác minh. TV2-004/007/009 chốt one effect/life credit và delivery dedup với TV1; GD-05 |
| CT-204 | N NetworkTransformReliable ServerToClient theo report/design và NetworkPlayer lifecycle | Pose replication không đồng nghĩa HP/life/score/config revision snapshot. TV2-002/004 và TV1 sync review; GD-01/04/08 |
| CT-205 | N ArenaNetworkManager spawn reservation, disconnect destroy/release | Không token retention/respawn/ready/load/match bridge. TV1 session, TV2 transitions, TV3 feedback; GD-04/06/07/08. Không suy Mirror connId là reconnect identity |

Vui lòng chốt fields/types/units/limits/order/reject reasons cùng examples ở TV2-004; task này chỉ đối chiếu gaps. Không triển khai bridge, không sửa TV1 movement. [TESTING](TESTING.md#tv2-001--phân-loại-bằng-chứng) phân biệt report cùng-process với multi-process chưa chạy.

## Hợp đồng phối hợp

| Contract | Dữ liệu logic / bên gửi | Xử lý / bên sở hữu | Bàn giao và test |
| --- | --- | --- | --- |
| CT-201 MovementIntent | TV3 thu movement axes, aim intent nếu chọn, thứ tự input nếu adapter cần; không authoritative position/speed | TV1 nhận, xác thực ownership, finite/bounded decode; TV2 quy định magnitude/units, speed và eligibility; TV1 simulate/collide | TV2-003/004; TC-202; missing/stale input policy TV1+TV2 |
| CT-202 ShotRequest | Client gửi ý định bắn và aim theo GD-03; shot identity chỉ nếu retry/delivery cần; không damage/hit target truth | TV1 suy actor từ authenticated connection; TV2 kiểm match/life/cooldown/server geometry/range/obstruction/target | TV2-004/006; TC-203/204 |
| CT-203 CombatResult | Server tạo accepted/rejected reason, shot/life context và HP/death/score revision cần thiết | TV2 tạo gameplay outcome, TV1 sync; client không gửi HP/damage/score đã quyết định | TV3 hiển thị authoritative result; TC-204/205/207 |
| CT-204 GameplayState | Server pose từ TV1, HP/life/score/cooldown và match state theo TV2; config revision | TV1 truyền baseline/update; TV3 dùng presentation contract, không tự trừ HP/credit; stale state không rollback | TC-205–208; fields/units/revision phải review cả ba |
| CT-205 LifecycleBridge | TV1 báo start eligibility/disconnect/resume/spawn availability; TV2 trả gameplay transitions/timers; TV3 báo load và hiển thị | TV1 sở hữu session/lobby/auth, TV2 sở hữu life/match rules; reconnect lấy state server hiện tại | TC-201/206–208; GD-04/06/07/08 |

## Gate server và tính đúng một lần

1. TV1 xác thực connection và gắn actor từ ownership; không tin participant/actor do client khai. Kiểm schema/size/rate ở ingress theo transport được chọn; TV2 xác định giá trị gameplay finite và trong miền. Nếu cần normalization/clamp movement, contract phải chỉ rõ miền nào reject và miền nào normalize; không chấp nhận NaN/infinity.
2. TV2 kiểm match Playing, life Alive, movement/fire eligibility, config hợp lệ và cooldown dùng server time. Client timestamp không quyết định hết cooldown. Range/damage/HP/score chỉ do server config/state quyết định.
3. Origin phải lấy từ pose/muzzle server hoặc đối chiếu với pose server trong dung sai GD-03. Direction phải hữu hạn, khác zero, normalized/validated theo aim contract; client aim tự do trong giới hạn đã duyệt khác việc tin vị trí bất kỳ. Chưa chốt camera không suy tia luôn theo movement-facing cũ.
4. Server raycast kiểm obstruction/layer/trigger/target và range; không dùng client hit claim. Quy tắc self-hit, dead target, điểm origin nằm trong collider và target nhiều colliders được review trước integration. Một shot hợp lệ không damage cùng target nhiều lần chỉ vì nhiều colliders.
5. Một accepted shot/effect không áp dụng damage hai lần; một Alive → Dead của cùng life chỉ award score một lần. Gắn life context để callback trước respawn không giết/credit life mới. HP clamp [0,maxHP]; explicit Dead/RespawnPending/Alive transitions theo server policy.
6. Chỉ bổ sung request sequence/id và bounded replay window khi TV1 xác nhận delivery/retry/reconnect path thực có khả năng phát lại intent. Không tự đặt global replay database/cache hay copy envelope MSG cũ. Dù adapter không retry, death callbacks và multi-collider vẫn cần idempotent gameplay effect. Cơ chế cụ thể, reset theo match/life/connection và giới hạn bộ nhớ là deliverable TV2-004 với TV1.
7. TV1 sync revision/state và current snapshot cho reconnect; TV3 hiển thị kết quả server, loại presentation event trùng nếu adapter có replay. Reconnect không tạo quyền tự reset cooldown/HP/score; việc timer tiếp tục hay tạm dừng, offline avatar và respawn theo GD-08 cần nhóm duyệt.

## Review trước tích hợp

TV1+TV2 chốt fields/types/units/limits/reject reasons/life identity, ownership ingress vs gameplay gate, ordering và missing/stale input. TV3 chốt camera/aim, HUD fields và feedback của reject/death/respawn/results. Lưu ví dụ hợp lệ/không hợp lệ và mapping CT → selected Mirror adapter sau TECH-001; chưa quy định RPC/SyncVar/channel theo phiên bản chưa kiểm. Contract thay phải review cả ba cùng tài liệu, không đợi cuối M3.

---

## LỊCH SỬ — Baseline ngày 2026-11-01, không dùng làm cổng nghiệm thu kế hoạch TV2

> Toàn bộ nội dung bên dưới được giữ để truy vết. Các nhãn CURRENT, CONFIRMED, mandatory, TODO và next action bên trong chỉ có nghĩa tại thời điểm baseline cũ. Không cộng các nghĩa vụ cũ vào kế hoạch TV2 mới.

# Protocol plan — framework-neutral, chưa triển khai

2026-11-01. Contract draft **PROPOSED**, implementation TODO; không RPC signature/serialization/channel nào được duyệt. [SRS](../Long_document/srs.md) sở hữu luật, [ARCHITECTURE](ARCHITECTURE.md) sở hữu authority/topology; protocol chỉ mô tả thông tin và validation dự kiến. F-014 phải thay placeholders bằng API/wire mapping của **một** framework sau TECH-001 và kiểm TC-115. Không REST/database/backend mới.

## Identity, lifecycle và compatibility

- `sessionId`: host session; `roomId`: membership; `matchId`: lượt FFA; `participantId`: identity ổn định qua reconnect; `entityId`: avatar; `connectionId` có thể thay đổi. Không dùng network object ID như identity proof.
- Envelope dự kiến: `protocolVersion`, `sessionId`, `matchId` khi vào trận, `connectionGeneration`, `sequence`, `clientTick` cho intent; server gắn `serverTick`, `stateRevision`, `eventId`. Exact types/ranges/window/rate/size/tick rates **UNDECIDED**; không dùng client clock làm authority.
- Identity proof qua approved anonymous auth hoặc opaque session-bound reconnect credential là PROPOSED; server bind identity từ authenticated connection, không tin actor ID trong payload. Không phát token trong snapshots/logs/room code. Invalid identity không được chiếm avatar/slot cũ.
- PROPOSED lifecycle: Room → Loading → Playing → Finished; host loss tại bất kỳ state nào → Aborted. Finished chứa Win hoặc Draw; Aborted không chứa winner. Match mới luôn matchId mới. Room capacity khác active contender count.
- New join chỉ Room; reconnect là restore existing participant sau identity check, không admission mới. Client/server protocol/config incompatibility phải reject rõ trước spawn; framework/package version không thay protocol compatibility policy. PROPOSED exact version equality trước khi có migration contract.

## Message catalogue dự kiến

Tên là logical messages, không executable API. Delivery “reliable” là yêu cầu về effect/retry ở adapter, không cam kết transport wire hiện tại. Các state mutations đều server-only. Mỗi command validation còn cần session/version/generation/identity/state checks chung.

| ID / hướng / delivery đề xuất | Payload tối thiểu dự kiến | Validation và effect server / client presentation | Trace / test |
| --- | --- | --- | --- |
| MSG-01 CreateRoom / C→S hoặc service adapter / reliable | request identity, protocol | authenticated owner, capacity config; tạo private code, không matchmaking | FR-104; TC-104/115 |
| MSG-02 JoinRoom / C→S / reliable | code, identity proof, version | room exists/not full/not Playing, no duplicate identity; roster trả sanitized | FR-104–105; TC-104–105/115 |
| MSG-03 LoadAck / C→S / reliable | room revision, content/config version | connection sở hữu participant, current load generation; đủ bốn mới eligible | FR-104; TC-104/115 |
| MSG-04 StartMatch / owner client→S / reliable | room revision, requestId | owner role, bốn connected+initialized, one start; server tạo matchId/unique spawns | FR-104–105; TC-104–105/115 |
| MSG-05 MoveIntent / local owner→S / sequenced transient | axes bounded, jump edge, seq/tick | rate/window/state/owner; finite axes, clamp magnitude; server grounded/speed/collision/facing; stale held movement expire policy còn UNDECIDED | FR-102/106/112; TC-102/106/112/117 |
| MSG-06 AttackIntent / local owner→S / reliable hoặc sequenced+ack | attackRequestId, seq/tick, không damage/pose truth | alive/Playing/owner, cooldown/window, forward range/geometry/wall; dedup attack và target colliders; server chọn damage/knockback/weapon state | FR-107–108/111; TC-107–108/111/117 |
| MSG-07 ContactPickup / server internal, không client grant | candidate contacts từ authoritative collision | alive/unarmed/available/Playing, resolve contention một winner; client hint nếu có không quyết định contact | FR-110; TC-110/117 |
| MSG-08 DropIntent / local owner→S / reliable+dedup | requestId, seq/tick | alive/Playing/hasWeapon, valid placement theo O-02; atomically clear owner và create available/drop state; E không collect | FR-110; TC-110/117 |
| MSG-09 StateSnapshot / S→clients / sequenced + full baseline reliable | tick/revision, roster poses/facing/HP/held weapon/elimination, contenders, pickups, match timer/state/result | clients loại snapshot cũ, chỉ present/interpolate; initial/reconnect full snapshot, deltas chỉ khi có baseline; thiếu baseline yêu cầu resync | FR-105–114; TC-105–114/115 |
| MSG-10 RuleEvent / S→clients / reliable idempotent | matchId/eventId/tick, damage/pickup/elimination/result và reason | side effects UI dedup; HP/result truth từ state revision, event mất/trễ không ngăn resync; không hai terminal results | FR-108–111/114; TC-108–111/114–115 |
| MSG-11 ResumeParticipant / C→S / reliable | session/match identity proof, last revision, reconnect requestId | valid existing membership/identity, grace, session state; bind new generation, revoke old, cùng avatar; gửi full current snapshot rồi unlock allowed input | FR-112–113; TC-112–113/115/117 |
| MSG-12 Leave/Disconnect/GraceExpiry / C→S hoặc server timer / reliable effect | leave intent hoặc detected disconnect, participant/time | avatar vulnerable/no new input; expiry/leave contender-removal idempotent theo O-05–06; distinct LEFT/RECONNECT_EXPIRED; không revive | FR-112–113; TC-112–113/117 |
| MSG-13 SessionAborted/Error / S→C hoặc client connection detection | reason enum, safe message, terminal revision nếu còn server | host lost không có authoritative winner; clients timeout báo Aborted, không tự suy victory từ remaining view | FR-113–114; TC-113–114/115 |

## Ordering, replay và authoritative step

PROPOSED dedup key `(sessionId, matchId, participantId, connectionGeneration, requestId/sequence)` cho command không idempotent tự nhiên. Unique applied attack/target effect ngăn double hit; retry ACK/state không reapply effect. Memory bound/replay window phải chốt từ match duration/rate và reconnect needs, không chọn SQLite retention từ assignment. Sau reconnect, input từ generation cũ bị reject ngay cả sequence lớn; duplicated/superseded connections không gửi input cho avatar nữa. Future-dated/too-old ticks, nonfinite values, oversized payload/rate flood reject với bounded safe diagnostics. Exact bounds UNDECIDED và là TECH/F-014 blocker trước acceptance.

Đề nghị step order O-06: receive/validate bounded intent → simulate motor và attacks → collect HP/ring-out conditions → idempotent elimination batch → leave/grace expiry removals → evaluate contenders → time-limit outcome → snapshot/events. Batch all confirmed eliminations trước result là CONFIRMED; priority expiry/timeout/dual-cause chỉ PROPOSED. Không evaluate win trong từng HP callback. Removal sets participant inactive một lần, disables actions/pickup và chuyển spectator; primary elimination cause HP_DEPLETED/RING_OUT khác forfeiture reason. Cùng một step nhiều removals vẫn chỉ một final outcome.

## Reconnect handshake và failure cases

1. Client mất connection: server ngừng new input cho participant, không freeze/teleport/restore HP; avatar tiếp tục gravity/knockback/HP/boundary rules. Stale movement intent phải hết hiệu lực theo timeout được duyệt.
2. Return gửi proof, không chỉ code/participantId. Server kiểm existing participant, session/match state và grace bằng server clock; expiry gây forfeit đúng một lần và outcome evaluation như removal path.
3. New generation supersedes old connection. Snapshot current gồm HP/weapon/pose/elimination/contenders/timer/result; không dùng client cached HP/weapon. ACK baseline trước mở gameplay input là PROPOSED.
4. Eliminated trở lại spectator, không respawn; active trở lại đúng avatar còn sống. Đang disconnected mà bị HP/ring-out loại vẫn giữ elimination status. Join bị khóa không chặn authenticated resume, nhưng không biến expired slot thành new admission.
5. Sau Finished proposal chỉ đọc results; sau Aborted reject resume và báo session ended. Intentional leave/temporary interruption distinction, same-step priority và grace value cần owner duyệt O-05–09. MPS membership reconnect không thay các bước gameplay này.

## Hoàn thiện contract và evidence

F-014 phải pin protocol version/field types/enum/limits, mỗi logical message → framework RPC/message/channel, quyền sender/receiver, retry/dedup/window và snapshot cadence; ghi compatibility rejection, service identity mapping, secret handling. TC-115 đối chiếu traffic thực với catalogue, TC-117 thử malformed/spoof/replay, TC-112 đo restore. Evidence phải có commit/build/config/run IDs và actual results; hiện chưa có packet captures, protocol implementation, passing test hay CI run.
