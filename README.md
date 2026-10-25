# GAME-3D-03: Small Arena - Multiplayer 3D Authoritative Server

[![CI Build](https://github.com/tasdus-117/SE2026-T17/workflows/CI/badge.svg)](https://github.com/tasdus-117/SE2026-T17/actions)

## Tổng quan dự án

Xây dựng arena 3D 4 người với server quyết định trạng thái chính nhằm hạn chế gian lận và sai lệch giữa client.

### Yêu cầu chính
- ✅ Lobby system
- ✅ 1 Arena (4 người chơi)
- ✅ Movement & Combat
- ✅ Authoritative hit validation
- ✅ Reconnect handling
- ✅ Latency simulation
- ✅ Protocol documentation
- ✅ Load/Latency tests
- ✅ CI/CD pipeline

### Tham số kỹ thuật
- **Concurrent users:** 775 người/tác vụ
- **p95 target:** ≤ 3.5 giây
- **Data retention:** 28 ngày
- **Response time:** 14 giờ

## Cấu trúc Project

```
SE2026-T17/
├── client/                          # Godot client
│   ├── scenes/
│   │   ├── lobby/
│   │   ├── arena/
│   │   └── ui/
│   ├── scripts/
│   │   ├── networking/
│   │   ├── gameplay/
│   │   └── utils/
│   └── project.godot
│
├── server/                          # ASP.NET Core authoritative server
│   ├── src/
│   │   ├── API/
│   │   ├── Services/
│   │   ├── Models/
│   │   ├── Database/
│   │   └── Networking/
│   └── ArenaServer.csproj
│
├── shared/                          # Protocol & common code
│   ├── protocol/
│   │   └── messages.proto
│   └── models/
│
├── tests/                           # Testing & benchmarking
│   ├── load-tests/
│   ├── latency-simulation/
│   └── cheat-tests/
│
├── docs/                            # Documentation
│   ├── PROTOCOL.md
│   ├── ARCHITECTURE.md
│   └── API.md
│
├── .github/workflows/               # CI/CD
│   ├── client-build.yml
│   └── server-build.yml
│
└── README.md
```

## Quick Start

### Client (Godot)
```bash
# Open with Godot 4.x
godot --path ./client/
```

### Server (ASP.NET Core)
```bash
cd server
dotnet restore
dotnet run
```

### Tests
```bash
cd tests/load-tests
dotnet run
```

## Kiến trúc

### Server Authoritative
- Server quản lý game state hoàn toàn
- Client gửi input, server validate
- Server broadcast state updates
- Hit detection & damage xử lý phía server

### Networking Flow
```
Client Input → Server Validate → Game State Update → Broadcast → All Clients
```

### Reconnect Strategy
- Session token tracking
- Player state restoration
- Automatic rejoin arena

## Phát triển

### Dependencies
- **Godot:** 4.2+
- **.NET:** 8.0+
- **PostgreSQL:** 14+

### Build Commands
```bash
# Client
godot --export-release Linux
godot --export-release Windows

# Server
dotnet publish -c Release

# Tests
dotnet test
```

## CI/CD Pipeline

GitHub Actions workflows:
- ✅ Build client (multi-platform)
- ✅ Build server
- ✅ Run unit tests
- ✅ Load testing
- ✅ Deploy staging

## Contributors

- Nhóm SE2026-T17 (3TC)
- Vai trò: Nhóm trưởng

## License

MIT License - See LICENSE file
