using System;
using System.Collections.Generic;
using System.Linq;

namespace SmallArena.Domain
{
    public enum MatchLifecyclePhase : byte { WaitingForPlayers, Countdown, InProgress, Ended }
    public enum ParticipantLifeState : byte { Alive, Respawning, Eliminated, Disconnected }
    public readonly struct MatchElimination
    {
        public MatchElimination(string attackerId, string victimId, double time) { AttackerId = attackerId; VictimId = victimId; ServerTime = time; }
        public string AttackerId { get; }
        public string VictimId { get; }
        public double ServerTime { get; }
    }

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
        public int Kills => Character.Score;
        public int Deaths => Character.Deaths;
        public long DamageDealt { get; internal set; }
        public int Rank { get; internal set; }
        public bool IsWinner { get; internal set; }
        internal long LastScoredDeathLife { get; set; }
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
        public string WinnerParticipantId { get; private set; } = "";
        public event Action<MatchElimination> Eliminated;
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
        public bool TryDamage(string id, long lifeId, int amount, double now, string attackerId = null, long attackerLifeId = 0)
        {
            Observe(now); var p = ByParticipant(id);
            if (amount <= 0 || !CanAct(p, now)) return false;
            var attacker = ByParticipant(attackerId);
            if (attackerId != null && (attacker == null || !CanAct(attacker, now) || attacker.LifeId != attackerLifeId)) return false;
            var actualDamage = Math.Min(amount, p.Character.Health);
            var hp = amount >= p.Character.Health ? 0 : p.Character.Health - amount;
            if (!p.Character.TryReduceHealth(lifeId, hp, now, out var next)) return false;
            p.Character = next;
            if (attacker != null && attacker != p) attacker.DamageDealt += actualDamage;
            if (hp == 0)
            {
                if (p.RemainingLives > 0) p.RemainingLives--;
                p.LifeState = p.RemainingLives == 0 ? ParticipantLifeState.Eliminated : ParticipantLifeState.Respawning;
                RecordElimination(attackerId, id);
                EvaluateWinCondition(now);
                Eliminated?.Invoke(new MatchElimination(attacker == p ? null : attackerId, id, now));
            }
            return true;
        }
        public bool RecordDamage(string attackerId, string victimId, int amount, double now)
        {
            var victim = ByParticipant(victimId); var attacker = ByParticipant(attackerId);
            return victim != null && TryDamage(victimId, victim.LifeId, amount, now, attackerId, attacker?.LifeId ?? 0);
        }
        public bool RecordElimination(string attackerId, string victimId)
        {
            var victim = ByParticipant(victimId);
            if (Phase != MatchLifecyclePhase.InProgress || victim == null || victim.Character.Health != 0
                || victim.LastScoredDeathLife == victim.LifeId) return false;
            var attacker = ByParticipant(attackerId);
            if (attackerId != null && (attacker == null || !attacker.IsConnected)) return false;
            victim.LastScoredDeathLife = victim.LifeId;
            victim.Character = victim.Character.WithStatistics(victim.Kills, victim.Deaths + 1);
            if (attacker != null && attacker != victim)
                attacker.Character = attacker.Character.WithStatistics(attacker.Kills + 1, attacker.Deaths);
            return true;
        }
        public void EvaluateWinCondition(double now)
        {
            Observe(now);
            if (Phase != MatchLifecyclePhase.InProgress) return;
            if (!Match.IsGameplayActive(now) || Configuration.TargetKills > 0 && participants.Values.Any(p => p.Kills >= Configuration.TargetKills)) End(now);
            EvaluateElimination(now);
        }
        public IReadOnlyList<MatchParticipantRuntime> ComputeFinalStandings()
        {
            var rows = participants.Values.OrderByDescending(p => p.Kills).ThenBy(p => p.Deaths)
                .ThenByDescending(p => p.DamageDealt).ThenBy(p => p.SlotIndex).ToArray();
            for (var n = 0; n < rows.Length; n++) rows[n].Rank = n + 1;
            return rows;
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
            ? Math.Max(0, Math.Min(Configuration.MatchDurationSeconds, Match.DeadlineSeconds.Value - now)) : Phase == MatchLifecyclePhase.Ended ? 0 : Configuration.MatchDurationSeconds;
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
            var standings = ComputeFinalStandings();
            if (Match.State == OnlineMatchState.Finished && standings.Count > 0)
            {
                WinnerParticipantId = standings[0].ParticipantId;
                standings[0].IsWinner = true;
            }
        }
        public void Teardown(double now)
        {
            Observe(now);
            foreach (var p in Participants) { p.IsConnected = false; p.IsSpawned = false; p.LifeState = ParticipantLifeState.Disconnected; }
            connections.Clear();
            if (Phase != MatchLifecyclePhase.Ended) Match.TryAbort();
            End(now);
        }
    }
}
