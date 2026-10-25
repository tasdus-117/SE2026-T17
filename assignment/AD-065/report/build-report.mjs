import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { resolve, join } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import puppeteer from 'puppeteer';
import { marked } from 'marked';

process.chdir(fileURLToPath(new URL('.', import.meta.url)));
const target = resolve('../submission/BT2_23001901_AD-065.pdf');
mkdirSync('../submission', { recursive: true });
mkdirSync('../../../tmp/pdfs', { recursive: true });
const read = path => readFileSync(path, 'utf8').replace(/^\uFEFF/, '');
const escape = text => text.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;');
const svg = name => read(`../diagrams/${name}.svg`).replace(/<\?xml[^>]*>/g, '');
const source = read('report.md');
const sections = source.split('<!-- page -->');
if (sections.length !== 7) throw new Error('Expected exactly seven main sections');
const body = sections.map((section, index) => {
  const content = marked.parse(section.replace('{{MODULE_DIAGRAM}}', `<figure class="module">${svg('module-view')}</figure>`).replace('{{RUNTIME_DIAGRAM}}', `<figure class="runtime">${svg('runtime-view')}</figure>`));
  return `<section class="sheet main" data-main="${index + 1}"><div class="content">${content}</div><footer>Đồng Vũ Hoàng Long · AD-065<span>Nội dung chính ${index + 1}/7</span></footer></section>`;
}).join('');
const appendixIntro = marked.parse(read('../evidence/ai/usage-log.md'));
const reviewPrompt = read('../evidence/ai/design-review-prompt.md');
const reviewOutput = read('../evidence/ai/design-review-output.md');
const original = read('../evidence/ai/original-user-prompt.md');
const html = `<!doctype html><html lang="vi"><head><meta charset="UTF-8"><style>
@page { size:A4 portrait; margin:0; }
* { box-sizing:border-box; }
body { margin:0; font-family:Arial,sans-serif; color:#172734; font-size:10.4pt; line-height:1.32; }
.sheet { width:210mm; height:297mm; padding:15mm 15mm 16mm; break-after:page; position:relative; }
.sheet:last-child { break-after:auto; }
.content { height:261mm; overflow:visible; }
h1 { font-size:18pt; line-height:1.16; color:#163d58; margin:0 0 4mm; padding-bottom:2.5mm; border-bottom:1.4pt solid #24758b; }
h2 { font-size:12pt; line-height:1.2; margin:4mm 0 2mm; color:#20546a; }
h3 { font-size:10.5pt; margin:3mm 0 1mm; }
p { margin:0 0 2.6mm; }
blockquote { background:#edf4f7; margin:3mm 0; padding:3mm 4mm; border-left:3pt solid #24758b; }
blockquote p { margin:0; }
strong { color:#163d58; }
code { font-family:Consolas,monospace; font-size:8.5pt; overflow-wrap:anywhere; }
table { width:100%; border-collapse:collapse; font-size:9.5pt; line-height:1.28; margin:2mm 0 3mm; table-layout:fixed; }
th { text-align:left; background:#e7eff4; font-weight:bold; }
td,th { vertical-align:top; border:0.5pt solid #becdd5; padding:2mm; overflow-wrap:anywhere; }
figure { margin:1mm 0 2mm; display:flex; justify-content:center; }
figure svg { display:block; width:100%; height:auto; max-width:100% !important; }
figure.module { height:153mm; }
figure.runtime { height:193mm; }
figure svg { max-height:100%; }
footer { position:absolute; bottom:9mm; left:15mm; right:15mm; border-top:0.5pt solid #b5c7d1; padding-top:2mm; color:#526b78; font-size:8pt; }
footer span { float:right; }
.appendix { font-size:10pt; }
.appendix .content { height:258mm; }
.appendix .content h1 { font-size:16pt; }
.appendix p { margin-bottom:2mm; }
.appendix pre { font-family:Arial,sans-serif; font-size:9.3pt; line-height:1.25; margin:0; white-space:pre-wrap; overflow-wrap:anywhere; }
.appendix pre p { margin:0 0 1.8mm; }
.main[data-main="3"] { font-size:10pt; }
.main[data-main="3"] p { margin-bottom:2mm; }
.main[data-main="5"] table { font-size:9.5pt; }
.main[data-main="7"] table { font-size:9pt; }
</style></head><body>${body}<div id="appendix-source" style="display:none">${appendixIntro}</div></body></html>`;
const htmlPath = resolve('../../../tmp/pdfs/report.html');
writeFileSync(htmlPath, html);
const options = JSON.parse(read('puppeteer.json'));
const browser = await puppeteer.launch({ ...options, headless: true });
try {
  const page = await browser.newPage();
  await page.goto(pathToFileURL(htmlPath).href);
  await page.evaluate(async () => document.fonts.ready);
  const createAppendix = async (title, paragraphs, preformatted = false) => page.evaluate(({ title, paragraphs, preformatted }) => {
    let sheet;
    let content;
    const newPage = () => {
      sheet = document.createElement('section'); sheet.className = 'sheet appendix';
      content = document.createElement('div'); content.className = 'content';
      const heading = document.createElement('h1'); heading.textContent = title; content.append(heading);
      sheet.append(content);
      const footer = document.createElement('footer'); footer.textContent = 'AD-065 · Phụ lục AI Usage Log · Ngoài 7 trang nội dung chính'; sheet.append(footer);
      document.body.append(sheet);
    };
    newPage();
    for (const paragraph of paragraphs) {
      const block = document.createElement(preformatted ? 'pre' : 'div');
      if (preformatted) block.textContent = paragraph; else block.innerHTML = paragraph;
      content.append(block);
      if (block.getBoundingClientRect().bottom > content.getBoundingClientRect().bottom) {
        block.remove(); newPage(); content.append(block);
        if (block.getBoundingClientRect().bottom > content.getBoundingClientRect().bottom) throw new Error('Appendix paragraph too tall');
      }
    }
  }, { title, paragraphs, preformatted });
  const introParts = await page.evaluate(() => [...document.querySelector('#appendix-source').children].map(element => element.outerHTML));
  await createAppendix('Phụ lục A · AI Usage Log', introParts);
  await createAppendix('Phụ lục B · Prompt và output review thực', [reviewPrompt, ...reviewOutput.split(/\r?\n\r?\n/)], true);
  const promptParts = original.split(/\r?\n/).filter(line => line.trim()).map(line => `${line}\n`);
  await createAppendix('Phụ lục C · Nguyên văn chỉ dẫn được cung cấp', promptParts, true);
  const audit = await page.evaluate(() => {
    const sheets = [...document.querySelectorAll('.sheet')];
    return sheets.map((sheet, index) => {
      const content = sheet.querySelector('.content');
      const bounds = content.getBoundingClientRect();
      const elements = [...content.querySelectorAll('p,table,h1,h2,figure,pre,div')];
      const overflow = elements.filter(element => element.getBoundingClientRect().bottom > bounds.bottom + 1 || element.getBoundingClientRect().right > bounds.right + 1).map(element => element.textContent.slice(0, 100));
      const diagramText = [...content.querySelectorAll('svg text')].map(element => {
        const style = getComputedStyle(element);
        const matrix = element.getScreenCTM();
        return { text: element.textContent, printedPt: parseFloat(style.fontSize) * Math.hypot(matrix.a, matrix.b) * 72 / 96 };
      });
      return { page: index + 1, main: sheet.classList.contains('main'), overflow, minDiagramPt: diagramText.length ? Math.min(...diagramText.map(text => text.printedPt)) : null, diagramText };
    });
  });
  mkdirSync('../evidence/pdf', { recursive: true });
  writeFileSync('../evidence/pdf/layout.json', JSON.stringify(audit, null, 2));
  const failed = audit.filter(page => page.overflow.length);
  if (failed.length) throw new Error(`Layout overflow: ${JSON.stringify(failed.map(({page,overflow})=>({page,overflow})))}`);
  const diagramFailure = audit.filter(page => page.minDiagramPt && page.minDiagramPt < 8);
  if (diagramFailure.length) throw new Error(`Diagram too small: ${JSON.stringify(diagramFailure.map(({page,minDiagramPt})=>({page,minDiagramPt})))}`);
  await page.pdf({ path: target, format: 'A4', printBackground: true, preferCSSPageSize: true, tagged: true });
  console.log(JSON.stringify({ target, mainPages: 7, totalPages: audit.length, minDiagramPt: audit.filter(page => page.minDiagramPt).map(page => page.minDiagramPt) }));
  writeFileSync('../../../tmp/pdfs/report-final.html', await page.content());
} finally { await browser.close(); }
