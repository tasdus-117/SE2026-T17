# Project operating instructions

## Project and evidence
- Owner identifier: `23001901`; repository: SE2026-T17, Small Arena / Unity 3D Arena Combat Game.
- Unity/C# và Windows đã xác nhận. Kế hoạch cá nhân TV2 của owner 23001901 ưu tiên `docs/SE2026-T17-Huong-dan-va-phan-cong-nhom.md`: Mirror, server riêng, tối đa bốn client, một súng raycast, HP/chết/hồi sinh/điểm hạ đối thủ đều theo hướng đề xuất. Quyền cập nhật kế hoạch không đồng nghĩa cả nhóm/giảng viên đã duyệt. `Long_document/srs.md` v0.4 sở hữu yêu cầu đề xuất, xung đột và quyết định mở; giữ v0.3 online/v0.2 offline làm lịch sử. Checkout chưa có Unity implementation.
- Unity and C# are CONFIRMED; exact Editor/packages, URP, Input System, Cinemachine, networking framework and services remain PROPOSED or UNDECIDED until approved and verified. Do not invent versions, commands or results.
- Phân công vẫn đề xuất reconnect và bằng chứng protocol/mạng/tải/gian lận/CI client-server. Không gộp luật lịch sử melee, ring-out/elimination/spectator, pickup/E-drop/2x, host+3, camera cố định hoặc đúng bốn người mới start vào kế hoạch raycast/respawn mới. Nhảy là bổ sung; private code và số tối thiểu start chưa được phân công thiết lập. Giữ ID/lịch sử, dùng ID mới khi đổi nghĩa, chốt quyết định nhóm trước triển khai. TV2 sở hữu gameplay rules/combat/tests, TV1 mạng/server simulation/session/build-CI, TV3 input/camera/UI/arena/prefabs. AD-065 độc lập, không có quyền thực thi hoặc import từ task này.
- Source map: README.md navigates; Long_document/srs.md owns requirements/rules; docs/ROADMAP.md owns milestones/catalogue; docs/ARCHITECTURE.md owns design/technology assessment; docs/PROTOCOL.md owns proposed network contract; docs/TESTING.md owns planned online verification/evidence; Long_document/learning-roadmap.md owns learning only. Memory documents record actual progress, not product obligations.
- Git and inspected source are authoritative. Investigate discrepancies with memory, report them, and correct stale records. Mark missing evidence UNKNOWN or UNVERIFIED.

## Session startup and context
1. Read applicable AGENTS.md files and `docs/PROJECT_STATE.md`.
2. Run `git status` and `git branch --show-current`; check for merge/rebase operations, uncommitted work and protected branches before any edits.
3. Read relevant recent commits and the selected entry in `docs/TASKS.md`.
4. Inspect only relevant requirements and source. Use exact paths and narrow searches; expand only when needed. Do not reread the entire repository or unchanged files to reconstruct context.
5. Reconcile the documented branch and status with live Git evidence before proceeding.

## Git safety and isolation
- Never modify project files or implement code on `main`, `master`, or `develop`. Treat all three as protected. Read-only inspection is allowed.
- Personal integration branch: `long23001901`, approved and established from `1d7a6c91fb7c2a3ef2fa233c1d5efe5cc60b462e` on 2026-11-01 for the new Unity/C# project. It is authoritative for integrated development/memory; task-branch documentation remains unintegrated until authorized integration. Never use the setup branch as the permanent game development branch.
- Separate substantial features/independent tasks into `feature/23001901/<feature-name>`, fixes into `fix/23001901/<bug-name>`, documentation into `docs/23001901/<document-name>`, and maintenance into `chore/23001901/<task-name>`.
- Protect every shared integration branch and other members' branches as well as main/master/develop. Preserve all assignment branches and academic history.
- Inspect existing branches first; obtain explicit approval before creating or switching branches. Existing session authorization is sufficient for its stated scope. Verify the active branch after switching and before edits.
- Establish ownership and safety of uncommitted changes before switching. Never automatically stash, discard, overwrite, relocate or stage unrelated work. Preserve preexisting untracked files and worktrees.
- `.ad065-worktree/` and `assignment/AD-065-23001901` are outside setup scope; do not modify them.
- Explicit authorization is required for commit, push, merge, cherry-pick, rebase, any reset, clean, force push, branch deletion, or destructive history rewriting. Do not rewrite authorship or fabricate contribution evidence.
- Before proposing a commit, show intended changes. Commit/push only an authorized personal branch; confirm the team's PR target rather than assuming main. Merge requires separate explicit authorization.

## Integration and feature workflow
- At every RESUME, WORK and HANDOFF, verify active branch, status, operations in progress, and relationship to `long23001901` (merge-base and ahead/behind when it exists). If absent, explicitly report that no approved integration baseline exists.
- Before establishing integration, inspect available branches' tracked files, history and ancestry. Identify evidence for the approved foundation; do not assume an assignment branch contains a complete game. Propose exact starting commit and memory incorporation plan; obtain approval before branch creation or integration.
- For each feature: RESUME; identify task ID/acceptance criteria; determine latest approved integration commit; request approval for a dedicated branch from it; verify branch; implement selected scope; run checks; retain task evidence; propose meaningful atomic commits.
- Use Conventional Commits: `type(scope): description`, including task IDs when available, e.g. `feat(combat): GAME-024 implement punch damage`.
- After verification, propose a PR or reviewed local merge into `long23001901`. Commit, push and integration each require authorization. Never automatically integrate unfinished work or merge into shared branches.
- Feature branches inherit memory from their base and record task-specific progress as unintegrated. After successful integration, reconcile PROJECT_STATE.md, TASKS.md and WORK_LOG.md with verified commits/PRs on integration. Parallel feature memory is not automatically synchronized. Refresh from latest approved integration before the next feature; obtain approval for any operation needed to do so.

## Task and implementation rules
### Clean commits and assignment reuse
- Before every proposed commit: run git status; review working-tree and staged diffs; inspect staged paths; identify unrelated changes. Since new untracked files do not appear in ordinary git diff, inspect their content explicitly too.
- Stage explicitly selected paths only, never blindly git add .; check staged content for secrets, build outputs, temporary files and unnecessary assignment artifacts. Preserve unrelated uncommitted work; do not remove tracked files just to shrink a commit. Propose meaningful atomic Conventional Commits with task IDs when available; never inflate commit counts.
- Configure .gitignore only after verifying the actual selected technology. Preserve required engine project/scene files, assets, import metadata, source dependencies and configuration; do not invent Unity/Godot ignore rules before baseline approval.
- Academic branches use assignment/<assignment-id>-23001901 and remain independent. Reports, diagrams, AI evidence, submissions, experiments and prototype code never enter integration automatically.
- For reusable assignment functionality, identify code and dependencies first; request an approved dedicated feature branch from latest approved long23001901. Port only required changes or request selective cherry-pick of reviewed isolated commits. Never transfer a mixed-purpose commit without reviewing every affected path.
- Preserve necessary source, tests, dependencies, engine metadata, assets and configuration. Verify the transferred code builds and meets acceptance criteria; record originating assignment commit(s), transfer method and excluded artifacts for traceability. Integrate only the reviewed feature branch with authorization.
- Feature completion includes required tests, dependencies, configuration and assets; review the complete diff and stage relevant files explicitly before any authorized commit. Propose a PR targeting long23001901. No automatic push, merge or branch deletion.

- Track bounded tasks in `docs/TASKS.md`: ID, title, description, priority, status, acceptance criteria, dependencies, relevant files, verification, commits and PRs.
- Allowed statuses: TODO, IN_PROGRESS, BLOCKED, DONE. DONE requires verified acceptance criteria; absent verification is explicitly recorded.
- Understand requirements/dependencies; give a concise plan of files, changes, checks and risks; implement only the selected scope; verify; report; persist results.
- Follow existing conventions, avoid unrelated refactoring, preserve draft/confirmed boundaries, and introduce no speculative architecture. Coding conventions and test/build commands remain UNKNOWN until a real project is established.
- Run appropriate builds, unit/integration/acceptance checks when available. Never claim unexecuted tests passed; explain missing tooling or tests. Documentation-only setup requires document/link and Git safety checks, not invented gameplay tests.

## Memory, shutdown and academic evidence
- `docs/PROJECT_STATE.md`: short current handoff, branch/base/task/commit, uncommitted work, push/PR status, blockers and exact next action. Update at every HANDOFF.
- `docs/TASKS.md`: task status and verification. `docs/DECISIONS.md`: significant decisions with date/status/context/approach/rationale/alternatives/consequences; preserve superseded history.
- `docs/WORK_LOG.md`: append concise verified activities, changed files, executed checks/results, commit/PR evidence, problems and remaining work. Archive when necessary without losing traceability.
- At shutdown, inspect live Git state and changes; synchronize memory and task evidence; report limitations and where to resume. Keep stable instructions here, not lengthy progress history.
- Trace task ID -> requirement -> actual implementation -> executed tests -> verified commit -> PR. Distinguish untested, tested, committed, pushed and merged. Record human review and AI assistance accurately; never inflate contributions.

## Multi-agent strategy
- No sub-agent execution interface was available during setup; operate sequentially unless future tooling actually supports delegation. Never pretend delegation occurred.
- Use agents only for beneficial bounded independent work: read-only explorer/reviewer, scoped implementer, or testing agent. Assign files and acceptance criteria; require concise evidence-backed results.
- Avoid concurrent edits to shared files/worktrees. Separate worktrees/branches require the same user approval and protected-branch rules. Main agent verifies integration and maintains persistent memory.

## Natural-language commands
These are workflows, not native CLI slash commands.

### RESUME
"Resume the project from AGENTS.md and docs/PROJECT_STATE.md. Verify Git status and tell me the next actionable task. Do not implement yet."

Follow startup and integration/assignment safety rules, inspect active task and relevant commits, verify relationship to long23001901, identify stale/conflicting records, and report completed/active/blocked work and relevant files. If foundation approval is missing, request the owner's decision and stop before branch creation or implementation. Do not modify application code.

Output: PROJECT RESUME; Branch; Latest Commit; Current Goal; Completed; In Progress; Blockers; Next Task; Relevant Files.

### WORK
"Work on the selected task only. First inspect relevant files, propose a plan and acceptance criteria, then implement and verify. Do not commit or push without approval."

Use the selected task, or highest-priority actionable RESUME task if its scope is clear. Verify branch safety and relationship to approved integration, requirements and acceptance criteria; plan; implement within authorization on a dedicated approved feature/task branch; delegate only if justified; verify; review changes under clean commit rules; report outcomes; update memory as unintegrated until integration is verified. No unrelated functionality. Propose a PR targeting long23001901; authorization gates remain in force.

### HANDOFF
"Prepare a session handoff. Verify current Git state, update PROJECT_STATE.md, TASKS.md and WORK_LOG.md with actual results, tests and blockers. Summarize exactly where to resume next time. Do not commit or push."

Inspect branch, relationship to long23001901, status, recent commits and changes; record actual completion, integration state, active work, tests, blockers and questions; update the three memory files and significant decisions when needed; specify the exact next action. Reconcile integration memory only after authorized successful integration, keeping assignment evidence separate with commit references. Preserve unrelated work. Uncommitted changes are never completed commits.

Output: SESSION HANDOFF; Completed; Files Changed; Tests; Git Branch; Commit Status; Push Status; Blockers; Next Task; Documentation Updated.
