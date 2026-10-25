# Inspection baseline

Examined 2026-10-25 (Asia/Bangkok). Origin: https://github.com/tasdus-117/SE2026-T17.
Default branch main: b5054a61258e8a4d274fe56198dd5d0a5304124b. Its tree contains README.md only (`git init`).
Fetched read-only references: tasdus117 = 1257ad73e57a238d2a86bdddb6f72c1278977541;
feature/system-skeleton = db886ddf2bdd5986cd4fc5140ca2e34b46700bc8.
Initial status: untracked AD-065-prompt.md and Long_document/. Preserved in original worktree.
Assignment worktree: .ad065-worktree, branch assignment/AD-065-23001901, based exclusively on origin/main.
No merge, cherry-pick, or source transfer from teammate branches.

Local SRS: Long_document/srs.md, 678 lines, version 0.2 dated 2026-10-24.
Read in full in separate bounded reads; copied as evidence only, with original path and digest recorded separately.
Lines 29-37 distinguish confirmed offline 1-player/1-bot MVP from future multiplayer.
Lines 80-136 describe proposed Unity/C# technology, not a required Go or PostgreSQL backend.
Part 7 (lines recorded in source-analysis.md) proposes server authority as future scope.
Existing Long_document report/diagrams were not used as assignment analysis or implementation.

Observed reference technologies: tasdus117 Godot + C#/ASP.NET + EF/PostgreSQL;
system-skeleton Go 1.23 + UDP + pgx/PostgreSQL, with variant parameters different from AD-065.
Neither reference implementation is certified correct by inspection. Their test suites were not executed.

Tool discovery: Node 25.9.0 and SQLite 3.51.3 available; Go and Python absent on PATH;
XeLaTeX and Poppler available; Docker engine 29.6.2 reachable after permission approval.
Selected original Node/SQLite slice for zero runtime dependencies, real ACID file database and repeatable tests.
This is an assignment experiment, not the SRS offline MVP nor a production multiplayer deployment.
PostgreSQL port and Unity client integration remain unverified future work.
