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
        private ulong sequence, feedbackSequence;
        private long life;
        private string session;
        private bool available;
        public event Action Invalidated;
        public ShotRejection Rejection { get; private set; }
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
            { sequence = feedbackSequence = 0; life = health.LifeId; session = health.SessionId; gate.Reset(); }
            available = CanSubmit; Rejection = ShotRejection.None;
            Invalidated?.Invoke();
        }
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
            if (rejection == ShotRejection.None) rejection = combat.PerformShot(intent.AimX, intent.AimZ);
            TargetFeedback(sender, health.SessionId, intent.LifeId, intent.Sequence, rejection);
        }
        [TargetRpc(channel = Channels.Reliable)]
        private void TargetFeedback(NetworkConnectionToClient target, string sessionId, long lifeId, ulong request, ShotRejection rejection)
        {
            if (!isLocalPlayer || health.LifeState != ParticipantLifeState.Alive || health.MatchPhase == MatchLifecyclePhase.Ended
                || health.SessionId != sessionId || health.LifeId != lifeId || request < feedbackSequence) return;
            feedbackSequence = request; Rejection = rejection;
        }
    }
}
