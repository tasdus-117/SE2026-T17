import { readFileSync, existsSync, writeFileSync } from 'node:fs';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

process.chdir(fileURLToPath(new URL('.', import.meta.url)));
const normalize = text => text.replace(/\r\n/g, '\n').trim();
const original = readFileSync('../../../../AD-065-prompt.md', 'utf8');
const logged = readFileSync('../evidence/ai/original-user-prompt.md', 'utf8');
if (normalize(original) !== normalize(logged)) throw new Error('Original prompt mismatch');
const base = 'fd4df4c4f13734eb812063e1b1861d4523d4a68b';
const characterizationPath = 'assignment/AD-065/implementation/test/characterization.test.mjs';
const before = spawnSync('git', ['show', `${base}:${characterizationPath}`], { encoding: 'utf8' });
if (before.status !== 0 || normalize(before.stdout) !== normalize(readFileSync('../implementation/test/characterization.test.mjs', 'utf8'))) throw new Error('Characterization tests changed');
const files = [
  '../README.md', '../verification-summary.md', '../implementation/CONTRACT.md',
  '../diagrams/module-view.mmd', '../diagrams/runtime-view.mmd',
  '../diagrams/module-view.svg', '../diagrams/runtime-view.svg',
  '../evidence/repository-baseline.md', '../evidence/source-analysis.md',
  '../evidence/ADR-065-01.md', '../evidence/debt-register.md',
  '../evidence/ai/design-review-prompt.md', '../evidence/ai/design-review-output.md',
  '../evidence/ai/usage-log.md', '../evidence/ai/original-user-prompt.md',
  '../evidence/tests/final-tests.txt', '../evidence/tests/static.txt',
  '../evidence/tests/initial-failures.md', '../evidence/tests/baseline-characterization.txt',
  '../evidence/rollback/result.md', '../evidence/performance/load.json',
  '../evidence/pdf/quality-gates.json', '../evidence/pdf/visual-review.md',
  '../submission/BT2_23001901_AD-065.pdf',
];
for (const path of files) if (!existsSync(path)) throw new Error(`Missing output ${path}`);
const content = readFileSync('report.md', 'utf8');
for (const match of content.matchAll(/(?:src|test|tools)\/[a-z-]+\.(?:mjs|test\.mjs):(\d+)(?:[–-](\d+))?/g)) {
  const location = match[0].split(':')[0];
  const path = `../implementation/${location}`;
  if (!existsSync(path)) throw new Error(`Missing citation path ${path}`);
  const lines = readFileSync(path, 'utf8').trimEnd().split(/\r?\n/).length;
  if (Number(match[1]) > lines || (match[2] && Number(match[2]) > lines)) throw new Error(`Citation exceeds file ${match[0]}`);
}
const result = { originalPromptMatches: true, unchangedCharacterizationTests: true, requiredFilesExist: files.length, reportSourceLineReferencesWithinFiles: true, mainSectionCount: content.split('<!-- page -->').length, limitation: 'Line bounds and existence checks do not establish semantic accuracy; source and report were reviewed separately.' };
writeFileSync('../evidence/pdf/evidence-audit.json', JSON.stringify(result, null, 2));
console.log(JSON.stringify(result, null, 2));
