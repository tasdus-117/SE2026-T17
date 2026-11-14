# BT cá nhân 3 — Nguyễn Chí Hoàng Tú / 23001947 / VT-066

Repository: https://github.com/tasdus-117/SE2026-T17
Baseline: `d939568aae110668df5f0114541e4498593b6108`.
Work branch: `codex/bt3-vt-066`. Không merge khi nhóm chưa review.

## Prerequisites

- Windows, Unity Hub license hợp lệ; Editor **6000.6.8f1 (237b6593792b)** với project import thành công.
- PowerShell, Python 3, Git; .NET compiler/runtime đi kèm Editor (script load dùng .NET 8.0.21 trong Editor).
- Checkout/worktree riêng; không mở cùng project bằng hai Editor. Không đưa Unity-generated settings/cache vào feature commit.
- Unity PlayMode là runner chính. `Run-DomainTests.ps1` chỉ compile Shared/Domain + 3 file EditMode cố định, không discover lobby.

## Reproduce

PowerShell tại repository root. Dùng label mới mỗi lần để giữ evidence cũ:

```powershell
$stamp = Get-Date -Format yyyyMMddHHmmss
python -X utf8 tools/Verify-Bt3Pairwise.py

powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-Bt3Tests.ps1 `
  -Label "repro-baseline-$stamp" `
  -Filter 'SmallArena.Tests.PlayMode.LobbyRoomManagementTests;SmallArena.Tests.PlayMode.LobbyGovernanceAndScreenTests'

powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-Bt3Tests.ps1 `
  -Label "repro-feature-$stamp" `
  -Filter 'SmallArena.Tests.PlayMode.LobbyStartOperationTests;SmallArena.Tests.PlayMode.LobbyStartProgressTests;SmallArena.Tests.PlayMode.LobbyStartBoundaryTests'

powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-Bt3Tests.ps1 `
  -Label "repro-regression-$stamp" `
  -Filter 'SmallArena.Tests.PlayMode.LobbyStartOperationTests;SmallArena.Tests.PlayMode.LobbyStartProgressTests;SmallArena.Tests.PlayMode.LobbyStartBoundaryTests;SmallArena.Tests.PlayMode.LobbyRoomManagementTests;SmallArena.Tests.PlayMode.LobbyGovernanceAndScreenTests;SmallArena.Tests.PlayMode.InGameMenuAndTeardownTests'

powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-Bt3Load.ps1 `
  -Rounds 20 -Label "repro-load-$stamp"
```

Results: `docs/assignments/bt3-vt-066/evidence/<label>/`. Kiểm XML đúng case IDs, exit code, source SHA và source hashes/diff, không chỉ đọc chữ Passed trong log. Combined baseline phải discover đúng hai fixtures, 12 + 7 cases; nếu thiếu suite thì chạy từng filter riêng.

T16 thực hiện 20 chu kỳ scene thật trong cùng Editor, kiểm overlay sau RoomPanel destruction và cleanup, singleton/observer giữa các phiên. CSV nằm tại `evidence/flaky/real-scene-cycles.csv`; khi tái chạy hãy archive CSV nếu cần giữ phiên cũ. Không dùng rerun che failure.

## Mutation

Sau khi commit target sạch trên branch làm việc và đóng Editor project đó:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-Bt3Mutation.ps1 `
  -Label "repro-mutation-$stamp"
```

Script chạy cùng T13(3/4/5) sạch → tạo nhánh/commit mutant tạm chỉ đổi `>` thành `>=` → mutant → quay lại branch ban đầu → khôi phục test. Script từ chối target dirty. Temporary branch được giữ để audit, không merge/push mutant. Kiểm summary `killedAtFour=true`, `targetRestored=true`; branch nộp giữ `>`.

Baseline guard ở ArenaNetworkManager.cs:203; số dòng sau patch thay đổi. OnServerAuthenticated của Mirror đặt incoming `isAuthenticated=true` trước callback; vì vậy count gồm connection mới: 3/4 admitted, 5 rejected. Mutation này kiểm capacity; T02/T03/T05 mới kiểm retry/failure.

## Evidence và phạm vi kết luận

- Baseline: `evidence/baseline`, 19/19 pass; exact suites 12+7.
- RED ban đầu compile failure: `evidence/red`, không phải behavioral RED; sửa UnityAction rồi `evidence/red-assertions` có T08/T09 và failure assertions thực sự.
- GREEN cùng filter: `evidence/green-attempt1`, 20/20; các run sau thêm regression/repeat cases, giữ log cũ.
- Recovery bổ sung: `evidence/recovery-red`, `recovery-green`, test manager mới sau startup failure ở MainMenu.
- Data coverage: pairwise-ai-v1.json lưu witness cho 32/32 feasible pairs, không phải coverage runtime.
- Review: giữ AI-before 18 cases, user xóa T17/T18, reviewed còn 16; `test-review-user-verbatim.md` và `pairwise-review-verification.json`.
- Load chính: `evidence/load-barrier100`. 100 independent room operations mỗi round, 300 requests/round cùng identity khi retry; 20 rounds. Native load/network/UI không chạy trong harness. Dependency giả là counter + delay 12–24 ms, 10% startup failure có chủ đích. Không dùng p95 harness làm p95 arena thật.
- `evidence/load` là run thăm dò trước khi thêm dispatch barrier, có peak in-flight <100; không phải workload chính để kết luận yêu cầu 100.
- Guest test T09 là client-only lifecycle characterization; virtual guests chỉ kiểm message/roster, chưa có multi-process guest UI qua scene. Host T08 dùng Unity scene loader thật.
- Project chưa cài `com.unity.testtools.codecoverage`; không báo line/branch percentage giả. Coverage theo AC/risk trong traceability; native errors/hang/Internet/reconnect chưa chứng minh.

Sinh viên tự viết reflection 250–350 từ và xác nhận cam kết trước PDF cuối. Chỉ push/PR sau quyền của sinh viên; không merge.
