# Tasks

Statuses: TODO, IN_PROGRESS, BLOCKED, DONE. DONE requires evidence for every acceptance criterion. Requirement approval does not mean implementation completion.

## SETUP-001 — Establish persistent project workflow
- Description: Create safe session instructions and concise memory/task/evidence records for identifier 23001901.
- Priority: HIGH
- Status: DONE
- Acceptance criteria: Read setup instructions in full; inspect repository and existing conventions; work on approved personal branch; preserve preexisting files/worktrees; create all five workflow documents and RESUME/WORK/HANDOFF definitions; record verified status/unknowns; check document consistency; no features, commit, push or merge.
- Dependencies: User authorization for chore/23001901/project-agent-setup (received).
- Relevant files: ../AGENTS.md; PROJECT_STATE.md; TASKS.md; DECISIONS.md; WORK_LOG.md; ../Persistent_Project_Memory_Task_Manage.md (input only).
- Verification: Full instruction read; targeted discovery and Git checks; required document sections, commands, local links and preservation checks passed. See WORK_LOG.md for scope and evidence limits.
- Related commits: NONE (setup uncommitted). PR: NONE created.

## PLAN-001 — Review draft implementation baseline
- Description: Review SRS metadata, open decisions and toolchain proposal with the owner/team; record approved choices and unresolved questions before project creation. Do not assume proposals are approved.
- Priority: HIGH
- Status: TODO
- Acceptance criteria: Preserve confirmed/proposed/future distinctions and existing requirement IDs; record actual approval evidence or explicit unresolved items for SRS D-01–D-12; establish exact toolchain/version selection and team integration workflow, or mark missing choices as blockers; identify one bounded next task with requirement IDs and verifiable acceptance criteria. No application implementation is included in this task.
- Dependencies: SETUP-001; owner/team input for approvals and toolchain choices.
- Relevant files: ../Long_document/srs.md sections 1.1–1.3 and 2; ../Long_document/learning-roadmap.md; DECISIONS.md; PROJECT_STATE.md.
- Verification: NOT RUN.
- Related commits: NONE. PR: NONE created.

## WF-002 — Persist personal integration and feature workflow
- Description: Apply the owner's mandatory branch architecture, naming, approval and cross-branch memory rules.
- Priority: HIGH
- Status: DONE
- Acceptance criteria: Record long23001901 authority, separate approved feature branches, Conventional Commits, integration approval, assignment preservation, and branch relationship checks for all session commands.
- Dependencies: Owner's mandatory workflow instruction (received).
- Relevant files: ../AGENTS.md; PROJECT_STATE.md; TASKS.md; DECISIONS.md; WORK_LOG.md.
- Verification: Instruction/document consistency checked; no branch creation or integration performed.
- Related commits: NONE. PR: NONE created.

## BASE-001 — Approve and establish personal integration baseline
- Description: Select the approved foundation and safely incorporate persistent memory; inspection/proposal recorded in PROJECT_STATE.md.
- Priority: HIGH
- Status: BLOCKED
- Acceptance criteria: Confirm foundation and exact commit with owner; verify freshness and tracked files/history; approve branch creation and memory incorporation separately as needed; preserve assignment work/history; verify integration branch and memory describe actual codebase.
- Dependencies: Explicit owner approval of starting commit, branch creation and documentation integration; resolve Unity draft versus existing Godot/.NET or Go foundation.
- Relevant files: PROJECT_STATE.md; ../AGENTS.md; DECISIONS.md; WORK_LOG.md; candidate branch trees (read-only).
- Verification: Cached branch trees, README files, manifests, histories and common ancestry inspected; no foundation approval established; branch not created. No application tests run.
- Related commits: NONE. PR: NONE created.

No application work is active. PLAN-001 must follow baseline selection rather than assume Unity is the implementation stack. Existing AD-065 completion has not been independently audited.

## WF-003 — Clean commits, assignment reuse and memory synchronization
- Description: Persist clean integration safeguards and refresh baseline evidence without creating branches.
- Priority: HIGH
- Status: DONE
- Acceptance criteria: Document explicit staging/diff/secret-output checks; technology-based ignore policy; assignment isolation and reviewed reusable-code transfer; required feature dependencies/tests/assets; PR target; RESUME/WORK/HANDOFF synchronization and approval gates.
- Dependencies: Owner workflow instruction (received); setup branch verified safe.
- Relevant files: ../AGENTS.md; PROJECT_STATE.md; TASKS.md; DECISIONS.md; WORK_LOG.md.
- Verification: Workflow requirements and document consistency checked; live remote heads match local candidates and no long23001901 exists. No build/tests run; no .gitignore created before technology approval.
- Related commits: NONE. PR: NONE created.
