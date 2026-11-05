# Kiểm thử TV2 và bằng chứng phối hợp

2026-11-02. TC-201–209 dưới đây **Planned / NOT RUN**. Không Unity project, tests executable, builds hoặc CI được tạo/chạy trong task này. TC-101–120 và TC-001–037 giữ lịch sử, không là gate song song. [SRS v0.4](../Long_document/srs.md) định nghĩa FR/AC mới; [TASKS](TASKS.md) định nghĩa công việc.

## Catalogue kiểm thử đề xuất

| TC / FR / AC | Task / người chính | Thao tác Planned | Expected / bằng chứng cần có |
| --- | --- | --- | --- |
| TC-201 / FR-201 / AC-201 | TV2-004/011; TV1 network, TV2 support | Server riêng + 2 clients đúng ownership và movement; M2 thêm đủ 4, thử người thứ 5 và start theo GD-07 | Một avatar mỗi người, không điều khiển hộ, capacity 4; topology/build/roster/log/video hai view; leave không crash |
| TC-202 / FR-202 / AC-202 | TV2-002/003/010; TV1 simulation | Zero/diagonal/at-bound/out-of-bound, NaN/infinity, state không được move; sửa speed client | Reject/normalize đúng CT-201, server speed không vượt approved rule; input vectors và pose/time traces; finite bounds/units rõ |
| TC-203 / FR-203 / AC-203 | TV2-005/010 | Server clock giả cho just-before/at/after cooldown; invalid config; range edge; ammo chỉ nếu GD-02 chọn | Fire eligibility đúng, config invalid không được dùng, không lấy client clock; expected table/log có config revision |
| TC-204 / FR-204 / AC-204 | TV2-006/010/011 | Valid hit/miss, wall, out of range, zero/nonfinite direction, fake origin/hit/damage, dead shooter/target, multi-collider, retry nếu adapter có | Chỉ server accepted shot gây một effect; obstruction ngăn hit; rejected request không đổi HP; geometry diagram/scene/layers/log và two-client state comparison |
| TC-205 / FR-205 / AC-205 | TV2-007/010 | Damage nhỏ/bằng/lớn HP, callbacks lặp, hai hit kết liễu trong cùng step, callback cũ sau respawn | HP>=0 và <=maxHP; death một lần mỗi life, không credit lặp; before/after state/life/event trace; GD-05 quyết credit |
| TC-206 / FR-206 / AC-206 | TV2-008/010/011; TV1 spawn, TV3 arena | Trước/đúng/sau respawn delay, spawn blocked/unsafe, callback trùng và shot trong respawn, protection nếu chọn | Server transition đúng một lần; safe spawn/fallback theo GD-04; reset HP/cooldown theo approved policy; spawn/life/timer logs và video |
| TC-207 / FR-207 / AC-207 | TV2-009/010/011 | Kill credit/suicide/environment theo GD-05; time/score/tie, start thiếu điều kiện, duplicate end, late callback | Score và terminal result đúng một lần theo GD-06/07; client không quyết winner; match timeline/state/score trace |
| TC-208 / FR-208 / AC-208 | TV2-012; TV1 reconnect, TV3 UI | Disconnect/resume alive, dead, pending respawn, cooldown chưa hết; invalid token, connection cũ; expiry và server loss theo GD-08 | Không cooldown bypass/duplicate HP/death/respawn/score; đúng phiên và state server; token redacted; session+life+timer timeline, không tự dùng spectator/forfeit policy cũ |
| TC-209 / FR-209 / AC-209 | TV2-011/012/013; cả nhóm M5–M6 | Run server+>=2 clients combat, thêm đủ 4 tải; impairment; negative matrix; smoke client/server artifacts | Mỗi case có expected/actual/verdict/defect và build/config; actual RTT/jitter/loss, CPU/RAM/tick/bandwidth theo TV1; CI artifact/run evidence TV1, combat sign-off thực TV2 |

## Phân tầng và test độc lập

TV2-010 có thể viết test vectors/pseudocode ngay: symbolic H,D,C,R,V cho HP/damage/cooldown/range/speed; kiểm biên, invariants, transition và expected result, không lấy giá trị offline. Pure rule tests dự kiến Assets/_Project/Tests/EditMode/ với clock/config fake; Unity physics raycast cần scene fixture/Play Mode phù hợp sau M0, không coi kiểm pure math là chứng minh wall occlusion. TV1 kiểm ownership/input reception/server motor; TV2 kiểm rule eligibility và combat; TV3 kiểm input/camera/HUD. Test shared input cần cùng trace để tránh hai người cùng nhận ownership hoặc bỏ lọt interface.

TV2-011 thiết kế run integration không cần project; execution cần M0/M1 + combat components, approved configs, server OS/transport thật. Một server riêng và ít nhất hai client độc lập: A bắn B, cả hai views khớp HP/death/respawn/score; đối chiếu authoritative log; thêm đủ 4 cho M2/M5. Không chờ reconnect để chạy combat cơ bản; TC-208 bổ sung khi M4 sẵn sàng.

## Cheat matrix M5 — Planned

| Injection | Chủ trì / hỗ trợ | Invariant / test |
| --- | --- | --- |
| Bắn quá nhanh, sửa client clock, bắn khi Dead/Waiting/Finished | TV2 / TV1 adapter | Không accepted effect ngoài eligibility/cooldown; TC-203/204 |
| Fake origin/direction/hit/damage, xuyên tường, quá range | TV2 / TV3 geometry | Server geometry/stats quyết định; TC-204 |
| Lặp shot khi delivery thực có retry; multi-collider/death callback trùng; callback life cũ | TV2 / TV1 contract | Một damage/score/death mỗi effect/life; TC-204/205/207 |
| Spoof actor/input, out-of-range/nonfinite, local speed hack | TV1 ingress/simulation / TV2 limits | Không control người khác hoặc vượt rule; TC-201/202 |
| Reconnect để reset cooldown/respawn/score; token sai/connection cũ | TV1 session / TV2 policy | Đúng identity và lifecycle; TC-208 |

Không mở rộng thành commercial anti-cheat, lag compensation, prediction hoặc anti-tamper server. Mỗi negative case kèm valid request sau đó để phát hiện reject làm hỏng state.

## Mạng, tải, CI và evidence

RTT 0/50/100/200 ms là điểm thử PROPOSED từ phân công, không threshold đạt. TV1 chọn công cụ theo exact transport; ghi one-way hay RTT và actual measurement, thêm jitter/loss sau khi chốt profile. Load scope một phòng tối đa 4, không many-room service; nhóm chốt duration/hardware/tick/resource budget trước verdict performance. TV2 quan sát cooldown/hit/death/respawn/score invariants dưới impairment, không nhận toàn profiling/CI.

Mỗi run cần TC/FR/AC/task, source commit, config/protocol/Editor/Mirror versions, server OS/topology/máy, client count, artifact hashes, người/ngày, steps + expected/actual, logs/video/raw samples, defects/retest, verdict PASS/FAIL/NOT RUN. Human review và AI assistance ghi thực; không tự chứng nhận reviewer. Không ghi credentials. TV1 chủ trì licensed runner/client-server build/CI; TV2 cung cấp test entry points sau khi có thật, config, combat smoke và evidence index cho M6. Không có command online đã kiểm để đưa ra lúc này.

---

## LỊCH SỬ — Baseline ngày 2026-11-01, không dùng làm cổng nghiệm thu kế hoạch TV2

> Toàn bộ nội dung bên dưới được giữ để truy vết. Các nhãn CURRENT, CONFIRMED, mandatory, TODO và next action bên trong chỉ có nghĩa tại thời điểm baseline cũ. Không cộng các nghĩa vụ cũ vào kế hoạch TV2 mới.

# Kế hoạch kiểm thử và bằng chứng online FFA

2026-11-01. Mọi TC-101–120 dưới đây **Planned / NOT RUN**. Không có Unity project/build/test harness/CI trong baseline này; kiểm Markdown không chứng minh gameplay. [SRS](../Long_document/srs.md) sở hữu yêu cầu và AC; đây là thủ tục/evidence, [ROADMAP](ROADMAP.md) gắn feature. Chỉ chuyển execution result khi có artifact/commit/config và người chạy thực; failure không phải waiver.

## Điều kiện chung và evidence format

Trước execution cần approved framework/versions/config, implementation trên task branch được duyệt, exact command từ project thật, license/module/runner và máy tham chiếu. Không có build/test command online đã được kiểm chứng để cung cấp lúc này. Một run ghi: TC/AC/feature IDs, source commit, protocol/config versions, artifact hashes, ngày/người thực hiện, AI assistance/human review thật, topology/máy/OS/region, steps, expected/actual, raw logs/samples/video, defects, verdict PASS/FAIL/NOT RUN. Không ghi token/auth credential trong evidence; GitHub evidence có đường dẫn/run URL thực sau execution. Đề nghị lưu index Markdown theo run; logs/artifacts dùng CI artifacts/release storage đã duyệt, không dump binary/build outputs vào source.

Dùng rules tests cho clamp/idempotency/batch outcomes/sequence gates; Play Mode/controlled scenes cho physics/contact/wall/ring-out; multi-process integration cho ownership/session/reconnect; manual Windows/camera/usability và WAN demo riêng. Test exact cooldown/range/grace boundaries, just-before/at/after theo approved server clock/tick. Các loại test này là kế hoạch, chưa tạo executable tests.

## Catalogue — traceability FR/NFR → AC → TC → feature

| TC | Requirement / AC / feature | Preconditions và thao tác Planned | Expected và evidence đặc thù |
| --- | --- | --- | --- |
| TC-101 | FR-101 / AC-101 / F-001, F-018 | Clean checkout trên máy/runner đã duyệt, restore pinned project; build Windows client + headless server, smoke-start cả hai | Đúng versions, không missing reference/module/scene; hai artifacts cùng commit, hashes/logs; không suy compile pass từ Markdown |
| TC-102 | FR-102 / AC-102 / F-002 | Từng instance move tám hướng, jump/land, dừng/đổi hướng, thử mouse orbit; spawn remote | Chỉ local input/camera hoạt động; facing theo move, attack forward cùng facing, fixed-heading elevated follow không orbit; lưu bốn view video/ownership logs |
| TC-103 | FR-103 / AC-103 / F-004 | Controlled terrain/walls, corners, jump trong boundary, knockback tới wall/ring-out; inspect explicit boundary | Wall chặn pose/hit, airborne trong bounds sống; vượt boundary báo server elimination, không dùng grounded làm contender; giữ diagram/tick traces |
| TC-104 | FR-104 / AC-104 / F-005, F-011 | Tạo private code; wrong code, fifth participant, non-owner start, <4, loading incomplete, duplicate start rồi đủ bốn loaded | Reject đúng lỗi; chỉ owner request hợp lệ start một lần; FFA không mode selector; roster/load ack/start log |
| TC-105 | FR-105 / AC-105 / F-003, F-006, F-011 | Spike hai instance trước; bốn instance và hai mạng khác nhau; retry spawn/load; fifth/late join và genuine resume | Một entity/identity, unique valid spawns/owners, no late admission; resume không new participant; record topology và server roster |
| TC-106 | FR-106 / AC-106 / F-003, F-007 | Host+3 move/jump/walls, inject speed/teleport/extra jump/invalid axes; compare authoritative snapshots | Invalid displacement rejected/corrected, bounds respected, clients hội tụ theo budget được duyệt; raw pose/tick/correction/delay data, không client pose truth |
| TC-107 | FR-107 / AC-107 / F-008 | Valid LMB hit, miss/range edge/behind/wall/self/dead target; before/at cooldown; duplicate request/multiple colliders; fake damage | Server chỉ apply mỗi valid hit/target một lần; mọi negative không HP/knockback sai; attack IDs/rejection/event logs |
| TC-108 | FR-108 / AC-108 / F-009 | Overkill HP, ring-out, một player HP=0 và cross boundary cùng step, duplicate callbacks, post-elimination inputs | HP>=0; một elimination/removal/spectator transition/player với HP_DEPLETED hoặc RING_OUT; không move/attack/pickup/gameplay effect; before/after contender set |
| TC-109 | FR-109 / AC-109 / F-009, F-011 | Từ hai contenders: loại một; loại cả hai cùng step; >=2 tới time limit; người nhảy ở timeout; terminal callback retry | One wins, zero draw, >=2 timeout draw; all eliminations batch trước result; airborne vẫn contender nếu chưa boundary; một result/event; server tick trace |
| TC-110 | FR-110 / AC-110 / F-010 | Unarmed contact; hai players contact cùng pickup/step; armed contact pickup khác; E armed/unarmed; drop/re-pickup/elim/out-of-arena theo approved policies | Auto pickup valid, one owner/max one/no stack; E không collect; bốn views khớp held/available; HP không reset; owner history/policy config |
| TC-111 | FR-111 / AC-111 / F-010 | Chuẩn đo bên dưới; matched unarmed/armed trials, target đủ HP tránh clamp che damage; same geometry/cooldown | Weapon damage 2x base và intended displacement 2x trong tolerance duyệt; không chứng minh bằng force field 2x; raw samples/trajectory/config |
| TC-112 | FR-112 / AC-112 / F-012 | Ngắt remote trong grace; avatar bị đánh/nhặt-drop liên quan/bị ring-out lúc offline; reconnect đúng proof, fake proof, old connection/input | Không new input lúc offline, avatar vulnerable; đúng identity/HP/weapon/elimination/timer hiện tại; một entity; eliminated spectates; no reset/revive/duplicate; generation/snapshot timeline |
| TC-113 | FR-113 / AC-113 / F-011, F-012 | Reconnect just-before/at/after grace, expiry lặp/intentional leave theo policy; host leave/kill/network loss trong lobby/Playing/results | Forfeit removal một lần, outcome từ current contenders; host loss Aborted với message, không award win/draw. Coincidence priority chỉ test theo O-06 đã duyệt; server/client logs |
| TC-114 | FR-114 / AC-114 / F-013 | Quan sát HUD lúc hit/pick/drop/eliminate/reconnect/finish/abort; spectator thử input và leave | UI khớp snapshots, HP/held/time/results/errors rõ; spectator không ảnh hưởng gameplay; controls theo approved O-08; screenshots/video và state comparison |
| TC-115 | NFR-101 / AC-N101 / F-014 | Review MSG-01–13 và map adapter; version mismatch, old/out-of-order snapshots, missing baseline, reconnect generation | Documented fields/limits/channels đúng implementation; mismatch reject, resync current revision, stale state không rollback; protocol diff/traffic đã redacted |
| TC-116 | NFR-102 / AC-N102 / F-015 | Theo impairment matrix dưới; chạy TC-106–113 dưới mỗi profile, reconnect cả active/eliminated | Invariants không vi phạm; ghi actual RTT/input response/correction/hit disagreement/reconnect duration và failures; quality verdict theo budget approved, không invent pass |
| TC-117 | NFR-103 / AC-N103 / F-016 | Theo cheat matrix dưới, valid command sau invalid để phát hiện lockout sai | No unauthorized state mutation, bounded reject/log; host tamper nằm ngoài claim; sanitized request/state diff và defect cho mọi violation |
| TC-118 | NFR-104 / AC-N104 / F-017 | Host+3 và headless+4 separate runs; representative move/jump/attack/pickup/reconnect/finish workload; máy/profile/run duration duyệt trước | Đo CPU/memory/tick duration/missed ticks/bandwidth/RTT/errors/disconnects; invariants intact; report budgets/failures/raw metrics; không assignment load scale |
| TC-119 | NFR-105 / AC-N105 / F-018 | Clean CI từ commit, licensed runner/module approved, build client/server, extract artifacts và smoke | Hai artifacts cùng commit/versions, startup success, logs/hashes/run URL; không token trong logs; failure fail gate, không chỉ YAML tồn tại |
| TC-120 | NFR-106 / AC-N106 / F-019 và toàn current scope | Reviewer dùng artifacts TC-119 chạy full demo bên dưới và regression TC-101–119 | Mandatory MVP đủ và reproducible, actual table/defects/reviewer/date; integration/commit/push ghi riêng, không suy merged từ branch push |

## Chuẩn đo 2x knockback — cần duyệt trước TC-111

Quy định bắt buộc là **displacement**, không force. Proposed fixture: mặt phẳng ngang không wall/slope/edge, cùng target mass/collider/controller/drag/friction/ground state, cùng attacker/target pose/facing và hit distance; target không input, không effect khác; target đứng yên trước hit. Pin fixed simulation timestep, seed nếu có, build/config/hardware; reset fixture đầy đủ mỗi trial. HP đủ cao đo raw unarmed damage D và armed 2D, không dùng overkill/clamp làm delta duy nhất.

PROPOSED đo horizontal displacement của target từ authoritative pose tại accepted impact tới settled pose khi horizontal speed dưới epsilon liên tiếp số steps đã duyệt; nếu motor dùng fixed-duration knockback thì endpoint là kết thúc interval đã duyệt. Chọn **một** endpoint, epsilon/steps hoặc interval trước khi chạy; không chọn endpoint sau để khớp 2x. Initial base displacement K, D, tolerance, số repetitions và measurement error đều UNDECIDED (O-09). Đề nghị matched repeated trials và báo từng displacement, ratio K_armed/K_unarmed, spread; acceptance ratio 2 trong tolerance được duyệt, không dùng mean để che trial vi phạm invariants.

Wall/terrain/ring-out integration là TC-103/108 riêng: obstruction có thể làm travel ngắn hơn, không đòi 2x khi chạm wall. Giữ same initial grounded/airborne state theo fixture; airborne variant không lẫn ground sample. Tuning force/velocity có thể phi tuyến; cần hiệu chỉnh theo measurement, không chỉ nhân physics force hai lần.

## Latency và load profiles PROPOSED

| Profile | Thiết lập đề nghị, cần ghi rõ hướng áp dụng | Ghi nhận |
| --- | --- | --- |
| L0 | Không artificial impairment; LAN và WAN ghi riêng | Actual RTT/jitter/loss, baseline frame/tick và host advantage |
| L1–L3 | Target RTT khoảng 50/100/200 ms; nếu tool là one-way delay thì chia/điều chỉnh đúng hai chiều và đo thực | Không gọi 100 ms packet delay là 100 ms RTT; record tool/version/pipeline/seed và actual RTT |
| L4 | Jitter/loss/reorder, lag spike và temporary disconnect; magnitudes/duration/repetitions chốt trước run | Delay/loss riêng từng link, reliable retransmission effects, reconnect deadline/correction/outcome consistency |

Các số latency là exploratory test points PROPOSED, không performance guarantee. Network Simulator hoặc Mirror wrapper chỉ chọn theo framework approved; không install hai stack. Chạy bốn participants với workload lặp và input trace đã ghi; host+3 là main demo, headless+4 là testing topology. Load duration/warmup/sample frequency/resource budgets/reference hardware/concurrent matches đều UNDECIDED; mặc định scope capacity là **một trận bốn người**, không tự thêm scale target. Ghi từng process/máy để không gộp client CPU vào server metrics. Báo hot spots/corrections và recommendation prediction nếu đo chứng minh cần; không thêm lag compensation trước thí nghiệm.

## Cheat và negative matrix Planned

| Nhóm | Injection | Invariant / TC |
| --- | --- | --- |
| Ownership | Fake participant/entity, remote start/control/drop, fake reconnect proof | No unauthorized movement/start/ownership/restore; TC-104/106/110/112/117 |
| Geometry/state | Teleport/speed, air-jump spam, attack through wall/out of range/behind, fake damage/HP/knockback | Server motor/geometry/stats truth; TC-106–108/111/117 |
| Replay/order | Duplicate attack/drop/pickup, multi-collider, old tick/seq, replay previous match/generation, superseded connection | Một effect/identity, no HP/weapon/result rollback; TC-107–112/115/117 |
| Lifecycle | Eliminated input, late join, grace expired resume, client claims winner, simultaneous HP/ring-out/expiry/timeout | No revival/admission, batch result và forfeit/abort đúng policy; TC-105/108–113/117 |
| Robustness | Nonfinite axes, oversized/malformed payload, request flood, unknown protocol | Bounded validation, safe diagnostics, valid next command vẫn xử lý; TC-115/117 |

Server-authoritative validation giảm remote cheating nhưng host-process modification ngoài trust guarantee; không gọi cheat matrix là anti-cheat toàn diện. Không nhập assignment-specific concurrency, retention hay SLA.

## CI và demo procedure Planned

CI workflow tương lai: checkout exact commit → activate licensed runner theo approved route → restore pinned Editor/modules/packages → rule/Play Mode suites phù hợp → build Windows client và headless server → smoke-start → publish logs/hashes/artifacts/test results. Không có YAML/scripts/run đã tạo ở task tài liệu. Runner OS/server target, Unity batch commands/build entry points và artifact retention cần chốt từ project thật; secrets chỉ trong approved CI secret store, entitlement chưa xác minh là blocker F-018.

Demo B/C: từ clean client artifact tạo private room/code, ba remote clients join qua ít nhất hai mạng; thử wrong/fifth join rồi bốn loaded owner start. Bốn movement/camera views; wall-blocked hit và valid melee; tranh pickup/auto collect, E drop, armed 2x theo fixture evidence. Ngắt một remote, gây damage/elimination khi offline, reconnect xác minh state và spectator; chạy grace-expiry case riêng. Chạy HP/ring-out và simultaneous last eliminations/timeout trên controlled runs, không ép mọi kết quả trong cùng trận. Lặp impairment profiles và host-loss Aborted riêng. Đối chiếu protocol/load/cheat/CI evidence với artifact commit. Reviewer ghi thực tế, defects và limits; không có fabricated human sign-off.

## Verification tài liệu đã chạy

DOC-001 ghi trong WORK_LOG kết quả link/ID/dependency/coverage/Git checks thực tế. Mục này không ghi pass gameplay; mọi TC-101–120 vẫn Planned cho tới feature implementation và execution evidence. Historical TC-001–037 thuộc offline v0.2, không tái sử dụng như online pass.
