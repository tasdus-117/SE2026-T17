# Network Protocol Documentation

## Overview

GAME-3D-03 uses a **server-authoritative protocol** where:
- Server maintains the single source of truth for all game state
- Client sends input, server validates and processes
- Server broadcasts state updates to all clients
- Hit detection and damage validation happens only on server

## Connection Flow

```
Client → WebSocket (SignalR) ← Server
      ↓
   Port 5000
   /hubs/lobby, /hubs/arena
```

## Message Format

All messages use JSON with the following structure:

```json
{
  "method": "MessageType",
  "data": {
    "field1": "value",
    "field2": 123
  }
}
```

## Lobby Messages

### Client → Server

#### JoinLobby
```json
{
  "method": "JoinLobby",
  "data": {
    "playerId": 1,
    "playerName": "PlayerName"
  }
}
```

#### ReadyUp / NotReady
```json
{
  "method": "ReadyUp",
  "data": {
    "playerId": 1
  }
}
```

### Server → Client

#### PlayerJoinedLobby
```json
{
  "method": "PlayerJoinedLobby",
  "data": {
    "playerId": 1,
    "playerName": "Player1",
    "totalPlayers": 2
  }
}
```

#### LobbyStatus
```json
{
  "method": "LobbyStatus",
  "data": {
    "totalPlayers": 3,
    "readyPlayers": 2,
    "canStart": false,
    "players": [
      {
        "playerId": 1,
        "playerName": "Player1",
        "isReady": true
      }
    ]
  }
}
```

## Arena Messages

### Client → Server

#### JoinArena
```json
{
  "method": "JoinArena",
  "data": {
    "arenaId": "arena_1",
    "playerId": 1
  }
}
```

#### SendPlayerInput
```json
{
  "method": "SendPlayerInput",
  "data": {
    "arenaId": "arena_1",
    "playerId": 1,
    "input": {
      "moveX": 0.5,
      "moveZ": -0.3,
      "yaw": 1.57,
      "pitch": 0.2,
      "isJumping": false,
      "isSprinting": true,
      "timestamp": 1234567890
    }
  }
}
```

#### SendCombatInput (Attack)
```json
{
  "method": "SendCombatInput",
  "data": {
    "arenaId": "arena_1",
    "playerId": 1,
    "combatInput": {
      "targetPlayerId": 2,
      "actionType": "attack",
      "timestamp": 1234567890
    },
    "clientDistance": 2.5
  }
}
```

### Server → Client

#### ArenaState
Full state update (sent on join):
```json
{
  "method": "ArenaState",
  "data": {
    "arenaId": "arena_1",
    "state": "InProgress",
    "timeRemaining": 300.5,
    "serverTimestamp": 1234567890,
    "players": [
      {
        "playerId": 1,
        "positionX": 0.0,
        "positionY": 1.0,
        "positionZ": 0.0,
        "yaw": 0.0,
        "pitch": 0.0,
        "health": 100,
        "maxHealth": 100,
        "isAlive": true,
        "kills": 0,
        "deaths": 0,
        "serverTimestamp": 1234567890
      }
    ]
  }
}
```

#### ArenaStateUpdate
Incremental state update (sent every tick):
```json
{
  "method": "ArenaStateUpdate",
  "data": {
    "arenaId": "arena_1",
    "state": "InProgress",
    "timeRemaining": 300.0,
    "serverTimestamp": 1234567891,
    "players": [
      {
        "playerId": 1,
        "positionX": 0.1,
        "positionY": 1.0,
        "positionZ": 0.05,
        "yaw": 0.05,
        "pitch": 0.0,
        "health": 100,
        "maxHealth": 100,
        "isAlive": true,
        "kills": 0,
        "deaths": 0,
        "serverTimestamp": 1234567891
      }
    ]
  }
}
```

#### HitValidation
Server validates and responds to attack:
```json
{
  "method": "HitValidation",
  "data": {
    "attackerId": 1,
    "targetId": 2,
    "damage": 22.5,
    "hitType": "critical",
    "isValid": true,
    "invalidReason": null,
    "serverTimestamp": 1234567890
  }
}
```

Failed attack:
```json
{
  "method": "HitValidation",
  "data": {
    "attackerId": 1,
    "targetId": 2,
    "damage": 0,
    "hitType": "miss",
    "isValid": false,
    "invalidReason": "Out of range (server distance: 5.5m, client reported: 5.0m)",
    "serverTimestamp": 1234567890
  }
}
```

#### PlayerDied
```json
{
  "method": "PlayerDied",
  "data": {
    "playerId": 2,
    "killerId": 1
  }
}
```

#### MatchStarted
```json
{
  "method": "MatchStarted",
  "data": {
    "matchId": 1,
    "arenaId": "arena_1",
    "playerIds": [1, 2, 3, 4],
    "durationSeconds": 600,
    "serverTimestamp": 1234567890
  }
}
```

#### MatchEnded
```json
{
  "method": "MatchEnded",
  "data": {
    "matchId": 1,
    "results": {
      "1": {
        "kills": 5,
        "deaths": 2,
        "damage": 150,
        "score": 350,
        "rank": "First"
      },
      "2": {
        "kills": 3,
        "deaths": 3,
        "damage": 100,
        "score": 250,
        "rank": "Second"
      }
    },
    "serverTimestamp": 1234567890
  }
}
```

## Update Rate

- **Lobby:** Event-driven
- **Arena:** 60 tick/second (16.67ms)
- **Player Input:** Sent every frame (variable client FPS)
- **State Broadcast:** 60 updates/second

## Latency Simulation

For testing, server can simulate network latency:

```json
{
  "Networking": {
    "SimulateLatency": true,
    "SimulatedLatencyMs": 50
  }
}
```

## Anti-Cheat Validation

### Server validates:
1. **Distance check**: Verify attacker is within range of target
2. **Distance desync**: Detect client/server position mismatch
3. **State validation**: Verify both players alive and in arena
4. **Speed hack detection**: Check for impossible movement speeds
5. **Angle anomalies**: Detect invalid pitch/yaw values

### Cheat detection response:
```json
{
  "method": "HitValidation",
  "data": {
    "isValid": false,
    "invalidReason": "Distance desynchronization detected (desync: 2.3m)",
    "serverTimestamp": 1234567890
  }
}
```

## Error Handling

All errors sent as:
```json
{
  "method": "Error",
  "data": "Error message"
}
```

## Reconnection

On unexpected disconnect:

1. Client attempts reconnection with token
2. Server restores session state
3. Client receives arena state or lobby state
4. Game resumes

```json
{
  "method": "Reconnect",
  "data": {
    "arenaId": "arena_1",
    "playerId": 1
  }
}
```

Response:
```json
{
  "method": "ReconnectSuccess",
  "data": true
}
```

## Performance Targets

- Average latency: < 50ms
- p95 latency: < 150ms
- Packet loss: < 1%
- Max concurrent connections: 775 players
- Database queries: < 10ms
