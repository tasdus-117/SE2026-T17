export class DomainError extends Error {
  constructor(message, status = 400) { super(message); this.status = status; }
}

export class PlayerId {
  #value;
  constructor(value) {
    if (typeof value !== 'string' || !/^[a-zA-Z0-9_-]{1,64}$/.test(value)) throw new DomainError('invalid player');
    this.#value = value;
  }
  equals(other) { return other instanceof PlayerId && this.#value === other.#value; }
  toString() { return this.#value; }
}

export class Health {
  #value;
  constructor(value) {
    if (!Number.isInteger(value) || value < 0 || value > 10) throw new Error('invalid health');
    this.#value = value;
  }
  hit() {
    if (this.#value === 0) throw new DomainError('finished', 409);
    return new Health(this.#value - 1);
  }
  toJSON() { return this.#value; }
}

export class Match {
  #state;
  constructor(state) { this.#state = state; }
  punch(actor) {
    if (!new PlayerId(this.#state.owner).equals(actor)) throw new DomainError('forbidden', 403);
    if (this.#state.distance > 1.5) throw new DomainError('out of range', 409);
    return new Health(this.#state.health).hit();
  }
}
