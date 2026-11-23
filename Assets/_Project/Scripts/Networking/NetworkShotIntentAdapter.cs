using System;
using Mirror;
using SmallArena.Domain;
using SmallArena.Protocol;
using UnityEngine;

namespace SmallArena.Networking
{
    [DisallowMultipleComponent, RequireComponent(typeof(NetworkHealth), typeof(NetworkCombat))]
    public sealed class NetworkShotIntentAdapter : NetworkBehaviour, IShotIntentSink
    {
        private NetworkHealth health;
        private NetworkCombat combat;
        private NetworkPlayerPresentationAdapter presentation;
        private readonly ShotRequestGate gate = new ShotRequestGate();
        private ulong sequence;
        private readonly ShotFeedbackState feedback = new ShotFeedbackState();
        private long life;
        private string session;
        private bool available;
        public event Action Invalidated;
        public ShotRejection Rejection { get { RefreshFeedback(); return feedback.Rejection; } }
        public ShotOutcome Outcome { get { RefreshFeedback(); return feedback.Outcome; } }
        public bool CanSubmit => isActiveAndEnabled && isLocalPlayer && isClient && NetworkClient.ready
            && NetworkClient.connection != null && NetworkClient.connection.isAuthenticated
            && health != null && health.IsLifecycleBound && health.CanAct && presentation != null
            && presentation.Current != null && presentation.SessionId == health.SessionId
            && presentation.Current.LifeId == health.LifeId && presentation.Current.Phase == health.MatchPhase;
        public double RequestIntervalSeconds => health != null ? health.AttackCooldownSeconds : double.NaN;
        private void Awake() { health = GetComponent<NetworkHealth>(); combat = GetComponent<NetworkCombat>(); presentation = GetComponent<NetworkPlayerPresentationAdapter>(); }
        private void OnEnable() { health.OnLifecycleChanged += Invalidate; }
        private void OnDisable() { health.OnLifecycleChanged -= Invalidate; Invalidate(); }
        public override void OnStopLocalPlayer() => Invalidate();
        public override void OnStopClient() => Invalidate();
        public override void OnStartAuthority() => Invalidate();
        public override void OnStopAuthority() => Invalidate();
        private void Update()
        {
            if (available != CanSubmit || life != health.LifeId || session != health.SessionId) Invalidate();
        }
        private void Invalidate()
        {
            if (life != health.LifeId || session != health.SessionId)
            { sequence = 0; life = health.LifeId; session = health.SessionId; gate.Reset(); }
            RefreshFeedback();
            available = CanSubmit;
            Invalidated?.Invoke();
        }
        private void RefreshFeedback()
        {
            feedback.SetContext(isActiveAndEnabled && isClient ? health.SessionId : "", health.LifeId, health.MatchPhase, health.LifeState);
            feedback.Tick(NetworkTime.localTime);
        }
        internal void ClearFeedback() => feedback.SetContext("", 0, MatchLifecyclePhase.WaitingForPlayers, ParticipantLifeState.Disconnected);
        public void SubmitAim(float worldX, float worldZ)
        {
            if (!CanSubmit || sequence == ulong.MaxValue) return;
            if (!CombatAim.TryNormalizeHorizontal(worldX, worldZ, out var dx, out var dz)) return;
            CmdSubmitShot(new ShotIntent { AimX = dx, AimZ = dz, Timestamp = NetworkTime.predictedTime,
                Sequence = ++sequence, LifeId = health.LifeId });
        }
        [Command(channel = Channels.Reliable, requiresAuthority = true)]
        private void CmdSubmitShot(ShotIntent intent, NetworkConnectionToClient sender = null)
        {
            if (sender == null || !sender.isAuthenticated || sender != connectionToClient || sender.identity != netIdentity
                || !NetworkServer.connections.TryGetValue(sender.connectionId, out var active) || !ReferenceEquals(active, sender)) return;
            ShotRejection rejection;
            if (!health.IsLifecycleBound || intent.LifeId != health.LifeId || health.LifeState != ParticipantLifeState.Alive)
                rejection = ShotRejection.WrongLife;
            else if (health.MatchPhase != MatchLifecyclePhase.InProgress || !health.CanAct) rejection = ShotRejection.NotPlaying;
            else rejection = gate.Process(health.LifeId, intent.LifeId, intent.Sequence, intent.Timestamp, NetworkTime.localTime);
            var outcome = ShotOutcome.Rejected;
            if (rejection == ShotRejection.None) rejection = combat.PerformShot(intent.AimX, intent.AimZ, out outcome);
            TargetFeedback(sender, health.SessionId, intent.LifeId, intent.Sequence, outcome, rejection);
        }
        [TargetRpc(channel = Channels.Reliable)]
        private void TargetFeedback(NetworkConnectionToClient target, string sessionId, long lifeId, ulong request, ShotOutcome outcome, ShotRejection rejection)
        {
            if (!isLocalPlayer || presentation == null || presentation.Current == null
                || health.LifeState != ParticipantLifeState.Alive || health.MatchPhase == MatchLifecyclePhase.Ended
                || health.SessionId != sessionId || health.LifeId != lifeId) return;
            RefreshFeedback();
            feedback.Receive(sessionId, lifeId, request, outcome, rejection, NetworkTime.localTime);
        }
    }
}
