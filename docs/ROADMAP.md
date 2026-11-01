# Small Arena — Roadmap và feature catalogue

Ngày 2026-11-01. Product roadmap cho ba sinh viên đang học Unity/C#, có Codex/Claude hỗ trợ; không phải lịch học. Horizon khoảng 60 ngày từ **2026-11-01 đến cuối tháng 12-2026** để quản trị rủi ro, không phải cam kết giao hàng hoặc giả định số giờ mỗi tuần. [SRS](../Long_document/srs.md) là nguồn luật; [TASKS](TASKS.md) ghi tiến độ thật. Chưa có feature gameplay nào DONE.

## Milestone và cổng nghiệm thu

Các milestone A → B → C có dependency tuyến tính. Chi tiết feature dependency trong catalogue là DAG. Hai instance ở A chỉ là spike, không được gọi là MVP. Mọi TC trỏ tới [TESTING](TESTING.md); hiện đều Planned.

| ID / feature IDs | Playable outcome | Entry conditions | Definition of Done / demo / evidence | Phần còn lại |
| --- | --- | --- | --- | --- |
| A — Foundation và validation: F-001–004, F-014 bản đầu | Arena placeholder, local controls và hai instance có ownership; tăng lên host+3 và thử khác mạng | BASE-001 đã thiết lập; framework/version/service approval và task branch được duyệt trước code | TC-101–103 và spike TC-105–106 chứng minh hai rồi bốn instance, camera riêng, host loss, internet khác mạng; lưu versions, build/log/video và protocol draft; không yêu cầu combat hoàn chỉnh | Lobby hoàn chỉnh, combat/weapon, reconnect, latency/load/cheat/CI evidence đầy đủ |
| B — FIRST playable online MVP: F-001–018 | Host+3 FFA private-code room → load/start → movement/combat/weapon → HP/ring-out → spectator/result, reconnect và latency simulation | A được kiểm chứng; gameplay proposals ảnh hưởng gate đã duyệt; build/license/connectivity sẵn sàng | Mọi AC-101–114 và AC-N101–N105 có execution evidence; demo TC-120 gồm pickup tự động/E drop, reconnect không revive/reset/duplicate, timeout/draw và host abort; protocol + load/latency/cheat + hai CI artifacts trong GitHub. Không được hoãn weapon hay reconnect | Regression độc lập/release freeze, usability và demo tái lập trên máy khác |
| C — Verified current FFA release: F-019 | Cùng phạm vi FFA đã qua regression và reviewer/demo | B đạt toàn bộ mandatory gates; defects blocking đã sửa và retest; commit/artifacts xác định | AC-N106/TC-120 trên clean artifacts, reviewer ghi ngày/kết quả/limitations; test failure không bị gọi là pass; giữ commit/build hashes, run IDs, logs/video, danh sách known issues | Chỉ FUTURE/optional; không còn missing mandatory MVP |
| D — FUTURE product direction | 2v2 và nhiều loại vũ khí | Ngoài baseline 60 ngày; cần yêu cầu mới và approval | Chỉ giữ FUT-001/FUT-002; chưa tạo mandatory milestone hoặc detailed tasks | Team/friendly-fire/outcome và thông số từng weapon chưa thiết kế/duyệt |
| E — Optional extensions | Art/audio polish, extra arena, practice bot, accessibility nâng cao | Không làm chậm B/C; cần scope riêng | Chưa có acceptance bắt buộc; ưu tiên sau evidence current release | Không ngầm trở thành yêu cầu khóa học |

Ưu tiên đầu horizon: chứng minh connectivity và authority trước khi làm offline game lớn. Giữa horizon tập trung đường đi end-to-end B; cuối horizon dành khả năng xử lý lỗi và C, không đặt deadline chi tiết khi chưa biết capacity. Nếu spike khác mạng/reconnect/CI license không khả thi, báo owner ngay và giữ mandatory gates; cắt skin, animation/audio polish, map/weapon bổ sung, prediction phức tạp chưa có bằng chứng cần, repeat-session convenience. Không cắt lobby, bốn người, weapon, reconnect hoặc bằng chứng khóa học để gọi slice nhỏ là MVP.

## Quy ước catalogue và phối hợp

Mọi F dưới đây có decision **CONFIRMED / CURRENT**, priority **P0**, implementation **TODO**; giải pháp cụ thể ghi PROPOSED và open decisions không làm feature thành implemented. Ngoại lệ: repeat-session trong F-011 là PROPOSED/optional. “AC” là điều kiện thực nghiệm; “Demo” là thủ tục Planned, không có kết quả hiện tại. Evidence chung cho mỗi feature: source commit, config/version, người/ngày kiểm, expected/actual, log/video/raw samples và defects; bảng chỉ ghi thêm evidence đặc thù. Không có commit/PR gameplay nào.

Bounded ownership đề xuất: một người nhận feature sau khi dependencies đạt cổng, hai người còn lại có thể review hoặc nhận feature độc lập; không gán tên hay thời lượng chưa biết. Mỗi dòng nêu shared artifacts **dự kiến**, chưa có scene/prefab thật. Một editor tại một thời điểm cho scene chung; thay player/weapon/UI ưu tiên prefab riêng và phối hợp contract trước integration. AI assistance phải ghi đúng, không thay human review bằng nhãn đã duyệt.

### F-001 — Reproducible Unity foundation

- Outcome/scope: clean checkout mở được arena skeleton và tạo Windows client/headless server; gồm version pin, source/meta/config cần thiết; loại trừ gameplay/prototype imports.
- Dependencies/sequence: BASE-001 và approval TECH-001; đầu A. SRS: FR-101; AC-101.
- AC/demo: TC-101 trên máy sạch, version/config nhất quán, hai artifacts smoke-start không missing scene/reference; không đoán build command trước project thật.
- Evidence: approved matrix Editor/framework/packages/modules/licenses, clean-build logs/hashes. Risk/open: exact Editor/render pipeline/input/camera packages và CI entitlement chưa duyệt.
- Coordination: project settings/package manifest và Arena/Bootstrap scenes dự kiến; cần người giữ foundation, liên kết ARCHITECTURE và README.

### F-002 — Movement, jump, facing, local camera

- Outcome/scope: keyboard move/jump, facing theo movement, elevated fixed-heading follow; không mouse orbit, dash/combo/bot.
- Dependencies/sequence: F-001; A. SRS: FR-102; AC-102.
- AC/demo: TC-102, tám hướng và jump/corners, dừng/đổi hướng; input/camera chỉ local owner khi tích hợp F-003; không điều khiển remote.
- Evidence: input/facing video và owner binding logs. Risk/open: O-01, speed/jump, camera occlusion và controller choice cần test.
- Coordination: Player prefab, input actions, camera rig; contract motor dùng lại F-007, một người giữ player prefab.

### F-003 — Early networking/ownership spike

- Outcome/scope: hai instance rồi bốn, host+3, join/connect thử khác mạng; authority/input/camera ownership rõ; không gọi spike là lobby/MVP hoàn chỉnh.
- Dependencies/sequence: F-001, TECH-001; sớm A song song nghiên cứu motor; SRS: FR-101, FR-105–106, FR-113.
- AC/demo: spike subset TC-105–106/TC-113: mỗi identity một avatar, remote không điều khiển nhau; hai mạng khác nhau thật, host loss báo abort; thất bại ghi blocker, không LAN-only pass internet.
- Evidence: topology/NAT/relay path, versions, connection logs, machine/network setup và video. Risk/open: credentials/quota, NAT, reconnect identity compatibility; chỉ framework đã chọn, không implement cả NGO/Mirror.
- Coordination: Bootstrap/NetworkPlayer prefabs, ARCHITECTURE/PROTOCOL draft, network adapter; kết hợp F-002 bằng review hook ownership.

### F-004 — Arena, collision, ring-out boundary

- Outcome/scope: một arena có terrain/walls và explicit ring-out boundary; không obstacle course hoặc extra map.
- Dependencies/sequence: F-001, F-002; A. SRS: FR-103; AC-103.
- AC/demo: TC-103, nhảy vẫn sống, wall/terrain collisions đúng, vượt boundary gửi điều kiện elimination cho authority; không dùng floor-contact làm contender truth.
- Evidence: boundary diagram, collision layers và video edge tests. Risk/open: kích thước/spawn positions/boundary geometry và knockback-wall handling chưa chọn.
- Coordination: Arena scene/collision setup, player collider; F-009 sở hữu removal, F-004 chỉ điều kiện môi trường.

### F-005 — Private room và readiness/start

- Outcome/scope: create/join code, capacity bốn, owner start khi connected+loaded; không matchmaking/mode selector; ready button chỉ proposal O-08.
- Dependencies/sequence: F-003; B. SRS: FR-104; AC-104.
- AC/demo: TC-104 thử wrong/full code, duplicate start, unauthorized start, missing load ack; match chỉ bắt đầu khi đủ bốn.
- Evidence: room roster/load/start logs. Risk/open: room code không phải reconnect credential, service lifecycle/quota.
- Coordination: Lobby scene/UI và session adapter; loading/start contract với F-006/PROTOCOL.

### F-006 — Authoritative spawn/start

- Outcome/scope: server gán bốn unique spawns/owners, khởi tạo state trước Playing, khóa new admission; không respawn.
- Dependencies/sequence: F-004, F-005; B. SRS: FR-105; AC-105.
- AC/demo: TC-105, retry load/start không thêm avatar, fifth/late join reject; roster authority chính xác cả local host client.
- Evidence: participant/entity mapping và start snapshot. Risk/open: loading timeout/failure recovery chưa duyệt.
- Coordination: Arena spawn markers, NetworkPlayer prefab, match/session adapter; không cùng lúc sửa Arena với F-004.

### F-007 — Authoritative movement/synchronization

- Outcome/scope: server validate input và simulate position/facing/collision/jump/knockback; replicate state; không chấp nhận arbitrary client pose.
- Dependencies/sequence: F-002, F-003, F-006; B. SRS: FR-106, FR-103; AC-106.
- AC/demo: TC-106 đối chiếu bốn views, speed/teleport/air-jump injection bị chặn; correction không vượt walls/boundary; quantify drift trong test.
- Evidence: input/snapshot/tick traces và correction distance. Risk/open: tick rate/budgets, interpolation/prediction chỉ chọn sau latency experiment.
- Coordination: Player motor/net prefab và arena colliders; authoritative displacement contract với F-008–010.

### F-008 — Melee và server hit validation

- Outcome/scope: LMB forward basic attack, server cooldown/range/geometry/occlusion và hit dedup; không combo/alternate attack/lag compensation mặc định.
- Dependencies/sequence: F-007; B. SRS: FR-107; AC-107.
- AC/demo: TC-107 hợp lệ gây một damage/knockback, miss/behind/wall/cooldown/multi-collider không tạo extra hit; client damage claim không có authority.
- Evidence: attack IDs, rejection reasons và controlled hit scene captures. Risk/open: cooldown/range/window/airborne attack và pending-hit ordering chưa chọn.
- Coordination: Combat/player prefab, hit layers và PROTOCOL; F-009 owns HP/elimination integration.

### F-009 — HP, knockback, unified elimination/outcomes

- Outcome/scope: HP clamp, displacement, shared HP/ring-out elimination batch, spectator action gating, last-one/zero/timeout results; không respawn.
- Dependencies/sequence: F-004, F-008; B. SRS: FR-108–109; AC-108–109.
- AC/demo: TC-108–109 gồm same-player dual cause và hai người cuối cùng bị loại cùng step; một removal/player, zero draw, timeout >=2 draw, no actions after elimination.
- Evidence: before/after contenders, reason/event IDs, tick batch/outcome logs. Risk/open: O-04/O-06, tuning/time limit; do not resolve result trong từng callback.
- Coordination: Health/match rule components, player/spectator transition, Arena boundary contract; UI chỉ đọc.

### F-010 — One weapon, auto pickup/E drop/2x

- Outcome/scope: server ownership/contact contention, max one, E drop, replicated held/available, 2x damage/displacement; không inventory/additional types.
- Dependencies/sequence: F-008, F-009; B, bắt buộc first MVP. SRS: FR-110–111; AC-110–111.
- AC/demo: TC-110–111 trên bốn instances; hai contacts chỉ một owner; E không collect; damage đúng 2x và displacement theo chuẩn TESTING.
- Evidence: pickup owner history, attack/damage and displacement samples. Risk/open: O-02–04, count/placement/repickup/elimination/out-of-arena policies phải duyệt trước acceptance.
- Coordination: WeaponPickup/held weapon prefabs, combat stats và Player prefab attach point; update PROTOCOL/TESTING đồng bộ.

### F-011 — FFA lifecycle và results

- Outcome/scope: room/loading/Playing/Finished/Aborted rõ; unique result; no admission/respawn; return-room/repeat match là PROPOSED optional, không mandatory B.
- Dependencies/sequence: F-006, F-009; B. SRS: FR-104–105, FR-109, FR-113; AC-104–105, AC-109, AC-113.
- AC/demo: TC-104–105/109/113 kiểm state gates, matchId isolation và no result after host loss; nếu thử repeat flow phải matchId mới và không carry HP cũ.
- Evidence: transition trace/result snapshot. Risk/open: O-06/O-08 và timeout collisions.
- Coordination: Match coordinator, Lobby/Result views, PROTOCOL lifecycle; không trộn room membership với active contenders.

### F-012 — Disconnect/reconnect/resynchronization

- Outcome/scope: existing identity reconnect, grace/forfeit, vulnerable disconnected avatar, current snapshot; eliminated spectates; host loss Aborted; không host migration.
- Dependencies/sequence: F-005, F-007, F-009, F-011; B, không defer sau MVP. SRS: FR-112–113; AC-112–113.
- AC/demo: TC-112–113 trước/sau grace, HP giảm/weapon đổi khi offline, eliminated trước quay lại, old connection/input và fake identity; không duplicate/reset/revive.
- Evidence: identity-redacted connection generations, roster/snapshot/event timeline. Risk/open: O-05–07, service membership expiry, session vs gameplay reconnect khác nhau.
- Coordination: Session/player lifecycle adapters và PROTOCOL identity contract; secrets không vào logs/version control.

### F-013 — HUD, spectator và errors

- Outcome/scope: controls/HP/held weapon/time/state/results, room/connect/reconnect/abort errors; spectator không gameplay; camera controls O-08, polish optional.
- Dependencies/sequence: F-010, F-011, F-012; B. SRS: FR-114; AC-114.
- AC/demo: TC-114 active/eliminated/reconnected views khớp server; leave spectator không thêm removal; Aborted khác victory/draw.
- Evidence: bốn view recordings và snapshot-to-UI comparison. Risk/open: spectator targets/camera và resolutions chưa chọn.
- Coordination: HUD/Result/Spectator prefabs, camera binding F-002 và match adapter; không UI write HP/outcome.

### F-014 — Protocol contract và compatibility

- Outcome/scope: framework-neutral contract rồi adapter tới một framework sau duyệt; authority/validation/version/ordering/reconnect docs; không REST/database.
- Dependencies/sequence: F-003; bắt đầu A, hoàn tất B. SRS: NFR-101; AC-N101.
- AC/demo: TC-115 review every message against observed traffic and rejection logs, version mismatch clean reject, snapshot resync và old generation tests.
- Evidence: versioned protocol và contract-to-test mapping. Risk/open: payload/rate/sequence windows và framework semantics chưa chọn.
- Coordination: PROTOCOL.md là shared contract, thay đổi phải thông báo F-005–013/015–018, không copy normative rules sang từng feature.

### F-015 — Latency simulation và reproducible suite

- Outcome/scope: baseline/delay/jitter/loss/disconnect có reproduce config; không lấy LAN result làm WAN proof.
- Dependencies/sequence: F-007, F-008, F-010, F-012, F-014; B. SRS: NFR-102; AC-N102.
- AC/demo: TC-116 theo TESTING, đo actual RTT/correction/hit/result/reconnect, invariants giữ đúng, failure ghi defect; approve quality budgets trước sign-off.
- Evidence: impairment settings/seeds, per-instance logs/raw samples/video. Risk/open: simulator one-way vs RTT và host advantage; threshold chưa duyệt.
- Coordination: controlled test scenes/config dự kiến, TESTING/PROTOCOL; tránh thay tuning trong measurement run.

### F-016 — Cheat/negative test catalogue execution

- Outcome/scope: spoof/spam/replay/teleport/hit/ownership/reconnect negatives; không hứa chống tamper host process.
- Dependencies/sequence: F-010, F-012, F-014; B. SRS: NFR-103; AC-N103.
- AC/demo: TC-117 inject từng invalid command, audit không HP/ownership/outcome sai, valid following command vẫn hoạt động.
- Evidence: sanitized requests/rejections/state diffs và defects. Risk/open: test harness chưa tồn tại; bounds phải duyệt, không silently drop all legitimate inputs.
- Coordination: test harness dự kiến, PROTOCOL validation, Health/motor/session contracts; không ship cheat harness trong release client.

### F-017 — Four-player load và resource evidence

- Outcome/scope: host+3 và headless+4 trên máy/network được ghi; đo CPU/memory/tick/bandwidth/errors, không massive assignment load.
- Dependencies/sequence: F-015, F-016; B. SRS: NFR-104; AC-N104.
- AC/demo: TC-118 thao tác movement/combat/pickup/reconnect representative, không duplicate/results sai; giữ raw metrics, báo vượt budget đã duyệt.
- Evidence: machine/process topology, sampling method và raw series. Risk/open: run length/budget/reference machines O-09, host/client trên cùng máy gây bias.
- Coordination: TESTING và performance instrumentation dự kiến; không thay scene/config đồng thời khi đo.

### F-018 — CI client/server artifacts

- Outcome/scope: clean CI builds Windows client và headless server, smoke test, artifact metadata; task này mới mô tả Markdown, YAML/scripts chỉ feature sau approval.
- Dependencies/sequence: F-001, F-003, F-014; B. SRS: NFR-105, FR-101; AC-N105, AC-101.
- AC/demo: TC-119 cùng source commit và pinned versions, client launch/headless startup, logs/hashes, secrets không xuất log; license entitlement hợp lệ.
- Evidence: CI run URL, build logs/artifact hashes, runner/modules/license method đã review. Risk/open: runners/cost/license chưa duyệt; không giả định Personal key dùng chung được.
- Coordination: build settings/entry points dự kiến, README/TESTING; không tự tạo workflow trong documentation task.

### F-019 — Integrated release/demo acceptance

- Outcome/scope: verified current FFA từ clean artifacts; không thêm 2v2/map/polish làm gate.
- Dependencies/sequence: F-013, F-015, F-016, F-017, F-018; C. SRS: NFR-106 và toàn bộ current FR/NFR; AC-N106.
- AC/demo: TC-120 end-to-end host+3 khác mạng, weapon/reconnect/elimination/timeouts/abort cùng regression matrix; reviewer xác nhận evidence, blocking defects đóng bằng retest.
- Evidence: release commit/artifact IDs, demo script/video, actual TC table, reviewer/date và limitations; không giả human review.
- Risk/open: package/config drift giữa evidence và release artifact, reviewer/hardware chưa bố trí; cần freeze và tái lập từ đúng commit, không che missing gate bằng demo video.
- Coordination: release scene/build freeze, SRS/README/PROJECT_STATE/TASKS/WORK_LOG/TESTING; specs/tests/state đi cùng feature integration.

## Backlog ngoài current baseline

- FUT-001 — 2v2: FUTURE/CONFIRMED direction, implementation TODO; không team allocation/friendly fire/last-team rules được duyệt, không task chi tiết hoặc current milestone.
- FUT-002 — Additional weapon types: FUTURE/CONFIRMED direction, implementation TODO; khác speed/range/damage/knockback cần specification sau.
- OPT-001 — Extra art/audio/arena/bot/accessibility và repeat-session convenience: PROPOSED/optional, TODO; cắt trước mandatory scope.

## Specification chi tiết kế tiếp — TECH-001

Chỉ expand feature khi đã đến lượt; template bên dưới áp dụng cho feature khác. TECH-001 là quyết định kỹ thuật bounded trước F-001/F-003, không phải task implement cả hai frameworks.

- **Mục tiêu:** owner/team duyệt một framework, Editor/package matrix và connectivity/license plan dựa trên [ARCHITECTURE](ARCHITECTURE.md); không coi recommendation là approval.
- **Input:** BASE-001 đã tạo/push tại `1d7a6c9`, SRS FR-101/104–106/112–113, NFR-101–105; tài liệu branch này chưa integrated.
- **Included/excluded:** review official support/dependency/releases, service costs/quotas, anonymous identity/reconnect, Windows client/headless build rights; không install/code/paid provisioning trong documentation task.
- **Ownership/coordination:** một technical owner đề xuất matrix, owner/team duyệt trong DECISIONS; chưa gán thành viên; ARCHITECTURE/PROTOCOL/README cần cập nhật cùng nhau.
- **AC:** ghi exact proposed Editor/framework/transport/services/package versions và release compatibility evidence; đánh dấu approval từng mục; budget/quota guardrails và CI license route được duyệt hoặc blocker rõ; chọn đúng một framework; định nghĩa thí nghiệm hai→bốn instance và different-network cho F-003.
- **Verification/evidence:** official links/date + decision reviewer/date; chưa có execution. Nếu compatibility cần spike thực thì ghi conditional approval và giữ F-001/F-003 BLOCKED tới authorization implementation.
- **Risks:** compatibility trong docs không chứng minh build; Relay membership không đảm bảo gameplay restoration; host trust và service cost; không import assignment.
- **Next:** sau duyệt và integration tài liệu, xin feature branch riêng từ latest approved long23001901 cho F-001; scope F-001 chỉ project skeleton/reproducibility.

Template feature specification sau này: ID/name → outcome/status/scope → requirement/AC → dependencies/base/branch approval → design/contracts/shared files → demo/tests → evidence → risks/open decisions → result/commit/PR/integration. Không tạo executable artifacts khi chỉ mới lập spec.

## Branch và integration

`long23001901` đã được owner duyệt từ setup commit `1d7a6c91fb7c2a3ef2fa233c1d5efe5cc60b462e`, không lấy assignment/Godot/Go làm nền. Nhánh docs kế thừa memory của commit đó và đang chờ integration; memory không tự đồng bộ. Không có SRS riêng theo nhánh. Sau reviewed integration được cho phép, reconcile state trên integration; feature branch sau đó mới lấy latest approved integration. Commit/push không đồng nghĩa merged; task này không merge/PR/cherry-pick.
