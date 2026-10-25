# Anti-Cheat Test Cases

## Overview
This document outlines test cases to verify the anti-cheat mechanisms in SmallArena3D.

## Test Environment Setup

### Prerequisites
1. Build server and client separately
2. Enable latency simulation for network condition testing
3. Set up logging for validation failures
4. Prepare modified client for cheat testing

### Test Configuration
```csharp
NetworkConfig settings for testing:
- maxPositionDrift = 2.0f
- movementValidationThreshold = 1.5f
- maxMovementSpeed = 15.0f
```

## Movement Cheat Tests

### TEST-MC-001: Speed Hack Detection
**Description**: Test if server detects and prevents speed hacking

**Setup**:
1. Start server and connect client
2. Modify client movement speed to 50 m/s (3x normal)

**Expected Behavior**:
- Server detects excessive speed
- Server sends position correction to client
- Warning logged: "Position drift detected"
- Player position is reset to server's authoritative position

**Pass Criteria**:
- [ ] Position correction sent within 0.5 seconds
- [ ] Client position matches server position after correction
- [ ] Warning logged in server console
- [ ] Player cannot maintain speed > maxMovementSpeed

---

### TEST-MC-002: Teleport Detection
**Description**: Verify server prevents instant teleportation

**Setup**:
1. Start server and connect client
2. Modify client to instantly change position by 20 meters

**Expected Behavior**:
- Server rejects teleport
- Position validation fails
- Client is corrected back to valid position
- Warning logged

**Pass Criteria**:
- [ ] Teleport is not accepted by server
- [ ] Client position is corrected
- [ ] No other players see the teleport

---

### TEST-MC-003: No-Clip Detection
**Description**: Test if server prevents walking through walls

**Setup**:
1. Place player next to a wall
2. Modify client to disable collision detection
3. Attempt to walk through wall

**Expected Behavior**:
- Server's physics simulation still has collision
- Server position doesn't match client position
- Position correction is sent
- Player cannot pass through wall

**Pass Criteria**:
- [ ] Player is stopped by wall on server
- [ ] Client receives position correction
- [ ] Player cannot move through obstacles

---

### TEST-MC-004: Fly Hack Detection
**Description**: Verify server prevents unauthorized flying

**Setup**:
1. Modify client to ignore gravity
2. Attempt to fly upward continuously

**Expected Behavior**:
- Server applies gravity normally
- Position drift increases
- Server corrects client position downward
- Flight is not possible

**Pass Criteria**:
- [ ] Server applies gravity correctly
- [ ] Client is corrected to ground
- [ ] Sustained flight is impossible

## Combat Cheat Tests

### TEST-CC-001: Fire Rate Hack
**Description**: Test detection of rapid-fire exploits

**Setup**:
1. Modify client to remove fire rate limit
2. Attempt to shoot 10 times per second (normal: 5/sec)

**Expected Behavior**:
- Server validates time between shots
- Excessive shots are ignored
- Warning logged: "fired too quickly - possible cheat"
- Only valid shots are processed

**Pass Criteria**:
- [ ] Fire rate is enforced server-side
- [ ] Extra shots are ignored
- [ ] Warning logged after 3 violations
- [ ] Player cannot exceed fireRate setting

---

### TEST-CC-002: Infinite Ammo
**Description**: Verify server prevents shooting without ammo

**Setup**:
1. Modify client to never decrease ammo count
2. Shoot more than maxAmmo times without reloading

**Expected Behavior**:
- Server maintains authoritative ammo count
- Server ignores shots when ammo = 0
- Warning logged: "fired without ammo"
- Client ammo display may desync but server is correct

**Pass Criteria**:
- [ ] Server ammo count is authoritative
- [ ] Shots are ignored when ammo = 0
- [ ] No damage dealt when out of ammo

---

### TEST-CC-003: Damage Multiplier
**Description**: Test if server prevents damage modification

**Setup**:
1. Modify client to send damage = 999
2. Shoot another player

**Expected Behavior**:
- Server uses its own damage value (10)
- Client-sent damage value is ignored
- Normal damage is applied
- No excessive damage detected

**Pass Criteria**:
- [ ] Server damage value is used
- [ ] Client damage value is ignored
- [ ] Damage dealt = server's damage setting
- [ ] Invalid damage values are logged

---

### TEST-CC-004: Aimbot Hit Validation
**Description**: Verify server validates hits with raycast

**Setup**:
1. Modify client to report hits without proper aiming
2. Send fake hit data to server

**Expected Behavior**:
- Server performs own raycast
- If server raycast misses, hit is rejected
- No damage is dealt
- Client's hit report is ignored

**Pass Criteria**:
- [ ] Server performs independent raycast
- [ ] Fake hits are not registered
- [ ] Only server-validated hits deal damage

---

### TEST-CC-005: Shoot Through Walls
**Description**: Test if server prevents shooting through obstacles

**Setup**:
1. Place obstacle between two players
2. Modify client to ignore raycasts hitting obstacles
3. Attempt to shoot through wall

**Expected Behavior**:
- Server raycast hits wall, not player
- No damage is dealt
- Server rejects the hit
- Wall blocks the shot

**Pass Criteria**:
- [ ] Server raycast detects wall
- [ ] No damage dealt through walls
- [ ] Hit detection respects collision layers

## Health System Cheat Tests

### TEST-HC-001: God Mode
**Description**: Verify server prevents health modification

**Setup**:
1. Modify client to set health to maximum on damage
2. Take damage from another player

**Expected Behavior**:
- Server health is authoritative
- Client health modification is ignored
- Damage is applied correctly
- Player can be killed normally

**Pass Criteria**:
- [ ] Server health is authoritative
- [ ] Client cannot modify health
- [ ] Damage reduces health correctly
- [ ] Player dies when health reaches 0

---

### TEST-HC-002: Auto-Heal
**Description**: Test if server prevents unauthorized healing

**Setup**:
1. Modify client to call Heal() function
2. Attempt to heal without pickup

**Expected Behavior**:
- Server requires authority for healing
- Client heal calls are ignored
- Health only changes by server commands
- No unauthorized healing occurs

**Pass Criteria**:
- [ ] Only server can heal players
- [ ] Client heal calls are rejected
- [ ] Health changes only through server

---

### TEST-HC-003: Instant Respawn
**Description**: Verify server controls respawn timing

**Setup**:
1. Die in-game
2. Modify client to respawn immediately

**Expected Behavior**:
- Server enforces respawn delay (3 seconds)
- Premature respawn attempts are ignored
- Player respawns only after delay
- Server controls respawn position

**Pass Criteria**:
- [ ] Respawn delay is enforced
- [ ] Early respawn attempts fail
- [ ] Server controls respawn timing and position

## Network Exploit Tests

### TEST-NE-001: Packet Flooding
**Description**: Test server handling of excessive messages

**Setup**:
1. Send 1000 movement updates per second
2. Monitor server performance

**Expected Behavior**:
- Server rate-limits messages
- Excessive messages are dropped
- Server remains stable
- Other players are not affected

**Pass Criteria**:
- [ ] Server doesn't crash
- [ ] Rate limiting is applied
- [ ] Other players maintain normal gameplay
- [ ] Flooding client is handled gracefully

---

### TEST-NE-002: Invalid Message Payloads
**Description**: Verify server handles malformed data

**Setup**:
1. Send corrupted network messages
2. Send messages with invalid values (NaN, Infinity)

**Expected Behavior**:
- Server validates message content
- Invalid messages are rejected
- Server logs warnings
- No crashes or exploits occur

**Pass Criteria**:
- [ ] Server validates all inputs
- [ ] Invalid data is rejected
- [ ] Server remains stable
- [ ] Proper error logging

---

### TEST-NE-003: Replay Attack
**Description**: Test if server prevents message replay

**Setup**:
1. Capture a "fire weapon" message
2. Replay it multiple times

**Expected Behavior**:
- Server validates fire rate
- Duplicate/old messages are ignored
- Ammo is only consumed once
- Replay is ineffective

**Pass Criteria**:
- [ ] Replayed messages don't bypass fire rate
- [ ] Server tracks message timing
- [ ] No duplicate damage from replay

## Latency Exploitation Tests

### TEST-LE-001: Artificial Latency Advantage
**Description**: Test if high latency provides unfair advantage

**Setup**:
1. Enable 500ms artificial latency on client
2. Engage in combat with low-latency player

**Expected Behavior**:
- Lag compensation works fairly
- High-latency player is not disadvantaged
- No "shooting around corners" exploits
- Both players have fair hit detection

**Pass Criteria**:
- [ ] Lag compensation functions correctly
- [ ] No unfair advantage from latency manipulation
- [ ] Hits register fairly for both players

---

### TEST-LE-002: Lag Switch Detection
**Description**: Verify detection of intentional network interruption

**Setup**:
1. Temporarily block network traffic for 2 seconds
2. Unblock and continue playing

**Expected Behavior**:
- Server detects timeout
- Player is marked as disconnected
- Movement during lag is not accepted
- Position is corrected on reconnection

**Pass Criteria**:
- [ ] Timeout is detected
- [ ] Movement during lag is rejected
- [ ] Player state is corrected

## Reconnection Exploit Tests

### TEST-RE-001: State Restoration Exploit
**Description**: Test if reconnection can restore better state

**Setup**:
1. Player has low health
2. Disconnect and reconnect immediately

**Expected Behavior**:
- Reconnection restores actual state
- Health is not reset to full
- Position is accurate
- No advantage from reconnecting

**Pass Criteria**:
- [ ] Health is restored correctly
- [ ] Position is accurate
- [ ] No stat improvements from reconnection

---

### TEST-RE-002: Duplicate Connection
**Description**: Verify server prevents multiple connections

**Setup**:
1. Connect with client
2. Attempt second connection with same client ID

**Expected Behavior**:
- Server detects duplicate
- First or second connection is terminated
- Only one instance per client ID
- State is not corrupted

**Pass Criteria**:
- [ ] Duplicate connection is handled
- [ ] Only one active connection per client
- [ ] No state corruption

## Test Automation

### Automated Test Suite
```csharp
// Example test structure
[Test]
public void TestMovementSpeedHack()
{
    // Setup
    StartServerAndClient();
    var player = GetLocalPlayer();
    
    // Execute
    ModifyPlayerSpeed(player, 50f);
    MoveForward(2.0f);
    
    // Verify
    Assert.That(player.position.magnitude, Is.LessThan(15f));
    Assert.That(ServerLogContains("Position drift detected"));
}
```

### Continuous Integration
1. Run cheat tests on every commit
2. Monitor for regression in anti-cheat
3. Generate test reports
4. Alert on validation failures

## Logging and Monitoring

### Required Logs
```
[CHEAT] Client {id} - Movement speed exceeded: {speed}
[CHEAT] Client {id} - Position drift: {distance}
[CHEAT] Client {id} - Fire rate violation
[CHEAT] Client {id} - Invalid ammo count
[CHEAT] Client {id} - Fake hit detected
```

### Monitoring Dashboard
- Cheat detection count per client
- Most common cheat types
- False positive rate
- Average position corrections per player

## Reporting Template

### Test Result Format
```
TEST ID: TEST-MC-001
Status: ✅ PASS / ❌ FAIL
Date: YYYY-MM-DD
Tester: Name
Server Version: 1.0.0
Client Version: 1.0.0

Description:
{Test description}

Results:
{Detailed results}

Issues Found:
{Any problems discovered}

Recommendations:
{Improvements suggested}
```

## Future Test Cases

### Planned Tests
1. **Memory Manipulation**: Detect runtime memory editing
2. **Input Injection**: Prevent automated bots
3. **Collision Exploit**: Test complex geometry exploits
4. **Score Manipulation**: Verify score integrity
5. **Time Manipulation**: Prevent client time modification

### Test Coverage Goals
- [ ] 100% of movement validation paths
- [ ] 100% of combat validation paths
- [ ] 100% of health system paths
- [ ] 90%+ of edge cases
- [ ] All known exploit patterns
