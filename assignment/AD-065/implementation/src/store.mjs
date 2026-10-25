import { DatabaseSync } from 'node:sqlite';
import { migrate } from './migrations.mjs';

export class Store {
  #db;
  #version;
  constructor(path, version = 2) {
    this.#db = new DatabaseSync(path);
    this.#version = version;
    this.#db.exec('PRAGMA foreign_keys=ON; PRAGMA journal_mode=WAL; PRAGMA synchronous=FULL; PRAGMA busy_timeout=5000;');
    migrate(this.#db, version);
  }
  seed(id, owner = 'alice', distance = 1) {
    this.#db.prepare('INSERT INTO matches(id,owner,health,distance) VALUES(?,?,10,?)').run(id, owner, distance);
  }
  transaction(operation) {
    this.#db.exec('BEGIN IMMEDIATE');
    try {
      const result = operation({
        match: id => this.#db.prepare('SELECT id,owner,health,distance FROM matches WHERE id=?').get(id),
        command: (match, id) => this.#db.prepare('SELECT actor,response FROM commands WHERE match_id=? AND id=?').get(match, id),
        save: (match, health) => this.#db.prepare('UPDATE matches SET health=? WHERE id=?').run(health, match),
        remember: (match, id, actor, response) => this.#db.prepare('INSERT INTO commands(match_id,id,actor,response) VALUES(?,?,?,?)').run(match, id, actor, JSON.stringify(response)),
        event: (match, id, now) => {
          if (this.#version === 1) this.#db.prepare("INSERT INTO events(match_id,command_id,created_at,kind) VALUES(?,?,?,'combat.v1')").run(match, id, now);
          else this.#db.prepare("INSERT INTO events(match_id,command_id,created_at,kind,detail) VALUES(?,?,?,'combat.v1',?)").run(match, id, now, '{"ruleset":"unarmed-1"}');
        },
      });
      this.#db.exec('COMMIT');
      return result;
    } catch (error) { this.#db.exec('ROLLBACK'); throw error; }
  }
  snapshot(id) { return this.#db.prepare('SELECT * FROM matches WHERE id=?').get(id); }
  eventCount() { return this.#db.prepare('SELECT count(*) count FROM events').get().count; }
  purge(cutoff) { return this.#db.prepare('DELETE FROM events WHERE created_at < ?').run(cutoff).changes; }
  close() { this.#db.close(); }
}
