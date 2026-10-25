
You are Codex CLI acting as a senior software architect, backend engineer, test engineer, and technical report author.

Your task is to complete my individual Software Architecture assignment, AD-065, using the REAL GitHub repository, while producing an original, independently implemented code slice and a thoroughly verified Vietnamese PDF report.

DO NOT stop at analysis, planning, or documentation. Execute the full workflow: inspect repository → create my branch → implement original code → run tests → collect evidence → conduct AI design review → create diagrams → generate PDF → validate deliverables.

Work autonomously, within the available environment and permissions. Inspect command failures and repair them where possible. Do not claim successful execution when it has not occurred.

==================================================
1. STUDENT AND ASSIGNMENT IDENTITY
==================================================

Student: Đồng Vũ Hoàng Long
Student ID: 23001901
Team: SE2026-T17
Project: GAME-3D-03 – Small Arena
Variant: AD-065
Role: Team member
Credits: 3TC

Repository:
https://github.com/tasdus-117/SE2026-T17

Required final PDF filename:
BT2_23001901_AD-065.pdf

Required final location:
assignment/AD-065/submission/BT2_23001901_AD-065.pdf

The final report must be written in Vietnamese.

EXCEPTION:
Both Mermaid diagrams must contain ONLY English text, including node names, labels, legends, edge labels, titles, and annotations.

The report must contain at most 7 A4 pages of MAIN CONTENT.

The AI Usage Log appendix may extend beyond those 7 pages.

The first page MUST include this exact Vietnamese declaration:

"Tôi chịu trách nhiệm về toàn bộ nội dung bài làm và đã khai báo việc sử dụng AI."

Academic integrity is mandatory.

This is an individual assignment. Never copy another student's diagrams, ADR, debt register, analysis, or source code.

AI-generated work must be clearly disclosed. Do not manufacture personal judgments, experiments, source evidence, or student verification.

==================================================
2. EXACT AD-065 VARIANT
==================================================

Analyze the authoritative multiplayer 3D server of GAME-3D-03.

Architecture incident:
A new deployment changes the database schema, but the system must support rollback when post-deployment verification fails.

Primary boundary:
Data ownership and transaction boundary.

Mandatory code smell:
Primitive obsession that destroys domain meaning.

Primary quality attribute:
Performance, specifically response time under concurrent load.

Exact parameters:
- Concurrent users/tasks: 764
- p95 target: <= 4.5 seconds
- Operational data retention: 7 days
- Change request response window: 12 hours

These values MUST appear correctly throughout:
- architecture drivers
- quality attribute scenario
- diagrams where appropriate
- contracts
- implementation/configuration
- tests
- performance evidence
- ADR
- traceability matrix
- final PDF

Never replace 764 with 786, 568, or another value.

Do not claim the 4.5-second target has been achieved unless a real load test provides supporting measurements.

State the exact load-test environment, request mix, duration, sample size, and p95 calculation method when reporting performance.

The 12-hour value describes the change-request response obligation, not necessarily implementation completion time.

==================================================
3. REPOSITORY INVESTIGATION
==================================================

Before modifying anything:

1. Inspect the current Git working directory.
2. Confirm the origin remote.
3. Inspect all local and remote branches.
4. Record git status and existing uncommitted changes.
5. Identify the default branch and its commit SHA.
6. Fetch the following branches read-only:

   origin/tasdus117
   origin/feature/system-skeleton

7. Inspect their file trees, relevant source, tests, and documentation.
8. Search the repository for:

   srs.md
   Long_document
   README.md
   architecture documents
   technology descriptions
   requirements
   game/server code
   persistence code
   deployment configuration
   test infrastructure

9. Locate and read the actual SRS, including the document under Long_document if it exists.

If the SRS is missing, search repository history and available local files. Do not fabricate its contents.

Use the SRS technology choices as a reference, but do NOT automatically implement the complete offline MVP described there.

Distinguish:
A. Confirmed project requirements
B. Confirmed existing implementation
C. Proposed AD-065 architecture
D. New code created for this assignment
E. Verified test results
F. Unverified hypotheses

Record repository inspection evidence:

- Branch names
- Exact commit SHAs
- Examination date
- Source paths
- Relevant line ranges
- Technology stack
- Observed implementation limitations

Important:

The two branches below contain teammate code and are READ-ONLY REFERENCES.

https://github.com/tasdus-117/SE2026-T17/tree/tasdus117

https://github.com/tasdus-117/SE2026-T17/tree/feature/system-skeleton

NEVER:
- Modify these branches
- Cherry-pick their implementation commits
- Copy their source files
- Copy their design diagrams
- Copy their ADRs or debt registers
- Present their source code as my work
- Merge their branches into my individual branch

You may inspect them to understand repository context and identify architectural constraints.

==================================================
4. CREATE MY OWN BRANCH
==================================================

Create this new branch:

assignment/AD-065-23001901

Prefer creating it from the official default branch at a recorded base commit, NOT from either teammate implementation branch.

Verify the resulting branch does not inherit copied implementation code from those branches.

Preserve all existing user changes.

If the workspace is dirty, use a safe Git worktree or another isolated method rather than deleting or overwriting changes.

If the branch already exists:
- Inspect it
- Preserve existing user changes
- Continue safely
- Never force-reset without explicit authorization

All new implementation and assignment artifacts belong to my own branch.

Use descriptive commits, for example:

- chore(ad065): establish independent assignment baseline
- feat(ad065): implement authoritative server slice
- test(ad065): verify invariants and schema rollback
- docs(ad065): add architecture evidence and report

Push only my new branch if authentication and permissions permit.

Do not open or merge a pull request unless needed for the workflow and explicitly authorized.

==================================================
5. IMPLEMENTATION SCOPE AND TECHNOLOGY
==================================================

I do NOT want to rebuild an entire multiplayer 3D game solely for this assignment.

I want an original, small but credible, executable vertical slice of the authoritative server.

It must directly support the assignment's architecture analysis.

Select the smallest cohesive feature that provides strong evidence for:

- Server-authoritative validation
- Domain invariants
- Explicit data ownership
- Database transactions
- Schema evolution
- Safe rollback
- Performance under concurrent load
- Primitive obsession
- Refactoring safety

Suggested feature:
Authoritative combat command processing with a persistent match/combat outcome and operational event logging.

This is a suggestion, not a mandatory implementation.

First inspect the real SRS and technology constraints.

Prefer its documented technology stack when suitable.

Examples:
- Unity/C# client integration
- Go authoritative server
- PostgreSQL persistence

But do NOT force these technologies if actual documented requirements contradict them.

Explain the technology selection with evidence.

Prefer a modular monolith for this individual vertical slice IF justified by coupling, transaction consistency, deployment cost, and testability.

Compare it with at least one realistic alternative.

Do not choose microservices merely because they are supposedly easier to scale.

Implement at least five meaningful modules/components, for example:

1. Transport/API
2. Command application service
3. Combat domain
4. Match aggregate/state management
5. Persistence adapter
6. Schema migration manager
7. Operational event/retention module
8. Metrics and load-test support

Select the actual modules based on implementation needs.

Ensure their responsibilities and dependency directions are explicit.

Domain rules must not be hidden in HTTP handlers, UI code, or arbitrary database helper functions.

==================================================
6. DATA OWNERSHIP AND TRANSACTION BOUNDARY
==================================================

Define the authoritative owner of match/combat state.

Document:
- Which module may mutate domain state
- Which module owns each persistent data set
- Where a transaction starts and commits
- Which operations must be atomic
- Which events occur after commit
- How other modules access the data
- How concurrent commands are handled

Provide a specific invariant.

For example:
A combat command must not apply damage twice for the same command identity.

Or:
A player's health must remain within its valid domain range.

Choose an invariant appropriate to the actual implementation.

Enforce the invariant at server/domain level, supported by database constraints or transactional mechanisms where applicable.

Do not depend only on frontend validation.

Implement a versioned API or event contract with documented:
- Inputs
- Outputs
- Command identifiers
- Domain identifiers
- Error semantics
- Versioning
- Compatibility expectations

Avoid distributed transactions unless there is compelling evidence that they are needed.

==================================================
7. SCHEMA EVOLUTION AND ROLLBACK
==================================================

Implement a realistic schema migration demonstration.

Use an expand-and-contract strategy when justified.

Demonstrate:

1. Starting schema
2. Additive migration
3. Application compatibility
4. Deployment of the new application version
5. Post-deployment verification
6. Failure detection
7. Application rollback
8. Database compatibility after rollback

Do not assume that automatically reverting a database migration is always safe.

If backward data migration is unsafe, explicitly document it and retain an application-compatible schema until the rollback window closes.

Include a practical failure scenario.

Provide automated tests where feasible for:
- Migration execution
- Old/new schema compatibility
- Rollback to previous application behavior
- Transaction atomicity
- Domain invariants
- Duplicate command handling
- Failure recovery

For any failure simulation, isolate test data from real systems.

NEVER perform destructive operations against an unknown production database.

Use local disposable test databases or containers.

==================================================
8. PRIMITIVE OBSESSION AND REFACTORING
==================================================

This assignment requires real code evidence, not hypothetical code smells.

Design and implement a small initial working feature.

Capture its baseline BEFORE refactoring.

For a genuinely relevant portion, permit a limited primitive-based design that represents a meaningful refactoring opportunity.

Do not deliberately introduce unsafe vulnerabilities or broad technical debt.

Possible domain types:

PlayerId
MatchId
CommandId
Health
Damage
MatchStatus

Choose appropriate types from the actual domain.

Before refactoring:
- Preserve the exact original code at a traceable baseline commit
- Record the file paths and line ranges
- Write characterization/regression tests
- Establish observable behavior

Then refactor primitive values into meaningful domain types or value objects.

After refactoring:
- Preserve externally observable behavior
- Preserve API compatibility where required
- Preserve error semantics
- Run the regression tests again
- Document changes in cohesion/coupling/information hiding

Avoid turning an invented before-refactoring issue into a claim about pre-existing team code.

Clearly distinguish:
"Newly authored baseline code"
from
"Pre-existing repository technical debt."

The final report must analyze at least three ACTUAL, verified code-quality concerns.

For every issue provide:

Evidence
→ Risk / impact surface
→ Refactoring step
→ Regression safety

Analyze concerns involving:
- Cohesion
- Coupling
- Information hiding
- Dependency direction
- Technical debt

At least one concern MUST be primitive obsession.

Prefer genuine trade-offs or measurable risks over artificially manufacturing defects.

If fewer than three justified problems can be established, explain the evidence gap and do not invent findings.

==================================================
9. ARCHITECTURE DRIVERS
==================================================

Identify EXACTLY three prioritized architecture drivers.

Select them from actual requirements, quality attributes, and project constraints.

The selected drivers must cover:

- Concurrent-load performance
- Deployment/schema rollback and reliability
- Data consistency/ownership or another strongly supported domain constraint

Explain:
- Source requirement
- Architectural consequence
- Priority
- Rationale

Create exactly one complete Performance quality attribute scenario with:

Source
Stimulus
Environment
Artifact
Response
Measurable response

The scenario must explicitly use:
764 concurrent users/tasks
p95 <= 4.5 seconds

Specify the measured request/operation and measurement window.

Explain a concrete trade-off between Performance and another quality attribute, such as Consistency, Reliability, or Modifiability.

Avoid generic explanations.

==================================================
10. MERMAID DIAGRAMS
==================================================

Create EXACTLY two primary diagrams.

Diagram 1:
Module View

Diagram 2:
Runtime View

Save the Mermaid source as:

assignment/AD-065/diagrams/module-view.mmd
assignment/AD-065/diagrams/runtime-view.mmd

ALL text inside both diagrams MUST be English.

Diagram 1 requirements:
- At least five modules/components
- Clear responsibilities
- Dependency directions
- Main business-rule owner
- Persistent data owner
- Transaction boundary
- Clear distinction between interfaces and implementations

Diagram 2 requirements:
- Requests
- Events
- Data flow
- Deployment with a schema change
- Post-deployment verification
- At least one failure path
- Application rollback
- Database compatibility
- Transaction and data ownership boundaries

Use Mermaid flowchart, sequenceDiagram, or another suitable Mermaid type.

Prefer:
- Flowchart for Module View
- Sequence Diagram for Runtime View

The diagrams MUST represent the actual proposed/implemented architecture, not fictional infrastructure presented as existing.

Use clear labels and avoid dense crossing arrows.

Render Mermaid into high-quality SVG files.

Validate the Mermaid syntax automatically.

Embed the rendered diagrams in the PDF.

Both diagrams must remain legible on printed A4 paper.

Inspect their rendered output to detect clipping, tiny text, overlapping labels, or incorrect arrows.

Do not merely paste Mermaid source code into the PDF.

==================================================
11. ADR AND ARCHITECTURAL TRADE-OFFS
==================================================

Compare at least two reasonable architecture approaches.

Examples:
A. Modular monolith with one transactional database
B. Service-oriented or event-driven design

You may select alternatives appropriate to the actual system.

Compare them in terms of:
- Performance
- Consistency
- Modifiability
- Deployability
- Schema rollback
- Complexity
- Operational overhead
- Team capacity

Create one complete ADR with:

- ADR ID and title
- Status
- Context
- Decision
- Alternatives
- Consequences
- Decision rationale
- Evidence/review triggers

Explain what future measurements or observed requirements would justify reconsidering the decision.

Include specific triggers, not statements such as "when the system gets bigger."

Do not fabricate existing performance results.

==================================================
12. AUTOMATED VERIFICATION
==================================================

Create and execute an automated verification suite.

Choose appropriate tools based on the actual selected stack.

Verification should include, where relevant:

- Compilation/build
- Unit tests
- Domain invariant tests
- API contract tests
- Transaction integration tests
- Database migration tests
- Application rollback compatibility test
- Concurrent command tests
- Formatting/lint/static analysis
- Mermaid syntax validation
- PDF quality validation

Implement a reproducible local load-test harness.

Use exactly 764 concurrent virtual users/tasks for the AD-065 performance experiment when local resources and test-tool capabilities permit.

Record:
- Hardware/runner environment
- Server/database setup
- Exact request mix
- Concurrency model
- Test duration
- Warm-up
- Number of samples
- p50, p95, p99 if available
- Error rate
- Throughput
- Limitations

Separate:
- Completed test
- Passed test
- Failed test
- Skipped test
- Test not executable in the current environment

If resources do not support 764 concurrent tasks, do not invent a result.

Produce a reproducible test command and clearly mark p95 compliance as NOT VERIFIED.

If testing is possible, compare the observed p95 against <= 4.5 seconds.

Fix implementation problems identified by the tests and rerun affected checks.

Keep genuine failure logs where they demonstrate a problem that was subsequently corrected.

==================================================
13. TECHNICAL DEBT REGISTER
==================================================

Create a debt register with at least three evidence-backed entries.

For each entry:

- Debt ID
- Relevant source location
- Actual evidence
- Impact: 1–5
- Urgency: 1–5
- Effort: 1–5
- Priority
- Proposed action
- Regression safety

Use a transparent prioritization method.

For example:
PriorityScore = Impact * Urgency / Effort

Acknowledge limitations of such a heuristic.

Explain why the highest-priority debt should be addressed first.

Include primitive obsession in the debt analysis when justified by the captured baseline code.

Do not describe a fully resolved refactoring as unresolved debt.

==================================================
14. AI-ASSISTED DESIGN REVIEW
==================================================

Perform an actual AI-assisted design review.

Preserve the genuine main instruction supplied by the user as part of the AI Usage Log.

You may also create shorter sub-prompts for:
- Architecture alternatives
- Boundary analysis
- Refactoring review
- Rollback design
- Test strategy
- Evidence validation

Save each meaningful prompt and relevant output excerpts.

Recommended directory:

assignment/AD-065/evidence/ai/

The review MUST include:

1. The actual prompt used
2. Relevant actual AI output
3. At least two AI suggestions evaluated against code or requirements
4. The decision to keep, modify, or reject each suggestion
5. The exact supporting evidence
6. A statement of remaining uncertainty

Examples of proposals to critically assess:
- "Split every domain into microservices"
- "Use an automatic destructive DOWN migration during rollback"
- "Move critical validation to the client"
- "Assume a cache automatically guarantees p95 <= 4.5s"

These are examples only. Evaluate actual suggestions made during the review.

Do not fabricate separate AI conversations, timestamps, or tools.

If the CLI cannot distinguish sub-agent outputs from its own reasoning, label these accurately as a single Codex-assisted review rather than inventing independent reviewers.

The AI Usage Log must be truthful and sufficiently detailed for academic verification.

==================================================
15. TRACEABILITY
==================================================

Create a traceability matrix:

Driver / Requirement
→ Architecture decision
→ Boundary / Contract
→ Exact code location
→ Planned or executed test
→ Evidence / Result

Use real repository-relative paths.

Where practical include file line numbers and commit SHA.

Use a stable inspected source commit for teammate repository context.

Use the actual new branch commit for original implementation evidence.

Clearly distinguish:
- Existing code
- Generated code
- Refactored code
- Tests executed
- Tests only planned

==================================================
16. REPORT GENERATION
==================================================

Generate one professional, submission-quality PDF.

Filename:
BT2_23001901_AD-065.pdf

Language:
Vietnamese, except the two completely English diagrams.

Paper:
A4 portrait.

Maximum:
7 pages of main content.

The AI Usage Log appendix does not count toward this limit.

Use a readable, academically appropriate Vietnamese Unicode font embedded in the PDF.

Ensure correct Vietnamese diacritics.

Avoid broken characters, clipped tables, overflowing paragraphs, and tiny illegible code.

Suggested layout:

Page 1:
- Student identity and declaration
- Scope and evidence baseline
- Three architecture drivers
- Performance quality scenario

Page 2:
- Module View diagram
- Module responsibilities
- Dependency direction
- Business-rule ownership

Page 3:
- Runtime View diagram
- Schema deployment and rollback scenario
- Data ownership and transaction boundaries
- Contract and invariant

Page 4:
- Architecture alternatives
- Complete ADR
- Trade-offs and evidence triggers

Page 5:
- Code quality analysis
- At least three evidence-backed issues
- Primitive obsession
- Before/after refactoring

Page 6:
- Regression safety and executed verification
- Debt register
- Prioritization rationale
- Performance evidence or limitations

Page 7:
- AI-assisted design review
- Two corrected/rejected AI suggestions
- Traceability matrix
- Overall conclusion
- Evidence references

Appendix, outside the main 7-page limit:
- AI Usage Log
- Actual prompts
- Selected authentic AI outputs
- Review decisions
- Verification references

You may adjust the allocation as necessary.

The seven-page main report must remain coherent and readable.

Use citations to:
- Repository files
- Source commits
- Actual requirements
- Local test artifacts
- Relevant specifications

Do not cite files or code that do not exist.

Do not invent external academic references.

Every major factual claim about the repository must be traceable.

Keep the PDF report focused on architectural reasoning rather than pasting source code.

==================================================
17. PDF TOOLING AND QUALITY GATES
==================================================

Use available reliable document-generation tools, for example:

- Markdown + Pandoc + LaTeX
- Typst
- HTML/CSS + PDF renderer
- Another reproducible PDF toolchain

Select based on installed capabilities.

Use Mermaid CLI or an equivalent genuine Mermaid renderer for diagrams.

Prefer SVG for sharp printed diagrams.

Do not hand-draw a diagram and claim it was rendered by Mermaid.

Automatically verify:

1. The PDF exists at the required path.
2. The exact filename is correct.
3. Main content is <= 7 pages.
4. Appendix pages are clearly marked.
5. Vietnamese Unicode is rendered correctly.
6. Diagram 1 is present.
7. Diagram 2 is present.
8. Both diagrams are readable on A4.
9. Both diagrams contain English only.
10. Student identity is correct.
11. Team/project/variant are correct.
12. Declaration text is exact.
13. Source paths and commits exist.
14. Evidence statements match actual results.
15. Tables are not cut off.
16. No placeholder content remains.
17. No invented performance results appear.

Render the final PDF to page images and inspect every page.

Where possible perform text extraction checks and layout checks.

Correct errors and regenerate.

If a quality gate cannot be satisfied, explicitly record the blocker rather than reporting a false success.

==================================================
18. PROJECT OUTPUT STRUCTURE
==================================================

Target structure:

assignment/AD-065/
  README.md
  implementation/
  diagrams/
    module-view.mmd
    runtime-view.mmd
    module-view.svg
    runtime-view.svg
  evidence/
    repository-baseline.md
    source-analysis.md
    tests/
    performance/
    rollback/
    ai/
  report/
    report.md
    build-report.*
  submission/
    BT2_23001901_AD-065.pdf
  verification-summary.md

The exact implementation substructure may vary by selected technology.

Keep temporary build files out of the submission folder.

Only the final PDF is intended for Google Classroom submission.

Do not submit the GitHub branch URL as a substitute for the PDF.

==================================================
19. AUTONOMOUS EXECUTION POLICY
==================================================

Execute the workflow in this order:

PHASE A — RESEARCH
- Inspect repository, branches and SRS
- Record baseline commits
- Select suitable implementation scope

PHASE B — ISOLATION
- Create my independent assignment branch
- Prepare source and evidence directories

PHASE C — IMPLEMENTATION
- Build original authoritative server vertical slice
- Establish a traceable baseline
- Implement domain ownership and transaction rules
- Implement safe schema evolution/rollback

PHASE D — REFACTORING
- Capture actual before-state evidence
- Add regression tests
- Refactor primitive obsession
- Preserve observable behavior

PHASE E — VERIFICATION
- Build and run tests
- Execute integration and rollback tests
- Run reproducible concurrency/performance tests if feasible
- Repair defects and rerun tests

PHASE F — ARCHITECTURE ANALYSIS
- Produce architecture drivers
- Produce quality scenario
- Produce boundary analysis
- Compare alternatives
- Write ADR
- Produce debt register and traceability

PHASE G — AI REVIEW
- Run focused design/refactoring prompts
- Capture authentic outputs
- Verify at least two suggestions against evidence
- Document accepted, modified and rejected proposals

PHASE H — DIAGRAMS AND PDF
- Generate two English Mermaid diagrams
- Render and inspect them
- Write the Vietnamese report
- Generate the final PDF
- Enforce the seven-page main-content limit
- Inspect all PDF pages

PHASE I — FINAL AUDIT
- Check every rubric requirement
- Verify source citations and code locations
- Verify file existence
- Verify test results
- Commit changes to my branch
- Push my branch if permitted

Iterate through implementation, testing, and repair as needed.

Do not ask for routine confirmations.

Never overwrite unrelated work.

Do not perform destructive remote Git operations.

Never bypass failing tests merely to make the result look successful.

If an external tool or dependency is unavailable, first try an equivalent local alternative. Document any genuinely unresolved blocker.

==================================================
20. FINAL ACCEPTANCE CHECKLIST
==================================================

The final result must cover ALL rubric criteria.

[ ] Exactly 3 architecture drivers
[ ] Full 6-part Performance scenario
[ ] 764 concurrent users/tasks
[ ] p95 target <= 4.5 seconds
[ ] Operational retention = 7 days
[ ] Change-request response = 12 hours
[ ] Performance trade-off
[ ] Mermaid Module View with >=5 components
[ ] Mermaid Runtime View with failure path
[ ] English-only diagram text
[ ] Responsibilities and dependency directions
[ ] Business-rule ownership
[ ] Explicit data ownership
[ ] Transaction boundary
[ ] API/event contract
[ ] Server/database invariant
[ ] At least 2 architectural alternatives
[ ] Complete ADR
[ ] Evidence-based ADR review triggers
[ ] Real code paths and exact commit evidence
[ ] At least 3 verified code-quality concerns
[ ] Primitive obsession analysis
[ ] Before/after refactoring
[ ] Observable-behavior preservation
[ ] Regression tests
[ ] Debt register with >=3 entries
[ ] Impact/Urgency/Effort scores
[ ] Explained priority order
[ ] Genuine AI-generated review material
[ ] At least 2 AI proposals modified or rejected
[ ] Evidence for review decisions
[ ] Complete traceability
[ ] Accurate AI Usage Log
[ ] Original code on my branch
[ ] No teammate source copied
[ ] Build/test results honestly reported
[ ] Safe schema rollback demonstrated or limitation stated
[ ] Vietnamese PDF
[ ] Correct student information
[ ] Exact declaration
[ ] <=7 pages of main content
[ ] AI Usage Log in separately marked appendix
[ ] Readable A4 diagrams
[ ] Exact required PDF filename

==================================================
21. FINAL RESPONSE FORMAT
==================================================

When complete, provide:

1. Branch name and final commit SHA
2. Final PDF absolute local path
3. Whether Git push succeeded
4. Implementation summary
5. Tests passed/failed/skipped
6. Actual load-test p95, or NOT VERIFIED
7. Rollback verification result
8. PDF total pages and main-content pages
9. Location of AI Usage Log
10. Remaining blockers or uncertainties

If any acceptance criterion fails, clearly report it.

Do not call the PDF "ready to submit" unless all document-level requirements are satisfied and the evidence is authentic.

Start executing PHASE A immediately.


