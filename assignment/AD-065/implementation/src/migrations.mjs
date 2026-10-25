export function migrate(db, version = 2) {
  db.exec('BEGIN IMMEDIATE');
  try {
    db.exec(`CREATE TABLE IF NOT EXISTS matches (
      id TEXT PRIMARY KEY, owner TEXT NOT NULL,
      health INTEGER NOT NULL CHECK(typeof(health) = 'integer' AND health BETWEEN 0 AND 10),
      distance REAL NOT NULL CHECK(distance >= 0));
      CREATE TABLE IF NOT EXISTS commands (
      match_id TEXT NOT NULL REFERENCES matches(id), id TEXT NOT NULL,
      actor TEXT NOT NULL, response TEXT NOT NULL, PRIMARY KEY(match_id,id));
      CREATE TABLE IF NOT EXISTS events (
      id INTEGER PRIMARY KEY, match_id TEXT NOT NULL REFERENCES matches(id),
      command_id TEXT NOT NULL, created_at INTEGER NOT NULL, kind TEXT NOT NULL,
      UNIQUE(match_id,command_id));
      CREATE INDEX IF NOT EXISTS events_time ON events(created_at);`);
    const columns = db.prepare('PRAGMA table_info(events)').all();
    if (version === 2 && !columns.some(column => column.name === 'detail')) db.exec('ALTER TABLE events ADD COLUMN detail TEXT');
    db.exec('COMMIT');
  } catch (error) { db.exec('ROLLBACK'); throw error; }
}
