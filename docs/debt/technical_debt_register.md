# Technical debt register

Audited in Task 1.7. Every open item has a stable ID, observed evidence, impact,
repayment trigger and accountable role. This register does not waive acceptance
criteria. The milestone tag identifies a structural checkpoint, not full M1
Editor/build sign-off. Evidence links are relative to this file.

| ID | Debt item | Status | Evidence | Impact | Repayment trigger | Owner |
| --- | --- | --- | --- | --- | --- | --- |
| TD-001 | Editor baseline documentation differs from the earlier proposal | Open - documentation alignment | Task 1.1 explicitly selects the local Editor; `ProjectSettings/ProjectVersion.txt` pins 6000.6.7f1, while `srs.md` Part 2 and `system_engineering.md` still describe 6000.3. The actual baseline is recorded in `docs/setup.md` and the M1 report. | A teammate following only the older proposal can choose a different Editor/package baseline. | Before another workstation imports the project or full M1 sign-off: reconcile both specification documents with the approved installed-patch decision and retain its history. | PM |
| TD-002 | Local Editor license blocks native verification | Open - blocking verification debt | Latest native initialization attempt exits 198 with `No valid Unity Editor license found. Please activate your license.` See [retained license excerpt](../testing/evidence/M1/native-license-excerpt.txt), source log/hash in the evidence summary, and `task_1_6_verification.md`. | Native PlayMode input/scene/startup tests, Unity import, clean-checkout verification and a Windows build have no passing evidence. Independent compilation and saved-file checks cannot close these gates. | Activate a valid local license, rerun targeted native suites, retain NUnit XML/logs, reproduce a clean checkout and Windows build before full M1 acceptance. | Lead Engineer |
| TD-003 | Package/import/build reproducibility evidence remains incomplete | Open - verification debt dependent on TD-002 | `Packages/manifest.json` and lockfile are pinned; Roslyn uses cached/template package DLLs. Metadata and render/reference graphs are validated from saved files. No successful Package Manager import or standalone build has been observed. | Native API/import differences, metadata migration or build-only errors remain unverified. | On the first licensed clean checkout: resolve/import the pinned project, review lockfile/meta changes, compile in Unity and produce a Windows smoke build with commit/version records. | Build Engineer |

M2-M7 locomotion, Health/HUD, combat, pickup and full victory behavior are planned
scope, not hidden M1 implementation debt. The engineering manual's prospective
DEBT-01/02/03 gameplay items have not been implemented and are not reported as
incurred debt here. Quantitative NFR targets remain unmeasured.

Closing an item requires an evidence path, resolving commit and review date in
its row. Owners are engineering roles because individual assignees are not
specified in the repository. Current evidence and deferred gates are consolidated
in [M1-verification.md](../testing/M1-verification.md).
