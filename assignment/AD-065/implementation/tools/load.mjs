import { fork } from 'node:child_process';
import { once } from 'node:events';
import { mkdtempSync, rmSync, writeFileSync, mkdirSync } from 'node:fs';
import os from 'node:os';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { policy } from '../src/config.mjs';
import { Store } from '../src/store.mjs';
import { CombatService } from '../src/application.mjs';
import { createServer } from '../src/transport.mjs';

const token = 'load-only-token-23001901';
if (process.argv[2] === 'server') {
  const store = new Store(process.argv[3]);
  for (let index = 0; index < policy.concurrency; index++) store.seed(`m${index}`);
  const server = createServer(new CombatService(store), { token });
  server.listen({ port: 0, host: '127.0.0.1', backlog: 2048 }, () => process.send({ port: server.address().port }));
  process.on('message', () => server.close(() => { store.close(); process.exit(0); }));
} else {
  const directory = mkdtempSync(join(os.tmpdir(), 'ad065-load-'));
  const child = fork(fileURLToPath(import.meta.url), ['server', join(directory, 'load.sqlite')], { stdio: ['ignore', 'inherit', 'inherit', 'ipc'] });
  try {
    const [{ port }] = await once(child, 'message');
    const samples = [];
    let errors = 0;
    const post = async (index, commandId, measured) => {
      const start = performance.now();
      try {
        const response = await fetch(`http://127.0.0.1:${port}/v1/combat`, { method: 'POST', headers: { authorization: `Bearer ${token}` }, body: JSON.stringify({ matchId: `m${index}`, commandId }), signal: AbortSignal.timeout(15000) });
        const body = await response.json();
        if (response.status !== 200 || !Number.isInteger(body.health)) errors++;
      } catch { errors++; }
      if (measured) samples.push(performance.now() - start);
    };
    const warmStart = performance.now();
    await Promise.all(Array.from({ length: policy.concurrency }, (_, index) => post(index, 'warmup', false)));
    const warmupMs = performance.now() - warmStart;
    const warmupErrors = errors;
    errors = 0;
    const startedAt = new Date().toISOString();
    const start = performance.now();
    await Promise.all(Array.from({ length: policy.concurrency }, async (_, index) => {
      for (let round = 0; round < 10; round++) await post(index, `cmd${Math.floor(round / 2)}`, true);
    }));
    const durationMs = performance.now() - start;
    const raw = [...samples];
    samples.sort((left, right) => left - right);
    const percentile = percent => samples[Math.ceil(percent * samples.length) - 1];
    const result = { startedAt, environment: { node: process.version, sqlite: '3.51.3', cpu: os.cpus()[0].model.trim(), logicalCores: os.availableParallelism(), memoryBytes: os.totalmem(), os: `${os.platform()} ${os.release()}`, database: 'SQLite file, WAL, synchronous FULL, one server process', client: 'separate Node process on same machine, loopback HTTP' }, concurrency: policy.concurrency, concurrencyModel: '764 closed-loop tasks, one outstanding HTTP request each; no think time', mix: '50% new commands, 50% same-command retries; 5 new commands per match, fixed owner and distance', warmupMs, warmupSamples: policy.concurrency, warmupErrors, durationMs, samples: samples.length, errors, errorRate: errors / samples.length, throughputPerSecond: samples.length / (durationMs / 1000), p50Ms: percentile(0.50), p95Ms: percentile(0.95), p99Ms: percentile(0.99), calculation: 'nearest-rank: sorted[ceil(p*N)-1], request start through JSON response including failures', targetMs: policy.p95Ms, passed: percentile(0.95) <= policy.p95Ms && errors === 0, limitations: 'Short finite local experiment; not open-loop, not WAN, no Unity physics, no PostgreSQL, no production capacity claim', latencyMs: raw };
    mkdirSync('../evidence/performance', { recursive: true });
    writeFileSync('../evidence/performance/load.json', JSON.stringify(result, null, 2));
    console.log(JSON.stringify({ ...result, latencyMs: '[raw samples saved in load.json]' }, null, 2));
    if (!result.passed) process.exitCode = 1;
  } finally {
    child.send('stop');
    await once(child, 'exit');
    rmSync(directory, { recursive: true, force: true });
  }
}
