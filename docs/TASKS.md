# Tasks — hiện hành 2026-11-08

## TV2-001 — scope và actual decisions

- Milestone M0; P0; owner TV2 / 23001901; collaborators TV1/TV3/nhóm.
- Status: DONE cho deliverable tài liệu/đối chiếu luật. Integration: PENDING PR review/maintainer merge. [Closure boundary](DECISIONS.md#tv2-001--kết-quả-chốt-luật-2026-11-08) tách closure tài liệu khỏi integration sau khi owner xác nhận nhóm đã chốt.
- Inputs: chỉ thị owner08/11 và luật v0.1 ngày06/11; main11849ad, nhánh feature/TV2-001-scope-reconciliation-main hiện hữu. Scope: actual decisions, SRS/contract/test expectations và readiness; không runtime/config/scenes/CI.
- AC/deliverables: GD-01–08 outcome/source/date; GD-09 accepted baseline tách OPEN technical matrix; FR/AC/TC-201–209, CT-201–205 mapped; luật versioned/history giữ nguyên; không invent reviewer/test; checks thực tại WORK_LOG.
- Verification: links/anchors/UTF-8/fences/traceability/DAG/history/scope/whitespace/Git ancestry ở WORK_LOG. Gameplay/Unity/build/network/CI NOT RUN. Commit/push/PR: PROJECT_STATE/Git history; không tự ghi hash commit chứa dòng này.

## Chuẩn bị task tiếp theo

| Task | Status / readiness | Exact next / gate còn lại |
| --- | --- | --- |
| TV2-002 | TODO; baseline đủ draft, chưa implementation | Sau merge001: fetch main chứa prerequisite, dedicated feature/TV2-002-character-state-config; inspect/reuse Domain/Health/TV1 lifecycle; schema Alive/Dead/Pending, connection orthogonal, match states; config rules-v0.1 + units/validity/clock/life invariants; test AC-202/205/206/207 |
| TV2-003 | TODO; cần schema draft002 | V5/τ0.25/analog/finite/state vectors; TV1 motor/camera basis TV3/jump confirmation; timeout exact-boundary comparator |
| TV2-004 | TODO; cần limits draft003 | CT examples/types/identity/order/replay/rates, camera/muzzle/layers, TV1+TV3 review; policy camera/aim đã chốt |
| TV2-005–009 | TODO; draft dependencies giữ ở DAG dưới | Numeric baseline/GD accepted; gun→raycast→death→respawn/match; approved interfaces/geometry/runtime vẫn cần |
| TV2-010–013 | TODO; usable-draft dependencies giữ dưới | Expected TESTING mới; execution đợi code/runner/build/physics/multi-process/auth phù hợp; designed tests không PASS |
| TECH-001 / GD-09 technical | TODO / OPEN, TV1 chủ trì | Exact compatible Editor/Mirror/transport/server OS/modules/license/build route/budgets/lịch; không suy approved từ luật |

Workflow: latest fetched origin/main có prerequisites→dedicated task branch→PR main. Không merge/direct push protected/source refs; task phụ thuộc chờ prerequisite merged, ngoại lệ stacking phải ghi dependency. DAG13 nodes/24 edges dưới tiếp tục là draft dependencies; các trạng thái/GD cũ được phần này supersede. Không implement tasks002–013 trong TV2-001.

Prerequisite TV1: feature/tv1-m2-lobby-room hiện8d71e62 có commit melee/10HP/1DMG, khác luật raycast100HP/25DMG đã chốt. Code đồng đội chưa vào main; không sửa/import trong TV2-001. TV2-002/004 đối chiếu TV1 trước reuse/integration. TV1 M1 tại5062c50 báo Editor6000.6.8f1, PlayMode214/216 (2 vendor skips), group212/212, EditMode34/34; vẫn harness in-engine (1 native client+1 virtual), chưa hai OS clients/KCP/reconnect. Report chưa TV2 rerun, không tự nâng main/toolchain.

---

> HỒ SƠ ĐẾN 2026-11-05 — giữ để truy vết. Các trạng thái PROPOSED/UNDECIDED/BLOCKED và next action bên dưới là snapshot lịch sử; phần cập nhật 2026-11-08 ở trên có ưu tiên. Workflow và DAG còn áp dụng theo giới hạn ở phần hiện hành.

> TV2 PROPOSAL / source archive, selectively adapted from 61aebf38ce40f9ad131405c202d7b82729ecbbcf; không thay tài liệu dùng chung của main. Workflow TV2 hiện hành (owner 2026-11-05 realign): baseline latest fetched origin/main có prerequisites; feature/<TASK-ID>-<description>, PR vào main; long23001901 chỉ giữ lịch sử. Các nhánh độc lập là siblings, task phụ thuộc thường chờ prerequisite merged; stacking ngoại lệ phải ghi dependency. Không merge/direct push main, không sửa nhánh nguồn. Quyết định này chỉ workflow owner, không phê duyệt gameplay nhóm.

# Nhiệm vụ và trạng thái thực

Cập nhật 2026-11-02. [Phân công](SE2026-T17-Huong-dan-va-phan-cong-nhom.md) ưu tiên cho trách nhiệm TV2; [SRS](../Long_document/srs.md) v0.4 giữ scope/GD, [ROADMAP](ROADMAP.md) giữ milestone. TV2 = 23001901; TV1/TV3 chưa điền danh tính. Personal integration chưa có Unity implementation; main/TV1 có source và teammate test reports, TV2 chưa rerun. TV2-001 BLOCKED chờ review, TV2-002–013 TODO, có thể bắt đầu đặc tả/test design ngay; chỉ execution cần project/approval/config được ghi ở từng task. TODO không có nghĩa gameplay đã hoàn tất.

## TV2-001 — trạng thái kiểm ngày 2026-11-05

Hiệu chỉnh current summary ngày 02-11: Unity/source/tests có trên main/TV1, source cũ chưa có trong personal integration (lịch sử; main có project), xem [inventory](ARCHITECTURE.md#tv2-001--inventory-theo-ref). DOC-002 content đã vào docs source commit 5338ba4, trái claim “Commit NONE” snapshot phía dưới; source cũ chưa tích hợp; delivery mới đề xuất vào main. TV2-002–013 vẫn TODO; không được implement trong phiên TV2-001. “Proposed paths chưa tồn tại” chỉ đúng personal tree, phải reuse/reconcile code có sẵn trước code task sau.

TV2-001 documentation complete, **Status BLOCKED** chờ TV1/TV3/nhóm review và ghi outcome GD-01–09. Ma trận có source refs/paths, 9 GD agendas, ownership/blockers, draft/execution DAG và current evidence correction. Source/prerequisite transfer commit d977f57 từ D=5338ba4 được tách khỏi task content; commit delivery/PR cập nhật trong handoff/PR, không tự tham chiếu hash. Không DONE vì human review/integration chưa có. [SRS matrix](../Long_document/srs.md#tv2-001--đối-chiếu-nguồn-ngày-2026-11-05), [agenda](DECISIONS.md#tv2-001--agenda-quyết-định-ngày-2026-11-05), [WORK_LOG](WORK_LOG.md) sở hữu nội dung/evidence tương ứng.

## TV2 — direct dependencies và gate song song

Các cạnh dưới đây phụ thuộc **usable draft**, không full task DONE/integration. Gate execution chung **E**: prerequisites/interfaces đã review và có trong approved main integration commit containing prerequisites, approved dedicated branch từ commit đó; GD liên quan/config versions, project/toolchain phù hợp và input TV1/TV3 đã được xác minh. Draft gate **D**: đầu vào nêu trong cột 2 có bản đủ dùng, ghi assumption/UNDECIDED rõ. Execution/design của từng task không được coi như được cấp quyền bởi prompt TV2-001. Bảng là nguồn cạnh DAG; giải thích trong ROADMAP cross-link bảng này.

| Task | Direct draft prerequisites | Draft-ready gate / output dùng được | Execution-ready ngoài E | TV1 / TV3 inputs | Shared files và conflict risk |
| --- | --- | --- | --- | --- | --- |
| TV2-001 | NONE | Nguồn phân công/SRS/ref đọc được → matrix/agenda | Review nhóm/GD outcomes và authorized integration để DONE; không cần Unity | TV1 current contract/toolchain; TV3 identity/camera/arena confirmation | SRS/DECISIONS/TASKS/memory; một owner, giữ history |
| TV2-002 | TV2-001 | Matrix/agenda draft → schema/life/config draft ký hiệu | GD-01/04/06/08, approved data interfaces và project reuse assessment | TV1 participant/session/clock; TV3 state presentation | Shared/Domain, Settings, ARCHITECTURE; không tạo schema/assembly trùng |
| TV2-003 | TV2-002 | Schema draft → limits/units/vectors | GD-01, motor/input contract được TV1 review; M1 pose evidence cho integration | TV1 actual validator/motor; TV3 axes/jump | Domain/tests/CT-201; không sửa motor đồng thời TV1 |
| TV2-004 | TV2-003 | Limits draft (schema transitive) → CT examples/gaps | TV1+TV3 duyệt wire/life/aim/clock/ordering; GD-03/07/08 | TV1 ownership/delivery/replay/session; TV3 aim/HUD/load | PROTOCOL, Shared/Protocol, Domain; contract change review cả ba |
| TV2-005 | TV2-002, TV2-004 | Schema+contract drafts → gun spec | GD-01/02/03, approved config/cooldown/aim interface | TV1 clock/adapter; TV3 aim/feedback | Settings/Combat/Domain/tests; schema freeze hoặc serial reconciliation |
| TV2-006 | TV2-004, TV2-005 | Contract+gun specs → validation/fixture draft | GD-03, server pose/layers/physics fixtures + gun config | TV1 ingress/pose; TV3 arena/layers | Combat/physics tests; scene changes TV3 phối hợp, không concurrent |
| TV2-007 | TV2-002, TV2-006 | States+validation spec → death/life effect draft | GD-01/05, accepted-hit/life identity và idempotency contract | TV1 sync/event delivery; TV3 death display | Domain/Health/Combat/events/tests; không mutate shared life schema tự do |
| TV2-008 | TV2-007, TV2-004 | Life spec+lifecycle bridge → respawn draft | GD-04 và GD-08 cho reconnect cases; approved spawn/life components | TV1 spawn/session clock; TV3 spawn points/obstruction | Domain/Combat/Settings/CT-205/scene prefab; life changes phối hợp TV2-009 |
| TV2-009 | TV2-007, TV2-004 | Death spec+lifecycle bridge → credit/match draft | GD-05/06/07; GD-08 cho reconnect; ready/load/score interfaces | TV1 lobby/clock/session; TV3 HUD/results | Domain/Combat/CT-205; cùng life/terminal contracts với TV2-008 |
| TV2-010 | TV2-002, TV2-004 | Schema+contract drafts → independent expected vectors | Rule interfaces/config approved, runnable production+runner; numerical expected theo GD liên quan | TV1 runner/fake clock; TV3 state meaning review | Tests/EditMode, TESTING; không mirror implementation hoặc giành shared code |
| TV2-011 | TV2-004, TV2-010 | Contract+test design → run sheet | M0/M1, combat components TV2-006–009 phù hợp cases, approved configs/builds/2 processes; không đợi M4 | TV1 server/client builds/logs; TV3 arena/HUD/two views | TESTING/harness/evidence; run config freeze, ≥2 clients không thay run đủ 4 |
| TV2-012 | TV2-006, TV2-007, TV2-009, TV2-011 | Specs+run design → negatives/impairment design | Combat suite sẵn sàng, GD-08/09, TV1 reconnect adapter/impairment cho cases tương ứng | TV1 injection/auth/network simulator; TV3 feedback/video | TESTING/harness/protocol; avoid real tokens, retain expected/actual riêng |
| TV2-013 | TV2-010, TV2-011, TV2-012 | Evidence designs → index/checklist | Actual runs/artifacts/config/defects+human sign-off; TV1 build/CI artifacts | TV1 license/OS/CI builds; TV3 demo/video | TESTING/README/WORK_LOG; không claim planned artifacts như actual results |

Topological order đã kiểm: 001,002,003,004,005,006,007,008,009,010,011,012,013. Số thứ tự ở đây là suffix TV2; không missing node/cycle. Gate E có thể yêu cầu components/runtime chưa thể hiện thành draft edge, không thêm cạnh ngược từ test design về production spec. Parallel groups/ownership tại [ROADMAP](ROADMAP.md#tv2-001--nhóm-công-việc-song-song).

## DOC-002 — Cập nhật kế hoạch cá nhân TV2

- Mốc M0; P0; owner 23001901, AI hỗ trợ tài liệu tuần tự; collaborators TV1/TV3 review chưa diễn ra.
- Status: DONE cho cập nhật tài liệu đã kiểm; không gameplay, không chứng nhận review nhóm hoặc integration. Phạm vi: 12 Markdown paths, không source/project/package/assignment.
- Input: update_prompt_1.md, phân công đầy đủ, live Git và docs trên exact docs branch; dependencies: owner cho phép sửa nhánh hiện tại.
- Deliverable: conflict/GD table, TV2 tasks, M0–M6, contracts/testing/learning và memory; acceptance: task đủ trường, không trộn scope, giữ IDs/history, checks link/anchor/whitespace/DAG.
- Verification: kiểm links/anchors/IDs/task fields/DAG/history/hash/UTF-8/fences/secrets và diff whitespace PASS, chi tiết WORK_LOG; open: team scope/technology approval chưa có. Next: owner/TV1/TV3 review tài liệu và TV2-001.
- Commit: NONE cho task này. Push/PR/integration: NONE thực hiện; thay đổi unintegrated.

## Quy ước nhiệm vụ TV2

Mỗi task gồm hai bước: draft/review không cần Unity và implementation/execution khi đủ gate. DONE cho task gameplay đòi deliverable đã tích hợp luồng liên quan, checks thực, human review/integration được phép và evidence; viết kế hoạch không đủ. Các proposed paths phải đối chiếu source trên main/TV1 trước triển khai; cần reuse/reconcile với project main hiện hữu. Mọi task cần approved feature branch trước code; M0/TECH-001, approved configs/contracts và phần TV1/TV3 tương ứng trước integration execution. Delivery TV2-001 ghi ở PROJECT_STATE/PR; TV2-002–013 chưa có commit/PR, gameplay tests TV2 NOT RUN.

Đầu ra bản nháp của dependency có thể dùng để thiết kế độc lập; không phải tuyên bố dependency DONE. Vòng đời task thực vẫn TODO/IN_PROGRESS/BLOCKED/DONE. TV2-009 đóng góp sớm M2 qua contract start; phần score hoàn chỉnh ở M3.

## TV2-001 — Đối chiếu scope và quyết định mở

- Milestone: M0. Priority: P0. Status: BLOCKED (2026-11-05: tài liệu hoàn tất, chờ human decisions/review/integration).
- Primary owner: TV2 / 23001901. Collaborators: TV1, TV3, cả nhóm.
- Objective/scope/exclusions: Đối chiếu yêu cầu, lập GD-01–09 và người quyết định; không tự phê duyệt toàn nhóm.
- Requirements: Nguồn phân công/SRS v0.4. Dependencies để bắt đầu: Không. Gate hoàn tất: review/ghi nhận quyết định thực từ TV1/TV3/nhóm; không cần Unity hoặc M0 để làm agenda và đối chiếu.
- Inputs: Phân công đầy đủ, hai baseline SRS.
- Outputs/deliverables: Bảng xung đột và decision agenda có owner/blocker. Proposed files: docs/DECISIONS.md; Long_document/srs.md.
- Acceptance criteria: Mọi khác biệt có resolution cá nhân, không cộng hai scope; nhóm ghi outcome khi review.
- Verification/evidence: Matrix đối chiếu D/M/N/P, agenda đủ GD-01–09, dependency DAG/history/link/UTF-8/Git checks tại WORK_LOG; gameplay NOT RUN, human review chưa có.
- Open decisions và next action: Gửi PR review TV1/TV3/nhóm, ghi actual decision/approver/date. TV2-002 có thể bắt đầu draft ký hiệu sau authorization riêng; chưa implementation.
- Source history: d977f57 / 4ecb97a / 61aebf3 giữ nguyên trên old head. Delivery hiện hành: selective adapted port từ 61aebf3 lên feature/TV2-001-scope-reconciliation-main, base origin/main 11849adbb7e4627e9d623958c950a780c4e58c5d, target PR main. Không retry old-target PR; Port content 7cda5959a93ebced0bd33776d9b7d6e57a5f50a4 đã push/remote verified. NEW main PR creation bị hủy ở connector; search chưa thấy PR, review chưa requested. Delivery hiện hành xem PROJECT_STATE; integration chưa thực hiện, merge cần quyền riêng.

## TV2-002 — Dữ liệu nhân vật, state và config

- Milestone: M0–M1. Priority: P0. Status: TODO.
- Primary owner: TV2 / 23001901. Collaborators: TV1 state/simulation, TV3 presentation.
- Objective/scope/exclusions: Định nghĩa shared data, life/match states và config validation; không network/UI.
- Requirements: FR-202/205/206/207. Dependencies để bắt đầu: TV2-001 bản đối chiếu. Gate execution: M0/approved branch + config/contract và phần tích hợp liên quan; không chặn draft.
- Inputs: SRS/GD draft, ownership map.
- Outputs/deliverables: Schema/transition table + config units/validity; components sau approval. Proposed files: Assets/_Project/Scripts/Shared/Domain/; Assets/_Project/Settings/.
- Acceptance criteria: Mỗi state có entry/exit/eligibility; config invalid phát hiện; disconnect trực giao life.
- Verification/evidence: Review schema và trace Alive→Dead→Pending→Alive; sau M0 rule tests TC-202/205/206. Planned / NOT RUN.
- Open decisions và next action: GD-01/04/06/08; soạn schema bằng ký hiệu.
- Commits: NONE. PR: NONE. Integration: chưa thực hiện.

## TV2-003 — M1: movement rules và giới hạn input

- Milestone: M1. Priority: P0. Status: TODO.
- Primary owner: TV2 / 23001901. Collaborators: TV1 motor/ingress, TV3 input.
- Objective/scope/exclusions: Speed/eligibility/axes finite và bounds; không nhận input hoặc simulate motor thay TV1.
- Requirements: FR-202. Dependencies để bắt đầu: TV2-002 schema draft. Gate execution: M0/approved branch + config/contract và phần tích hợp liên quan; không chặn draft.
- Inputs: Data states, TV1 input/motor draft, TV3 axes.
- Outputs/deliverables: Rule table/input vectors và pure rules sau M0. Proposed files: Assets/_Project/Scripts/Shared/Domain/; Assets/_Project/Tests/EditMode/.
- Acceptance criteria: Zero/diagonal/invalid/nonfinite/state cấm có expected; client speed không đổi speed server.
- Verification/evidence: Review TC-202 vectors ngay; sau M1 đo pose/server time với TV1 + 2 clients, logs. Planned / NOT RUN.
- Open decisions và next action: GD-01 input/units/speed; gửi bounds draft cho TV1.
- Commits: NONE. PR: NONE. Integration: chưa thực hiện.

## TV2-004 — Hợp đồng input, shot, result và presentation

- Milestone: M1–M2. Priority: P0. Status: TODO.
- Primary owner: TV2 / 23001901. Collaborators: TV1 network/ownership, TV3 aim/HUD.
- Objective/scope/exclusions: Chốt CT-201–205 với TV1/TV3; không viết Mirror adapter.
- Requirements: FR-201/202/204/208. Dependencies để bắt đầu: TV2-003 limits draft. Gate execution: M0/approved branch + config/contract và phần tích hợp liên quan; không chặn draft.
- Inputs: Data/input draft, aim và session questions.
- Outputs/deliverables: Contract fields/types/units/reasons, valid/invalid examples, adapter checklist. Proposed files: docs/PROTOCOL.md; Assets/_Project/Scripts/Shared/Domain/.
- Acceptance criteria: Actor suy connection; không client damage/HP/score; retry/dedup chỉ theo delivery thực; cả ba review.
- Verification/evidence: Walkthrough input→server→HUD và reconnect; lưu review notes; mapping API chỉ sau TECH-001. Planned / NOT RUN.
- Open decisions và next action: GD-03/07/08/09; đưa 5 contract examples ra review.
- Commits: NONE. PR: NONE. Integration: chưa thực hiện.

## TV2-005 — Cấu hình một súng và cooldown/range/damage

- Milestone: M3. Priority: P0. Status: TODO.
- Primary owner: TV2 / 23001901. Collaborators: TV1 clock/state, TV3 aim/feedback.
- Objective/scope/exclusions: Một raycast gun, server time, config validity; không nhiều súng/pickup; ammo chỉ nếu chọn.
- Requirements: FR-203. Dependencies để bắt đầu: TV2-002 config draft; TV2-004 contract draft. Gate execution: M0/approved branch + config/contract và phần tích hợp liên quan; không chặn draft.
- Inputs: Approved hoặc symbolic H,D,C,R, ammo decision.
- Outputs/deliverables: Gun rule spec + boundary vectors; config/component sau M0. Proposed files: Assets/_Project/Settings/; Assets/_Project/Scripts/Shared/Domain/.
- Acceptance criteria: Cooldown before/at/after rõ, range/damage hợp lệ, không client clock; không nhập offline defaults.
- Verification/evidence: TC-203 fake clock/config và bảng expected; giữ results sau execution. Planned / NOT RUN.
- Open decisions và next action: GD-01/02; soạn schema không tự điền tuning.
- Commits: NONE. PR: NONE. Integration: chưa thực hiện.

## TV2-006 — Server raycast validation

- Milestone: M3. Priority: P0. Status: TODO.
- Primary owner: TV2 / 23001901. Collaborators: TV1 ingress/pose/sync, TV3 arena/camera.
- Objective/scope/exclusions: Validate life/match/cooldown/origin/direction/obstruction/target/range; không lag compensation.
- Requirements: FR-204. Dependencies để bắt đầu: TV2-004 contract draft; TV2-005 gun spec. Gate execution: M0/approved branch + config/contract và phần tích hợp liên quan; không chặn draft.
- Inputs: Server pose TV1, aim contract, arena layers TV3, gun config.
- Outputs/deliverables: Validation decision table, physics fixture design; server component sau M0. Proposed files: Assets/_Project/Scripts/Server/Combat/.
- Acceptance criteria: Wall/range/fake origin/hit reject; valid shot một effect/target kể cả nhiều colliders.
- Verification/evidence: TC-204 pure gates + physics fixture + server/2 clients; diagram/layer config/logs. Planned / NOT RUN.
- Open decisions và next action: GD-03; review origin/aim và self/trigger/layer policy.
- Commits: NONE. PR: NONE. Integration: chưa thực hiện.

## TV2-007 — HP và death đúng một lần

- Milestone: M3. Priority: P0. Status: TODO.
- Primary owner: TV2 / 23001901. Collaborators: TV1 state sync, TV3 death display.
- Objective/scope/exclusions: HP clamp và Alive→Dead/life identity; không nhận sync/UI.
- Requirements: FR-205. Dependencies để bắt đầu: TV2-002 states; TV2-006 validation spec. Gate execution: M0/approved branch + config/contract và phần tích hợp liên quan; không chặn draft.
- Inputs: Accepted server hit, damage/config, credit questions.
- Outputs/deliverables: HP/death transition/effect contract và tests; components sau M0. Proposed files: Assets/_Project/Scripts/Server/Combat/; Assets/_Project/Scripts/Shared/Domain/.
- Acceptance criteria: Overkill không âm; một death/life; duplicate/old callback không damage/credit life mới.
- Verification/evidence: TC-205 boundary và two-lethal-hit traces; logs before/after life/HP/events. Planned / NOT RUN.
- Open decisions và next action: GD-01/05; soạn transition/idempotency table.
- Commits: NONE. PR: NONE. Integration: chưa thực hiện.

## TV2-008 — Respawn và safe spawn phối hợp

- Milestone: M3–M4. Priority: P1. Status: TODO.
- Primary owner: TV2 / 23001901. Collaborators: TV1 spawn/session, TV3 arena/spawn points.
- Objective/scope/exclusions: Respawn timer/eligibility/protection nếu chọn; TV1 spawn, TV3 điểm/arena.
- Requirements: FR-206/208. Dependencies để bắt đầu: TV2-007 life spec; TV2-004 bridge draft. Gate execution: M0/approved branch + config/contract và phần tích hợp liên quan; không chặn draft.
- Inputs: Life/timer state, spawn candidates, GD-04/08.
- Outputs/deliverables: Respawn policy + spawn selection constraints/fallback contract; component sau M0. Proposed files: Assets/_Project/Scripts/Server/Combat/; Assets/_Project/Scripts/Shared/Domain/.
- Acceptance criteria: Một respawn đúng thời hạn/life; safe spawn theo policy; stale callback không revive sai.
- Verification/evidence: TC-206 với fake clock và unsafe/blocked fixture; TC-208 sau TV1 reconnect, timer/spawn logs. Planned / NOT RUN.
- Open decisions và next action: GD-04/08; mô tả tiêu chí và hỏi TV3 điểm spawn.
- Commits: NONE. PR: NONE. Integration: chưa thực hiện.

## TV2-009 — Kill score và gameplay match lifecycle

- Milestone: M2–M3. Priority: P1. Status: TODO.
- Primary owner: TV2 / 23001901. Collaborators: TV1 lobby/session, TV3 HUD/results.
- Objective/scope/exclusions: Credit/start/end/tie policy; không lobby/session implementation.
- Requirements: FR-207/208. Dependencies để bắt đầu: TV2-007 death spec; TV2-004 lifecycle draft. Gate execution: M0/approved branch + config/contract và phần tích hợp liên quan; không chặn draft.
- Inputs: Death events, ready/load từ TV1, GD-05–08.
- Outputs/deliverables: Scoring/match transition tables và component sau M0. Proposed files: Assets/_Project/Scripts/Shared/Domain/; Assets/_Project/Scripts/Server/Combat/.
- Acceptance criteria: Một credit/death theo approved policy; terminal một lần; không suy minStart=4 hoặc last survivor.
- Verification/evidence: TC-207 fake clock/events và integrated results; score/match trace; TC-208 sau M4. Planned / NOT RUN.
- Open decisions và next action: GD-05/06/07/08; soạn cases suicide/environment/tie cho nhóm.
- Commits: NONE. PR: NONE. Integration: chưa thực hiện.

## TV2-010 — Thiết kế và chạy independent rule tests

- Milestone: M1–M3. Priority: P1. Status: TODO.
- Primary owner: TV2 / 23001901. Collaborators: TV1 runner/clock interface, TV3 review state meanings.
- Objective/scope/exclusions: Test design bắt đầu ngay; execution pure rules sau code; không coi math tests là raycast physics pass.
- Requirements: FR-202–207/209. Dependencies để bắt đầu: TV2-002 schema; TV2-004 contract draft. Gate execution: M0/approved branch + config/contract và phần tích hợp liên quan; không chặn draft.
- Inputs: Rule specs symbolic; config/clock fake.
- Outputs/deliverables: Input/expected tables trước; rule suite/results sau M0. Proposed files: Assets/_Project/Tests/EditMode/; docs/TESTING.md.
- Acceptance criteria: Coverage biên cooldown/HP/input/death/respawn/score, invalid config; không tests mirror implementation.
- Verification/evidence: Review TC-202–207 vectors; chạy actual runner sau khi có, lưu command/version/result/defects. Planned / NOT RUN.
- Open decisions và next action: GD tương ứng chặn expected số cụ thể; bắt đầu TC-202/203/205 symbolic vectors.
- Commits: NONE. PR: NONE. Integration: chưa thực hiện.

## TV2-011 — Integration combat: server và ít nhất 2 clients

- Milestone: M3–M5. Priority: P1. Status: TODO.
- Primary owner: TV2 / 23001901. Collaborators: TV1 server/builds, TV3 client/arena/HUD.
- Objective/scope/exclusions: Thiết kế run ngay; execution cần M0/M1, TV2-006–009 components/config; không đợi M4 cho combat cơ bản.
- Requirements: FR-201/204–207/209. Dependencies để bắt đầu: TV2-004 contract draft; TV2-010 test design. Gate execution: M0/approved branch + config/contract và phần tích hợp liên quan; không chặn draft.
- Inputs: Builds TV1, arena/HUD TV3, configs approved.
- Outputs/deliverables: Reproduction script, topology/expected table rồi logs/results. Proposed files: docs/TESTING.md; Assets/_Project/Tests/ (harness loại nào cần chốt).
- Acceptance criteria: A bắn B: cả hai views khớp server HP/death/respawn/score; run đủ 4 riêng cho M5.
- Verification/evidence: TC-201/204–207/209, record build/config/two-view video/authoritative log/defects. Planned / NOT RUN.
- Open decisions và next action: GD-01/03–07; viết run sheet trước project.
- Commits: NONE. PR: NONE. Integration: chưa thực hiện.

## TV2-012 — Combat cheats, reconnect và mạng xấu

- Milestone: M4–M5. Priority: P1. Status: TODO.
- Primary owner: TV2 / 23001901. Collaborators: TV1 session/adapter/tooling, TV3 UI/two-client capture.
- Objective/scope/exclusions: Thiết kế cheat matrix ngay; execution M4 cần TV1 reconnect, simulator; không commercial anti-cheat.
- Requirements: FR-204/205/208/209. Dependencies để bắt đầu: TV2-006, TV2-007, TV2-009 specs; TV2-011 run design. Gate execution: M0/approved branch + config/contract và phần tích hợp liên quan; không chặn draft.
- Inputs: Contracts, policy GD-08, build/transport/impairment config.
- Outputs/deliverables: Negative payloads/traces, impairment matrix, expected/actual và defects. Proposed files: docs/TESTING.md; Assets/_Project/Tests/ (harness dự kiến).
- Acceptance criteria: Không bypass cooldown/range/wall/state; không duplicate score/death/respawn, valid request sau invalid vẫn chạy.
- Verification/evidence: TC-208/209 + TC-203–205/207 negatives, actual RTT/loss/logs trước-sau và retest. Planned / NOT RUN.
- Open decisions và next action: GD-08/09; chuẩn bị injection spec với TV1.
- Commits: NONE. PR: NONE. Integration: chưa thực hiện.

## TV2-013 — Evidence combat và hỗ trợ M6

- Milestone: M5–M6. Priority: P2. Status: TODO.
- Primary owner: TV2 / 23001901. Collaborators: TV1 build/CI, TV3 video/demo, cả nhóm reviewer.
- Objective/scope/exclusions: Chuẩn bị evidence index; execution bundle sau các run; không nhận toàn CI pipeline.
- Requirements: FR-209. Dependencies để bắt đầu: TV2-010, TV2-011, TV2-012 evidence design. Gate execution: M0/approved branch + config/contract và phần tích hợp liên quan; không chặn draft.
- Inputs: Actual results/artifacts/config, CI do TV1, defects/review.
- Outputs/deliverables: Combat run index/config/tests/known issues + smoke notes. Proposed files: docs/TESTING.md; README.md; docs/WORK_LOG.md; artifact store được nhóm duyệt.
- Acceptance criteria: Mỗi claim trỏ đúng commit/build/config/log; unrun ghi NOT RUN; review người thật, không tự nhận merged.
- Verification/evidence: TC-209 đối chiếu evidence; chạy combat smoke trên artifacts TV1 cấp và ghi actual; không invented command. Planned / NOT RUN.
- Open decisions và next action: GD-09 runner/license/OS/budget; soạn checklist evidence.
- Commits: NONE. PR: NONE. Integration: chưa thực hiện.

## Trạng thái task cũ sau đối chiếu

SETUP-001/WF-002/WF-003/BASE-001/DOC-001 vẫn là evidence lịch sử đã làm, không bị hạ trạng thái. PLAN-001 vẫn BLOCKED cho phê duyệt baseline/technology toàn nhóm; TECH-001 TODO, TV1 chủ trì exact Unity/Mirror/transport/server OS/modules/license matrix theo hướng mới, không NGO recommendation mặc định. GAME-001 BLOCKED cho setup thực chưa approved; TV1 phụ trách foundation, TV2 hỗ trợ rules, không nhận ownership Unity project. Acceptance FR-101/F-001 cũ dưới đây là lịch sử; gate setup mới theo FR-201/M0 cần nhóm chốt. Không execute task cũ theo scope melee/host+3.

---

## LỊCH SỬ — Baseline ngày 2026-11-01, không dùng làm cổng nghiệm thu kế hoạch TV2

> Toàn bộ nội dung bên dưới được giữ để truy vết. Các nhãn CURRENT, CONFIRMED, mandatory, TODO và next action bên trong chỉ có nghĩa tại thời điểm baseline cũ. Không cộng các nghĩa vụ cũ vào kế hoạch TV2 mới.

# Tasks

Cập nhật 2026-11-01. Status chỉ TODO, IN_PROGRESS, BLOCKED, DONE; quyết định sản phẩm khác implementation completion. Mọi gameplay feature F-001–019 trong [ROADMAP](ROADMAP.md) là TODO; không có gameplay commit/PR/test result. Các task setup DONE dưới đây có evidence tại commit thực `1d7a6c91fb7c2a3ef2fa233c1d5efe5cc60b462e`; các log cũ “uncommitted” được giữ dưới dạng lịch sử trong WORK_LOG.

## SETUP-001 — Persistent project workflow

- Description: Tạo AGENTS và bốn memory docs, RESUME/WORK/HANDOFF cho owner 23001901.
- Priority: HIGH. Status: DONE.
- Acceptance criteria: Instructions đầy đủ, branch riêng approved, memory/safety/link checks, preserve preexisting work; không application implementation.
- Dependencies: Owner setup authorization đã nhận.
- Relevant files: ../AGENTS.md; PROJECT_STATE.md; TASKS.md; DECISIONS.md; WORK_LOG.md.
- Verification: Historical setup checks ghi WORK_LOG; live Git kiểm 1d7a6c9 đã chứa docs và origin/setup cùng SHA.
- Commits: 1d7a6c91fb7c2a3ef2fa233c1d5efe5cc60b462e. PR: NONE created; remote PR existence UNVERIFIED.

## WF-002 — Personal integration/feature workflow

- Description: Naming/approval/integration memory rules.
- Priority: HIGH. Status: DONE.
- Acceptance criteria: Integration authority sau establishment, branch riêng, Conventional Commits, approvals, assignment preservation, branch relationship checks.
- Dependencies: Owner workflow instructions.
- Relevant files: ../AGENTS.md; PROJECT_STATE.md; DECISIONS.md; WORK_LOG.md.
- Verification: Historical content/Git checks; setup commit verified live. Không dùng setup làm permanent game branch.
- Commits: 1d7a6c9. PR: NONE created.

## WF-003 — Clean commits và assignment isolation

- Description: Explicit staging/diff review, engine ignore evidence, reviewed reuse và memory synchronization.
- Priority: HIGH. Status: DONE.
- Acceptance criteria: Preserve assignment/worktrees, no blind staging/import, spec/test/state cùng feature integration, clean commit safeguards.
- Dependencies: Owner workflow instructions; WF-002.
- Relevant files: ../AGENTS.md; DECISIONS.md; WORK_LOG.md.
- Verification: Historical checks; current task không tạo engine ignores hoặc import prototype. Live main/assignment tips vẫn b5054a6/f4835ce.
- Commits: 1d7a6c9. PR: NONE created.

## BASE-001 — Establish personal integration baseline

- Description: Owner chọn setup commit làm nền cho Unity/C# mới và incorporation memory bằng ancestry.
- Priority: HIGH. Status: DONE.
- Acceptance criteria: Explicit exact-base/creation/push approval; verify commit/tree/status/ancestry; create long23001901 and push upstream; preserve existing work/history; no prototype import.
- Dependencies: Owner approval trong phiên 2026-11-01 đã nhận.
- Relevant files: Git refs; DECISIONS.md; PROJECT_STATE.md; WORK_LOG.md.
- Verification: Integration created từ 1d7a6c9; `git push -u origin long23001901` success, upstream established. Docs branch created from it, initial merge-base same and ahead/behind 0/0. Prompt hash preserved; assignment worktree untouched.
- Commits: Baseline 1d7a6c91fb7c2a3ef2fa233c1d5efe5cc60b462e (không new integration commit). PR: NONE created.

## PLAN-001 — Reconcile product/technical decisions

- Description: Thay interpretation offline cũ bằng online confirmed, giữ unresolved toolchain/tuning approvals minh bạch.
- Priority: HIGH. Status: BLOCKED.
- Acceptance criteria: Preserve requirement IDs/history, map D-01–12 supersession, distinguish approved product/Git vs proposed exact tools, bounded next task; missing approvals explicit, không giả architecture approval.
- Dependencies: Owner product prompt, BASE-001; DOC-001 verification và TECH-001 cho exact stack review.
- Relevant files: ../Long_document/srs.md; ../Long_document/learning-roadmap.md; ARCHITECTURE.md; DECISIONS.md; ROADMAP.md.
- Verification: Product reconciliation, D-01–12 supersession và unknowns kiểm ở DOC-001; còn BLOCKED trên owner/team exact framework/version/service/license/tuning decision trong TECH-001, không gọi PLAN hoàn tất lựa chọn.
- Commits: Product documentation `178ee30ecaaecccb44743e18d8189ef08ca8a06f`, đã push. PR: NONE created. Integration: pending.

## DOC-001 — Online FFA documentation baseline

- Description: Vietnamese current requirements/rules, product roadmap/catalogue, architecture comparison, protocol/testing/evidence, navigation và corrected memory.
- Priority: HIGH. Status: DONE.
- Acceptance criteria: Current FFA/Unity/C#/host+3; mandatory weapon/reconnect/evidence trong first MVP; 2v2 future; stable old IDs/full SRS history; feature/requirement/AC/TC mapping, acyclic dependencies; official dated assessment; Markdown links; docs-only diff; preserve prompt/assignment; verified commit/push trên authorized docs branch, awaiting integration.
- Dependencies: BASE-001, full task prompt, owner docs branch/commit/push approval đã nhận; consequential implementation choices không chặn độc lập documentation.
- Relevant files: ../README.md; ../AGENTS.md; ../Long_document/srs.md; ../Long_document/learning-roadmap.md; ROADMAP.md; ARCHITECTURE.md; PROTOCOL.md; TESTING.md; PROJECT_STATE.md; TASKS.md; DECISIONS.md; WORK_LOG.md.
- Verification: Document checks PASS: 52 local links/11 anchors, 14 FR/6 NFR/5 UC/20 AC/20 TC unique và traceability; 19-feature DAG, historical SRS/prompt preservation, append-only WORK_LOG, UTF-8/fences/common secret patterns, manual mandatory-scope review; working/staged git diff --check và exact 12-path staged-content review PASS. Tất cả gameplay TC-101–120 NOT RUN; không CI YAML/code/config/test executable được tạo. DONE chỉ cho tài liệu đã kiểm/commit/push, không gameplay/integration.
- Commits/push: `178ee30ecaaecccb44743e18d8189ef08ca8a06f` — `docs(roadmap): DOC-001 define online FFA documentation baseline`; push origin/docs/23001901/online-ffa-roadmap success/upstream tracking. Handoff delivery record ghi sau content push. PR: NONE created. Integration: AWAITING INTEGRATION into long23001901.

## TECH-001 — Approve bounded technology matrix

- Description: Review NGO/Mirror direction và exact Editor/framework/transport/services/module/license/budget choices, không cài/implement trong task docs.
- Priority: HIGH. Status: TODO.
- Acceptance criteria: Một framework duyệt, exact compatible matrix có official evidence, service identity/reconnect feasibility và cost/quota guardrails, CI license route reviewed hoặc explicit blockers; xác định hai→bốn instance và different-network spike criteria.
- Dependencies: DOC-001 reviewed docs; owner/team decision; trước implementation cần authorized documentation integration và feature branch từ latest approved long23001901.
- Relevant files: ARCHITECTURE.md; PROTOCOL.md; ROADMAP.md (selective detailed specification); DECISIONS.md; ../Long_document/srs.md O-01–09.
- Verification: NOT RUN; recommendation PROPOSED, no approval fabricated.
- Commits: NONE. PR: NONE created.

## GAME-001 — Reproducible Unity foundation (F-001)

- Description: Project skeleton sau TECH-001; bounded foundation, không big offline game.
- Priority: HIGH. Status: BLOCKED.
- Acceptance criteria: FR-101/AC-101/TC-101: approved pins/source/meta/config, clean Windows client và headless build/smoke artifacts, reproducible README commands; no prototype import.
- Dependencies: TECH-001, authorized docs integration và approved feature branch from latest long23001901; sau đó F-003 networking spike sớm, hai rồi bốn instances và khác mạng.
- Relevant files: Project paths/build entry points UNKNOWN cho tới implementation; README.md; ROADMAP.md; TESTING.md.
- Verification: NOT RUN; chưa có Unity project/toolchain pin.
- Commits: NONE. PR: NONE created.
