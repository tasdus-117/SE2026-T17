-- Initial database schema for Arena Game Server

-- Players table
CREATE TABLE IF NOT EXISTS players (
    id SERIAL PRIMARY KEY,
    username VARCHAR(255) UNIQUE NOT NULL,
    email VARCHAR(255),
    level INTEGER DEFAULT 1,
    experience INTEGER DEFAULT 0,
    wins INTEGER DEFAULT 0,
    losses INTEGER DEFAULT 0,
    win_rate FLOAT DEFAULT 0.0,
    status VARCHAR(50) DEFAULT 'Offline',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_seen_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_players_username ON players(username);
CREATE INDEX idx_players_level ON players(level DESC);
CREATE INDEX idx_players_created_at ON players(created_at DESC);

-- Sessions table
CREATE TABLE IF NOT EXISTS sessions (
    id SERIAL PRIMARY KEY,
    player_id INTEGER NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    token VARCHAR(255) UNIQUE NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP NOT NULL,
    is_active BOOLEAN DEFAULT true,
    connection_id VARCHAR(255),
    last_known_arena_id VARCHAR(255),
    last_heartbeat TIMESTAMP
);

CREATE INDEX idx_sessions_token ON sessions(token);
CREATE INDEX idx_sessions_player_id ON sessions(player_id);
CREATE INDEX idx_sessions_expires_at ON sessions(expires_at);

-- Matches table
CREATE TABLE IF NOT EXISTS matches (
    id SERIAL PRIMARY KEY,
    arena_id VARCHAR(255) NOT NULL,
    state VARCHAR(50) DEFAULT 'Waiting',
    max_players INTEGER DEFAULT 4,
    current_player_count INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    started_at TIMESTAMP,
    ended_at TIMESTAMP,
    duration_seconds INTEGER DEFAULT 600
);

CREATE INDEX idx_matches_arena_id ON matches(arena_id);
CREATE INDEX idx_matches_state ON matches(state);
CREATE INDEX idx_matches_created_at ON matches(created_at DESC);

-- Match players (many-to-many)
CREATE TABLE IF NOT EXISTS match_players (
    match_id INTEGER NOT NULL REFERENCES matches(id) ON DELETE CASCADE,
    player_id INTEGER NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    PRIMARY KEY (match_id, player_id)
);

-- Match results table
CREATE TABLE IF NOT EXISTS match_results (
    id SERIAL PRIMARY KEY,
    match_id INTEGER NOT NULL REFERENCES matches(id) ON DELETE CASCADE,
    player_id INTEGER NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    kills INTEGER DEFAULT 0,
    deaths INTEGER DEFAULT 0,
    damage INTEGER DEFAULT 0,
    damage_taken INTEGER DEFAULT 0,
    rank VARCHAR(50) DEFAULT 'None',
    is_winner BOOLEAN DEFAULT false,
    score INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_match_results_match_id ON match_results(match_id);
CREATE INDEX idx_match_results_player_id ON match_results(player_id);
CREATE INDEX idx_match_results_created_at ON match_results(created_at DESC);

-- Cheat reports table (for future use)
CREATE TABLE IF NOT EXISTS cheat_reports (
    id SERIAL PRIMARY KEY,
    player_id INTEGER NOT NULL REFERENCES players(id),
    report_type VARCHAR(100),
    description TEXT,
    evidence TEXT,
    status VARCHAR(50) DEFAULT 'Pending',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    reviewed_at TIMESTAMP,
    reviewed_by VARCHAR(255)
);

CREATE INDEX idx_cheat_reports_player_id ON cheat_reports(player_id);
CREATE INDEX idx_cheat_reports_status ON cheat_reports(status);
CREATE INDEX idx_cheat_reports_created_at ON cheat_reports(created_at DESC);

-- Server logs table (for future use)
CREATE TABLE IF NOT EXISTS server_logs (
    id SERIAL PRIMARY KEY,
    log_level VARCHAR(20),
    message TEXT,
    exception TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_server_logs_log_level ON server_logs(log_level);
CREATE INDEX idx_server_logs_created_at ON server_logs(created_at DESC);

-- Player statistics table (for caching)
CREATE TABLE IF NOT EXISTS player_stats (
    id SERIAL PRIMARY KEY,
    player_id INTEGER NOT NULL REFERENCES players(id) ON DELETE CASCADE UNIQUE,
    total_matches INTEGER DEFAULT 0,
    total_kills INTEGER DEFAULT 0,
    total_deaths INTEGER DEFAULT 0,
    total_damage INTEGER DEFAULT 0,
    average_kills FLOAT DEFAULT 0.0,
    average_damage FLOAT DEFAULT 0.0,
    last_updated TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Grants (for production - create restricted user)
-- GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO arena_app;
-- GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO arena_app;
