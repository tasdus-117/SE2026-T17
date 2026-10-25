import test from 'node:test';
import assert from 'node:assert/strict';
import { strike } from '../src/combat.mjs';

test('characterization: all live HP values lose exactly one HP', () => {
  for (let health = 1; health <= 10; health++) assert.equal(strike(health, 'alice', 'alice'), health - 1);
});
test('characterization: forbidden precedes terminal conflict', () => {
  assert.throws(() => strike(0, 'mallory', 'alice'), { message: 'forbidden', status: 403 });
  assert.throws(() => strike(0, 'alice', 'alice'), { message: 'finished', status: 409 });
});
test('characterization: malformed stored health fails closed', () => {
  for (const health of [-1, 11, 0.5, NaN, '10']) assert.throws(() => strike(health, 'alice', 'alice'), /invalid health/);
});
