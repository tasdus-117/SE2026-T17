# Unity baseline — kết quả thực tế

Baseline source: `d939568aae110668df5f0114541e4498593b6108`.
Branch: `codex/bt3-vt-066`; worktree `.worktrees/bt3-vt-066`.
Editor: `6000.6.8f1 (237b6593792b)`, Windows, batchmode + nographics.

Command (PowerShell, worktree root):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Run-Bt3Baseline.ps1
```

Run: 2026-11-14 09:54:19–09:55:37 UTC (16:54:19–16:55:37 Asia/Saigon). Exit code 0. `combined.xml`: total=19, passed=19, failed=0, skipped=0. Reported test duration 4.318424 seconds, excluding project import/startup. This duration is not the 100-operation p95 measurement.

Verified exact fixture fullnames in XML:

| Fixture | Cases | Result |
|---|---:|---|
| SmallArena.Tests.PlayMode.LobbyRoomManagementTests | 12 | Passed |
| SmallArena.Tests.PlayMode.LobbyGovernanceAndScreenTests | 7 | Passed |

Combined filter discovered both expected suites; no single-suite fallback was needed. XML lists each actual test ID. No RED/new-feature result is claimed.

License/import: log lines 13/22 contain initial validation/access-token warnings, followed by licensing connection success at 20 and `Successfully updated license` at 34. Import and compilation completed sufficiently to execute both suites. `Test run completed. Exiting with code 0 (Ok)` at log line 11464. Do not truncate the log to hide earlier warnings.

Fresh worktree was clean before copying assignment evidence/adding tooling and starting Unity. Unity import then changed the same four settings paths that were already dirty in the original checkout. Worktree-only import diff saved as `unity-import-settings.diff`; it is not a feature patch. Original checkout was not launched or modified by this run. `worktree-state.json` records separate paths/status and original settings hashes for later preservation checks. Production Scripts/Tests have not been patched.

Evidence: `combined.xml`, `combined.log`, `combined-manifest.json`, `runs.json`; manifests include exact argv, source SHA, times and exit code. License/import/discovery success applies to this actual run, not all future runs or guest multi-process E2E.
