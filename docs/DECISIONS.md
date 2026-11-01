# Decisions

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
