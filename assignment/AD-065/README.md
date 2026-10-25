# AD-065 — Đồng Vũ Hoàng Long (23001901)

Independent, AI-assisted authoritative combat slice for GAME-3D-03 / SE2026-T17.
The supplied SRS describes an offline Unity MVP; this assignment deliberately explores
a small future server boundary, not a full game or a production server.

## Deliverable

Only `submission/BT2_23001901_AD-065.pdf` is intended for Classroom submission.
Seven Vietnamese main pages; separately marked AI Usage Log appendix includes the
actual full supplied prompt and authentic single-session review material.

## Reproduce

Node 25.9.0 (built-in SQLite 3.51.3); no runtime package installation:

```powershell
cd assignment/AD-065/implementation
node --test test/*.test.mjs
node tools/check.mjs
node tools/load.mjs
```

Load creates 764 closed-loop tasks, a separate server process and disposable SQLite file;
50% new commands, 50% retry, 764 warm-up + 7640 measured requests. It overwrites
`evidence/performance/load.json`; regenerate the report if evidence changes.

To seed a demo database and run the loopback-only server (PowerShell):

```powershell
node --input-type=module -e "import { Store } from './src/store.mjs'; const store = new Store('arena.sqlite'); store.seed('demo'); store.close();"
$env:AD065_TOKEN = 'replace-with-local-token-at-least-16-characters'
node src/server.mjs
```

POST /v1/combat with bearer token and `{"matchId":"demo","commandId":"cmd1"}`.
See `implementation/CONTRACT.md`. Demo token maps to alice; movement, real geometry,
cooldown, client integration and multi-user authentication are outside the slice.

Report tooling: `cd ../report; npm.cmd ci` (PUPPETEER_SKIP_DOWNLOAD=true if using
the existing Chrome path in puppeteer.json). Render each Mermaid source with the
local mmdc CLI and `-p puppeteer.json -b white`, then `node build-report.mjs` and
`node validate-pdf.mjs`. Chrome path is Windows-specific and must be configured on
another runner. Mermaid CLI, marked, Puppeteer and PDF.js versions are locked.
Poppler `pdffonts`/`pdftoppm` are required for font checks and page rendering.
Tool-generated temporary pages belong under ignored `tmp/pdfs`, not submission.

## Evidence

- Baseline: `evidence/repository-baseline.md`, `source-analysis.md`.
- Before/after commits: fd4df4c / 1ff1033 (full SHA in report and source-analysis).
- Tests and actual corrected failures: `evidence/tests/`.
- Raw measured samples/environment: `evidence/performance/load.json`.
- Executed rollback narrative: `evidence/rollback/result.md`.
- AI disclosure, real prompts and review decisions: `evidence/ai/`.
- PDF extraction, fonts and layout audit: `evidence/pdf/`.
- Final audit: `verification-summary.md`.

The measured local p95 is evidence for this finite experiment only. No production,
WAN, Unity, PostgreSQL or multi-writer performance claim is made. Refactoring
baseline is newly authored AI-assisted code, not teammate technical debt.
