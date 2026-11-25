using System;
using NUnit.Framework;
using SmallArena.Domain;

namespace SmallArena.Tests.EditMode
{
    public sealed class NetworkAdaptersUnitTests
    {
        [Test] public void SequencesMustIncreaseWithinCurrentLife()
        {
            var gate = new ShotRequestGate();
            Assert.That(gate.Process(1, 1, 0, 10, 10), Is.EqualTo(ShotRejection.StaleSequence));
            Assert.That(gate.Process(1, 1, 1, 10, 10), Is.EqualTo(ShotRejection.None));
            Assert.That(gate.Process(1, 1, 1, 10, 10), Is.EqualTo(ShotRejection.StaleSequence));
            Assert.That(gate.Process(1, 1, 2, 10, 10), Is.EqualTo(ShotRejection.None));
        }
        [Test] public void NewLifeStartsAtOne_AndOldLifeCannotResetIt()
        {
            var gate = new ShotRequestGate(); gate.Process(1, 1, 900, 10, 10);
            Assert.That(gate.Process(2, 2, 1, 11, 11), Is.EqualTo(ShotRejection.None));
            Assert.That(gate.Process(2, 1, 999, 11, 11), Is.EqualTo(ShotRejection.WrongLife));
            Assert.That(gate.Process(2, 2, 1, 11, 11), Is.EqualTo(ShotRejection.StaleSequence));
        }
        [TestCase(.249, ShotRejection.None)] [TestCase(.25, ShotRejection.None)] [TestCase(.251, ShotRejection.Expired)]
        public void AgeWindowIsInclusiveAtQuarterSecond(double age, ShotRejection expected)
            => Assert.That(new ShotRequestGate().Process(1, 1, 1, 10 - age, 10), Is.EqualTo(expected));
        [TestCase(10.1)] [TestCase(double.MaxValue)]
        public void FutureClockIsClampedToServerNow(double timestamp)
            => Assert.That(new ShotRequestGate().Process(1, 1, 1, timestamp, 10), Is.EqualTo(ShotRejection.None));
        [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)] [TestCase(-1d)]
        public void InvalidTimestampIsRejected(double timestamp)
            => Assert.That(new ShotRequestGate().Process(1, 1, 1, timestamp, 10), Is.EqualTo(ShotRejection.InvalidTime));
        [Test] public void ExpiredSequenceCannotBeRetriedWithFreshTimestamp()
        {
            var gate = new ShotRequestGate();
            Assert.That(gate.Process(1, 1, 1, 9, 10), Is.EqualTo(ShotRejection.Expired));
            Assert.That(gate.Process(1, 1, 1, 10, 10), Is.EqualTo(ShotRejection.StaleSequence));
            Assert.That(gate.Process(1, 1, 2, 10, 10), Is.EqualTo(ShotRejection.None));
        }
        [Test] public void SequenceCannotWrap_ButNewSessionCanResetSameLifeNumber()
        {
            var gate = new ShotRequestGate(); gate.Process(1, 1, ulong.MaxValue, 10, 10);
            Assert.That(gate.Process(1, 1, 1, 10, 10), Is.EqualTo(ShotRejection.StaleSequence));
            gate.Reset(); Assert.That(gate.Process(1, 1, 1, 10, 10), Is.EqualTo(ShotRejection.None));
        }
        [TestCase(10, ParticipantLifeState.Alive, 1, MatchLifecyclePhase.InProgress, "Playing", true, false)]
        [TestCase(0, ParticipantLifeState.Respawning, 1, MatchLifecyclePhase.InProgress, "Playing", false, false)]
        [TestCase(10, ParticipantLifeState.Alive, 2, MatchLifecyclePhase.InProgress, "Playing", true, true)]
        [TestCase(10, ParticipantLifeState.Alive, 1, MatchLifecyclePhase.Ended, "Victory", true, false)]
        [TestCase(0, ParticipantLifeState.Eliminated, 1, MatchLifecyclePhase.Ended, "Defeat", false, false)]
        public void SnapshotCarriesAuthoritativeLifeBuffStatsClockAndResult(int hp, ParticipantLifeState life, int damage,
            MatchLifecyclePhase phase, string result, bool alive, bool buffed)
        {
            var context = new object();
            var s = new PlayerPresentationSnapshot(context, 4, hp, 10, 2, 42, result, "Cooldown", 7, life, phase,
                damage, 1, 3, 12, 2, .5, life == ParticipantLifeState.Respawning ? 3 : 0);
            Assert.That(s.Context, Is.SameAs(context)); Assert.That(s.Revision, Is.EqualTo(4));
            Assert.That(s.Health, Is.EqualTo(hp)); Assert.That(s.MaxHealth, Is.EqualTo(10)); Assert.That(s.LifeId, Is.EqualTo(7));
            Assert.That(s.IsAlive, Is.EqualTo(alive)); Assert.That(s.IsWeaponBuffed, Is.EqualTo(buffed));
            Assert.That(s.LifeState, Is.EqualTo(life)); Assert.That(s.Phase, Is.EqualTo(phase));
            Assert.That(s.Kills, Is.EqualTo(2)); Assert.That(s.Deaths, Is.EqualTo(3)); Assert.That(s.DamageDealt, Is.EqualTo(12));
            Assert.That(s.Rank, Is.EqualTo(2)); Assert.That(s.RemainingSeconds, Is.EqualTo(42));
            Assert.That(s.CooldownRemainingSeconds, Is.EqualTo(.5)); Assert.That(s.Result, Is.EqualTo(result));
            Assert.That(s.Rejection, Is.EqualTo("Cooldown"));
        }
        [TestCase(-1d)] [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)]
        public void SnapshotRejectsInvalidCooldown(double cooldown)
            => Assert.Throws<ArgumentOutOfRangeException>(() => new PlayerPresentationSnapshot(new object(), 1, 10, 10, 0, 1, "", "", cooldownRemainingSeconds: cooldown));
    }
}
