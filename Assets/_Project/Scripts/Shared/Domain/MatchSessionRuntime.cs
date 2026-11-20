using System;
using System.Collections.Generic;
using System.Linq;

namespace SmallArena.Domain
{
    public enum MatchLifecyclePhase : byte { WaitingForPlayers, Countdown, InProgress, Ended }
    public enum ParticipantLifeState : byte { Alive, Respawning, Eliminated, Disconnected }

    public sealed class MatchParticipantRuntime
    {
        internal MatchParticipantRuntime(string sessionId, int connectionId, int slot, OnlineCharacterState character, int lives)
        { SessionId = sessionId; ConnectionId = connectionId; SlotIndex = slot; Character = character; RemainingLives = lives; }
        public string SessionId { get; }
        public string ParticipantId => Character.ParticipantId;
        public int ConnectionId { get; }
        public int SlotIndex { get; }
        public long LifeId => Character.LifeId;
        public bool IsConnected { get; internal set; } = true;
        public bool IsSpawned { get; internal set; }
        public int RemainingLives { get; internal set; } // -1 means unlimited.
        public ParticipantLifeState LifeState { get; internal set; } = ParticipantLifeState.Alive;
        public OnlineCharacterState Character { get; internal set; }
        public double RespawnRemainingSeconds(double now) => LifeState == ParticipantLifeState.Respawning
            ? Math.Max(0, Character.RespawnAtSeconds.Value - now) : 0;
    }

    /// <summary>Single authoritative clock domain. No Unity, Mirror, transport objects or geometry.</summary>
    public sealed class MatchSessionRuntime
    {
        private readonly Dictionary<string, MatchParticipantRuntime> participants = new Dictionary<string, MatchParticipantRuntime>(StringComparer.Ordinal);
        private readonly Dictionary<int, MatchParticipantRuntime> connections = new Dictionary<int, MatchParticipantRuntime>();
        private bool sealedRoster;
        private double lastTime;
        private double spawnDeadline;
        public MatchSessionRuntime(string sessionId, GameplayConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(sessionId)) throw new ArgumentException("Session required.", nameof(sessionId));
            SessionId = sessionId; Match = new OnlineMatch(configuration);
        }
        public string SessionId { get; }
        public OnlineMatch Match { get; }
        public GameplayConfiguration Configuration => Match.Configuration;
        public MatchLifecyclePhase Phase { get; private set; }
        public double PhaseDeadline { get; private set; }
        public int ActiveBindingCount => connections.Count;
        public IReadOnlyList<MatchParticipantRuntime> Participants => participants.Values.OrderBy(p => p.SlotIndex).ToArray();
        public MatchParticipantRuntime ByConnection(int id) => connections.TryGetValue(id, out var p) ? p : null;
        public MatchParticipantRuntime ByParticipant(string id) => id != null && participants.TryGetValue(id, out var p) ? p : null;
        private void Observe(double now)
        {
            OnlineMatch.RequireTime(now);
            if (now < lastTime) throw new ArgumentOutOfRangeException(nameof(now), "Server time cannot go backwards.");
            lastTime = now;
        }
        public bool AddParticipant(string id, int connectionId, int slot, double now)
        {
            Observe(now);
            if (sealedRoster || Phase != MatchLifecyclePhase.WaitingForPlayers || string.IsNullOrWhiteSpace(id)
                || slot < 0 || slot > 3 || participants.Count >= 4 || participants.ContainsKey(id)
                || connections.ContainsKey(connectionId) || participants.Values.Any(p => p.SlotIndex == slot)) return false;
            var character = OnlineCharacterState.CreateInitial(Match, id, now);
            var p = new MatchParticipantRuntime(SessionId, connectionId, slot, character, Configuration.MaximumLives == 0 ? -1 : Configuration.MaximumLives);
            participants.Add(id, p); connections.Add(connectionId, p); return true;
        }
        public bool LockRoster(double now)
        {
            Observe(now);
            if (sealedRoster || participants.Count == 0 || Phase != MatchLifecyclePhase.WaitingForPlayers) return false;
            spawnDeadline = OnlineMatch.AddDuration(now, Configuration.SpawnGraceSeconds);
            sealedRoster = true; return true;
        }
        public bool BindSpawn(int connectionId, double now)
        {
            Observe(now); var p = ByConnection(connectionId);
            if (Phase != MatchLifecyclePhase.WaitingForPlayers || p == null || p.IsSpawned || !p.IsConnected) return false;
            p.IsSpawned = true; return true;
        }
        public bool Disconnect(int connectionId, double now)
        {
            Observe(now); var p = ByConnection(connectionId);
            if (p == null) return false;
            connections.Remove(connectionId); p.IsConnected = false; p.IsSpawned = false; p.LifeState = ParticipantLifeState.Disconnected;
            if (p.Character.TrySetConnection(CharacterConnectionState.Disconnected, out var next)) p.Character = next;
            EvaluateElimination(now); return true;
        }
        public void Tick(double now)
        {
            Observe(now);
            if (Phase == MatchLifecyclePhase.Ended || !sealedRoster) return;
            if (Phase == MatchLifecyclePhase.WaitingForPlayers)
            {
                if (now >= spawnDeadline)
                    foreach (var p in Participants.Where(p => p.IsConnected && !p.IsSpawned)) Disconnect(p.ConnectionId, now);
                var connected = Participants.Where(p => p.IsConnected).ToArray();
                if (connected.Length == 0) { End(now); return; }
                if (connected.Any(p => !p.IsSpawned)) return;
                Phase = MatchLifecyclePhase.Countdown;
                PhaseDeadline = Configuration.CountdownDurationSeconds == 0 ? now : OnlineMatch.AddDuration(now, Configuration.CountdownDurationSeconds);
            }
            if (Phase == MatchLifecyclePhase.Countdown && now >= PhaseDeadline)
            {
                // Start from the observed transition, never grant gameplay time before all spawns/countdown.
                Match.TryStart(now, true); Phase = MatchLifecyclePhase.InProgress;
            }
            if (Phase == MatchLifecyclePhase.InProgress && !Match.IsGameplayActive(now)) End(now);
            EvaluateElimination(now);
        }
        public bool CanAct(MatchParticipantRuntime p, double now) => p != null && p.SessionId == SessionId
            && ByParticipant(p.ParticipantId) == p && p.IsConnected && p.IsSpawned && p.LifeState == ParticipantLifeState.Alive
            && Phase == MatchLifecyclePhase.InProgress && p.Character.CanReceiveControl(now);
        public bool TryAttack(string id, long lifeId, double now)
        {
            Observe(now); var p = ByParticipant(id);
            if (!CanAct(p, now) || !p.Character.TryBeginAttack(lifeId, now, out var next)) return false;
            p.Character = next; return true;
        }
        public bool TryDamage(string id, long lifeId, int amount, double now)
        {
            Observe(now); var p = ByParticipant(id);
            if (amount <= 0 || !CanAct(p, now)) return false;
            var hp = amount >= p.Character.Health ? 0 : p.Character.Health - amount;
            if (!p.Character.TryReduceHealth(lifeId, hp, now, out var next)) return false;
            p.Character = next;
            if (hp == 0)
            {
                if (p.RemainingLives > 0) p.RemainingLives--;
                p.LifeState = p.RemainingLives == 0 ? ParticipantLifeState.Eliminated : ParticipantLifeState.Respawning;
                EvaluateElimination(now);
            }
            return true;
        }
        public bool TryRespawn(string id, long expectedLifeId, double now, bool safeSpawn)
        {
            Observe(now); var p = ByParticipant(id);
            if (p == null || p.LifeState != ParticipantLifeState.Respawning || !p.IsConnected || !p.IsSpawned
                || Phase != MatchLifecyclePhase.InProgress || !Match.IsGameplayActive(now)) return false;
            var state = p.Character;
            if (state.TryMarkRespawnPending(now, out var pending)) state = pending;
            if (!state.TryRespawn(expectedLifeId, now, safeSpawn, out var next)) { p.Character = state; return false; }
            p.Character = next; p.LifeState = ParticipantLifeState.Alive; return true;
        }
        public double ClockRemaining(double now) => Phase == MatchLifecyclePhase.InProgress
            ? Math.Max(0, Match.DeadlineSeconds.Value - now) : Phase == MatchLifecyclePhase.Ended ? 0 : Configuration.MatchDurationSeconds;
        private void EvaluateElimination(double now)
        {
            if (Phase == MatchLifecyclePhase.InProgress && Configuration.MaximumLives > 0 && participants.Count >= 2
                && participants.Values.Count(p => p.IsConnected && p.LifeState != ParticipantLifeState.Eliminated) <= 1) End(now);
        }
        private void End(double now)
        {
            if (Phase == MatchLifecyclePhase.Ended) return;
            if (Match.State == OnlineMatchState.Playing) Match.TryFinishEarly(now); else Match.TryAbort();
            Phase = MatchLifecyclePhase.Ended;
        }
        public void Teardown(double now)
        {
            Observe(now);
            foreach (var p in Participants) { p.IsConnected = false; p.IsSpawned = false; p.LifeState = ParticipantLifeState.Disconnected; }
            connections.Clear(); End(now);
        }
    }
}
