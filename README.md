# SmallArena3D - Multiplayer Arena Shooter

[![CI Build](https://github.com/SE2026-T17/SmallArena3D/workflows/CI%20Build%20and%20Test/badge.svg)](https://github.com/SE2026-T17/SmallArena3D/actions)
[![Unity Version](https://img.shields.io/badge/Unity-2022.3%20LTS-blue.svg)](https://unity3d.com/get-unity/download)
[![Netcode](https://img.shields.io/badge/Netcode-1.7.1-green.svg)](https://docs-multiplayer.unity3d.com/)

A 4-player 3D multiplayer arena shooter with authoritative server architecture designed to prevent cheating and ensure fair gameplay.

## 🎮 Features

### Core Gameplay
- **4-Player Arena Combat**: Fast-paced multiplayer action
- **Authoritative Server**: Server-side validation prevents cheating
- **Client Prediction**: Responsive controls with server validation
- **Hit Detection**: Server-authoritative hit validation with lag compensation
- **Lobby System**: Player matchmaking with ready-up system
- **Reconnection**: Graceful handling of disconnects with state restoration

### Technical Features
- **Anti-Cheat System**: 
  - Movement validation (speed, position, collision)
  - Combat validation (fire rate, ammo, damage)
  - Server-authoritative health and scoring
- **Network Optimization**:
  - Client-side prediction
  - Server reconciliation
  - Snapshot interpolation
  - Bandwidth optimization
- **Latency Simulation**: Built-in tools for network testing
- **Comprehensive Testing**: Unit tests, load tests, and cheat test cases

## 📋 Requirements

### Development
- **Unity**: 2022.3 LTS or later
- **IDE**: Visual Studio 2022 or JetBrains Rider
- **.NET**: 6.0 or later
- **OS**: Windows 10+, macOS 12+, or Ubuntu 20.04+

### Runtime
- **RAM**: 4GB minimum, 8GB recommended
- **Network**: Broadband internet connection
- **GPU**: DirectX 11 compatible

## 🚀 Quick Start

### Clone Repository
```bash
git clone https://github.com/SE2026-T17/SmallArena3D.git
cd SmallArena3D
```

### Open in Unity
1. Open Unity Hub
2. Click "Add" and select the project folder
3. Open with Unity 2022.3 LTS

### Running Locally

#### Option 1: Host and Play
1. Open the Lobby scene
2. Click Play in Unity Editor
3. Click "Host" button
4. Build and run a client to connect

#### Option 2: Dedicated Server
1. Build server: `Build > Build Server`
2. Run the server executable
3. Build client: `Build > Build Client`
4. Run client(s) and connect to server IP

### Configuration
Edit `Assets/Resources/NetworkConfig.asset`:
- Server IP and port
- Max players per arena
- Movement settings
- Anti-cheat thresholds
- Latency simulation

## 📖 Documentation

- **[Protocol Documentation](Documentation/PROTOCOL.md)**: Network architecture and message formats
- **[Anti-Cheat Tests](Documentation/CHEAT_TESTS.md)**: Test cases for cheat detection
- **[Load Testing Guide](Documentation/LOAD_TESTS.md)**: Performance and latency testing

## 🏗️ Project Structure

```
SmallArena3D/
├── Assets/
│   ├── Scenes/
│   │   ├── LobbyScene.unity          # Main lobby
│   │   └── ArenaScene.unity          # Combat arena
│   ├── Scripts/
│   │   ├── Network/                   # Network infrastructure
│   │   │   ├── GameNetworkManager.cs  # Main network controller
│   │   │   ├── NetworkConfig.cs       # Network settings
│   │   │   ├── LatencySimulator.cs    # Testing tool
│   │   │   └── ReconnectionManager.cs # Disconnect handling
│   │   ├── Lobby/                     # Lobby system
│   │   │   └── LobbyManager.cs
│   │   ├── Player/                    # Player controller
│   │   │   └── PlayerController.cs    # Movement with validation
│   │   ├── Combat/                    # Combat system
│   │   │   ├── WeaponController.cs    # Shooting mechanics
│   │   │   └── HealthSystem.cs        # Health and damage
│   │   ├── Arena/                     # Arena management
│   │   │   └── ArenaManager.cs        # Match flow
│   │   └── UI/                        # User interface
│   │       ├── LobbyUI.cs
│   │       └── GameplayHUD.cs
│   ├── Prefabs/                       # Game objects
│   ├── Materials/                     # Textures and materials
│   └── Resources/                     # Configuration assets
├── Documentation/                     # Project documentation
├── Packages/                          # Unity packages
└── ProjectSettings/                   # Unity project settings
```

## 🎯 Game Modes

### Current: Free-For-All Deathmatch
- 4 players compete
- 5-minute time limit or first to 20 kills
- Respawn after 3 seconds
- Score tracked per player

### Future Modes (Planned)
- Team Deathmatch (2v2)
- Capture the Flag
- King of the Hill

## 🔒 Anti-Cheat Features

### Movement Validation
- ✅ Speed hack detection
- ✅ Teleport prevention
- ✅ No-clip detection
- ✅ Fly hack prevention
- ✅ Position drift correction

### Combat Validation
- ✅ Fire rate enforcement
- ✅ Ammo count validation
- ✅ Damage calculation on server
- ✅ Hit detection verification
- ✅ Shoot-through-walls prevention

### Health System
- ✅ Server-authoritative health
- ✅ God mode prevention
- ✅ Respawn time enforcement

See [CHEAT_TESTS.md](Documentation/CHEAT_TESTS.md) for detailed test cases.

## 🧪 Testing

### Run Unit Tests
```bash
# In Unity Editor
Window > General > Test Runner
Run All Tests
```

### Load Testing
```bash
# Enable latency simulation in NetworkConfig
# Run multiple clients
# Monitor performance in Unity Profiler
```

See [LOAD_TESTS.md](Documentation/LOAD_TESTS.md) for comprehensive testing guide.

## 🛠️ Building

### Build Server
```bash
# In Unity Editor
Build > Build Server

# Or via command line
unity -quit -batchmode -projectPath . -executeMethod BuildScripts.BuildServer
```

### Build Client
```bash
# In Unity Editor
Build > Build Client (Windows/Linux/macOS)

# Or via command line
unity -quit -batchmode -projectPath . -executeMethod BuildScripts.BuildClient
```

### Build All
```bash
# In Unity Editor
Build > Build All
```

## 🌐 Network Settings

### Default Configuration
- **Port**: 7777
- **Max Players**: 4
- **Tick Rate**: 60 Hz
- **Timeout**: 10 seconds
- **Reconnect Window**: 30 seconds

### Firewall Configuration
Ensure the following ports are open:
- **UDP 7777**: Game traffic
- **TCP 7777**: Connection establishment (if using TCP transport)

### Port Forwarding
If hosting behind NAT, forward port 7777 (UDP/TCP) to your server.

## 🤝 Contributing

### Team Members
- Developer 1: Network infrastructure and lobby system
- Developer 2: Player controller and movement validation
- Developer 3: Combat system and hit detection
- Developer 4: UI/UX and arena management
- Developer 5: Testing and documentation

### Development Workflow
1. Create feature branch from `develop`
2. Implement feature with tests
3. Submit pull request
4. Code review by team
5. Merge to `develop`
6. Periodic releases to `main`

### Code Standards
- Follow C# naming conventions
- Add XML documentation comments
- Write unit tests for new features
- Keep functions focused and small
- Use dependency injection where appropriate

## 📊 Performance Targets

### Server
- CPU Usage: < 50%
- Memory: < 2GB
- Tick Rate: 60 Hz
- Message Processing: < 10ms

### Client
- FPS: 60+ (30 minimum)
- Input Latency: < 50ms
- Network Usage: < 50 KB/s
- Memory: < 2GB

## 🐛 Known Issues

- [ ] Interpolation stutters under extreme packet loss (>20%)
- [ ] Reconnection may fail if match ends during disconnect
- [ ] Health bars sometimes flicker during rapid damage

See [Issues](https://github.com/SE2026-T17/SmallArena3D/issues) for full list.

## 📝 License

This project is developed for educational purposes as part of SE2026 coursework.

## 🙏 Acknowledgments

- **Unity Technologies**: For Unity Engine and Netcode for GameObjects
- **SE2026 Course Staff**: For guidance and project requirements
- **Community**: For networking best practices and resources

## 📞 Contact

- **Project Repository**: https://github.com/SE2026-T17/SmallArena3D
- **Team Email**: se2026-t17@example.com
- **Discord**: SmallArena3D Development Server

## 🗺️ Roadmap

### Version 1.0 (Current)
- [x] Core networking infrastructure
- [x] Lobby system with ready-up
- [x] Player movement with validation
- [x] Combat system with hit detection
- [x] Anti-cheat measures
- [x] Reconnection system
- [x] Basic UI

### Version 1.1 (Planned)
- [ ] Team deathmatch mode
- [ ] Voice chat
- [ ] Spectator mode
- [ ] Replay system
- [ ] Leaderboards
- [ ] Player statistics

### Version 2.0 (Future)
- [ ] Multiple arenas
- [ ] Weapon variety
- [ ] Power-ups and pickups
- [ ] Character customization
- [ ] Ranked matchmaking
- [ ] Tournament system

---

**Built with ❤️ by SE2026-T17 Team**
