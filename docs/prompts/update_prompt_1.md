You are working in this repository:
https://github.com/tasdus-117/SE2026-T17
Documentation branch to inspect:
docs/23001901/online-ffa-roadmap
I am team member 23001901, responsible for:
TV2 — Gameplay, match rules, and combat.
Update the documentation describing my work using the attached file “docs\SE2026-T17-Huong-dan-va-phan-cong-nhom.md”, titled “SE2026-T17 — Hướng dẫn khởi động và phân công nhóm”, as the primary reference.
This is a documentation and planning task. Do not implement gameplay yet. Write the resulting repository documentation and your final report in Vietnamese.
1. Source priority and conflicting requirements
Read the entire attached task allocation file first. It takes priority when determining TV2’s responsibilities for this task.
Then inspect the relevant documentation on the exact branch docs/23001901/online-ffa-roadmap. Do not silently substitute files from main:
docs/SE2026-T17-Huong-dan-va-phan-cong-nhom.md
Applicable AGENTS.md files.
README.md.
Current sections and relevant history in Long_document/srs.md.
Long_document/learning-roadmap.md.
docs/ROADMAP.md.
docs/ARCHITECTURE.md.
docs/PROTOCOL.md.
docs/TESTING.md.
docs/TASKS.md.
docs/PROJECT_STATE.md.
docs/DECISIONS.md.
Relevant recent entries in docs/WORK_LOG.md.
Inspect the actual repository structure and source before describing implementation progress. Proposed paths do not prove that directories exist, and planning documents do not prove that code runs.
The allocation file describes itself as a proposal for team agreement. My instruction authorizes using it to update my personal TV2 work plan; it does not establish that every proposal has been approved by the whole team or instructor.
For conflicts, produce a concise table:
existing source/section → allocation file guidance → impact on TV2 → resolution → remaining team decision.
Specifically inspect these differences:
Unity + Mirror versus the previous NGO recommendation.
One raycast gun versus melee combat.
Death, respawning, and kill scores versus elimination, ring-out, and spectator rules.
A separately running server with clients versus host+3.
Jumping, weapon pickup/drop, knockback, private room codes, and requiring exactly four players to start, where these are not established by the allocation file.
Do not combine both rule sets into a larger mandatory scope. Preserve history and do not reuse an existing ID for a requirement with a different meaning.
2. Git inspection and editing authorization
Before editing, verify the repository, active branch, working tree, any merge/rebase in progress, and the relationship to long23001901.
I authorize documentation changes for this task on docs/23001901/online-ffa-roadmap. If another branch is active, switch to this branch only when existing work can be preserved and repository instructions allow it. If there is a concrete blocker, explain it and continue any independent analysis that remains possible.
Do not edit main/master/develop or another member’s branch. Do not automatically stash, reset, clean, overwrite existing work, recreate the branch from main, or import assignment code.
Do not commit, push, create a PR, or merge in this task.
3. Documentation outcome
Rewrite the sections describing my work so that I can clearly understand:
What I own.
Where I coordinate with TV1 and TV3.
What starts immediately in M1 and what belongs to M3.
What I can do independently before the Unity project or networking is ready.
Each task’s inputs and deliverables.
Verification steps and completion criteria.
Unresolved decisions and which work they block.
Prefer updating existing locations over creating duplicate documents.
Use docs/TASKS.md for actionable tasks and actual status. Use docs/ROADMAP.md for sequencing, milestones, and coordination. If a separate TV2 plan is necessary, create exactly one clearly named document, link it from these files, and define which document owns each type of information.
Keep the overall team plan as context. Do not turn the entire repository into TV2-only documentation.
4. Responsibility boundaries
TV2 leads:
Shared character data, gameplay states, and rules.
Movement speed rules, movement eligibility, and input value limits, in coordination with TV1.
One raycast gun’s cooldown, range, and damage; ammunition only if the team chooses to include it.
Server-side shot validation rules.
HP, death, respawning, and kill scores.
Gameplay match lifecycle rules, coordinated with TV1’s lobby and connection lifecycle.
Independent rule tests, integrated combat tests, and combat cheating scenarios.
Documentation, configuration, and evidence for this work.
TV1 leads:
Project initialization, Mirror installation, connections, spawning, and ownership.
Input reception and authoritative server movement simulation.
State synchronization, server-side lobby, reconnect, and build/CI.
TV3 leads:
Client input collection, camera, UI/HUD, arena, scenes, and prefab assembly.
Do not assign TV2 ownership of networking, UI, or the entire CI pipeline. Define TV2’s supporting contracts, components, tests, and integration responsibilities.
These paths are proposed by the allocation file and must be checked against the actual repository:
Assets/_Project/Scripts/Server/Combat/
Assets/_Project/Scripts/Shared/Domain/
Assets/_Project/Settings/
Assets/_Project/Tests/EditMode/
5. Break down TV2’s work
Create small tasks that can be reviewed, verified, and handed over independently. Use the following groups as a starting point and adjust them based on the sources:
Reconcile scope and identify unresolved gameplay decisions.
Define character data, gameplay states, and rule configuration.
M1: define movement rules and input limits for integration with TV1’s simulation.
Agree on input, shot request, combat result, and presentation contracts with TV1 and TV3.
Define gun configuration and cooldown/range/damage rules.
Define server-side raycast validation.
Define HP changes and exactly-once death handling.
Define respawning and coordinate safe spawn selection.
Define kill scoring and match rules.
Plan independent rule tests.
Plan integration tests with one server and at least two clients.
Prepare combat cheating scenarios, evidence, and support for M5–M6.
You may introduce IDs such as TV2-001 after checking for collisions. Preserve existing task IDs and their history.
Each task must include:
ID, title, milestone, and priority.
Primary owner and collaborators.
Objective, scope, and exclusions.
Concrete dependencies.
Inputs, outputs, and proposed file paths.
Observable acceptance criteria.
Verification procedure and required evidence.
Actual status: TODO, IN_PROGRESS, BLOCKED, or DONE.
Open decisions and next action.
Do not mark gameplay DONE because its plan has been written. Do not mark every task BLOCKED merely because the Unity project is unavailable: distinguish specification and test design that can begin now from execution that needs the project.
6. Contracts and server authority
Describe logical contracts without writing code or imposing version-specific Mirror APIs before checking the selected version.
Clarify that:
Clients submit intent; the server derives actor identity from the authenticated connection.
Clients do not decide damage, HP, scores, or hit results.
TV1 owns ownership checks and input reception; TV2 defines gameplay eligibility and rules.
The server validates player state, match state, cooldown, and bounded input values.
Shot origin and direction must be valid relative to server state; arbitrary client positions are not trusted.
The server validates obstruction, targets, and range.
Repeated requests or callbacks must not apply damage or award scores twice.
Cooldowns use server time.
HP cannot become negative, and death/respawn transitions are explicit.
TV3 displays authoritative results.
Introduce duplicate/replay handling only as justified by the actual contract. Do not expand the task into lag compensation, prediction, commercial anti-cheat, or a multi-service architecture.
7. Decisions that must remain open
List the decisions requiring team agreement:
Initial HP, movement speed, damage, cooldown, and firing range.
Whether ammunition and reloading are included.
Camera perspective and aiming behavior, coordinated with TV3.
Respawn delay, spawn locations, and possible spawn protection.
Suicide, environmental damage, and kill credit.
Match end conditions, time/score limits, and ties.
Minimum player count required to start.
How disconnect/reconnect affects cooldown, death, and respawn state.
Any suggested values must be labelled PROPOSED rather than approved acceptance criteria. Do not import 10 HP, 1 damage, or cooldown values from the historical offline SRS as current defaults.
8. Targeted documentation updates
Update the necessary files to make the TV2 plan consistent:
TASKS: TV2 tasks and actual status.
ROADMAP: TV2’s mapping to M0–M6, especially its M1 contribution and M3 ownership.
learning-roadmap: learning needed for TV2, using my Java/OOP background as a bridge to C#, Unity lifecycle, configuration, Physics.Raycast, server authority, and testing. Separate learning outcomes from product deliverables.
PROTOCOL, ARCHITECTURE, and TESTING: relevant contracts, ownership boundaries, and TV2 verification.
srs.md: preserve historical offline and online baselines; explicitly identify superseded requirements and unresolved decisions. Use new IDs and mappings if a new baseline is needed.
README and AGENTS: update only scope statements that directly conflict with this plan; preserve Git and workflow safeguards.
PROJECT_STATE, DECISIONS, and WORK_LOG: record the priority source, planning changes, open decisions, and actual results. Preserve historical logs.
Do not rewrite AD-065 coursework or submission artifacts. Do not create a Unity project, install packages, or implement gameplay in this task.
9. Verification and final report
Before finishing:
Check the diff, whitespace, links, headings/anchors, and IDs.
Check for circular dependencies.
Verify that each task has a deliverable, acceptance criteria, and a verification method.
Check for overlapping ownership among TV1, TV2, and TV3.
Ensure conflicting rule sets are not both presented as current.
Label unexecuted tests Planned or NOT RUN.
Distinguish documentation changes, implementation, testing, commits, pushes, and integration.
Report in Vietnamese:
What the allocation file changes relative to the previous plan.
Files changed and why.
TV2’s tasks in implementation order.
What TV2 should start immediately.
Decisions requiring TV1, TV3, or team agreement.
Actual verification results and Git status.
Perform the authorized documentation edits. Do not merely return another plan or prompt.

