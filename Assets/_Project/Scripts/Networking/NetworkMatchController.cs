using System;
using Mirror;
using SmallArena.Domain;
using UnityEngine;

namespace SmallArena.Networking
{
    /// <summary>Read-only client projection of a server-owned session. No client phase/timer mutation.</summary>
    public sealed class NetworkMatchController : NetworkBehaviour
    {
        [SyncVar] private string sessionId = "";
        [SyncVar] private MatchLifecyclePhase phase;
        [SyncVar] private double matchClock;
        [SyncVar] private double phaseDeadline;
        [SyncVar] private double sampledAt;
        public string SessionId => sessionId;
        public MatchLifecyclePhase Phase => phase;
        public double MatchClock => matchClock;
        public double CountdownRemaining => phase == MatchLifecyclePhase.Countdown ? Math.Max(0, phaseDeadline - NetworkTime.time) : 0;
        public double DisplayClock => phase == MatchLifecyclePhase.InProgress
            ? Math.Max(0, matchClock - Math.Max(0, NetworkTime.time - sampledAt)) : matchClock;
        [Server] public void Publish(MatchSessionRuntime session, double now)
        {
            if (!isServer) return;
            sessionId = session.SessionId; phase = session.Phase;
            phaseDeadline = session.PhaseDeadline; matchClock = session.ClockRemaining(now); sampledAt = now;
        }
    }
}
