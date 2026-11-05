# Decisions

> Bản ghi dưới đây giữ lịch sử nguyên trạng. Các claim setup uncommitted, chưa chọn Unity hoặc chưa có integration chỉ đúng tại thời điểm ghi; xem các quyết định ngày 2026-11-01 bổ sung ở cuối cho trạng thái hiện hành.

## WF-001 — Persistent memory with personal branch isolation
- Date: 2026-11-01 (Asia/Bangkok)
- Status: ACCEPTED for workflow setup by explicit user instruction; not a gameplay architecture approval.
- Context: The repository base contains only README.md; local reference documents and a separate assignment worktree must be preserved. Sessions need a compact, verified handoff.
- Selected approach: Stable instructions in AGENTS.md, current state/tasks in PROJECT_STATE.md and TASKS.md, decisions here, append-oriented evidence in WORK_LOG.md. Setup uses the approved chore/23001901/project-agent-setup branch from current main; all shared branches are protected.
- Rationale: Resume with minimal context, distinguish plans from evidence, and preserve shared branch history and unrelated work.
- Alternatives considered: Chat-only memory (not persistent); a single growing history file (poor navigation); direct work on main (forbidden).
- Consequences: Update memory at handoff, verify live Git state each session, require branch-switch approval and explicit authorization for commit/push/merge. Setup is local and uncommitted.

## Technical decision status
No gameplay architecture or toolchain decision is adopted by setup. The existing proposed D-01–D-12 register remains in ../Long_document/srs.md; do not duplicate or silently approve it here. Use PLAN-001 to capture approval evidence and later significant decisions, preserving superseded history.

## WF-002 — Personal integration and feature branch architecture
- Date: 2026-11-01 (Asia/Bangkok)
- Status: ACCEPTED workflow by owner instruction; supersedes WF-001 branch strategy for future game development, preserving its setup history. Starting commit remains PROPOSED, not approved.
- Context: Setup branch has no game implementation; cached branches contain distinct assignment, Go spike and Godot/.NET work.
- Selected approach: long23001901 is authoritative after establishment; substantial work uses approved dedicated branches from its latest approved commit; integration uses authorized PR/reviewed merge. Reconcile memory after integration.
- Rationale: Separate completed development, unfinished features and academic assignment evidence with traceable history.
- Alternatives considered: Permanent setup-branch development; assuming assignment is a complete game; automatic integration. All conflict with owner requirements.
- Consequences: BASE-001 awaits approval; candidate 1257ad73e57a238d2a86bdddb6f72c1278977541 needs foundation confirmation and validation. Dedicated memory commit and later incorporation require explicit authorization. No technical stack is approved by this decision.

## WF-003 — Clean integration and assignment transfer policy
- Date: 2026-11-01 (Asia/Bangkok)
- Status: ACCEPTED by owner instruction; extends WF-002. Its historical candidate preference does not approve a foundation.
- Context: Academic artifacts and divergent technology prototypes must remain separate from production integration.
- Selected approach: Explicit path staging and staged-content review; technology-specific ignore policy only after selection; reviewed ports/isolated cherry-picks on feature branches with origin commit evidence and build verification; reconcile memory only after verified authorized integration.
- Rationale: Preserve complete functional dependencies and academic history while preventing unrelated artifacts entering game commits.
- Alternatives considered: Bulk assignment merging, blind staging, premature engine ignore rules; rejected as contrary to owner requirements.
- Consequences: No .gitignore change until technology selection; BASE-001 remains blocked. No foundation is approved; Godot/.NET candidate 1257ad73e57a238d2a86bdddb6f72c1278977541 or clean root b5054a61258e8a4d274fe56198dd5d0a5304124b for a fresh Unity baseline requires an explicit owner decision. Neither is a verified runnable game foundation.

## BASE-001 — Nền integration cho Unity/C# mới

- Ngày: 2026-11-01, Asia/Bangkok.
- Status: ACCEPTED bởi chủ dự án trong phiên; supersedes các foundation proposals ở WF-002/WF-003, không xóa lịch sử.
- Context: Setup commit `1d7a6c91fb7c2a3ef2fa233c1d5efe5cc60b462e` đã commit/push; memory cũ vẫn ghi uncommitted. Live Git/ref/tree đã kiểm; assignment/Godot/Go không phải selected Unity foundation.
- Approach: Owner duyệt tạo `long23001901` đúng commit setup này, push upstream, rồi tạo `docs/23001901/online-ffa-roadmap` từ integration cho documentation task. Đã thực hiện branch creation/push integration thành công; docs đang chờ integration riêng.
- Rationale: Kế thừa workflow/SRS/learning/history hiện có, không nhập prototype implementation hoặc rewrite academic history. Đây là foundation Git cho project mới, chưa là runnable Unity project.
- Alternatives: Godot/.NET hoặc Go/Node assignment không phù hợp confirmed Unity/C#; root-only base cần incorporation riêng trong khi owner chọn setup commit.
- Consequences: Nền đã có setup docs theo ancestry, không cần cherry-pick memory. Tài liệu online mới chỉ đi vào integration qua thao tác reviewed/được duyệt sau này; task này không merge/cherry-pick/PR. Exact packages/framework/services chưa approved. Integration memory tại setup còn stale, chỉ reconcile lên integration sau authorized integration tài liệu.

## PROD-001 — Current online FFA thay offline prototype

- Ngày: 2026-11-01.
- Status: CONFIRMED bằng task prompt do chủ dự án chỉ định đọc/thực hiện; thay current-scope interpretation cũ, không chứng nhận implementation.
- Context/approach: [SRS](../Long_document/srs.md) v0.3 sở hữu FR-101–114, NFR-101–106, UC-101–105 và AC mới; giữ nguyên toàn bộ offline v0.2 với retired IDs. Unity/C#, Windows, host+3 FFA, private code room, weapon auto pickup/E drop/2x, HP/ring-out elimination, reconnect, latency/protocol/load/cheat/CI evidence đều current confirmed. 2v2/additional weapons future.
- Rationale/alternatives: Tiếp tục gọi offline bot slice là MVP sẽ thiếu yêu cầu bắt buộc; không sửa nghĩa ID cũ để che scope revision.
- Consequences: PLAN-001 phần product reconciliation được giải quyết ở DOC-001; tuning/version/connection/lifecycle details O-01–09 và TECH-001 vẫn chưa duyệt. Không dùng 10 HP/1 damage từ lịch sử.

## TECH-001 — Hướng networking đề nghị, chưa duyệt

- Ngày: 2026-11-01.
- Status: PROPOSED; không có framework/service/version approval.
- Context/approach: [ARCHITECTURE](ARCHITECTURE.md) so sánh official NGO/Mirror support, authority, internet rooms, reconnect, impairment, CI/licensing và cost/quota. Đề nghị NGO + Unity Transport + MPS sessions/Relay, candidate 6.3 LTS patch sau kiểm compatibility.
- Rationale: Đường official GameObject/session/code/relay có thể giảm connectivity work cho ba sinh viên; không performance superiority claim.
- Alternatives: Mirror nếu approved spike cho thấy phù hợp hơn; không implement cả hai. Dedicated hosting/database/REST không cần cho scope hiện tại.
- Consequences: Owner/team phải duyệt exact version/dependency matrix, service budget/identity/reconnect và CI license route. F-001/F-003 chưa chạy; version-sensitive sources/date nằm ARCHITECTURE; project quota/entitlements còn UNVERIFIED.

## TV2-PLAN-001 — Ưu tiên phân công cho kế hoạch cá nhân

- Ngày: 2026-11-02 (Asia/Bangkok). Status: ACCEPTED về quyền cập nhật kế hoạch TV2 theo owner; gameplay/framework toàn nhóm vẫn PROPOSED/UNDECIDED.
- Context: Owner chỉ định đọc update_prompt_1.md và làm việc trên exact docs branch; đã đọc toàn bộ phân công. Phân công tự nhận là đề xuất, có scope khác PROD-001/TECH-001 ngày trước và hướng Git main khác workflow cá nhân đã được duyệt.
- Approach: SRS v0.4 dùng FR/AC-201–209 mới, GD-01–09 và conflict/mapping; lưu nguyên v0.3/v0.2. TASKS sở hữu TV2-001–013 và trạng thái, ROADMAP M0–M6; không thêm TV2 plan riêng. Chỉ cập nhật AGENTS scope, giữ mọi Git safeguards. Phân công và prompts untracked giữ nguyên.
- Rationale: Rõ việc TV2 M1/M3 mà không giả định proposal thành team approval hoặc nhập cả hai rule sets. Không dùng code/path đề xuất như evidence implementation.
- Alternatives: Gộp melee+raycast, elimination+respawn, NGO+Mirror thành mandatory scope; nhận luôn networking/UI/CI cho TV2; thay main hoặc import assignment — loại vì sai nguồn/quyền/phạm vi.
- Consequences: PROD-001/TECH-001 cũ giữ lịch sử, superseded trong kế hoạch cá nhân về topology/combat. TECH-001 hiện hướng Mirror/version/server OS/license do TV1 phối hợp nhóm, chưa pin/install. GD-01–09 chặn execution từng phần, không chặn draft/test design. Git baseline long23001901 vẫn 1d7a6c9; không commit/push/PR/merge trong phiên này. Cần review TV1/TV3 và nhóm trước triển khai; chưa có human review độc lập.

## WF-TV2-001 — nhánh cá nhân và scope ủy quyền

- Ngày: 2026-11-05 (Asia/Bangkok). Status: ACCEPTED cho workflow cá nhân bởi owner 23001901 qua chỉ thị đọc/thực hiện prompt TV2-001; không gameplay approval.
- Context: Quy tắc tên nhánh cá nhân cũ và ví dụ Git nhóm §7 khác yêu cầu mới. Prompt tồn tại untracked ở worktree gốc, được đọc đầy đủ và giữ nguyên; không nhập prompt vào Git.
- Approach: `feature/<TASK-ID>-<descriptive-kebab-case-feature-name>` cho nhiệm vụ cá nhân sau này, nhánh hiện tại `feature/TV2-001-scope-reconciliation-and-decisions`. Base latest verified origin/long23001901 = 1d7a6c91fb7c2a3ef2fa233c1d5efe5cc60b462e. Mỗi task độc lập một worktree/branch sau khi prerequisites có trong integration; không tạo các nhánh tương lai trong task này.
- Rationale: Truy vết task và giữ integration cá nhân tách khỏi main/nhánh thành viên khác. Supersedes riêng naming cá nhân cũ trong AGENTS; không thay phân công §7 hoặc history WF-002.
- Alternatives: Nhánh từ docs/main, merge cả docs, push integration, đổi permissions — không thuộc quyền được cấp.
- Consequences: Owner cho phép fetch, isolated worktree/branch, selective documentation transfer, commit/push exact task branch, PR vào long23001901, summary và một `@codex review`. Không merge/rebase/reset/force push/delete/protection changes. Integration và human review còn gate riêng; AI review không thay nhóm duyệt luật.

## TV2-001 — agenda quyết định ngày 2026-11-05

Nguồn yêu cầu/nghĩa GD giữ tại [SRS v0.4](../Long_document/srs.md#sổ-quyết-định-cần-thống-nhất); [ma trận nguồn](../Long_document/srs.md#tv2-001--đối-chiếu-nguồn-ngày-2026-11-05) định nghĩa D/M/N/P cùng full SHA/path. Không đổi nghĩa GD-01–09. Tất cả recommendation dưới đây là PROPOSED của TV2/AI hỗ trợ; **Status mỗi GD: UNDECIDED; actual decision: CHƯA CÓ; approver: UNKNOWN; approval date: UNKNOWN**. Không có buổi review nhóm/TV1/TV3 được quan sát trong phiên; commit/PR merged code không phải approval cho luật mới. Task TV2-001 BLOCKED ở human decision/review gate, phần tài liệu có thể hoàn tất và gửi review ngay.

### GD-01 — tuning và miền input

- Question: H/maxHP, D, C giây, R mét, V m/s, input axes/magnitude/units và timeout τ nào được dùng cho PvP?
- Alternatives: Mang nguyên offline defaults; dùng observed TV1 constants làm baseline; dùng config ký hiệu rồi nhóm duyệt bảng số/version.
- Recommendation/rationale: Chọn phương án config ký hiệu H,D,C,R,V,τ, yêu cầu H>0, D>0, C>0, R>0, V≥0 hữu hạn; giữ world-XZ analog, clamp magnitude>1 và reject nonfinite như N làm candidate để tránh thay motor không cần thiết. Ngưỡng clamp/reject, timeout và numeric tuning phải được review; chưa tạo schema TV2-002.
- Proposer/reviewers: TV2 đề xuất; TV1 review simulation/time/ingress; TV3 review input/feel; cả nhóm phê duyệt.
- Blocks: TV2-002/003/005/007 numeric execution; TV2-010 expected số; CT-201/202/204; FR/AC/TC-202–205. Symbolic drafts vẫn làm được.
- Evidence: M Health/CombatController/WeaponPickup fields; N ServerPlayerMovement constants, MovementInputValidator.TryClamp và N protocol §MovementInput. Static inspected, numeric approval UNKNOWN.
- Review outcome: chưa nhận phản hồi; cần bảng config có units/version/người duyệt và ca biên TC-202/203/205 trước execution.

### GD-02 — ammo và reload

- Question: Bản đầu có ammo/reload không; nếu có, empty/reload/cancel khi chết có nghĩa gì?
- Alternatives: Unlimited ammo với cooldown; magazine/reload; finite match ammunition.
- Recommendation/rationale: Unlimited ammo cho vòng raycast đầu để giảm states/UI; giữ cooldown server và không thêm pickup/2x mặc định. Cần nhóm chọn rõ, không gọi đây là yêu cầu confirmed.
- Proposer/reviewers: TV2; TV3 review HUD/input; TV1 review server state; cả nhóm duyệt scope.
- Blocks: TV2-005 scope/TV2-002 config phần ammo, CT-202/204, FR/AC/TC-203. Không chặn gun spec ghi rõ hai nhánh lựa chọn.
- Evidence: D phân công §2 “đạn nếu có”; M WeaponPickup buff không phải ammo implementation.
- Review outcome: chưa có; cần quyết định yes/no và nếu yes, transition/units/empty expected trước integration.

### GD-03 — camera, aim và geometry

- Question: Góc nhìn nào; origin từ muzzle/pose server hay đối chiếu client; aim direction/range/tolerance, obstruction, layers/triggers/self-hit thế nào?
- Alternatives: Fixed-heading aim; camera/ray-directed aim; server-facing aim. Origin server-only hoặc bounded client suggestion.
- Recommendation/rationale: TV3 cung cấp camera/aim draft; ưu tiên origin server từ pose/muzzle và direction intent hữu hạn, nonzero, được kiểm/normalize; server raycast nearest obstruction, bỏ self và trigger không liên quan theo layer mask được review. Không chọn góc nhìn/tolerance số thay TV3; không suy yaw +Z của motor là hướng gun bắt buộc.
- Proposer/reviewers: TV3 perspective/input, TV2 geometry, TV1 pose/time/adapter; cả ba phải review.
- Blocks: TV2-004/006 integration, CT-202/203, FR/AC/TC-204. TV2-005 có thể draft R bằng ký hiệu.
- Evidence: M ThirdPersonCameraController.ApplyFixedRotation và CombatController.Update (sphere khác ray); N ServerPlayerMovement.FixedUpdate ép rotation identity; D GD-03.
- Review outcome: chưa có; cần diagram camera→aim→server ray, self/trigger/wall/inside-collider cases và contract trước code.

### GD-04 — respawn và safe spawn

- Question: Delay Δrespawn, safe spawn/fallback, protection và cooldown/HP reset khi life mới?
- Alternatives: Fixed delay+round-robin; candidate scoring có obstruction/distance checks; retry khi mọi spawn blocked; protection có/không.
- Recommendation/rationale: Server timer Δrespawn và life generation mới; chọn candidate hợp lệ theo TV3 arena, nếu không có safe candidate giữ Pending rồi retry bounded thay vì spawn vào vật cản. Protection là lựa chọn riêng chưa duyệt. Hồi HP về H và cooldown reset/carry phải được quyết định trước test, không tự mặc định.
- Proposer/reviewers: TV2 life/timer; TV3 spawn geometry; TV1 spawn authority/lifecycle; nhóm duyệt.
- Blocks: TV2-008 execution/TV2-002 transitions, CT-204/205, FR/AC/TC-206; liên quan TC-208.
- Evidence: D FR-206/GD-04; N ArenaNetworkManager reservedSpawns chỉ chọn điểm trống theo connection, chưa là combat safe spawn; M Health không respawn API.
- Review outcome: chưa có; cần spawn availability/fallback/protection bảng quyết định và replay callback expected.

### GD-05 — death và kill credit

- Question: Suicide/environment, simultaneous lethal hits, attacker rời đi và credit từ callback cũ xử lý thế nào?
- Alternatives: Final valid lethal-hit credit; last-damager assist window; no credit environment; batch simultaneous adjudication.
- Recommendation/rationale: Một accepted lethal effect cho mỗi victim life cấp tối đa một credit, khóa theo match/life/effect; environment/suicide không cộng opponent kill trừ khi nhóm duyệt last-damager policy riêng. Server deterministic ordering cho cùng-step hits cần TV1/TV2 thống nhất; không suy guard object Health đủ cho life mới.
- Proposer/reviewers: TV2; TV1 ordering/event delivery; TV3 death/score display; cả nhóm duyệt fairness.
- Blocks: TV2-007/009 acceptance, CT-203/204, FR/AC/TC-205/207 và TC-208 stale-life cases.
- Evidence: M Health.TakeDamage reject dead và finally OnDeath; D FR-205/207; không có life-aware score implementation được xác minh.
- Review outcome: chưa có; cần traces hai lethal hits, suicide/environment, callback sau respawn và expected credit.

### GD-06 — match end và terminal order

- Question: Time limit Tmatch/score limit K hay kết hợp; tie, same-step death/timeout/respawn ưu tiên nào?
- Alternatives: Time limit với score ranking; score limit; kết hợp; last survivor thuộc baseline cũ, không nhập mặc định.
- Recommendation/rationale: Time-limited FFA, score ranking và explicit draw khi hòa là candidate đơn giản cho respawn; K optional sau review. Server commit terminal một lần, bỏ gameplay mutation sau terminal; exact step ordering cần review, không tự chọn số Tmatch/K.
- Proposer/reviewers: TV2 rules; TV1 lifecycle/clock; TV3 results; cả nhóm duyệt.
- Blocks: TV2-009 execution, CT-204/205, FR/AC/TC-207, TC-206/208 terminal callbacks.
- Evidence: D FR-207/GD-06; M MatchStateMachine.TriggerVictory/GameManager.ReportBotDefeated chỉ offline bot victory.
- Review outcome: chưa có; cần transition/order/tie table và expected result không dùng Victory cũ như chứng minh Finished mới.

### GD-07 — capacity, start và join

- Question: Nmin khác capacity=4 thế nào; ready/load barrier, join khi Playing và private code có cần không?
- Alternatives: Start khi có 2–4 ready; bắt buộc 4; host/manual start; late admission hoặc chỉ Waiting.
- Recommendation/rationale: Đề nghị start với ≥2 ready+loaded để test/delivery sớm, capacity 4; chỉ join khi Waiting cho vòng đầu, không private code bắt buộc. Đây là recommendation, Nmin vẫn UNDECIDED tới approval; demo đủ 4 không chứng minh minimum.
- Proposer/reviewers: TV1 lobby, TV2 gameplay eligibility, TV3 UI/load; nhóm duyệt admission policy.
- Blocks: TV2-009/M2 start acceptance, TV2-004 CT-205, FR/AC/TC-201/207.
- Evidence: D phân công §5 M2 và SRS conflict; N ArenaNetworkManager.OnServerAddPlayer không có gameplay ready/load; M readiness kiểm scene.
- Review outcome: chưa có; cần roster/ready/load/start transitions và late-join reject expected.

### GD-08 — reconnect và server loss

- Question: Grace G, offline avatar, cooldown/respawn clocks, expiry, old connection/life callbacks và server loss policy?
- Alternatives: Server clocks tiếp tục; pause timer; destroy/recreate avatar; retain session/avatar. Server restart recovery hoặc abort.
- Recommendation/rationale: Trong server session còn sống, server clock tiếp tục cho cooldown, giữ authoritative HP/life/score; reject old connection generation và stale life effects; respawn/offline vulnerability phải chọn rõ trước execution. Đề nghị server loss Aborted, không restart recovery theo phân công; không nhập forfeit/spectator policy cũ. Không dùng connId làm credential.
- Proposer/reviewers: TV1 auth/session/timers, TV2 gameplay policy, TV3 feedback; nhóm duyệt grace/expiry và fairness.
- Blocks: TV2-008/009 reconnect behavior, TV2-012/TC-208 execution, CT-204/205, FR/AC-208; combat integration cơ bản không chờ M4.
- Evidence: N ArenaNetworkManager.OnServerDisconnect destroy/release, NetworkPlayer.ConnectionId; N report loại trừ token recovery; D phân công §2 không yêu cầu server restart recovery.
- Review outcome: chưa có; cần timeline alive/dead/pending/cooldown + invalid token/expiry/old generation/server loss expected.

### GD-09 — baseline, toolchain và bằng chứng

- Question: Nhóm có thay baseline sang server riêng/Mirror/raycast/respawn không; exact Editor/vendor/transport/server OS/build/license, budgets và lịch M0–M6 nào được duyệt?
- Alternatives: Approve proposal sau bounded evidence spike; tiếp tục baseline cũ; chia scope theo milestone có decision record rõ. Không triển khai hai networking stacks song song.
- Recommendation/rationale: Review source inventory hiện hữu thay vì tạo project thứ hai; TV1 xác nhận matrix Unity 6000.6.7f1/Mirror metadata 96.0.1 và transport thực bằng clean build/multi-process evidence. Chỉ inventory hiện hữu đã kiểm, không tự chốt compatibility, license hoặc timing budgets. Thống nhất acceptance mới rồi approved integration riêng.
- Proposer/reviewers: TV1 TECH-001/toolchain và lịch nhóm; TV2 scope/tests; TV3 client/arena; cả nhóm và giảng viên nếu yêu cầu môn học cần.
- Blocks: Approved implementation/integration/M0, TV2-002–013 execution tương ứng, FR/AC/TC-201/209 và CT bridge. Agenda/test design không bị chặn.
- Evidence: M ProjectVersion/manifest/vendor metadata, PR #4 merged; N test report chỉ cùng-process, UDP/KCP/latency NOT RUN trong task này. [Inventory](ARCHITECTURE.md#tv2-001--inventory-theo-ref) và [evidence levels](TESTING.md#tv2-001--phân-loại-bằng-chứng).
- Review outcome: chưa có; cần explicit approval record (decision, approver, date, scope, source commit/config), OS/license route, budgets và deadline. Không ghi DONE chỉ do document/PR được tạo.
