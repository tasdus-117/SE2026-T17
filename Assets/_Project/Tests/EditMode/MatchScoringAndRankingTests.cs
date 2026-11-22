using System;
using System.Linq;
using NUnit.Framework;
using SmallArena.Domain;

namespace SmallArena.Tests.EditMode
{
    public sealed class MatchScoringAndRankingTests
    {
        private static MatchSessionRuntime Create(int count = 2, int target = 0, int lives = 0, double duration = 100)
        {
            var s = new MatchSessionRuntime("scores", new GameplayConfiguration(maxHealth: 10, shotDamage: 1,
                countdownDurationSeconds: 0, respawnDelaySeconds: .1, spawnProtectionSeconds: .01,
                targetKills: target, maximumLives: lives, matchDurationSeconds: duration));
            for (var slot = 0; slot < count; slot++) s.AddParticipant("P" + slot, slot, slot, 0);
            s.LockRoster(0); for (var slot = 0; slot < count; slot++) s.BindSpawn(slot, 0);
            s.Tick(0); return s;
        }
        [Test] public void ClockNeverExceedsConfiguredDurationAtFractionalStartTimes()
        {
            for (var n = 1; n <= 100; n++)
            {
                var now = n * .137;
                var s = new MatchSessionRuntime("clock", new GameplayConfiguration(countdownDurationSeconds: 0));
                s.AddParticipant("P", 0, 0, now); s.AddParticipant("Q", 1, 1, now);
                s.LockRoster(now); s.BindSpawn(0, now); s.BindSpawn(1, now); s.Tick(now);
                Assert.That(s.Phase, Is.EqualTo(MatchLifecyclePhase.InProgress));
                Assert.That(s.ClockRemaining(now), Is.InRange(0d, s.Configuration.MatchDurationSeconds));
            }
        }
        [Test] public void ActualDamageAndLethalHitCreditOneKillAndDeath()
        {
            var s = Create(); Assert.That(s.RecordDamage("P0", "P1", 7, 1), Is.True);
            Assert.That(s.RecordDamage("P0", "P1", 50, 1), Is.True);
            Assert.That(s.ByParticipant("P0").DamageDealt, Is.EqualTo(10)); Assert.That(s.ByParticipant("P0").Kills, Is.EqualTo(1));
            Assert.That(s.ByParticipant("P1").Deaths, Is.EqualTo(1)); Assert.That(s.ByParticipant("P1").Character.Health, Is.Zero);
            Assert.That(s.RecordDamage("P0", "P1", 1, 1), Is.False);
        }
        [Test] public void OverkillNeverCreditsMoreThanRemainingHealth()
        { var s = Create(); s.RecordDamage("P0", "P1", int.MaxValue, 1); Assert.That(s.ByParticipant("P0").DamageDealt, Is.EqualTo(10)); }
        [Test] public void SelfDeathHasNoKillOrDamageCredit()
        {
            var s = Create(); MatchElimination notice = default; s.Eliminated += value => notice = value;
            s.RecordDamage("P0", "P0", 10, 1);
            var p = s.ByParticipant("P0"); Assert.That(p.Deaths, Is.EqualTo(1)); Assert.That(p.Kills, Is.Zero); Assert.That(p.DamageDealt, Is.Zero);
            Assert.That(notice.AttackerId, Is.Null);
        }
        [Test] public void EnvironmentDeathDoesNotCreditPreviousAttacker()
        {
            var s = Create(); s.RecordDamage("P0", "P1", 7, 1); s.RecordDamage(null, "P1", 10, 1);
            Assert.That(s.ByParticipant("P0").DamageDealt, Is.EqualTo(7)); Assert.That(s.ByParticipant("P0").Kills, Is.Zero);
            Assert.That(s.ByParticipant("P1").Deaths, Is.EqualTo(1));
        }
        [Test] public void DuplicateEliminationCannotAwardStatsOrFeedAgain()
        {
            var s = Create(); var events = 0; s.Eliminated += _ => events++;
            s.RecordDamage("P0", "P1", 10, 1);
            Assert.That(s.RecordElimination("P0", "P1"), Is.False); Assert.That(events, Is.EqualTo(1)); Assert.That(s.ByParticipant("P0").Kills, Is.EqualTo(1));
        }
        [Test] public void AliveVictimCannotBeDeclaredEliminated() => Assert.That(Create().RecordElimination("P0", "P1"), Is.False);
        [Test] public void RespawnRetainsLedgerAndAllowsOnlyOneDeathForNewLife()
        {
            var s = Create(); s.RecordDamage("P0", "P1", 10, 1); s.TryRespawn("P1", 1, 2, true);
            Assert.That(s.TryDamage("P1", 1, 10, 3, "P0", 1), Is.False);
            Assert.That(s.RecordDamage("P0", "P1", 10, 3), Is.True);
            Assert.That(s.ByParticipant("P0").Kills, Is.EqualTo(2)); Assert.That(s.ByParticipant("P1").Deaths, Is.EqualTo(2));
            Assert.That(s.ByParticipant("P0").DamageDealt, Is.EqualTo(20));
        }
        [Test] public void KillsAreFirstRankingCriterion()
        {
            var s = Create(3); s.RecordDamage("P0", "P1", 10, 1);
            Assert.That(s.ComputeFinalStandings().Select(p => p.ParticipantId), Is.EqualTo(new[] { "P0", "P2", "P1" }));
        }
        [Test] public void EqualKillsPreferFewerDeaths()
        {
            var s = Create(3); s.RecordDamage(null, "P0", 10, 1); s.TryRespawn("P0", 1, 2, true);
            s.RecordDamage("P0", "P1", 10, 3); s.TryRespawn("P1", 1, 4, true); s.RecordDamage("P2", "P1", 10, 5);
            Assert.That(s.ComputeFinalStandings()[0].ParticipantId, Is.EqualTo("P2"));
        }
        [Test] public void EqualKillsAndDeathsPreferHigherActualDamage()
        {
            var s = Create(3); s.RecordDamage("P0", "P2", 1, 1); s.RecordDamage("P1", "P2", 2, 1);
            Assert.That(s.ComputeFinalStandings()[0].ParticipantId, Is.EqualTo("P1"));
        }
        [Test] public void FullTiesUseSlotIndexAndRanksAreContiguous()
        {
            var s = Create(4); var rows = s.ComputeFinalStandings();
            Assert.That(rows.Select(p => p.SlotIndex), Is.EqualTo(new[] { 0, 1, 2, 3 })); Assert.That(rows.Select(p => p.Rank), Is.EqualTo(new[] { 1, 2, 3, 4 }));
        }
        [Test] public void TargetKillsEndsImmediatelyAndFreezesWinner()
        {
            var s = Create(target: 2); s.RecordDamage("P0", "P1", 10, 1);
            Assert.That(s.Phase, Is.EqualTo(MatchLifecyclePhase.InProgress)); s.TryRespawn("P1", 1, 2, true);
            s.RecordDamage("P0", "P1", 10, 3);
            Assert.That(s.Phase, Is.EqualTo(MatchLifecyclePhase.Ended)); Assert.That(s.WinnerParticipantId, Is.EqualTo("P0"));
            Assert.That(s.ByParticipant("P0").IsWinner, Is.True); Assert.That(s.RecordDamage("P0", "P1", 10, 4), Is.False);
            s.Disconnect(0, 4); Assert.That(s.WinnerParticipantId, Is.EqualTo("P0"));
        }
        [Test] public void TimeoutRanksDamageAndFreezesAtInclusiveDeadline()
        {
            var s = Create(duration: 10); s.RecordDamage("P1", "P0", 2, 1);
            Assert.That(s.RecordDamage("P0", "P1", 10, 10), Is.False); s.Tick(10);
            Assert.That(s.Phase, Is.EqualTo(MatchLifecyclePhase.Ended)); Assert.That(s.WinnerParticipantId, Is.EqualTo("P1"));
        }
        [Test] public void TimeoutWithNoScoreUsesLowestSlot() { var s = Create(duration: 1); s.Tick(1); Assert.That(s.WinnerParticipantId, Is.EqualTo("P0")); }
        [Test] public void FiniteLivesTriggerEndsAndKeepsAllRows()
        { var s = Create(lives: 1); s.RecordDamage("P0", "P1", 10, 1); Assert.That(s.Phase, Is.EqualTo(MatchLifecyclePhase.Ended)); Assert.That(s.ComputeFinalStandings().Count, Is.EqualTo(2)); }
        [TestCase(0)] [TestCase(-1)] public void InvalidDamageLeavesLedgerUntouched(int amount)
        { var s = Create(); Assert.That(s.RecordDamage("P0", "P1", amount, 1), Is.False); Assert.That(s.ByParticipant("P0").DamageDealt, Is.Zero); }
        [Test] public void InvalidOrDisconnectedAttackerCannotRecordDamage()
        {
            var s = Create(); Assert.That(s.RecordDamage("missing", "P1", 1, 1), Is.False); s.Disconnect(0, 1);
            Assert.That(s.RecordDamage("P0", "P1", 1, 1), Is.False); Assert.That(s.ByParticipant("P1").Character.Health, Is.EqualTo(10));
        }
        [Test] public void SpawnProtectionRejectsAllCredit()
        { var s = Create(); Assert.That(s.RecordDamage("P0", "P1", 10, 0), Is.False); Assert.That(s.ByParticipant("P0").DamageDealt, Is.Zero); }
        [Test] public void DisabledKillTargetContinuesAfterSeveralKills()
        {
            var s = Create(); s.RecordDamage("P0", "P1", 10, 1); s.TryRespawn("P1", 1, 2, true); s.RecordDamage("P0", "P1", 10, 3);
            Assert.That(s.Phase, Is.EqualTo(MatchLifecyclePhase.InProgress));
        }
        [Test] public void TeardownIsNotACompetitiveWin()
        { var s = Create(); s.Teardown(1); Assert.That(s.WinnerParticipantId, Is.Empty); Assert.That(s.ByParticipant("P0").IsWinner, Is.False); }
        [Test] public void NewSessionHasZeroStatsAndNoWinner()
        {
            var s = Create(target: 1); s.RecordDamage("P0", "P1", 10, 1); var next = Create();
            Assert.That(next.Participants.All(p => p.Kills == 0 && p.Deaths == 0 && p.DamageDealt == 0), Is.True); Assert.That(next.WinnerParticipantId, Is.Empty);
        }
        [Test] public void InvalidCompetitiveConfigRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameplayConfiguration(targetKills: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameplayConfiguration(postMatchDurationSeconds: double.NaN));
        }
    }
}
