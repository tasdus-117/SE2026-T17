using System;
using NUnit.Framework;
using SmallArena.Domain;

namespace SmallArena.Tests.EditMode
{
    public sealed class MatchLifecycleDomainTests
    {
        private static MatchSessionRuntime Create(int lives = 0, double countdown = 3)
        {
            var s = new MatchSessionRuntime("session", new GameplayConfiguration(maxHealth: 10, shotDamage: 1,
                shotCooldownSeconds: 1, matchDurationSeconds: 10, respawnDelaySeconds: 2,
                countdownDurationSeconds: countdown, maximumLives: lives, spawnGraceSeconds: 5));
            s.AddParticipant("A", 1, 0, 0); s.AddParticipant("B", 2, 1, 0); s.LockRoster(0);
            s.BindSpawn(1, 0); s.BindSpawn(2, 0); s.Tick(0); return s;
        }
        [Test] public void OrderedLifecycle_StartsClockAfterCountdown_AndNeverRestarts()
        {
            var s = Create(); Assert.That(s.Phase, Is.EqualTo(MatchLifecyclePhase.Countdown));
            Assert.That(s.ClockRemaining(2), Is.EqualTo(10)); s.Tick(3);
            Assert.That(s.Phase, Is.EqualTo(MatchLifecyclePhase.InProgress));
            Assert.That(s.ClockRemaining(7), Is.EqualTo(6)); s.Tick(13);
            Assert.That(s.Phase, Is.EqualTo(MatchLifecyclePhase.Ended)); s.Tick(20);
            Assert.That(s.BindSpawn(1, 20), Is.False); Assert.That(s.Match.State, Is.EqualTo(OnlineMatchState.Finished));
        }
        [Test] public void CountdownRejectsAllActions()
        {
            var s = Create(); Assert.That(s.CanAct(s.ByConnection(1), 2), Is.False);
            Assert.That(s.TryAttack("A", 1, 2), Is.False); Assert.That(s.TryDamage("B", 1, 1, 2), Is.False);
        }
        [Test] public void BindingsAreStableAndDisconnectRemovesOnlyActiveConnection()
        {
            var s = Create(); var a = s.ByConnection(1); Assert.That(s.ByParticipant("A"), Is.SameAs(a));
            s.Disconnect(1, 1); Assert.That(s.ByConnection(1), Is.Null); Assert.That(s.ByParticipant("A"), Is.SameAs(a));
            Assert.That(a.LifeState, Is.EqualTo(ParticipantLifeState.Disconnected)); Assert.That(s.Disconnect(1, 1), Is.False);
            Assert.That(s.ByConnection(2).ParticipantId, Is.EqualTo("B"));
        }
        [Test] public void DeathRespawnUsesConfiguredDelayAndNewLifeToken()
        {
            var s = Create(); s.Tick(3); var b = s.ByParticipant("B");
            Assert.That(s.TryDamage("B", 1, 10, 3), Is.True);
            Assert.That(b.LifeState, Is.EqualTo(ParticipantLifeState.Respawning)); Assert.That(b.RemainingLives, Is.EqualTo(-1));
            Assert.That(s.TryRespawn("B", 1, 4.99, true), Is.False);
            Assert.That(s.TryRespawn("B", 1, 5, true), Is.True);
            Assert.That(b.LifeId, Is.EqualTo(2)); Assert.That(b.Character.Health, Is.EqualTo(10));
            Assert.That(b.LifeState, Is.EqualTo(ParticipantLifeState.Alive));
            Assert.That(s.TryDamage("B", 1, 1, 6), Is.False); Assert.That(s.TryAttack("B", 1, 6), Is.False);
        }
        [Test] public void UnsafeSpawnRemainsPendingAndCanRetry()
        {
            var s = Create(); s.Tick(3); s.TryDamage("B", 1, 10, 3);
            Assert.That(s.TryRespawn("B", 1, 5, false), Is.False);
            Assert.That(s.ByParticipant("B").Character.Health, Is.Zero);
            Assert.That(s.TryRespawn("B", 1, 5.5, true), Is.True);
        }
        [Test] public void FiniteStockExhaustionEndsMatchWithoutRespawn()
        {
            var s = Create(1); s.Tick(3); s.TryDamage("B", 1, 10, 3);
            Assert.That(s.ByParticipant("B").RemainingLives, Is.Zero);
            Assert.That(s.ByParticipant("B").LifeState, Is.EqualTo(ParticipantLifeState.Eliminated));
            Assert.That(s.Phase, Is.EqualTo(MatchLifecyclePhase.Ended)); Assert.That(s.TryRespawn("B", 1, 5, true), Is.False);
        }
        [Test] public void FiniteStockCanRespawnBeforeExhaustion()
        {
            var s = Create(2); s.Tick(3); s.TryDamage("B", 1, 10, 3);
            Assert.That(s.ByParticipant("B").RemainingLives, Is.EqualTo(1)); Assert.That(s.TryRespawn("B", 1, 5, true), Is.True);
        }
        [Test] public void MatchDeadlinePrecedesRespawn()
        {
            var s = Create(); s.Tick(3); s.TryDamage("B", 1, 10, 12); s.Tick(13);
            Assert.That(s.TryRespawn("B", 1, 14, true), Is.False); Assert.That(s.TryAttack("A", 1, 14), Is.False);
        }
        [Test] public void UnlimitedDisconnectDoesNotEndRemainingPlayersClock()
        {
            var s = Create(); s.Tick(3); s.Disconnect(2, 4); s.Tick(5);
            Assert.That(s.Phase, Is.EqualTo(MatchLifecyclePhase.InProgress)); Assert.That(s.ClockRemaining(5), Is.EqualTo(8));
        }
        [Test] public void MissingSpawnExpiresAtGraceDeadline()
        {
            var s = new MatchSessionRuntime("s", new GameplayConfiguration(spawnGraceSeconds: 2));
            s.AddParticipant("A", 1, 0, 0); s.AddParticipant("B", 2, 1, 0); s.LockRoster(0); s.BindSpawn(1, 0);
            s.Tick(1.99); Assert.That(s.Phase, Is.EqualTo(MatchLifecyclePhase.WaitingForPlayers)); s.Tick(2);
            Assert.That(s.ByConnection(2), Is.Null); Assert.That(s.Phase, Is.EqualTo(MatchLifecyclePhase.Ended));
            Assert.That(s.Match.State, Is.EqualTo(OnlineMatchState.Aborted)); Assert.That(s.WinnerParticipantId, Is.Empty);
        }
        [TestCase(1.999, true)] [TestCase(2d, false)] [TestCase(2.001, false)]
        public void F2_SpawnAdmissionEnforcesDeadlineEvenBeforeNextTick(double now, bool accepted)
        {
            var s = new MatchSessionRuntime("deadline", new GameplayConfiguration(spawnGraceSeconds: 2));
            s.AddParticipant("A", 1, 0, 0); s.AddParticipant("B", 2, 1, 0); s.LockRoster(0); s.BindSpawn(1, 0);
            Assert.That(s.BindSpawn(2, now), Is.EqualTo(accepted));
            s.Tick(now);
            Assert.That(s.Phase, Is.EqualTo(accepted ? MatchLifecyclePhase.Countdown : MatchLifecyclePhase.Ended));
        }
        [Test] public void F2_TwoSpawnedParticipantsProceedAfterThirdTimesOutWithoutScoring()
        {
            var s = new MatchSessionRuntime("deadline", new GameplayConfiguration(spawnGraceSeconds: 2));
            for (var i = 0; i < 3; i++) s.AddParticipant("P" + i, i, i, 0);
            s.LockRoster(0); s.BindSpawn(0, 0); s.BindSpawn(1, 0); s.Tick(2);
            Assert.That(s.Phase, Is.EqualTo(MatchLifecyclePhase.Countdown)); Assert.That(s.ByConnection(2), Is.Null);
            Assert.That(s.ByParticipant("P2").Deaths, Is.Zero); Assert.That(s.ByParticipant("P2").Kills, Is.Zero);
        }
        [Test] public void F2_DisconnectDuringCountdownAbortsBeforeSinglePlayerGameplay()
        {
            var s = Create(); s.Disconnect(2, 1); s.Tick(3);
            Assert.That(s.Match.State, Is.EqualTo(OnlineMatchState.Aborted)); Assert.That(s.WinnerParticipantId, Is.Empty);
        }
        [Test] public void EmptySpawnedRosterAbortsAtDeadline()
        {
            var s = new MatchSessionRuntime("s", new GameplayConfiguration(spawnGraceSeconds: 1));
            s.AddParticipant("A", 1, 0, 0); s.LockRoster(0); s.Tick(1);
            Assert.That(s.Phase, Is.EqualTo(MatchLifecyclePhase.Ended)); Assert.That(s.ActiveBindingCount, Is.Zero);
        }
        [Test] public void ZeroCountdownStartsImmediately() => Assert.That(Create(countdown: 0).Phase, Is.EqualTo(MatchLifecyclePhase.InProgress));
        [Test] public void CooldownIsServerClockBased()
        {
            var s = Create(); s.Tick(3); Assert.That(s.TryAttack("A", 1, 3), Is.True);
            Assert.That(s.TryAttack("A", 1, 3.999), Is.False); Assert.That(s.TryAttack("A", 1, 4), Is.True);
        }
        [TestCase(0)] [TestCase(-1)] public void InvalidDamageDoesNotChangeLife(int amount)
        { var s = Create(); s.Tick(3); Assert.That(s.TryDamage("A", 1, amount, 3), Is.False); Assert.That(s.ByParticipant("A").Character.Health, Is.EqualTo(10)); }
        [Test] public void DuplicateIdentifiersSlotsAndConnectionsAreRejected()
        {
            var s = new MatchSessionRuntime("s", new GameplayConfiguration()); s.AddParticipant("A", 1, 0, 0);
            Assert.That(s.AddParticipant("A", 2, 1, 0), Is.False); Assert.That(s.AddParticipant("B", 1, 1, 0), Is.False);
            Assert.That(s.AddParticipant("B", 2, 0, 0), Is.False); s.LockRoster(0); Assert.That(s.AddParticipant("B", 2, 1, 0), Is.False);
        }
        [Test] public void BackwardsMutationTimeRejected() { var s = Create(); s.Tick(3); Assert.Throws<ArgumentOutOfRangeException>(() => s.Tick(2)); }
        [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)] [TestCase(-1d)]
        public void InvalidCountdownRejected(double countdown) => Assert.Throws<ArgumentOutOfRangeException>(() => new GameplayConfiguration(countdownDurationSeconds: countdown));
        [Test] public void NegativeLivesRejected() => Assert.Throws<ArgumentOutOfRangeException>(() => new GameplayConfiguration(maximumLives: -1));
        [Test] public void TeardownClearsReferencesAndFreezesSession()
        { var s = Create(); s.Teardown(1); Assert.That(s.ActiveBindingCount, Is.Zero); Assert.That(s.ByParticipant("A").IsConnected, Is.False); Assert.That(s.Phase, Is.EqualTo(MatchLifecyclePhase.Ended)); }
    }
}
