# Development Guide

## Project Structure

```
SE2026-T17/
├── client/                    # Godot game client
│   ├── scenes/               # Scene files (.tscn)
│   ├── scripts/              # GDScript (.gd)
│   │   ├── networking/       # Network code
│   │   ├── gameplay/         # Game logic
│   │   └── utils/           # Utilities
│   └── project.godot         # Project config
│
├── server/                    # C# backend
│   ├── src/
│   │   ├── Controllers/      # HTTP endpoints
│   │   ├── Services/         # Business logic
│   │   ├── Models/           # Data models
│   │   ├── Database/         # EF Core
│   │   ├── Hubs/            # SignalR hubs
│   │   └── Program.cs        # Entry point
│   ├── ArenaServer.csproj
│   ├── appsettings.json
│   └── Dockerfile
│
├── tests/                    # Testing & benchmarks
│   ├── LoadTest.cs
│   ├── CheatTests.cs
│   └── LatencyTests.cs
│
├── docs/                     # Documentation
│   ├── PROTOCOL.md          # Network protocol
│   ├── ARCHITECTURE.md      # System design
│   └── API.md               # REST API
│
└── docker-compose.yml        # Local dev environment
```

## Getting Started

### 1. Clone Repository
```bash
git clone https://github.com/tasdus-117/SE2026-T17.git
cd SE2026-T17
```

### 2. Setup Development Environment

#### Option A: Docker Compose (Recommended)
```bash
docker-compose up -d
```

Starts:
- PostgreSQL on port 5432
- Server on port 5000/5001
- Mounted volume for hot reload

#### Option B: Manual Setup
```bash
# Install PostgreSQL
# Linux: sudo apt-get install postgresql
# macOS: brew install postgresql
# Windows: Download from postgresql.org

# Create database
createdb arena_game

# Start server
cd server
dotnet restore
dotnet run

# In another terminal, open Godot
godot --path ./client/
```

### 3. Verify Setup
```bash
# Test server health
curl http://localhost:5000/health

# Test database connection
psql -h localhost -U postgres -d arena_game -c "SELECT version();"

# Test WebSocket
wscat -c ws://localhost:5000/hubs/lobby
```

## Development Workflow

### Server Development (C#)

#### Adding a New Feature

1. **Create Model**
```csharp
// server/src/Models/NewFeature.cs
public class NewFeature
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

2. **Add Database Context**
```csharp
// server/src/Database/GameDbContext.cs
public DbSet<NewFeature> NewFeatures { get; set; }
```

3. **Create Migration**
```bash
cd server
dotnet ef migrations add AddNewFeature
dotnet ef database update
```

4. **Create Service**
```csharp
// server/src/Services/NewFeatureService.cs
public class NewFeatureService
{
    private readonly GameDbContext _context;
    
    public async Task<NewFeature> CreateAsync(string name)
    {
        var feature = new NewFeature { Name = name };
        _context.NewFeatures.Add(feature);
        await _context.SaveChangesAsync();
        return feature;
    }
}
```

5. **Add API Endpoint**
```csharp
// server/src/Controllers/NewFeatureController.cs
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateRequest request)
{
    var result = await _service.CreateAsync(request.Name);
    return Ok(result);
}
```

6. **Register Service**
```csharp
// server/src/Program.cs
builder.Services.AddScoped<NewFeatureService>();
```

#### Testing
```bash
cd server
dotnet test

# Run specific test
dotnet test --filter "NameContains:TestName"

# With coverage
dotnet test /p:CollectCoverage=true
```

#### Code Style
- Use PascalCase for class/method names
- Use camelCase for variables
- Add XML documentation comments
- Max line length: 120 characters

### Client Development (GDScript)

#### Adding a New Scene

1. **Create Scene File**
```bash
# In Godot Editor
# Right-click in FileSystem → New Scene
# Save as: client/scenes/new_scene.tscn
```

2. **Attach Script**
```gdscript
# client/scripts/gameplay/NewScene.gd
extends Node3D

class_name NewScene

func _ready() -> void:
    print("Scene ready!")

func _process(delta: float) -> void:
    pass
```

3. **Connect to NetworkManager**
```gdscript
var network_manager: NetworkManager

func _ready() -> void:
    network_manager = get_tree().root.get_child(0).get_node("NetworkManager")
    network_manager.arena_state_received.connect(_on_arena_state)

func _on_arena_state(state: Dictionary) -> void:
    print("Received state update: %s" % state)
```

#### Testing in Editor
- Open scene in Godot Editor
- Press F5 to play
- Monitor Network logs in Output panel

#### Code Style
- Use snake_case for function/variable names
- Use UPPER_CASE for constants
- Add type hints: `var health: float = 100.0`
- Max line length: 100 characters

## Common Tasks

### Adding Authentication

1. Generate JWT token in login
2. Include in subsequent requests
3. Validate on each protected endpoint

```csharp
// Server: Validate token middleware
app.UseAuthentication();
app.UseAuthorization();

[Authorize]
[HttpGet("protected")]
public IActionResult ProtectedEndpoint()
{
    return Ok("Authenticated!");
}
```

### Adding Database Migration

```bash
cd server

# Create migration
dotnet ef migrations add MigrationName

# Apply to database
dotnet ef database update

# Revert last migration
dotnet ef migrations remove
dotnet ef database update LastGoodMigration
```

### Adding SignalR Message

1. **Server method (Hub)**
```csharp
public async Task MyMessage(string data)
{
    await Clients.All.SendAsync("MyMessage", data);
}
```

2. **Client handler (GDScript)**
```gdscript
func _on_message_received(data: String) -> void:
    print("Received: %s" % data)

func _ready() -> void:
    network_manager.my_message_signal.connect(_on_message_received)
```

### Running Load Test

```bash
cd tests
dotnet run

# With specific duration
dotnet run -- --duration 60
```

### Debugging Network Issues

```bash
# Client: Enable verbose logging
# In NetworkManager.gd, add debug prints

# Server: Check logs
docker-compose logs -f server

# Monitor network traffic
tcpdump -i lo port 5000

# Test WebSocket manually
wscat -c ws://localhost:5000/hubs/lobby
```

## Performance Profiling

### Server

```bash
# Generate CPU profile
dotnet tool install -g dotnet-counters
dotnet-counters monitor ArenaServer --counters System.Runtime

# Memory profiling
dotnet tool install -g dotrace
dotrace collect -- dotnet run
```

### Client (Godot)

```
Debugger → Profiler
→ Select "Network" to profile WebSocket calls
→ Select "Memory" to check memory usage
```

## Git Workflow

### Branch Naming
- Feature: `feature/login-system`
- Bug fix: `fix/websocket-crash`
- Docs: `docs/protocol-update`

### Commit Messages
```
[CATEGORY] Brief description

- Detailed explanation
- Links to issues
- Breaking changes if any

CATEGORY: Feature, Fix, Docs, Perf, Refactor
```

### Pull Request Process
1. Create branch from `develop`
2. Make changes
3. Write tests
4. Push and create PR
5. Link related issues
6. Wait for CI checks
7. Get code review
8. Merge to `develop`
9. Deploy to staging
10. Test on staging
11. Merge `develop` → `main`

## Testing

### Unit Tests (C#)
```csharp
[Fact]
public void CombatService_ValidateAttack_WithValidDistance_ReturnsValid()
{
    // Arrange
    var attacker = new PlayerController { PlayerId = 1 };
    var target = new PlayerController { PlayerId = 2 };
    var distance = 2.5f;

    // Act
    var result = _service.ValidateAndProcessAttack(attacker, target, input, distance);

    // Assert
    Assert.True(result.IsValid);
    Assert.Equal(20f, result.Damage, 1);
}
```

### Integration Tests
```bash
# Test with real database
dotnet test --filter "IntegrationTest"
```

### Godot Tests
- Use GdUnit: https://github.com/MikeSchulze/gdUnit4
- Test scene logic independently
- Mock NetworkManager

## Release Process

### Version Bump
```bash
# Semantic versioning: MAJOR.MINOR.PATCH
# Edit version in:
# - server/ArenaServer.csproj
# - client/project.godot
# - GitHub releases

git tag -a v1.0.0 -m "Release 1.0.0"
git push origin --tags
```

### Build Artifacts
```bash
# Server
dotnet publish -c Release -o ./publish

# Client
godot --export-release Windows ./exports/arena.exe
godot --export-release Linux ./exports/arena.x86_64
```

### Release Notes
- List features added
- List bugs fixed
- List known issues
- Add upgrade instructions

## Useful Commands

### Server
```bash
dotnet run                          # Start dev server
dotnet build                        # Build project
dotnet test                         # Run tests
dotnet watch run                    # Auto-reload on changes
dotnet ef migrations list           # Show migrations
dotnet ef database drop             # Reset database
```

### Client (Godot)
```bash
godot --path ./client/              # Open editor
godot --export-release Linux        # Export for Linux
godot --script --debug              # Debug mode
```

### Database
```bash
psql -h localhost -U postgres -d arena_game
# Inside psql:
\dt                                 # List tables
SELECT * FROM players;              # Query
\d players                          # Show schema
```

### Docker
```bash
docker-compose up -d                # Start all services
docker-compose down                 # Stop all services
docker-compose logs -f server       # View server logs
docker-compose exec server bash     # Shell into container
```

## Troubleshooting

### "Connection refused" on localhost:5000
- Check if server is running: `docker-compose ps`
- Start server: `docker-compose up -d server`

### Database migration fails
- Check PostgreSQL is running
- Delete migrations if corrupt: `rm -rf server/Migrations/*`
- Recreate: `dotnet ef migrations add Init`

### WebSocket connection fails in Godot
- Check server is running and accessible
- Verify firewall allows port 5000
- Check browser console for CORS errors
- Test with `wscat`: `wscat -c ws://localhost:5000/hubs/lobby`

### High memory usage
- Check for memory leaks in game loop
- Profile with `dotnet-counters`
- Reduce player count in tests

## Resources

- [Godot Documentation](https://docs.godotengine.org)
- [.NET Documentation](https://docs.microsoft.com/dotnet)
- [PostgreSQL Documentation](https://www.postgresql.org/docs)
- [SignalR Documentation](https://docs.microsoft.com/aspnet/core/signalr)

## Getting Help

- **Issues:** Create GitHub issue with details
- **Discussions:** Use GitHub Discussions for questions
- **Discord:** [Link to team Discord if available]
- **Email:** [Team email]

## Code Review Checklist

Before submitting PR:
- [ ] Code follows style guide
- [ ] Tests added/updated
- [ ] Documentation updated
- [ ] No console errors
- [ ] No performance regressions
- [ ] Handles edge cases
- [ ] SQL queries optimized
- [ ] Network messages compressed
