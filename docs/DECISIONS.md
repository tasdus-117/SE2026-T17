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
