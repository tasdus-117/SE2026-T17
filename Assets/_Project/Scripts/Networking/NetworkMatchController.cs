using System;
using Mirror;
using SmallArena.Domain;
using UnityEngine;

namespace SmallArena.Networking
{
    public struct MatchScoreEntry
    {
        public string ParticipantId;
        public int SlotIndex, Rank, Kills, Deaths;
        public long DamageDealt;
        public ParticipantLifeState LifeState;
        public bool IsConnected;
    }
    public struct MatchKillFeedEntry
    {
        public string AttackerId, VictimId;
        public int AttackerSlot, VictimSlot;
        public double ServerTime;
    }
    /// <summary>Read-only client projection of a server-owned session. No client phase/timer mutation.</summary>
    public sealed class NetworkMatchController : NetworkBehaviour
    {
        [SyncVar] private string sessionId = "";
        [SyncVar] private MatchLifecyclePhase phase;
        [SyncVar] private double matchClock;
        [SyncVar] private double phaseDeadline;
        [SyncVar] private double sampledAt;
        [SyncVar] private string winnerId = "";
        [SyncVar] private double postMatchDeadline;
        private readonly SyncList<MatchScoreEntry> scores = new SyncList<MatchScoreEntry>();
        private readonly SyncList<MatchKillFeedEntry> feed = new SyncList<MatchKillFeedEntry>();
        public System.Collections.Generic.IReadOnlyList<MatchScoreEntry> Scores => scores;
        public System.Collections.Generic.IReadOnlyList<MatchKillFeedEntry> Feed => feed;
        public string WinnerParticipantId => winnerId;
        public double PostMatchRemaining => phase == MatchLifecyclePhase.Ended
            ? Math.Max(0, Math.Min(postMatchDeadline - sampledAt, postMatchDeadline - NetworkTime.time)) : 0;
        public string SessionId => sessionId;
        public MatchLifecyclePhase Phase => phase;
        public double MatchClock => matchClock;
        public double CountdownRemaining => phase == MatchLifecyclePhase.Countdown ? Math.Max(0, phaseDeadline - NetworkTime.time) : 0;
        public double DisplayClock => phase == MatchLifecyclePhase.InProgress
            ? Math.Max(0, matchClock - Math.Max(0, NetworkTime.time - sampledAt)) : matchClock;
        [Server] public void Publish(MatchSessionRuntime session, double now, double returnDeadline = 0)
        {
            if (!isServer) return;
            sessionId = session.SessionId; phase = session.Phase;
            phaseDeadline = session.PhaseDeadline; matchClock = session.ClockRemaining(now); sampledAt = now;
            winnerId = session.WinnerParticipantId; postMatchDeadline = returnDeadline;
            var standings = session.ComputeFinalStandings();
            for (var i = 0; i < standings.Count; i++)
            {
                var p = standings[i];
                var row = new MatchScoreEntry { ParticipantId = p.ParticipantId, SlotIndex = p.SlotIndex,
                    Rank = p.Rank, Kills = p.Kills, Deaths = p.Deaths, DamageDealt = p.DamageDealt,
                    LifeState = p.LifeState, IsConnected = p.IsConnected };
                if (i >= scores.Count) scores.Add(row); else if (!scores[i].Equals(row)) scores[i] = row;
            }
        }
        [Server] public void PublishElimination(MatchElimination elimination, MatchSessionRuntime session)
        {
            if (!isServer) return;
            var attacker = session.ByParticipant(elimination.AttackerId); var victim = session.ByParticipant(elimination.VictimId);
            if (victim == null) return;
            if (feed.Count == 5) feed.RemoveAt(0);
            feed.Add(new MatchKillFeedEntry { AttackerId = attacker?.ParticipantId ?? "", VictimId = victim.ParticipantId,
                AttackerSlot = attacker?.SlotIndex ?? -1, VictimSlot = victim.SlotIndex, ServerTime = elimination.ServerTime });
        }
    }
}
