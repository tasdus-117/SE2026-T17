using System;

namespace SmallArena.Domain
{
    public enum ShotOutcome : byte { None, Hit, Blocked, Missed, Rejected }

    public static class ShotHitRules
    {
        // Infinity means no intersection. Equal distances favour cover, never a wall-through hit.
        public static ShotOutcome Resolve(double targetDistance, double blockerDistance, double range)
        {
            if (double.IsNaN(range) || double.IsInfinity(range) || range <= 0
                || double.IsNaN(targetDistance) || targetDistance < 0
                || double.IsNaN(blockerDistance) || blockerDistance < 0)
                throw new ArgumentOutOfRangeException(nameof(range));
            if (blockerDistance <= range && blockerDistance <= targetDistance) return ShotOutcome.Blocked;
            return targetDistance <= range ? ShotOutcome.Hit : ShotOutcome.Missed;
        }
    }

    /// <summary>Owner-only, correlated feedback; expiry never resets the request watermark.</summary>
    public sealed class ShotFeedbackState
    {
        public const double DurationSeconds = 1.5;
        public ShotOutcome Outcome { get; private set; }
        public ShotRejection Rejection { get; private set; }
        private string session;
        private long life;
        private MatchLifecyclePhase phase;
        private ParticipantLifeState lifeState;
        private ulong sequence;
        private double expires;
        public void SetContext(string nextSession, long nextLife, MatchLifecyclePhase nextPhase, ParticipantLifeState nextState)
        {
            if (session != nextSession || life != nextLife || phase != nextPhase || lifeState != nextState)
            { Clear(); sequence = 0; }
            session = nextSession; life = nextLife; phase = nextPhase; lifeState = nextState;
        }
        public bool Receive(string requestSession, long requestLife, ulong request, ShotOutcome outcome, ShotRejection rejection, double now)
        {
            if (string.IsNullOrEmpty(session) || requestSession != session || life <= 0 || requestLife != life
                || phase == MatchLifecyclePhase.Ended || phase == MatchLifecyclePhase.WaitingForPlayers
                || lifeState != ParticipantLifeState.Alive || request == 0 || request < sequence
                || double.IsNaN(now) || double.IsInfinity(now) || now < 0) return false;
            sequence = request; Outcome = outcome; Rejection = rejection; expires = now + DurationSeconds;
            return true;
        }
        public void Tick(double now) { if (now >= expires) Clear(); }
        private void Clear() { Outcome = ShotOutcome.None; Rejection = ShotRejection.None; expires = 0; }
    }
}
