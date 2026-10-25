# Load and Latency Testing Guide

## Overview
This document provides guidelines for testing the performance and scalability of SmallArena3D under various network conditions.

## Test Environment

### Hardware Requirements
**Server:**
- CPU: 4 cores minimum
- RAM: 8GB minimum
- Network: 100 Mbps uplink
- OS: Windows/Linux/macOS

**Client Machines:**
- CPU: 2 cores minimum
- RAM: 4GB minimum
- Network: 10 Mbps minimum
- GPU: DirectX 11 compatible

### Software Requirements
- Unity 2022.3 LTS or later
- .NET 6.0 or later
- Network monitoring tools (Wireshark, Unity Profiler)

## Load Testing Scenarios

### LT-001: Maximum Player Capacity
**Objective**: Verify server can handle 4 concurrent players

**Setup**:
1. Start dedicated server
2. Connect 4 clients simultaneously
3. All players join arena

**Metrics to Measure**:
- Server CPU usage
- Server memory usage
- Network bandwidth per client
- Frame rate on server
- Tick rate consistency

**Success Criteria**:
- Server CPU < 50%
- Server memory < 2GB
- Bandwidth < 50 KB/s per client
- Server tick rate maintains 60 Hz
- No disconnections

**Test Duration**: 10 minutes

---

### LT-002: Combat Load Test
**Objective**: Stress test combat system with continuous action

**Setup**:
1. Connect 4 clients
2. All players shoot continuously for 5 minutes
3. Maximum movement and combat activity

**Metrics to Measure**:
- Messages per second
- Hit validation time
- Position update frequency
- Packet loss rate
- Average latency

**Success Criteria**:
- < 200 messages/sec per client
- Hit validation < 10ms
- Position updates at 60 Hz
- Packet loss < 1%
- Average latency < 100ms (local network)

**Test Duration**: 5 minutes continuous action

---

### LT-003: Connection Burst Test
**Objective**: Test server stability during rapid connections

**Setup**:
1. Start server
2. Connect 4 clients within 1 second
3. Monitor server response

**Metrics to Measure**:
- Connection acceptance time
- Server response during load
- Memory allocation spikes
- Network queue size

**Success Criteria**:
- All 4 clients connect successfully
- Connection time < 2 seconds each
- No server crashes or hangs
- Memory stable after connections

---

### LT-004: Long Duration Stability Test
**Objective**: Verify server stability over extended period

**Setup**:
1. Run complete 5-minute match
2. Repeat 10 times consecutively
3. Monitor for memory leaks and performance degradation

**Metrics to Measure**:
- Memory usage over time
- CPU usage trends
- Network statistics consistency
- Frame time variance

**Success Criteria**:
- No memory leaks (memory stable)
- CPU usage consistent
- No performance degradation
- All matches complete successfully

**Test Duration**: 50+ minutes

---

### LT-005: Reconnection Stress Test
**Objective**: Test reconnection system under load

**Setup**:
1. Start match with 4 players
2. Disconnect 2 players simultaneously
3. Reconnect within grace period
4. Repeat 10 times

**Metrics to Measure**:
- Reconnection success rate
- State restoration accuracy
- Server memory during disconnects
- Impact on remaining connected players

**Success Criteria**:
- 100% successful reconnections
- State restored accurately
- No impact on other players
- Memory properly cleaned up

## Network Condition Testing

### NC-001: Latency Simulation Tests

#### Test Case: Low Latency (0-50ms)
**Setup**:
```csharp
LatencySimulator.SetSimulationParameters(
    enabled: true,
    latency: 25,
    packetLoss: 0
);
```

**Expected Behavior**:
- Smooth gameplay
- Minimal prediction errors
- Hits register immediately
- Movement feels responsive

---

#### Test Case: Medium Latency (50-150ms)
**Setup**:
```csharp
LatencySimulator.SetSimulationParameters(
    enabled: true,
    latency: 100,
    packetLoss: 0
);
```

**Expected Behavior**:
- Slight delay noticeable
- Interpolation smooths movement
- Occasional position corrections
- Still playable experience

**Acceptance Criteria**:
- [ ] No rubber-banding
- [ ] Hits still register correctly
- [ ] Movement is smooth
- [ ] No major desync

---

#### Test Case: High Latency (150-300ms)
**Setup**:
```csharp
LatencySimulator.SetSimulationParameters(
    enabled: true,
    latency: 250,
    packetLoss: 0
);
```

**Expected Behavior**:
- Noticeable delay
- More frequent position corrections
- Prediction errors visible
- Gameplay becomes difficult

**Acceptance Criteria**:
- [ ] Game remains playable
- [ ] No disconnections
- [ ] Corrections don't break gameplay
- [ ] Server authority maintained

---

#### Test Case: Extreme Latency (300-500ms)
**Setup**:
```csharp
LatencySimulator.SetSimulationParameters(
    enabled: true,
    latency: 500,
    packetLoss: 0
);
```

**Expected Behavior**:
- Severe delay
- Frequent corrections
- Difficult to aim
- Close to unplayable

**Acceptance Criteria**:
- [ ] No crashes
- [ ] No infinite loops of corrections
- [ ] Server doesn't kick player
- [ ] State remains consistent

---

### NC-002: Packet Loss Simulation

#### Test Case: Low Packet Loss (1-5%)
**Setup**:
```csharp
LatencySimulator.SetSimulationParameters(
    enabled: true,
    latency: 50,
    packetLoss: 3.0f
);
```

**Expected Behavior**:
- Minor occasional hiccups
- Interpolation compensates
- Mostly smooth gameplay

**Acceptance Criteria**:
- [ ] Gameplay is smooth
- [ ] Missing updates don't cause issues
- [ ] State converges quickly

---

#### Test Case: Medium Packet Loss (5-10%)
**Setup**:
```csharp
LatencySimulator.SetSimulationParameters(
    enabled: true,
    latency: 50,
    packetLoss: 7.5f
);
```

**Expected Behavior**:
- Noticeable stuttering
- More reliance on prediction
- Some position jumps

**Acceptance Criteria**:
- [ ] Game remains playable
- [ ] State eventually syncs
- [ ] No permanent desync

---

#### Test Case: High Packet Loss (10-20%)
**Setup**:
```csharp
LatencySimulator.SetSimulationParameters(
    enabled: true,
    latency: 100,
    packetLoss: 15.0f
);
```

**Expected Behavior**:
- Frequent stuttering
- Significant prediction errors
- Difficult gameplay

**Acceptance Criteria**:
- [ ] No disconnections
- [ ] State remains consistent
- [ ] Server authority maintained

---

### NC-003: Jitter Simulation
**Objective**: Test variable latency conditions

**Setup**:
```csharp
// Latency varies between 50-150ms
LatencySimulator.jitterMs = 50;
LatencySimulator.latencyMs = 100;
```

**Expected Behavior**:
- Inconsistent response times
- Interpolation handles variance
- No major gameplay disruption

**Acceptance Criteria**:
- [ ] Smooth movement despite jitter
- [ ] Timestamps handled correctly
- [ ] No timing exploits possible

---

### NC-004: Mixed Network Conditions
**Objective**: Test asymmetric network conditions

**Setup**:
- Client 1: 20ms latency, 0% loss
- Client 2: 150ms latency, 5% loss
- Client 3: 100ms latency, 2% loss
- Client 4: 50ms latency, 0% loss

**Expected Behavior**:
- All clients can play together
- Low-latency clients aren't disadvantaged
- High-latency clients are compensated
- Fair gameplay for all

**Acceptance Criteria**:
- [ ] All clients can interact
- [ ] Hits register for all clients
- [ ] No systematic advantage/disadvantage
- [ ] Server reconciles fairly

## Performance Benchmarks

### Target Performance Metrics

#### Server Performance
```
Metric                  Target      Maximum
─────────────────────────────────────────────
CPU Usage              < 30%        50%
Memory Usage           < 1GB        2GB
Tick Rate              60 Hz        60 Hz
Message Processing     < 5ms        10ms
Hit Validation         < 5ms        15ms
```

#### Client Performance
```
Metric                  Target      Minimum
─────────────────────────────────────────────
Frame Rate             60 FPS       30 FPS
Input Latency          < 50ms       100ms
Network Usage          < 30 KB/s    50 KB/s
Memory Usage           < 2GB        4GB
```

#### Network Performance
```
Metric                  Target      Maximum
─────────────────────────────────────────────
Bandwidth per Client   < 30 KB/s    50 KB/s
Messages per Second    < 150        200
Average Latency        < 50ms       200ms
Packet Loss            < 0.1%       1%
```

## Test Tools and Scripts

### Automated Load Test Script
```csharp
using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class AutomatedLoadTest : MonoBehaviour
{
    public int numberOfBots = 3;
    public float testDuration = 300f; // 5 minutes
    
    IEnumerator RunLoadTest()
    {
        // Start server
        NetworkManager.Singleton.StartServer();
        yield return new WaitForSeconds(1f);
        
        // Spawn bots
        for (int i = 0; i < numberOfBots; i++)
        {
            SpawnBot();
            yield return new WaitForSeconds(0.2f);
        }
        
        // Run test
        float startTime = Time.time;
        while (Time.time - startTime < testDuration)
        {
            // Bots perform actions
            foreach (var bot in GetBots())
            {
                bot.PerformRandomAction();
            }
            
            yield return null;
        }
        
        // Collect metrics
        GenerateReport();
    }
    
    void SpawnBot()
    {
        // Spawn AI-controlled bot
        var bot = Instantiate(botPrefab);
        bot.GetComponent<NetworkObject>().Spawn();
    }
    
    void GenerateReport()
    {
        Debug.Log($"Average FPS: {CalculateAverageFPS()}");
        Debug.Log($"Peak Memory: {GetPeakMemory()} MB");
        Debug.Log($"Messages Sent: {GetTotalMessagesSent()}");
        Debug.Log($"Average Latency: {GetAverageLatency()} ms");
    }
}
```

### Network Statistics Logger
```csharp
using UnityEngine;
using Unity.Netcode;

public class NetworkStatsLogger : MonoBehaviour
{
    private float logInterval = 1f;
    private float nextLogTime;
    
    void Update()
    {
        if (Time.time >= nextLogTime)
        {
            LogNetworkStats();
            nextLogTime = Time.time + logInterval;
        }
    }
    
    void LogNetworkStats()
    {
        var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport;
        
        Debug.Log($"[STATS] Time: {Time.time:F2}");
        Debug.Log($"[STATS] Connected Clients: {NetworkManager.Singleton.ConnectedClients.Count}");
        Debug.Log($"[STATS] Bytes Sent: {GetBytesSent()}");
        Debug.Log($"[STATS] Bytes Received: {GetBytesReceived()}");
        Debug.Log($"[STATS] Messages/sec: {GetMessagesPerSecond()}");
        Debug.Log($"[STATS] RTT: {GetRoundTripTime()} ms");
    }
}
```

## Monitoring Tools

### Recommended Tools
1. **Unity Profiler**
   - CPU usage
   - Memory allocation
   - Network messages
   - Frame time

2. **Wireshark**
   - Packet inspection
   - Bandwidth measurement
   - Protocol analysis

3. **System Monitor**
   - Overall system resources
   - Process-specific metrics

4. **Custom Dashboard**
   - Real-time game metrics
   - Player statistics
   - Network health

## Test Reports

### Report Template
```markdown
# Load Test Report

**Date**: YYYY-MM-DD
**Test ID**: LT-XXX
**Duration**: XX minutes
**Server Version**: X.X.X

## Configuration
- Players: X
- Map: Arena
- Network Conditions: Latency Xms, Loss X%

## Results

### Performance Metrics
| Metric | Average | Peak | Target | Status |
|--------|---------|------|--------|--------|
| Server CPU | X% | X% | <50% | ✅/❌ |
| Memory | XMB | XMB | <2GB | ✅/❌ |
| FPS | X | X | >30 | ✅/❌ |
| Latency | Xms | Xms | <100ms | ✅/❌ |

### Issues Found
1. Issue description
2. Issue description

### Recommendations
1. Recommendation
2. Recommendation

## Conclusion
Summary of test results and overall status.
```

## Continuous Integration

### CI/CD Pipeline
```yaml
name: Load Tests

on:
  push:
    branches: [ main, develop ]
  schedule:
    - cron: '0 0 * * 0'  # Weekly

jobs:
  load-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      
      - name: Build Server
        run: ./build-server.sh
      
      - name: Build Client
        run: ./build-client.sh
      
      - name: Run Load Tests
        run: ./run-load-tests.sh
      
      - name: Generate Report
        run: ./generate-report.sh
      
      - name: Upload Results
        uses: actions/upload-artifact@v2
        with:
          name: load-test-results
          path: test-results/
```

## Best Practices

### Before Testing
1. ✅ Clean build of server and client
2. ✅ Disable debug logging (affects performance)
3. ✅ Close unnecessary applications
4. ✅ Use dedicated test environment
5. ✅ Document hardware specifications

### During Testing
1. ✅ Monitor system resources
2. ✅ Log all metrics
3. ✅ Take screenshots of issues
4. ✅ Record network traffic if needed
5. ✅ Document any anomalies

### After Testing
1. ✅ Generate comprehensive report
2. ✅ Compare with baseline metrics
3. ✅ Identify performance regressions
4. ✅ Create tickets for issues
5. ✅ Archive test data

## Troubleshooting

### Common Issues

**Issue**: Server CPU spikes to 100%
- **Cause**: Too many physics updates or inefficient code
- **Solution**: Profile to find bottleneck, optimize hot paths

**Issue**: Client disconnects randomly
- **Cause**: Timeout due to high latency or packet loss
- **Solution**: Increase timeout threshold, improve reconnection

**Issue**: Memory usage grows continuously
- **Cause**: Memory leak in network code
- **Solution**: Profile memory, check object pooling

**Issue**: Desync between clients
- **Cause**: Insufficient validation or correction
- **Solution**: Increase validation frequency, improve reconciliation

## Future Testing Plans

### Upcoming Test Scenarios
1. **Scalability Test**: Test with 8+ players (future expansion)
2. **Geographic Distribution**: Test with players across regions
3. **Mobile Client**: Test on mobile devices
4. **Spectator Mode**: Test with spectators + players
5. **Match Series**: Test tournament-style consecutive matches
