# Debt register (AI-assisted estimates)

Source commit 1ff103387097911312fdf0666bf993ad384a0e98; paths prefixed by
assignment/AD-065/implementation/. Impact/Urgency/Effort each 1-5;
Priority = Impact * Urgency / Effort. Scores are proposed judgments from this AI
analysis, not measured remediation cost or a student's independent evaluation.

| ID | Source/evidence | Impact | Urgency | Effort | Score/order | Action | Regression safety |
|---|---|---|---|---|---|---|---|
| TD1 | src/store.mjs:16-29 synchronous SQL inside request event loop | 4 | 4 | 3 | 5.33 / 1 | Measure lag/lock; migrate adapter to worker or async DB if threshold exceeded | Atomicity, duplicate/restart tests plus 764-task benchmark |
| TD2 | src/transport.mjs:22-23 observer called even for replay, best effort | 4 | 3 | 3 | 4.00 / 2 | Add outbox only when reliable external effect is required | Existing observer-failure test; add consumer crash/replay tests with actual outbox |
| TD3 | src/migrations.mjs:8-10 permanent commands, src/retention.mjs:3-4 event-only purge | 3 | 3 | 3 | 3.00 / 3 | Define closed-match archival and reject late commands before identity deletion | Existing replay-after-purge test; planned archival boundary tests |

TD1 first: every fresh command traverses blocking SQL and therefore threatens D1.
TD2 has no mandatory business effect today; TD3 needs long-duration size measurements.
Heuristic is sensitive to uncertain estimates, interactions, and workload; replace
scores when operational evidence appears. Tests marked planned do not exist yet.

Resolved Q1: primitive obsession in newly authored baseline fd4df4c, combat.mjs:1-6.
Refactored into PlayerId/Health/Match at 1ff1033; unchanged 3 characterization tests pass.
It is not listed as unresolved debt and is not attributed to teammate source.
