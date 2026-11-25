using NUnit.Framework;
using SmallArena.Domain;

namespace SmallArena.Tests.EditMode
{
    public sealed class ShotResolutionAndPresentationCleanupTests
    {
        [TestCase(2d, 1d, 3d, ShotOutcome.Blocked)]
        [TestCase(1d, 2d, 3d, ShotOutcome.Hit)]
        [TestCase(1d, 1d, 3d, ShotOutcome.Blocked)]
        [TestCase(4d, 5d, 3d, ShotOutcome.Missed)]
        [TestCase(3d, double.PositiveInfinity, 3d, ShotOutcome.Hit)]
        [TestCase(double.PositiveInfinity, 0d, 3d, ShotOutcome.Blocked)]
        [TestCase(double.PositiveInfinity, double.PositiveInfinity, 3d, ShotOutcome.Missed)]
        public void NearestIntersectionWithinConfiguredRangeWins(double target, double cover, double range, ShotOutcome expected)
            => Assert.That(ShotHitRules.Resolve(target, cover, range), Is.EqualTo(expected));

        [TestCase(ShotOutcome.Hit, ShotRejection.None)]
        [TestCase(ShotOutcome.Blocked, ShotRejection.None)]
        [TestCase(ShotOutcome.Missed, ShotRejection.None)]
        [TestCase(ShotOutcome.Rejected, ShotRejection.Cooldown)]
        public void FeedbackPopulatesSnapshotAndExpiresWithoutReplayingOlderResults(ShotOutcome outcome, ShotRejection reason)
        {
            var feedback = Feedback();
            Assert.That(feedback.Receive("match", 1, 2, outcome, reason, 10), Is.True);
            var snapshot = new PlayerPresentationSnapshot(new object(), 1, 10, 10, 0, 60, "Playing", feedback.Rejection.ToString(),
                shotOutcome: feedback.Outcome);
            Assert.That(snapshot.ShotOutcome, Is.EqualTo(outcome)); Assert.That(snapshot.Rejection, Is.EqualTo(reason.ToString()));
            feedback.Tick(11.49); Assert.That(feedback.Outcome, Is.EqualTo(outcome));
            feedback.Tick(11.5); Assert.That(feedback.Outcome, Is.EqualTo(ShotOutcome.None)); Assert.That(feedback.Rejection, Is.EqualTo(ShotRejection.None));
            Assert.That(feedback.Receive("match", 1, 1, ShotOutcome.Hit, ShotRejection.None, 12), Is.False);
            Assert.That(feedback.Outcome, Is.EqualTo(ShotOutcome.None));
        }

        [TestCase("match", 2, MatchLifecyclePhase.InProgress, ParticipantLifeState.Alive)]
        [TestCase("match", 1, MatchLifecyclePhase.Ended, ParticipantLifeState.Alive)]
        [TestCase("match", 1, MatchLifecyclePhase.InProgress, ParticipantLifeState.Respawning)]
        [TestCase("", 0, MatchLifecyclePhase.WaitingForPlayers, ParticipantLifeState.Alive)]
        [TestCase("next-match", 1, MatchLifecyclePhase.InProgress, ParticipantLifeState.Alive)]
        public void LifeDeathEndAndLobbyResetClearFeedbackAndRejectOldContext(string session, long life, MatchLifecyclePhase phase, ParticipantLifeState state)
        {
            var feedback = Feedback(); feedback.Receive("match", 1, 90, ShotOutcome.Rejected, ShotRejection.Cooldown, 10);
            feedback.SetContext(session, life, phase, state);
            Assert.That(feedback.Outcome, Is.EqualTo(ShotOutcome.None)); Assert.That(feedback.Rejection, Is.EqualTo(ShotRejection.None));
            Assert.That(feedback.Receive("match", 1, 91, ShotOutcome.Hit, ShotRejection.None, 11), Is.False);
            if (phase == MatchLifecyclePhase.InProgress && state == ParticipantLifeState.Alive && session != "")
                Assert.That(feedback.Receive(session, life, 1, ShotOutcome.Hit, ShotRejection.None, 11), Is.True);
        }
        private static ShotFeedbackState Feedback()
        {
            var feedback = new ShotFeedbackState();
            feedback.SetContext("match", 1, MatchLifecyclePhase.InProgress, ParticipantLifeState.Alive); return feedback;
        }
    }
}
