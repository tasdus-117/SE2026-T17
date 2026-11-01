const fs = require('fs');
const { pathToFileURL } = require('url');
const path = require('path');
const { chromium } = require('C:/Users/hoang/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const tempModules = 'C:/Users/hoang/AppData/Local/Temp/ad065-mermaid-validation/node_modules';
(async () => {
  const { marked } = await import(pathToFileURL(tempModules + '/marked/lib/marked.esm.js').href);
  const source = fs.readFileSync('BT2_23001901_AD-065.md', 'utf8');
  const diagrams = [...source.matchAll(/```mermaid\r?\n([\s\S]*?)```/g)].map(m => m[1]);
  if (diagrams.length !== 2) throw new Error('Expected exactly two Mermaid diagrams');
  const browser = await chromium.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: true });
  const page = await browser.newPage({ viewport: { width: 920, height: 1300 }, deviceScaleFactor: 1 });
  await page.setContent('<html><head><meta charset="utf-8"></head><body></body></html>');
  await page.addScriptTag({ path: tempModules + '/mermaid/dist/mermaid.js' });
  const rendered = await page.evaluate(async (diagrams) => {
    mermaid.initialize({ startOnLoad: false, theme: 'neutral', fontFamily: 'Arial', fontSize: 18, flowchart: { htmlLabels: true, nodeSpacing: 12, rankSpacing: 12, padding: 6, wrappingWidth: 160 } });
    const output = [];
    for (let i = 0; i < diagrams.length; i++) {
      const parsed = await mermaid.parse(diagrams[i]);
      const { svg } = await mermaid.render('diagram' + i, diagrams[i]);
      output.push({ type: parsed.diagramType, svg });
    }
    return output;
  }, diagrams);
  let index = 0;
  const substituted = source.replace(/```mermaid\r?\n[\s\S]*?```/g, () => '<div class="diagram">' + rendered[index++].svg + '</div>');
  const blocks = substituted.split(/<div style="page-break-before: always;">\s*<\/div>/);
  const css = `*{box-sizing:border-box}body{background:#ddd;margin:0;color:#171717;font-family:Arial,sans-serif;font-size:9.5pt;line-height:1.14}.sheet{background:white;width:186mm;margin:16px auto;padding:0;display:flow-root}h1{font-size:15pt;margin:0 0 7px}h2{font-size:12pt;margin:0 0 6px}h3{font-size:10.5pt;margin:7px 0 4px}p{margin:4px 0}table{border-collapse:collapse;width:100%;table-layout:fixed;font-size:9pt;line-height:1.12;margin:4px 0}th,td{border:1px solid #aaa;padding:3px;overflow-wrap:anywhere;vertical-align:top}pre{font-size:8.6pt;line-height:1.12;background:#f3f3f3;padding:4px;white-space:pre-wrap}code{font-family:Consolas,monospace;font-size:.94em}a{color:#17456a}blockquote{border-left:2px solid #aaa;padding-left:8px;margin:7px}svg{display:block;width:100%;height:auto;max-width:100%}.diagram{margin:5px 0}`;
  const html = '<!doctype html><html lang="vi"><meta charset="utf-8"><style>' + css + '</style><body>' + blocks.map((b,i) => '<article class="sheet" data-block="' + (i + 1) + '">' + marked.parse(b) + '</article>').join('') + '</body></html>';
  fs.writeFileSync('.assignment-validation/preview.html', html);
  await page.setContent(html);
  await page.evaluate(() => document.fonts.ready);
  const metrics = await page.locator('.sheet').evaluateAll(nodes => nodes.map((e, i) => ({ block: i + 1, heightMm: +(e.getBoundingClientRect().height / (96 / 25.4)).toFixed(1), widthMm: +(e.getBoundingClientRect().width / (96 / 25.4)).toFixed(1), textOverflow: e.scrollWidth > e.clientWidth })));
  for (let i = 0; i < rendered.length; i++) {
    fs.writeFileSync('.assignment-validation/diagram-' + (i + 1) + '.svg', rendered[i].svg);
    await page.locator('.diagram').nth(i).screenshot({ path: '.assignment-validation/diagram-' + (i + 1) + '.png' });
  }
  const result = { mermaidVersion: JSON.parse(fs.readFileSync(tempModules + '/mermaid/package.json')).version, diagrams: rendered.map(x => x.type), a4ContentHeightMm: 273, css: { bodyPt: 9.5, tablePt: 9, contentWidthMm: 186 }, blocks: metrics };
  fs.writeFileSync('.assignment-validation/audit.json', JSON.stringify(result, null, 2));
  console.log(JSON.stringify(result));
  await browser.close();
})().catch(e => { console.error(e); process.exitCode = 1; });
