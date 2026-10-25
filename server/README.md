# Arena Server - ASP.NET Core Backend

Authoritative multiplayer game server using ASP.NET Core 8.0 and SignalR.

## Architecture

### Authoritative Server Pattern
- **Single source of truth:** Server maintains all game state
- **Client input validation:** All player actions validated server-side
- **Anti-cheat:** Hit detection, distance validation, movement validation
- **State synchronization:** 60 Hz state broadcasts to connected clients

### Key Components

1. **Hubs (SignalR)**
   - `LobbyHub` - Matchmaking and lobby
   - `ArenaHub` - Real-time gameplay

2. **Services**
   - `GameStateService` - In-memory game state, 60 Hz game loop
   - `ArenaService` - Arena management
   - `CombatService` - Hit validation and anti-cheat
   - `LobbyService` - Waiting player management
   - `MatchmakingService` - Automatic match creation
   - `PlayerService` - Authentication and statistics

3. **Database (EF Core + PostgreSQL)**
   - Players, Matches, Sessions, Match Results

## Quick Start

### Prerequisites
```bash
# .NET 8.0 SDK
dotnet --version

# PostgreSQL
psql --version
```

### Local Development
```bash
# Create database
createdb arena_game

# Start server
dotnet run

# Server on http://localhost:5000
```

### Docker Compose
```bash
docker-compose up -d server

# Server on http://localhost:5000
```

## Project Structure

```
src/
├── Program.cs                 # Entry point, configuration
├── Controllers/
│   └── PlayersController.cs  # HTTP REST endpoints
├── Services/
│   ├── GameStateService.cs   # Main game loop & state
│   ├── ArenaService.cs       # Arena management
│   ├── CombatService.cs      # Hit validation & anti-cheat
│   ├── LobbyService.cs       # Lobby state
│   ├── MatchmakingService.cs # Match creation
│   └── PlayerService.cs      # Auth & stats
├── Hubs/
│   ├── LobbyHub.cs           # Matchmaking real-time
│   └── ArenaHub.cs           # Gameplay real-time
├── Models/
│   ├── Player.cs             # Player data model
│   ├── Match.cs              # Match & MatchResult
│   ├── Session.cs            # Session tokens
│   └── NetworkMessages.cs    # Protocol messages
└── Database/
    └── GameDbContext.cs      # EF Core context

appsettings.json             # Configuration
Dockerfile                    # Container image
```

## Key Features

### Authoritative Game State
```csharp
// Server processes all game logic
public class GameStateService
{
    private Arena _arena; // Single source of truth
    private Timer _gameLoop; // 60 Hz update tick
}
```

### Anti-Cheat Validation
```csharp
// Server validates every attack
public CombatResult ValidateAndProcessAttack(
    PlayerController attacker,
    PlayerController target,
    CombatInput input,
    float clientDistance)
{
    // Check distance: server-side distance check
    // Detect desync: |server_dist - client_dist| > threshold
    // Validate state: both players alive and in arena
}
```

### Real-Time Updates
```csharp
// 60 Hz game loop broadcasts state
private void GameLoop(object state)
{
    foreach (var arena in _arenas.Values)
        arena.Update(1.0 / 60.0);
    
    // Broadcast to all clients
    await Clients.All.SendAsync("ArenaStateUpdate", state);
}
```

## Configuration

### appsettings.json
```json
{
  "GameSettings": {
    "MaxPlayersPerArena": 4,
    "MatchDurationSeconds": 600,
    "TickRate": 60
  },
  "Networking": {
    "HeartbeatIntervalMs": 1000,
    "SimulateLatency": false,
    "SimulatedLatencyMs": 50
  }
}
```

### Environment Variables
```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Database=arena_game;Username=postgres;Password=postgres"
export ASPNETCORE_ENVIRONMENT=Production
export ASPNETCORE_URLS=https://0.0.0.0:5001
```

## API Endpoints

### REST API
- `POST /api/players/register` - Create player
- `POST /api/players/login` - Authenticate
- `GET /api/players/{id}` - Get profile
- `GET /api/players/leaderboard` - Top 10 players
- `GET /health` - Health check

### SignalR Hubs
- `ws://localhost:5000/hubs/lobby` - Matchmaking
- `ws://localhost:5000/hubs/arena` - Gameplay

## Development

### Build
```bash
dotnet build
```

### Run Tests
```bash
dotnet test
```

### Add Migration
```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```

### Watch Mode
```bash
dotnet watch run
```

## Performance

### Benchmarks (Target: 775 concurrent players)
- Connection spike (100 players): ~50ms avg
- Lobby throughput (200 players): 50 matches in <5s
- Match start latency: p95 < 3.5s
- Arena state update: 60 Hz (16.67ms)
- Hit validation: < 10ms

### Optimization Tips
- Use in-memory arena storage (O(1) lookup)
- Batch database operations
- Lazy session cleanup
- Connection pooling for DB
- Enable SignalR message compression

## Logging

Configured with Serilog:
```bash
# Console output
# File: logs/arena-server-YYYY-MM-DD.txt
# Log levels: Information, Warning, Error, Fatal
```

## Security

### Authentication
- Session tokens (32-char random)
- Token expiration (60 minutes)
- HTTPS for REST API
- WSS (WebSocket Secure) in production

### Anti-Cheat
- Server-side distance validation
- Desynchronization detection
- Speed hack detection
- Invalid angle detection
- Movement boundary checks

### Database
- Parameterized queries (EF Core)
- SQL injection prevention
- Prepared statements
- Connection string from env

## Deployment

### Docker
```bash
docker build -t arena-server:1.0 .
docker run -p 5000:5000 arena-server:1.0
```

### Kubernetes
See [DEPLOYMENT.md](../DEPLOYMENT.md)

### AWS / Azure
See [DEPLOYMENT.md](../DEPLOYMENT.md)

## Monitoring

### Health Check
```bash
curl http://localhost:5000/health
```

### Logs
```bash
docker-compose logs -f server
```

### Metrics (Future)
- Player count
- Match count
- Average match duration
- Hit validation pass rate
- Cheat detection rate

## Troubleshooting

### Server won't start
```bash
# Check database connection
psql -h localhost -U postgres -d arena_game -c "SELECT version();"

# Check ports
lsof -i :5000

# View detailed error
dotnet run
```

### High memory usage
```bash
# Profile with .NET
dotnet-counters monitor ArenaServer

# Check for memory leaks
dotnet-trace collect -- dotnet run
```

### WebSocket connection fails
```bash
# Test locally
wscat -c ws://localhost:5000/hubs/lobby

# Check firewall
sudo ufw allow 5000
```

## Contributing

See [CONTRIBUTING.md](../CONTRIBUTING.md)

## License

MIT License - See LICENSE file
