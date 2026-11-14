# Traceability và coverage theo risk

Baseline `d939568aae110668df5f0114541e4498593b6108`.
Production sau review: `02db6c3affce0252e60c3abf20d8bc3f48ef11e7` (gồm feature `c4719b4e998f83c1c6eb784ea5b5c239202573d9`).
Các file test dưới đây thuộc `Assets/_Project/Tests/PlayMode/`.

| AC/risk | Case | File và assertion chính | Kết quả/evidence |
|---|---|---|---|
| AC1 quyền/readiness, combinatorial | T01 | LobbyStartOperationTests.cs, T01_Pairwise_AuthorityRosterAndDelivery, 12 rows; accept iff host + 2/4 all Ready, rejected B=0 | final-regression/results.xml; pairwise-ai-v1.json 32/32 feasible pairs |
| AC2 tight retry | T02 | LobbyStartBoundaryTests.cs, T02_RealMirrorRetry_CountsActualProductionBoundaryAndNativeLoad: B=1, native L=1, S=1 | final-regression/results.xml Passed; real Mirror messages + real Unity host scene |
| AC2 retry Loading/Playing | T03 | LobbyStartOperationTests.cs, T02_T03_T04_RetryWhileLoadingAndAfterTerminal_CountsBoundaryAndCompletion; direct production operation callbacks, B=1/fake L=1 | final-regression/results.xml Passed; does not claim Mirror dispatch during pause |
| AC4 repeated callback | T04 | Same production-operation test, Complete true then false, S=1 | final-regression/results.xml Passed |
| AC5/AC2 failure retry, late callback | T05 | LobbyStartBoundaryTests.cs, T05_RealMissingScene_RetryAndLateCompleteCannotReopenFailedOperation: actual unavailable-scene preflight; B=1, native L=0, F=1; retry+late rejected; Failed then Offline | final-regression/results.xml Passed; injected exception companion in LobbyStartOperationTests.cs |
| AC1 guest negative | T06 | LobbyRoomManagementTests.cs OnlyReadyHost_CanLoadArena_AndPlayersSpawnAfterSceneLoad; LobbyGovernanceAndScreenTests.cs StartGuard_OneReadyHostBlocked_ThenAllReady_ThenUnreadyLocksImmediately; plus T01 guest rows | baseline/combined.xml and final-regression/results.xml Passed |
| AC1 solo/not-ready negative | T07 | HostCannotStart_UntilEveryMemberIsReady; StartGuard_* and T01 solo/two_partial | baseline/combined.xml and final-regression/results.xml Passed |
| AC3 host overlay through actual scene | T08 | LobbyStartProgressTests.cs T08_HostRealScene_OverlaySurvivesRoomPanelThenCleansAtLocalReady: sceneLoaded observer sees destroyed RoomPanel + overlay before local-ready; native load=1, cleanup | red-assertions/results.xml Failed → green-attempt1/results.xml Passed; final-regression Passed |
| AC3 guest local lifecycle | T09 | LobbyStartProgressTests.cs T09_ClientOnlyHook_ServerCallbackDoesNotCompleteGuestOverlay: local start, RoomPanel destroyed, server callback cannot hide; stop cleans | final-regression/results.xml Passed; **manual client-only hooks, no native multi-process guest UI evidence** |
| AC5 readable failure | T10 | LobbyStartProgressTests.cs T10_T14_RealPreflightFailure_ReturnsReadableNoticeWithoutLoading, visible TMP text after teardown/menu, 0 native arena loads | final-regression/results.xml Passed |
| AC5 guest isolation | T11 | LobbyStartProgressTests.cs T11_GuestDisconnect_DoesNotFailServerLoad, virtual guest disconnect, server/other member continue | final-regression/results.xml Passed; guest process UI unproven |
| AC2 new session; AC5 recovery | T12 | LobbyStartOperationTests.cs T12_NewSessionCanStart_*; LobbyStartProgressTests.cs T12_StartupFailure_RecreatesMenuManagerAndAllowsNewSessionStart | recovery-red/results.xml Failed (no manager) → recovery-green/results.xml Passed; final-regression Passed |
| Capacity and >→>= mutant | T13 | LobbyStartBoundaryTests.cs T13_AuthenticatedCount_RealConnectCallbackBoundary(3/4/5); count includes incoming authenticated connection | mutation-final/clean 3/3 → mutant fails only (4) → restored 3/3; not retry evidence |
| AC5 true boundary signals | T14 | Actual unavailable scene + inactive boundary throws + existing wrong MainMenu scene rejected before Mirror | final-regression/results.xml Passed; deep native hang/crash unproven |
| AC4 spawn gate | T15 | LobbyRoomManagementTests.cs AddPlayerDuringLobby_CannotBypassTheServerSpawnGate; T05 server delegate false after Failed; real host E2E no spawn until Playing | baseline/final-regression Passed; no claim full native guest spawn before/after all failure types |
| AC3/NFC flaky/shared state | T16 | LobbyStartProgressTests.cs T16_TwentyRealSceneCycles_CheckTimingAndPersistentSharedState, 20 consecutive real scene sessions, exactly 1 observer/no stale overlay | final-regression/results.xml Passed; flaky/real-scene-cycles.csv 20/20 |

All evidence paths above are relative to `evidence/`. Full NUnit test IDs and exact command/filter/SHA/exit code are in XML/manifests. Final regression is 57 cases, not 57 independent rubric case IDs: T01 is parameterized and T16 loops 20 cycles.

## Coverage limitations and assertion strength

No line/branch percentage collected: project manifest lacks `com.unity.testtools.codecoverage`. This is a missing measurement, not a claim that PlayMode cannot support coverage. No package was added to production solely to obtain a percentage.

Risk evidence is stronger than label/bool-only tests: separate boundary/native-load/terminal counters, real preflight failure, real scene window, recovery RED, and mutant caught at exact boundary. T01's fake load count cannot establish native IO; T09's local hook cannot establish multi-process guest timing. The initial unit failure test's constant fakeLoads=0 assertion was removed after review; native zero-side-effect assertion is in integration T05.

Remaining evidence gaps: guest UI in independent process through actual scene; native load hang/crash after startup; Internet packet loss/reconnect; real arena/render/network p95; randomized suite order; thread safety if callers concurrently mutate one shared room (production is server-thread confined).

## Flaky and load

Timing risk: lifecycle window could be missed by a fixed delay. T08 uses real sceneLoaded checkpoint before Mirror ready and a deadline, repeated in T16 for 20/20 passes. Shared-state risk: persistent observer/singleton could leak across sessions; same 20 sequential full scene cycles recreate fixture/session and assert one observer/no stale UI. These are 20 cycles in one test invocation, not 20 fresh Editor invocations.

Concurrency harness: 20 rounds × 100 independent sessions, 6000 total requests; each round 100 submitted in-flight via barriers; independent identities, 3 requests per operation. 1800 successful operations + 200 expected startup failures; unexpected errors/timeouts/duplicate B/fake L/terminal = 0. Per-round p95 26.0288–32.1951 ms; first round 32.1951 ms <3000 ms. Only core production room/operation logic runs; fake loader counter+12–24 ms delay, fake Mirror marker. No claim of actual arena/network p95.
