# Arena Client - Godot 4.2 Game Client

Multiplayer 3D game client built with Godot 4.2, featuring real-time networking and server-authoritative gameplay.

## Architecture

### Client-Server Model
- **Server authoritative:** All game logic validated by server
- **Client rendering:** Local rendering with server-provided state
- **Interpolation:** Smooth movement between server updates
- **Input prediction:** Responsive local movement with server correction

### Network Communication
- **SignalR WebSocket:** Real-time bidirectional communication
- **JSON messages:** Serialized protocol messages
- **Reconnection:** Automatic with session token

## Quick Start

### Prerequisites
```bash
# Godot 4.2.1+
godot --version

# Git (for clone)
git --version
```

### Open Project
```bash
# Clone and navigate
git clone https://github.com/tasdus-117/SE2026-T17.git
cd SE2026-T17/client

# Open with Godot Editor
godot .
```

### Run Game
```bash
# In Godot Editor
# Press F5 to play current scene
# Or click ▶ (Play) button
```

## Project Structure

```
client/
├── project.godot           # Godot project configuration
├── scenes/
│   ├── main.tscn          # Entry point scene
│   ├── lobby/
│   │   └── lobby.tscn     # Lobby UI scene
│   ├── arena/
│   │   └── arena.tscn     # Arena gameplay scene
│   ├── player.tscn        # Player character scene
│   └── ui/
│       └── hud.tscn       # Game HUD
├── scripts/
│   ├── networking/
│   │   ├── NetworkManager.gd    # WebSocket manager
│   │   └── PlayerInput.gd       # Input data class
│   ├── gameplay/
│   │   ├── Player.gd            # Player controller
│   │   ├── LobbyManager.gd      # Lobby logic
│   │   └── ArenaManager.gd      # Arena logic
│   └── utils/
│       └── Helpers.gd           # Utility functions
├── assets/
│   ├── models/                  # 3D models
│   ├── textures/                # Textures
│   ├── sounds/                  # Audio
│   └── ui/                      # UI resources
└── exports/                     # Built executables
```

## Key Features

### Real-Time Networking
```gdscript
# NetworkManager handles all communication
var network_manager = NetworkManager.new()
await network_manager.connect_to_server()
network_manager.join_lobby("MyPlayer")
```

### Player Controller
```gdscript
# Player receives server state and interpolates
var player = Player.new()
player.apply_server_state(server_state)
player.perform_attack(target)
```

### Lobby System
```gdscript
# Join lobby and wait for match
await network_manager.join_lobby("PlayerName")
network_manager.ready_up()

# Signals fire when match starts
network_manager.match_started.connect(_on_match_started)
```

### Arena Gameplay
```gdscript
# Gameplay with server-validated hits
var arena = ArenaManager.new()
await arena.spawn_player(player_id, "PlayerName", spawn_pos)
arena.process_arena_state(server_state)
```

## Controls

### Movement
- **W/A/S/D** - Move forward/left/backward/right
- **Shift** - Sprint
- **Space** - Jump

### Combat
- **Mouse Right** - Look around
- **Left Click** - Attack (when in range)

### UI
- **ESC** - Pause menu
- **Tab** - Scoreboard

## Network Protocol

See [PROTOCOL.md](../docs/PROTOCOL.md) for detailed protocol documentation.

### Key Messages

**Lobby:**
- JoinLobby
- ReadyUp / NotReady
- PlayerJoinedLobby
- MatchCanStart

**Arena:**
- JoinArena
- SendPlayerInput (movement)
- SendCombatInput (attack)
- ArenaStateUpdate
- HitValidation
- PlayerDied

## Configuration

### Networking
Edit `scripts/networking/NetworkManager.gd`:
```gdscript
var _server_url: String = "ws://localhost:5000/hubs"
var _max_reconnect_attempts: int = 5
var _reconnect_interval: float = 2.0
```

### Game Settings
Edit `scripts/gameplay/Player.gd`:
```gdscript
@export var move_speed: float = 10.0
@export var sprint_speed: float = 15.0
@export var jump_force: float = 5.0
@export var attack_range: float = 5.0
```

## Development

### Creating a New Scene

1. **Create Scene**
   - In FileSystem, right-click → New Scene
   - Save as `scenes/my_scene.tscn`

2. **Attach Script**
   - Scene → Attach Node Script
   - Save as `scripts/gameplay/MyScene.gd`

3. **Connect to Network**
   ```gdscript
   extends Node3D
   
   var network_manager: NetworkManager
   
   func _ready() -> void:
       network_manager = get_tree().root.get_child(0).get_node("NetworkManager")
       network_manager.arena_state_received.connect(_on_arena_state)
   ```

### Adding Player Animation

1. **Create AnimationPlayer**
   - Add AnimationPlayer node to player scene
   - Create animations: idle, walk, run, attack, hit, death

2. **Update Player.gd**
   ```gdscript
   var animation_player: AnimationPlayer
   
   func _ready() -> void:
       animation_player = $AnimationPlayer
   
   func _play_attack_animation() -> void:
       animation_player.play("attack")
   ```

### Debugging Network

```gdscript
# In NetworkManager.gd, enable verbose logging
func _handle_message(message: String) -> void:
    print("[Network] Received: %s" % message)
    # ... parse and handle message
```

### Testing Locally

1. **Start Server**
   ```bash
   cd server
   dotnet run
   ```

2. **Run Multiple Clients**
   ```bash
   # Terminal 1
   godot --path ./client/ --debug
   
   # Terminal 2
   godot --path ./client/ --debug
   
   # Or export and run multiple instances
   ```

## Export

### Windows
```bash
godot --export-release Windows ./exports/arena-windows.exe
```

### Linux
```bash
godot --export-release Linux ./exports/arena-linux.x86_64
```

### HTML5 (Web)
```bash
godot --export-release HTML5 ./exports/arena.html
```

## Performance

### Optimization Tips
- Use instancing for repeated objects
- LOD (Level of Detail) for distant players
- Frustum culling enabled by default
- Reduce particle effects at distance
- Compress textures (VRAM compression)
- Use MultiMeshes for identical objects

### Profiling
```
Debugger → Profiler
→ Select "Network" for message tracking
→ Select "Memory" for memory usage
→ Select "Performance" for frame timing
```

### Target Performance
- 60 FPS on target hardware
- < 500MB memory usage
- < 50MB network bandwidth per match

## Troubleshooting

### Server connection fails
- Check server is running: `dotnet run` in server directory
- Verify firewall allows port 5000
- Check console for CORS/WebSocket errors
- Test with wscat: `wscat -c ws://localhost:5000/hubs/lobby`

### Game crashes on start
- Check scene path is correct in project.godot
- Verify all script dependencies are loaded
- Check console output for GDScript errors
- Try scene in isolation (F5)

### Network lag or desync
- Check ping in Settings
- Enable latency simulation to test: `Networking.SimulateLatency = true`
- Verify server is handling load
- Check for packet loss

### Animation not playing
- Verify AnimationPlayer node exists
- Check animation names match calls
- Play animations in correct thread
- Use `yield` for animation completion

## Resources

- [Godot Documentation](https://docs.godotengine.org)
- [Godot 4.2 Release Notes](https://godotengine.org/article/godot-4-2-arrives-in-strength/)
- [SignalR Protocol](https://docs.microsoft.com/aspnet/core/signalr)
- [Protocol Documentation](../docs/PROTOCOL.md)

## Contributing

See [CONTRIBUTING.md](../CONTRIBUTING.md)

## License

MIT License - See LICENSE file
