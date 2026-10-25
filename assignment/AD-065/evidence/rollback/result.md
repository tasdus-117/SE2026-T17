# Executed rollback evidence

Command: `cd assignment/AD-065/implementation; node --test test/*.test.mjs`.
Result: 10 passed, 0 failed, 0 skipped; see ../tests/final-tests.txt.

Test `expand, v2 deployment, failed HTTP verification, v1 rollback on same database`:
1. Disposable v1 SQLite file: HP 10, first old command -> HP 9.
2. Stop old store; run v2 migration (nullable events.detail). Existing columns retained.
3. Start v2 writer and HTTP transport with isolated faulty-response flag; new command commits HP 8,
   but response is 9. Assert discrepancy with authoritative snapshot, detecting verification failure.
4. Stop v2 HTTP and close v2 DB; restart v1 writer/HTTP on the expanded same file.
5. New v1 command returns HP 7; three durable events exist.
6. Retry v2 identity through v1 returns original HP 8, no new damage.
7. Inspect retained detail column and v2 ruleset value. No DOWN migration.

This is a local process/component deployment simulation using v1/v2 writer modes,
not an orchestrator, production release or historical binary deployment.
No compensation of legitimate committed damage is attempted. App rollback restores
behavior; it does not undo committed business data. Additive detail remains until
a separately approved compatibility window closes and old readers are retired.
The 12-hour obligation is to respond to a change request, not a rollback-window length.
