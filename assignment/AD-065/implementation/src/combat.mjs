export function strike(health, attacker, owner) {
  if (attacker !== owner) throw Object.assign(new Error('forbidden'), { status: 403 });
  if (!Number.isInteger(health) || health < 0 || health > 10) throw new Error('invalid health');
  if (health === 0) throw Object.assign(new Error('finished'), { status: 409 });
  return Math.max(0, health - 1);
}
