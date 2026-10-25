# Genuine failed checks and fixes

First integration execution: characterization 3 passed; atomicity, CHECK/FK,
ownership, API, retention passed. 764 concurrent retries and rollback cleanup failed.
Focused rerun exposed `connect ECONNREFUSED 127.0.0.1` at concurrency 764:
the default listener backlog was inadequate for the initial connection burst.
Fix: explicitly configure backlog 2048 in production entrypoint, test fixture and load child.
Backlog adjustment alone did not fix the same-process test (second run: 9/10 pass).
The server and load generator sharing one event loop prevented timely accept processing
during the 764-connection burst. The separate-process load test succeeded with 0 errors.
Fix to the integration fixture: run the HTTP server in its own process, matching the load
harness and actual deployment. This preserves all 764 concurrent requests and assertions.
Backlog remains an OS hint; no claim that backlog alone fixed the failure.

Rollback assertions completed but fixture cleanup failed with EPERM removing a temporary
directory while the reopened v1 SQLite handle remained open. Fix: track reopened stores
and close them before directory deletion. No schema DOWN or production data touched.
One diagnostic --test-force-exit run also printed a Node Windows libuv assertion;
that flag is not used in the final suite.

Dependency installation initially failed EACCES due network sandbox; approved retry succeeded.
Git fetch initially failed writing FETCH_HEAD; approved retry succeeded.
