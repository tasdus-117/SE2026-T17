import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';
import { getDocument } from 'pdfjs-dist/legacy/build/pdf.mjs';

process.chdir(fileURLToPath(new URL('.', import.meta.url)));
const pdfPath = '../submission/BT2_23001901_AD-065.pdf';
const pdf = await getDocument({ data: new Uint8Array(readFileSync(pdfPath)), useSystemFonts: false }).promise;
const pages = [];
for (let number = 1; number <= pdf.numPages; number++) {
  const page = await pdf.getPage(number);
  const content = await page.getTextContent();
  const text = content.items.map(item => item.str).join(' ').replace(/\s+/g, ' ').normalize('NFC');
  const { width, height } = page.getViewport({ scale: 1 });
  if (Math.abs(width - 595.28) > 1 || Math.abs(height - 841.89) > 1) throw new Error(`Page ${number} not A4`);
  if (text.includes('\uFFFD') || /\{\{[A-Z_]+\}\}|TODO|TBD|lorem ipsum/i.test(text)) throw new Error(`Broken text or placeholder on ${number}`);
  if (number > 7 && !text.includes('Phụ lục')) throw new Error(`Unmarked appendix ${number}`);
  pages.push({ number, text, width, height });
}
const requireText = (text, value) => { if (!text.includes(value)) throw new Error(`Missing ${value}`); };
for (const value of ['Đồng Vũ Hoàng Long', '23001901', 'SE2026-T17', 'GAME-3D-03', 'Small Arena', 'AD-065', '3TC', 'Tôi chịu trách nhiệm về toàn bộ nội dung bài làm và đã khai báo việc sử dụng AI.']) requireText(pages[0].text, value);
requireText(pages[1].text, 'UnitOfWork');
requireText(pages[2].text, 'ROLLBACK');
requireText(pages[6].text, 'Ma trận truy vết');
for (const name of ['module-view', 'runtime-view']) {
  const source = readFileSync(`../diagrams/${name}.mmd`, 'utf8');
  if (/[^\x00-\x7F]/.test(source)) throw new Error(`Non-English diagram source: ${name}`);
}
const layout = JSON.parse(readFileSync('../evidence/pdf/layout.json', 'utf8'));
if (layout.length !== pdf.numPages || layout.filter(page => page.main).length !== 7) throw new Error('Page count mismatch');
if (layout.some(page => page.overflow.length)) throw new Error('Layout overflow');
const load = JSON.parse(readFileSync('../evidence/performance/load.json', 'utf8'));
if (load.concurrency !== 764 || load.samples !== 7640 || load.errors !== 0 || !load.passed) throw new Error('Load evidence mismatch');
const raw = [...load.latencyMs].sort((left, right) => left - right);
if (raw[Math.ceil(0.95 * raw.length) - 1] !== load.p95Ms) throw new Error('p95 calculation mismatch');
for (const commit of ['b5054a61258e8a4d274fe56198dd5d0a5304124b', '1257ad73e57a238d2a86bdddb6f72c1278977541', 'db886ddf2bdd5986cd4fc5140ca2e34b46700bc8', 'fd4df4c4f13734eb812063e1b1861d4523d4a68b', '1ff103387097911312fdf0666bf993ad384a0e98']) {
  if (spawnSync('git', ['cat-file', '-e', `${commit}^{commit}`]).status !== 0) throw new Error(`Missing commit ${commit}`);
}
const fonts = spawnSync('pdffonts', [pdfPath], { encoding: 'utf8' });
if (fonts.status !== 0) throw new Error(fonts.stderr);
const fontRows = fonts.stdout.split(/\r?\n/).slice(2).filter(line => line.trim());
if (fontRows.length === 0 || fontRows.some(line => !/\byes\s+yes\s+yes\b/.test(line))) throw new Error(`Fonts not embedded/subset/unicode: ${fonts.stdout}`);
mkdirSync('../evidence/pdf', { recursive: true });
writeFileSync('../evidence/pdf/fonts.txt', fonts.stdout);
writeFileSync('../evidence/pdf/extracted-text.txt', pages.map(page => `PAGE ${page.number}\n${page.text}`).join('\n\n'));
const result = { pdf: pdfPath, pages: pdf.numPages, mainPages: 7, appendixPages: pdf.numPages - 7, a4: true, identity: true, declaration: true, unicodeText: true, embeddedUnicodeFonts: true, englishDiagramSources: true, diagramCount: 2, layoutOverflow: false, commitsExist: true, p95Recalculated: load.p95Ms, minimumDiagramPt: layout.filter(page => page.minDiagramPt).map(page => page.minDiagramPt), visualReview: 'See visual-review.md; automatic checks do not replace visual inspection.' };
writeFileSync('../evidence/pdf/quality-gates.json', JSON.stringify(result, null, 2));
console.log(JSON.stringify(result, null, 2));
await pdf.destroy();
