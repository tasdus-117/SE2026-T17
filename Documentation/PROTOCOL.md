# Network Protocol Documentation

## Overview
SmallArena3D uses Unity Netcode for GameObjects with an authoritative server architecture to ensure fair gameplay and prevent cheating.

## Architecture

### Client-Server Model
- **Authoritative Server**: All game-critical decisions are made on the server
- **Client Prediction**: Clients predict movement locally for responsiveness
- **Server Reconciliation**: Server validates and corrects client state when needed

### Network Topology
```
[Client 1] ----\
[Client 2] -----\
                 \---- [Server] (Authoritative)
[Client 3] -----/
[Client 4] ----/
```

## Message Types

### Connection Messages

#### Join Request
```json
{
  "messageType": "JoinRequest",
  "playerName": "string",
  "clientVersion": "string"
}
```

#### Join Response
```json
{
  "messageType": "JoinResponse",
  "accepted": "boolean",
  "clientId": "ulong",
  "reason": "string"
}
```

### Lobby Messages

#### Player Ready
```csharp
SetPlayerReadyServerRpc(ulong clientId, bool isReady)
```
- Client -> Server
- Updates player's ready status in lobby

#### Match Start
```csharp
NotifyMatchStartingClientRpc()
```
- Server -> All Clients
- Triggered when all players are ready or host forces start

### Gameplay Messages

#### Movement Update
```csharp
SendMovementStateServerRpc(Vector3 position, Quaternion rotation, Vector3 velocity, bool isGrounded)
```
- Client -> Server
- Frequency: Every frame when moving
- Purpose: Client sends predicted movement for server validation

**Validation Rules:**
- Maximum speed: 15 m/s
- Maximum position drift: 2 meters
- Position difference per frame must be reasonable

#### Position Correction
```csharp
CorrectPositionClientRpc(Vector3 correctPosition, Quaternion correctRotation)
```
- Server -> Specific Client
- Sent when server detects invalid movement
- Client must accept server's position as authoritative

#### Fire Weapon
```csharp
FireServerRpc(Vector3 origin, Vector3 direction, float clientTime)
```
- Client -> Server
- Client sends shot information
- Server performs authoritative raycast

**Validation Rules:**
- Fire rate must be respected (0.2s minimum between shots)
- Ammo count must be valid
- Raycast performed on server with lag compensation

#### Hit Notification
```csharp
NotifyHitClientRpc(Vector3 hitPoint, Vector3 hitNormal, NetworkObjectReference target)
```
- Server -> All Clients
- Sent when server validates a hit
- Used for visual effects on all clients

#### Damage Application
```csharp
TakeDamage(float damage, ulong attackerId)
```
- Server-side only
- Never called by clients directly
- All damage calculations happen on server

### Reconnection Messages

#### Disconnect Detected
- Server stores player state for 30 seconds
- Includes: position, rotation, health, score

#### Reconnect Request
- Client attempts reconnection with same client ID
- Server restores saved state if within timeout window

## Anti-Cheat Measures

### Movement Validation
1. **Speed Check**: Server validates movement speed doesn't exceed maximum
2. **Position Drift**: Server checks client position against server position periodically
3. **Teleport Detection**: Large position jumps are rejected
4. **Physics Validation**: Gravity and collision are server-authoritative

### Combat Validation
1. **Fire Rate Enforcement**: Server tracks time between shots
2. **Ammo Validation**: Server maintains authoritative ammo count
3. **Hit Detection**: Server performs own raycast, doesn't trust client
4. **Damage Values**: Server controls all damage calculations
5. **Lag Compensation**: Server adjusts for network delay

### Reconnection Security
1. **Client ID Verification**: Reconnecting client must have valid stored state
2. **Timeout Enforcement**: State expires after 30 seconds
3. **State Restoration**: Only server can restore player state

## Network Variables

### Synchronized Variables
All network variables use Unity Netcode's built-in synchronization:

```csharp
// Player Health (Server -> All Clients)
NetworkVariable<float> currentHealth

// Player Position (Server -> All Clients)
NetworkVariable<Vector3> networkPosition
NetworkVariable<Quaternion> networkRotation

// Lobby State (Server -> All Clients)
NetworkList<LobbyPlayerData> lobbyPlayers

// Match State (Server -> All Clients)
NetworkVariable<float> matchTimeRemaining
NetworkVariable<bool> matchInProgress
```

### Update Frequency
- Position/Rotation: Every frame when changed
- Health: On damage/heal events
- Lobby State: On player join/leave/ready changes
- Match State: Once per second

## Bandwidth Optimization

### Update Strategies
1. **Dead Reckoning**: Clients interpolate between position updates
2. **Event-Driven**: State only sent when changed
3. **Snapshot Interpolation**: 100ms interpolation delay for smooth movement
4. **Area of Interest**: (Future) Only send updates for nearby players

### Estimated Bandwidth
- Position Update: ~50 bytes @ 60 Hz = 3 KB/s per player
- Weapon Fire: ~30 bytes @ ~5 Hz = 150 bytes/s per player
- Total per player: ~5-10 KB/s

## Latency Handling

### Client-Side Prediction
1. Client moves immediately based on input
2. Client sends input to server
3. Server validates and sends back authoritative state
4. Client reconciles if mismatch detected

### Lag Compensation
1. Server stores recent player positions
2. On hit detection, server rewinds time based on shooter's ping
3. Performs raycast in the past where targets actually were
4. Prevents "shooting behind" fast-moving targets

### Interpolation
- Remote players rendered 100ms in the past
- Smooths out network jitter
- Acceptable trade-off for visual smoothness

## Error Handling

### Connection Errors
- **Timeout**: 10 seconds without response
- **Disconnect**: Store state for 30 seconds
- **Kicked**: Immediate cleanup, no reconnection allowed

### Validation Failures
- **Movement Cheat**: Force position correction, log warning
- **Fire Rate Cheat**: Ignore shot, log warning
- **Multiple Violations**: Potential kick (not implemented)

### State Desync
- **Periodic Validation**: Every 0.5 seconds
- **Automatic Correction**: Server forces client to correct position
- **No Player Disruption**: Correction is smooth and quick

## Security Considerations

### Server Authority
✅ **Server controls:**
- All damage calculations
- Health values
- Match state and timing
- Score and statistics
- Spawn positions
- Ammo counts

❌ **Clients cannot:**
- Deal damage directly
- Modify health
- Change match state
- Teleport
- Shoot without ammo
- Exceed fire rate

### Future Enhancements
1. **Encryption**: Add TLS/SSL for production
2. **Authentication**: Player login and account system
3. **Anti-Tamper**: Memory protection and code signing
4. **Behavioral Analysis**: Detect unusual patterns
5. **Replay System**: Record matches for review

## Testing Recommendations

### Unit Tests
- Movement validation logic
- Damage calculation
- Fire rate enforcement
- Spawn point selection

### Integration Tests
- Client-server communication
- State synchronization
- Reconnection flow
- Match lifecycle

### Load Tests
- 4 players shooting continuously
- Network saturation scenarios
- Packet loss simulation
- High latency simulation

### Cheat Tests
See CHEAT_TESTS.md for detailed test cases
