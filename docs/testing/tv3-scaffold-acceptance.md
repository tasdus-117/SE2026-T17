# TV3 scaffold verification / #19 — 27-Nov-2026

Source: `89f2674fa993ff13e30fcd56cca46cec5333494c`; Unity project version
6000.6.8f1. Static verification only, **not Unity runtime testing**.

The existing verifier already excludes root `Assets.meta` and checks children.
No verifier/asset/settings change was necessary. Reused the eight original
positive/negative runs after reading the helper, JSON, every case log and source.

Command for every case (absolute project path appears in JSON):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File TestResults/TV3-20261127/Scaffold/tools/Verify-Scaffold.ps1 -SkipEditorCheck
```

| Case | Expected and actual diagnostic | Recorded exit |
|---|---|---|
| clean, no root Assets.meta | PASS; 7 asmdefs, 1530 metadata pairs | 0 |
| missing child metadata | Missing metadata: `_Project/Input/README.md` | 1 |
| duplicate GUID | Duplicate GUID: copied README metadata GUID | 1 |
| orphan metadata | Orphan metadata: `Assets/scaffold-fixture.txt.meta` | 1 |
| Domain asmdef engine reference enabled | Domain must exclude Unity engine references | 1 |
| MainMenu disabled | MainMenu, Bootstrap and Arena must be enabled at indices 0, 1 and 2 | 1 |
| input-system manifest version changed | Manifest/lock mismatch: com.unity.inputsystem | 1 |
| restored, no root Assets.meta | PASS; 1530 metadata pairs | 0 |

`check-scaffold.py` mutates only ignored `Scaffold`, restoring original bytes
in `finally`. Missing-input-meta fallback selected README.md.meta; that is a
real child metadata test. The duplicate log names the original GUID's second
occurrence, which is expected regardless of enumeration order.

Follow-up `python TestResults/TV3-20261127/verify-identity.py`, exit0, compared
all 2917 tracked files in restored Scaffold and baseline.zip with Git objects at
the SHA above (CRLF/LF normalized), checked eight logs and recorded exits,
absence of both fixture files and root Assets.meta. No original helper rerun.
Main working tree and existing acceptance worktree were clean at recovery.

Raw ignored evidence: [case JSON](../../TestResults/TV3-20261127/scaffold-results.json),
[helper](../../TestResults/TV3-20261127/check-scaffold.py),
[identity audit](../../TestResults/TV3-20261127/identity-audit.json),
`scaffold-{clean,missing-child,duplicate-guid,orphan,assembly,scenes,package,restored}.log`
in that directory. These files are local handoff material, absent in fresh clones;
the commands and table above are the durable record.

`-SkipEditorCheck` bypasses installed Editor resolution/version validation and
does not compile probes, import assets, build or execute native tests. It still
checks project version text, assembly graph, metadata/GUIDs, settings/URP/scenes,
package closure and Git hygiene. #27 must supply its own runtime/build gates.
Technical #19 acceptance has evidence; issue remains open for review of this
delivery. No human approval is inferred.
