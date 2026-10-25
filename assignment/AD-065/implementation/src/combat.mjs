import { Health, PlayerId, DomainError } from './domain.mjs';

export function strike(health, attacker, owner) {
  if (!new PlayerId(attacker).equals(new PlayerId(owner))) throw new DomainError('forbidden', 403);
  return new Health(health).hit().toJSON();
}
