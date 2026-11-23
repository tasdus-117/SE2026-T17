using System;
using Mirror;
using SmallArena.Domain;
using UnityEngine;

namespace SmallArena.Networking
{
    [DisallowMultipleComponent, RequireComponent(typeof(NetworkHealth), typeof(NetworkCombat))]
    public sealed class NetworkPlayerPresentationAdapter : NetworkBehaviour, IPlayerPresentationSource
    {
        private NetworkHealth health;
        private NetworkCombat combat;
        private NetworkShotIntentAdapter shots;
        private NetworkMatchController state;
        private object context;
        private ulong revision;
        public PlayerPresentationSnapshot Current { get; private set; }
        public string SessionId { get; private set; } = "";
        public event Action<PlayerPresentationSnapshot> Changed;
        private void Awake() { health = GetComponent<NetworkHealth>(); combat = GetComponent<NetworkCombat>(); shots = GetComponent<NetworkShotIntentAdapter>(); }
        private void OnEnable() { health.OnLifecycleChanged += Clear; }
        private void OnDisable() { health.OnLifecycleChanged -= Clear; Clear(); }
        public override void OnStopClient() => Clear();
        public override void OnStartAuthority() => Clear();
        public override void OnStopAuthority() => Clear();
        public override void OnStopLocalPlayer() => Clear();
        private void Clear()
        {
            if (shots != null) shots.ClearFeedback();
            context = null; revision = 0; SessionId = "";
            if (Current == null) return;
            Current = null; Changed?.Invoke(null);
        }
        private void LateUpdate()
        {
            if (!isClient || !health.IsLifecycleBound || health.LifeId <= 0) { Clear(); return; }
            if (state == null || state.SessionId != health.SessionId)
            {
                Clear();
                state = null;
                foreach (var identity in NetworkClient.spawned.Values)
                    if (identity != null && identity.TryGetComponent<NetworkMatchController>(out var candidate)
                        && candidate.SessionId == health.SessionId) { state = candidate; break; }
            }
            if (state == null || state.Phase != health.MatchPhase) { Clear(); return; }
            MatchScoreEntry row = default; var found = false;
            foreach (var candidate in state.Scores)
                if (candidate.ParticipantId == health.ParticipantId) { row = candidate; found = true; break; }
            if (!found || row.LifeId != health.LifeId || row.LifeState != health.LifeState) { Clear(); return; }
            if (Current != null && (Current.LifeId != health.LifeId || SessionId != health.SessionId)) Clear();
            if (revision == ulong.MaxValue) Clear();
            if (context == null) context = new object();
            SessionId = health.SessionId;
            var result = state.Phase != MatchLifecyclePhase.Ended ? "Playing" : string.IsNullOrEmpty(state.WinnerParticipantId)
                ? "Aborted" : state.WinnerParticipantId == health.ParticipantId ? "Victory" : "Defeat";
            var rejection = isLocalPlayer && shots != null && shots.Rejection != ShotRejection.None ? shots.Rejection.ToString() : "";
            Current = new PlayerPresentationSnapshot(context, ++revision, health.CurrentHealth, health.MaxHealth,
                row.Kills, state.DisplayClock, result, rejection, health.LifeId, health.LifeState, state.Phase,
                combat.CurrentDamage, health.BaseDamage, row.Deaths, row.DamageDealt, row.Rank,
                combat.CooldownRemaining, health.RespawnRemainingSeconds, isLocalPlayer && shots != null ? shots.Outcome : ShotOutcome.None);
            Changed?.Invoke(Current);
        }
    }
}
