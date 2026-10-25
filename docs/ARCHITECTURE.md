# System Architecture

## High-Level Overview

```
┌─────────────────┐
│   Godot Client  │
│                 │
│  - UI/Scenes    │
│  - Input Collect│
│  - Rendering    │
│                 │
└────────┬────────┘
         │ WebSocket
         │ (SignalR)
         │
┌────────▼─────────────────────────────────┐
│   ASP.NET Core Authoritative Server      │
│                                          │
│  ┌────────────────────────────────────┐  │
│  │  Hubs (SignalR)                    │  │
│  │  - LobbyHub                        │  │
│  │  - ArenaHub                        │  │
│  └────────────────────────────────────┘  │
│                                          │
│  ┌────────────────────────────────────┐  │
│  │  Services                          │  │
│  │  - GameStateService                │  │
│  │  - ArenaService                    │  │
│  │  - CombatService                   │  │
│  │  - LobbyService                    │  │
│  │  - MatchmakingService              │  │
│  │  - PlayerService                   │  │
│  └────────────────────────────────────┘  │
│                                          │
│  ┌────────────────────────────────────┐  │
│  │  Database Layer (EF Core)          │  │
│  │  - Player                          │  │
│  │  - Match                           │  │
│  │  - MatchResult                     │  │
│  │  - Session                         │  │
│  └────────────────────────────────────┘  │
│                                          │
└────────────────────────────────────────────┘
         │
         │ SQL
         │
┌────────▼──────────┐
│   PostgreSQL DB   │
│                   │
│  - Players        │
│  - Matches        │
│  - Sessions       │
│  - Stats          │
│                   │
└───────────────────┘
```

## Core Components

### Client (Godot)

#### NetworkManager
- Handles WebSocket connection to server
- Sends/receives JSON messages
- Reconnection logic with exponential backoff
- Session token management

#### Player Controller
- Local player input collection
- Server state synchronization
- Animation state machine
- Combat request handling

#### Lobby Manager
- Player list display
- Ready status management
- Match waiting interface

#### Arena Manager
- Player spawning
- State updates
- Match management
- HUD updates

### Server (C#)

#### Hubs (Real-Time Communication)

**LobbyHub**
- Player matchmaking
- Ready state tracking
- Lobby broadcasts

**ArenaHub**
- Player state synchronization
- Combat validation
- Hit confirmation
- Reconnection handling

#### Services

**GameStateService**
- Maintains server-side game state
- Arena/Player controller management
- 60 Hz game loop
- Movement physics

**ArenaService**
- Arena lifecycle management
- Player spawning
- Arena state queries

**CombatService**
- Hit validation (authoritative)
- Cheat detection
- Damage calculation
- Distance validation

**LobbyService**
- Waiting player management
- Ready status tracking
- Match start conditions

**MatchmakingService**
- Automatic match creation
- Player grouping
- Arena assignment

**PlayerService**
- Authentication/Session management
- Player statistics
- Database persistence

#### Models

**Player**
- Username, level, experience
- Win/loss record
- Session management

**Match**
- Arena ID, state, duration
- Player list
- Match results

**Session**
- Token-based authentication
- Connection ID tracking
- Last heartbeat time

### Database (PostgreSQL)

**Tables:**
- `players` - Player data
- `matches` - Match history
- `match_players` - Many-to-many join
- `match_results` - Per-player match stats
- `sessions` - Active sessions

## Game Loop

### Client Side (Per Frame)
```
1. Collect Input
   ↓
2. Process Local State
   ↓
3. Send Input to Server
   ↓
4. Receive State from Server
   ↓
5. Interpolate/Extrapolate Movement
   ↓
6. Render Frame
```

### Server Side (60 Hz Tick)
```
1. Receive Client Inputs
   ↓
2. Validate Inputs (Cheat Detection)
   ↓
3. Update Player State
   ↓
4. Process Combat
   ↓
5. Check Win Conditions
   ↓
6. Broadcast State to All Clients
```

## State Synchronization

### On Join
1. Client sends JoinArena
2. Server verifies player
3. Server sends full ArenaState
4. Client spawns all players
5. Server begins sending updates

### During Match
1. Client sends PlayerInput every frame
2. Server validates movement input
3. Server updates player position
4. Server broadcasts ArenaStateUpdate
5. Other clients interpolate positions

### On Hit
1. Client sends CombatInput with distance
2. Server validates:
   - Both players alive
   - Distance < max_range
   - Client/server distance match
3. Server applies damage
4. Server broadcasts HitValidation
5. Clients update health/state

## Anti-Cheat Strategy

### Server-Side Validation
- **Distance Check**: `distance(attacker, target) ≤ 5m`
- **Desync Detection**: `|client_distance - server_distance| ≤ 2m`
- **Speed Validation**: `speed ≤ sprint_speed + tolerance`
- **Angle Validation**: `-120° ≤ pitch ≤ 120°`
- **State Validation**: Both players must be alive and in arena

### Cheat Detection Response
- Log suspicious behavior
- Reject invalid action
- Notify attacker of rejection
- Optional: Flag player for review

## Reconnection

### Timeout Values
- Session expiration: 60 minutes
- Arena player timeout: 30 seconds
- Last heartbeat check: Every 5 seconds

### Reconnection Flow
```
1. Client detects disconnect
2. Client waits 2 seconds
3. Client attempts reconnect
4. Server matches token to player
5. Server finds player's arena
6. Server sends full state
7. Match resumes
```

## Database Schema

```sql
CREATE TABLE players (
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

CREATE TABLE matches (
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

CREATE TABLE match_results (
  id SERIAL PRIMARY KEY,
  match_id INTEGER REFERENCES matches(id),
  player_id INTEGER REFERENCES players(id),
  kills INTEGER DEFAULT 0,
  deaths INTEGER DEFAULT 0,
  damage INTEGER DEFAULT 0,
  rank VARCHAR(50) DEFAULT 'None',
  is_winner BOOLEAN DEFAULT false,
  score INTEGER DEFAULT 0,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE sessions (
  id SERIAL PRIMARY KEY,
  player_id INTEGER REFERENCES players(id),
  token VARCHAR(255) UNIQUE NOT NULL,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  expires_at TIMESTAMP NOT NULL,
  is_active BOOLEAN DEFAULT true,
  connection_id VARCHAR(255),
  last_known_arena_id VARCHAR(255),
  last_heartbeat TIMESTAMP
);
```

## Performance Optimization

### Server-Side
- In-memory arena storage (Dictionary)
- 60 Hz update tick (no busy-wait)
- Efficient player lookup (O(1))
- Lazy session cleanup

### Network
- Binary protocol ready (can switch from JSON)
- Incremental state updates
- Configurable update rate
- Latency simulation support

### Database
- Indexed queries on username, token
- Batch match result inserts
- Connection pooling
- Prepared statements (EF Core)

## Deployment

### Production Considerations
- Use managed PostgreSQL (AWS RDS, Azure Database)
- Configure HTTPS/WSS
- Set up monitoring/logging
- Implement rate limiting
- Add DDoS protection
- Load balance if needed
- Database backups
- Session persistence

### Configuration
- Update `appsettings.json` for prod
- Set `ASPNETCORE_ENVIRONMENT=Production`
- Use environment variables for secrets
- Configure CORS properly
