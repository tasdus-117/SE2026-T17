# Lobby member search

`LobbyRoom.SearchMembers(requester, ready = null, host = null, page = 0, pageSize = 4)` is an additive **server-only API** over the existing four-member room. It has no UI or network handler. A future server adapter must derive requester from its trusted Mirror connection, never a client-supplied ID. Room operations are single-threaded.

Only current members in Waiting may query (`UnauthorizedAccessException`, before argument validation). Nullable bool filters match exactly and combine with AND. Results sort Ready first, then ConnectionId ascending before zero-based paging. Size 1–4 is accepted; negative page or invalid size throws `ArgumentOutOfRangeException`. No match/out-of-range returns an empty copy, including huge pages without overflow. Empty/offline rooms have no authorized member. Ordering holds only while data is unchanged between requests.

Legacy `Members` remains ID-ascending; Mirror message shapes and existing Ready/Start handlers remain unchanged. The API does not replace all server-side membership reads. Tests preserve the original snapshot caller after query failure, plus an explicit baseline positional decoder using Mirror's actual generated writer. No old binary client or two-process search E2E is claimed.

## Run

Use the repository-pinned Unity `6000.6.8f1`, existing Mirror and NUnit; no package installation. PowerShell at the worktree root:

```powershell
$editorRoot = 'C:/Program Files/Unity/Hub/Editor/6000.6.8f1/Editor'
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-LobbySearchTests.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-LobbySearchTests.ps1 -Load
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-DomainTests.ps1 -EditorRoot $editorRoot
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Compile-UnityAssemblies.ps1 -EditorRoot $editorRoot
& "$editorRoot/Unity.exe" -batchmode -nographics -projectPath (Get-Location).Path -runTests -testPlatform PlayMode -testFilter 'SmallArena.Tests.PlayMode.LobbyMemberSearchTests;SmallArena.Tests.PlayMode.LobbyRoomManagementTests' -testResults "$PWD/TestResults/LobbySearch/Unity.xml" -logFile "$PWD/Logs/LobbySearch.log"
```

Portable runner compiles the actual LobbyRoom/LobbyMessages, substituting **only** Mirror's marker interface. Expect 18 NUnit cases and XML/log/run metadata under `TestResults/LobbySearch`. It excludes serialization/transport/engine/UI. Unity adds actual-writer/frozen-decoder C14 and 12 existing lobby regressions (31 total). `-Baseline` is exclusively a reproducible old-Members ordering seam: on test-only commit `1316e5b`, it compiles and C01 fails expected 2/actual 0; it is not a baseline search endpoint.

Load mode warms up 1000 queries, starts 200 isolated LongRunning tasks behind a common gate, repeats three populations, and checks exact expected IDs. Four-member fixture: host 0 unready, ready guests 2/7/9; page sizes 1/2/4 and ties across pages. `latency.csv`, `load-summary.txt`, `load-run.json` record release-to-completion latency including scheduling and oracle, nearest-rank p95, successes/errors/budget violations, observed peak active scopes, SHA and harness hash. Completion/gate deadline 30 s; success budget p95 <= 5 s. This is an in-process simulation, not 200 clients; room capacity stays four.

Mutation reproduction: at a verified clean commit, create a detached worktree, remove **only** ` || !member.IsHost` in `LobbyRoom.TryStart`, run the same portable command, then restore it. C11 must fail with actual True for a valid ready guest; restore must pass. Never commit the mutation.

Private execution evidence is intentionally ignored: `Artifacts/VT065/` in the verification worktree (RED/GREEN XML/logs, mutation diff and runs, raw latency, AI prompts/reviews and report). Fresh clones reproduce results using these commands; private logs/reports are not part of the feature branch. Instrumented line/branch percentages are unavailable without additional tooling; no coverage dependency was added. Behavioral assertions cover the new branches, but not a search transport/UI flow.

AI assistance: Codex proposed the plan, contract/tests, minimal method and diff review. Main workflow checked each proposal against source and execution; no teammate/human approval is implied.
