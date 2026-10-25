import { DomainError, Match, PlayerId } from './domain.mjs';

export class CombatService {
  constructor(unitOfWork, { now = Date.now, beforeEvent = () => {} } = {}) {
    this.unitOfWork = unitOfWork;
    this.now = now;
    this.beforeEvent = beforeEvent;
  }
  execute(matchId, commandId, actor) {
    if (![matchId, commandId].every(value => typeof value === 'string' && /^[a-zA-Z0-9_-]{1,64}$/.test(value))) throw new DomainError('invalid identifier');
    const player = new PlayerId(actor);
    return this.unitOfWork.transaction(transaction => {
      const state = transaction.match(matchId);
      if (!state) throw new DomainError('not found', 404);
      if (state.owner !== player.toString()) throw new DomainError('forbidden', 403);
      const previous = transaction.command(matchId, commandId);
      if (previous) {
        if (previous.actor !== actor) throw new DomainError('command conflict', 409);
        return JSON.parse(previous.response);
      }
      const health = new Match(state).punch(player).toJSON();
      const result = { version: 1, matchId, commandId, health, status: health === 0 ? 'finished' : 'playing' };
      transaction.save(matchId, health);
      transaction.remember(matchId, commandId, actor, result);
      this.beforeEvent();
      transaction.event(matchId, commandId, this.now());
      return result;
    });
  }
}
