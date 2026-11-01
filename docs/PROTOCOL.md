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
