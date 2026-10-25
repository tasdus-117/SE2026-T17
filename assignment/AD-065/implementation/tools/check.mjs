import { readdirSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { spawnSync } from 'node:child_process';

let checked = 0;
for (const directory of ['src', 'test', 'tools']) {
  for (const name of readdirSync(directory).filter(name => name.endsWith('.mjs'))) {
    const path = join(directory, name);
    const result = spawnSync(process.execPath, ['--check', path], { encoding: 'utf8' });
    if (result.status !== 0) throw new Error(result.stderr);
    const source = readFileSync(path, 'utf8');
    if (/\t| +$/m.test(source)) throw new Error(`Whitespace check failed: ${path}`);
    checked++;
  }
}
console.log(`PASS: syntax and whitespace, ${checked} modules; no type checker configured`);
