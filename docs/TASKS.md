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
- Priority: HIGH. Status: IN_PROGRESS.
- Acceptance criteria: Preserve requirement IDs/history, map D-01–12 supersession, distinguish approved product/Git vs proposed exact tools, bounded next task; missing approvals explicit, không giả architecture approval.
- Dependencies: Owner product prompt, BASE-001; DOC-001 verification và TECH-001 cho exact stack review.
- Relevant files: ../Long_document/srs.md; ../Long_document/learning-roadmap.md; ARCHITECTURE.md; DECISIONS.md; ROADMAP.md.
- Verification: Product reconciliation được viết ở DOC-001; exact framework/version/service selection chưa duyệt, không gọi PLAN hoàn tất lựa chọn.
- Commits: Chưa có cho phần online tại thời điểm chuẩn bị commit. PR: NONE created.

## DOC-001 — Online FFA documentation baseline

- Description: Vietnamese current requirements/rules, product roadmap/catalogue, architecture comparison, protocol/testing/evidence, navigation và corrected memory.
- Priority: HIGH. Status: IN_PROGRESS.
- Acceptance criteria: Current FFA/Unity/C#/host+3; mandatory weapon/reconnect/evidence trong first MVP; 2v2 future; stable old IDs/full SRS history; feature/requirement/AC/TC mapping, acyclic dependencies; official dated assessment; Markdown links; docs-only diff; preserve prompt/assignment; verified commit/push trên authorized docs branch, awaiting integration.
- Dependencies: BASE-001, full task prompt, owner docs branch/commit/push approval đã nhận; consequential implementation choices không chặn độc lập documentation.
- Relevant files: ../README.md; ../AGENTS.md; ../Long_document/srs.md; ../Long_document/learning-roadmap.md; ROADMAP.md; ARCHITECTURE.md; PROTOCOL.md; TESTING.md; PROJECT_STATE.md; TASKS.md; DECISIONS.md; WORK_LOG.md.
- Verification: Document checks PASS: 52 local links/11 anchors, 14 FR/6 NFR/5 UC/20 AC/20 TC unique và traceability; 19-feature DAG, historical SRS/prompt preservation, append-only WORK_LOG, UTF-8/fences/common secret patterns, manual mandatory-scope review; git diff --check không whitespace errors. Staged review/commit/push còn pending. Tất cả gameplay TC-101–120 NOT RUN; không CI YAML/code/config/test executable được tạo.
- Commits/push: PENDING verification/commit/push. PR: NONE created. Integration: AWAITING INTEGRATION into long23001901.

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
