import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtempSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { fork } from 'node:child_process';
import { once } from 'node:events';
import { fileURLToPath } from 'node:url';
import { DatabaseSync } from 'node:sqlite';
import { Store } from '../src/store.mjs';
import { CombatService } from '../src/application.mjs';
import { createServer } from '../src/transport.mjs';
import { retainOperationalEvents } from '../src/retention.mjs';
import { policy } from '../src/config.mjs';

const token = 'local-test-token-23001901';
function fixture(context, version = 2) {
  const directory = mkdtempSync(join(tmpdir(), 'ad065-'));
  const path = join(directory, 'test.sqlite');
  const store = new Store(path, version);
  const stores = [store];
  context.after(() => {
    for (const opened of stores) { try { opened.close(); } catch {} }
    rmSync(directory, { recursive: true, force: true });
  });
  store.seed('match');
  return { store, path, stores };
}
async function serve(context, service, options = {}) {
  const server = createServer(service, { token, ...options });
  await new Promise(resolve => server.listen({ port: 0, host: '127.0.0.1', backlog: 2048 }, resolve));
  context.after(() => new Promise(resolve => server.close(resolve)));
  const post = async (body, authorization = `Bearer ${token}`) => {
    const response = await fetch(`http://127.0.0.1:${server.address().port}/v1/combat`, { method: 'POST', headers: { authorization }, body: typeof body === 'string' ? body : JSON.stringify(body) });
    return { status: response.status, body: await response.json(), observer: response.headers.get('x-observer-status') };
  };
  return { server, post };
}

test('atomic HP + dedup response + event, fault recovery and restart', context => {
  const { store, path } = fixture(context);
  const service = new CombatService(store, { beforeEvent: () => { throw new Error('disk simulation'); } });
  assert.throws(() => service.execute('match', 'cmd', 'alice'), /disk simulation/);
  assert.equal(store.snapshot('match').health, 10);
  assert.equal(store.eventCount(), 0);
  const expected = new CombatService(store).execute('match', 'cmd', 'alice');
  store.close();
  const reopened = new Store(path);
  try {
    assert.deepEqual(new CombatService(reopened).execute('match', 'cmd', 'alice'), expected);
    assert.equal(reopened.snapshot('match').health, 9);
    assert.equal(reopened.eventCount(), 1);
  } finally { reopened.close(); }
});

test('database CHECK and FK reject invalid state', context => {
  const { path } = fixture(context);
  const db = new DatabaseSync(path);
  try {
    db.exec('PRAGMA foreign_keys=ON');
    assert.throws(() => db.exec("UPDATE matches SET health=-1"), /CHECK/);
    assert.throws(() => db.exec("UPDATE matches SET health=1.5"), /CHECK/);
    assert.throws(() => db.exec("INSERT INTO commands VALUES('unknown','cmd','alice','{}')"), /FOREIGN KEY/);
  } finally { db.close(); }
});

test('ownership, range, terminal rules, scoped command identity', context => {
  const { store } = fixture(context);
  const service = new CombatService(store);
  assert.throws(() => service.execute('match', 'cmd', 'mallory'), { status: 403 });
  assert.throws(() => service.execute('absent', 'cmd', 'alice'), { status: 404 });
  store.seed('far', 'alice', 2);
  assert.throws(() => service.execute('far', 'cmd', 'alice'), /out of range/);
  for (let index = 0; index < 10; index++) service.execute('match', `cmd${index}`, 'alice');
  assert.throws(() => service.execute('match', 'extra', 'alice'), /finished/);
  assert.equal(service.execute('match', 'cmd9', 'alice').health, 0);
  assert.equal(store.snapshot('match').health, 0);
  store.seed('other');
  assert.equal(service.execute('other', 'cmd9', 'alice').health, 9);
});

test('API contract, damage spoof rejection, malformed body, observer after commit', async context => {
  const { store } = fixture(context);
  const { post } = await serve(context, new CombatService(store), { onCommitted: () => { assert.equal(store.snapshot('match').health, 9); throw new Error('observer unavailable'); } });
  assert.equal((await post({ matchId: 'match', commandId: 'cmd' }, 'wrong')).status, 401);
  assert.equal((await post('{')).status, 400);
  assert.equal((await post({ matchId: 'match', commandId: 'cmd', damage: 999 })).status, 400);
  assert.equal((await post('x'.repeat(4097))).status, 413);
  const response = await post({ matchId: 'match', commandId: 'cmd' });
  assert.equal(response.status, 200);
  assert.equal(response.observer, 'failed');
  assert.deepEqual(response.body, { version: 1, matchId: 'match', commandId: 'cmd', health: 9, status: 'playing' });
});

test('764 simultaneous HTTP retries apply one command once', async context => {
  const { store, path } = fixture(context);
  assert.equal(policy.concurrency, 764);
  const child = fork(fileURLToPath(new URL('../tools/load.mjs', import.meta.url)), ['server', path], { stdio: ['ignore', 'inherit', 'inherit', 'ipc'] });
  try {
    const [{ port }] = await once(child, 'message');
    const replies = await Promise.all(Array.from({ length: policy.concurrency }, async () => {
      const response = await fetch(`http://127.0.0.1:${port}/v1/combat`, { method: 'POST', headers: { authorization: 'Bearer load-only-token-23001901' }, body: JSON.stringify({ matchId: 'match', commandId: 'same' }) });
      return { status: response.status, body: await response.json() };
    }));
    assert.ok(replies.every(reply => reply.status === 200 && reply.body.health === 9));
    assert.equal(store.eventCount(), 1);
    assert.equal(store.snapshot('match').health, 9);
  } finally { child.send('stop'); await once(child, 'exit'); }
});

test('expand, v2 deployment, failed HTTP verification, v1 rollback on same database', async context => {
  const { store, path, stores } = fixture(context, 1);
  new CombatService(store).execute('match', 'old', 'alice');
  store.close();
  const expanded = new Store(path, 2);
  const deployed = await serve(context, new CombatService(expanded), { faultyResponse: true });
  const failed = await deployed.post({ matchId: 'match', commandId: 'new' });
  assert.notEqual(failed.body.health, expanded.snapshot('match').health);
  await new Promise(resolve => deployed.server.close(resolve));
  expanded.close();
  const rolledBack = new Store(path, 1);
  stores.push(rolledBack);
  const restored = await serve(context, new CombatService(rolledBack));
  const result = await restored.post({ matchId: 'match', commandId: 'after-rollback' });
  assert.equal(result.status, 200);
  assert.equal(result.body.health, 7);
  assert.equal(rolledBack.eventCount(), 3);
  assert.equal((await restored.post({ matchId: 'match', commandId: 'new' })).body.health, 8);
  const inspect = new DatabaseSync(path);
  assert.ok(inspect.prepare('PRAGMA table_info(events)').all().some(column => column.name === 'detail'));
  assert.equal(inspect.prepare('SELECT detail FROM events WHERE command_id=?').get('new').detail, '{"ruleset":"unarmed-1"}');
  inspect.close();
});

test('7 day retention exact boundary; permanent dedup survives purge', context => {
  const { store } = fixture(context);
  const now = Date.UTC(2026, 9, 25);
  const cutoff = now - 7 * 86400000;
  const old = new CombatService(store, { now: () => cutoff - 1 });
  const expected = old.execute('match', 'old', 'alice');
  new CombatService(store, { now: () => cutoff }).execute('match', 'boundary', 'alice');
  assert.equal(retainOperationalEvents(store, now), 1);
  assert.equal(store.eventCount(), 1);
  assert.deepEqual(new CombatService(store).execute('match', 'old', 'alice'), expected);
  assert.equal(store.snapshot('match').health, 8);
  assert.equal(policy.changeResponseHours, 12);
  assert.equal(policy.p95Ms, 4500);
});
