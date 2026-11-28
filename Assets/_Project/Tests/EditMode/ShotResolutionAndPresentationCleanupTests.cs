using NUnit.Framework;
using SmallArena.Domain;

namespace SmallArena.Tests.EditMode
{
    public sealed class ShotResolutionAndPresentationCleanupTests
    {
        private static MatchSessionRuntime GunSession(double cooldown = .5)
        {
            var session = new MatchSessionRuntime("gun", new GameplayConfiguration(shotCooldownSeconds: cooldown,
                countdownDurationSeconds: 0));
            session.AddParticipant("a", 1, 0, 0); session.AddParticipant("b", 2, 1, 0);
            session.LockRoster(0); session.BindSpawn(1, 0); session.BindSpawn(2, 0); session.Tick(0);
            return session;
        }
        [TestCase(10.499999, false)]
        [TestCase(10.5, true)]
        [TestCase(10.500001, true)]
        public void GunCooldownUsesInclusiveServerDeadline_IndependentCases(double now, bool accepted)
        {
            var session = GunSession();
            Assert.That(session.TryAttack("a", 1, 10), Is.True);
            Assert.That(session.ByParticipant("a").Character.NextShotAtSeconds, Is.EqualTo(10.5));
            Assert.That(session.TryAttack("a", 1, now), Is.EqualTo(accepted));
            Assert.That(session.ByParticipant("a").Character.NextShotAtSeconds, Is.EqualTo(accepted ? now + .5 : 10.5));
            Assert.That(session.ByParticipant("b").Character.Health, Is.EqualTo(100));
        }
        [Test] public void FreshFutureClientTimestampCannotShortenCustomServerCooldown()
        {
            var session = GunSession(.75); var gate = new ShotRequestGate();
            Assert.That(session.TryAttack("a", 1, 10), Is.True);
            Assert.That(gate.Process(1, 1, 1, 999, 10.5), Is.EqualTo(ShotRejection.None));
            Assert.That(session.TryAttack("a", 1, 10.5), Is.False);
            Assert.That(session.ByParticipant("a").Character.NextShotAtSeconds, Is.EqualTo(10.75));
            Assert.That(session.TryAttack("a", 99, 10.75), Is.False);
            Assert.That(session.TryAttack("a", 1, 10.75), Is.True);
            Assert.That(session.ByParticipant("a").Character.NextShotAtSeconds, Is.EqualTo(11.5));
        }
        [TestCase(29.999999, ShotOutcome.Hit)]
        [TestCase(30, ShotOutcome.Hit)]
        [TestCase(30.000001, ShotOutcome.Missed)]
        public void GunRangeIncludesExactlyThirtyMetres(double distance, ShotOutcome expected)
            => Assert.That(ShotHitRules.Resolve(distance, double.PositiveInfinity, 30), Is.EqualTo(expected));
        [TestCase(0)] [TestCase(-1)] [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)]
        public void InvalidGunCooldownCannotBecomeMatchConfiguration(double cooldown)
            => Assert.Throws<System.ArgumentOutOfRangeException>(() => new GameplayConfiguration(shotCooldownSeconds: cooldown));

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
