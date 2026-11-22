using System;
using NUnit.Framework;
using SmallArena.Domain;

namespace SmallArena.Tests.EditMode
{
    public sealed class FairLootSpawnTests
    {
        [Test] public void PickUpDoesNotStartTimer_EvenAfterLongCarry()
        {
            var t = new LootRespawnTracker(); Assert.That(t.TryPickUp("A"), Is.True); t.Tick(1000);
            Assert.That(t.State, Is.EqualTo(LootLifecycleState.CarriedByPlayer)); Assert.That(t.CarrierId, Is.EqualTo("A"));
            Assert.That(t.RemainingSeconds, Is.Zero); Assert.That(t.IsReady, Is.False);
        }
        [Test] public void CarrierLossStartsFullThirtySeconds_AndDuplicateLossDoesNotRestartIt()
        {
            var t = new LootRespawnTracker(); t.TryPickUp("A");
            Assert.That(t.CarrierLost("B"), Is.False); Assert.That(t.CarrierLost("A"), Is.True);
            t.Tick(29.5); Assert.That(t.IsReady, Is.False); Assert.That(t.RemainingSeconds, Is.EqualTo(.5));
            Assert.That(t.CarrierLost("A"), Is.False); t.Tick(.5); Assert.That(t.IsReady, Is.True);
            Assert.That(t.State, Is.EqualTo(LootLifecycleState.RespawningAfterDeath), "Wait for a successful physical spawn.");
            Assert.That(t.ConfirmSpawn(), Is.True); Assert.That(t.ConfirmSpawn(), Is.False);
            Assert.That(t.State, Is.EqualTo(LootLifecycleState.AvailableOnMap));
        }
        [Test] public void ExactlyOneCarrierCanWinPickup_AndSecondCycleHasFreshTimer()
        {
            var t = new LootRespawnTracker(.25); Assert.That(t.TryPickUp("A"), Is.True); Assert.That(t.TryPickUp("B"), Is.False);
            t.CarrierLost("A"); Assert.That(t.TryPickUp("B"), Is.False); t.Tick(.25); t.ConfirmSpawn();
            Assert.That(t.TryPickUp("B"), Is.True); t.CarrierLost("B"); Assert.That(t.RemainingSeconds, Is.EqualTo(.25));
        }
        [Test] public void MatchEndCancelsReadyPendingAndCarriedStates()
        {
            foreach (var elapsed in new[] { 0d, 10d, 40d })
            {
                var t = new LootRespawnTracker(); t.TryPickUp("A"); t.CarrierLost("A"); t.Tick(elapsed); t.Cancel(); t.Tick(100);
                Assert.That(t.IsReady, Is.False); Assert.That(t.ConfirmSpawn(), Is.False); Assert.That(t.TryPickUp("B"), Is.False);
                Assert.That(t.CarrierId, Is.Null); Assert.That(t.RemainingSeconds, Is.Zero);
            }
        }
        [TestCase(null)] [TestCase("")] [TestCase(" ")]
        public void InvalidCarrierCannotConsumeAvailableLoot(string id)
        { var t = new LootRespawnTracker(); Assert.That(t.TryPickUp(id), Is.False); Assert.That(t.State, Is.EqualTo(LootLifecycleState.AvailableOnMap)); }
        [TestCase(0d)] [TestCase(-1d)] [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)]
        public void InvalidDelayRejected(double delay) => Assert.Throws<ArgumentOutOfRangeException>(() => new LootRespawnTracker(delay));
        [TestCase(-1d)] [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)]
        public void InvalidTickRejectedWithoutChangingTimer(double dt)
        { var t = new LootRespawnTracker(); t.TryPickUp("A"); t.CarrierLost("A"); Assert.Throws<ArgumentOutOfRangeException>(() => t.Tick(dt)); Assert.That(t.RemainingSeconds, Is.EqualTo(30)); }
        [Test] public void ClusteredPlayersPreferOppositeQuadrantByNearestDistance()
        {
            var candidates = new[] { new LootPoint(-7, -7), new LootPoint(0, 0), new LootPoint(7, 7), new LootPoint(7, -7) };
            var players = new[] { new LootPoint(-8, -8), new LootPoint(-6, -8) };
            Assert.That(FairLootSpawnSelector.SelectIndex(candidates, players), Is.EqualTo(2));
        }
        [Test] public void MaximinDoesNotFavorLargeAverageDistanceWithOnePlayerAdjacent()
        {
            var candidates = new[] { new LootPoint(.1, 0), new LootPoint(5, 0) };
            Assert.That(FairLootSpawnSelector.SelectIndex(candidates, new[] { new LootPoint(0, 0), new LootPoint(10, 0) }), Is.EqualTo(1));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(-1)]
        public void EmptyPlayerSetHasDeterministicSeededFallback(int tie)
        {
            var candidates = new[] { new LootPoint(0, 0), new LootPoint(1, 1) };
            Assert.That(FairLootSpawnSelector.SelectIndex(candidates, Array.Empty<LootPoint>(), tie), Is.EqualTo((tie % 2 + 2) % 2));
        }
        [Test] public void SinglePlayerChoosesFarthest_RegardlessOfTieSeed()
        { Assert.That(FairLootSpawnSelector.SelectIndex(new[] { new LootPoint(1, 0), new LootPoint(8, 8) }, new[] { new LootPoint(0, 0) }, 1), Is.EqualTo(1)); }
        [Test] public void EqualScoresAreRepeatableAndSeedCanSelectEitherSide()
        {
            var candidates = new[] { new LootPoint(-5, 0), new LootPoint(5, 0) }; var players = new[] { new LootPoint(0, 0) };
            Assert.That(FairLootSpawnSelector.SelectIndex(candidates, players, 0), Is.EqualTo(0));
            Assert.That(FairLootSpawnSelector.SelectIndex(candidates, players, 1), Is.EqualTo(1));
        }
        [Test] public void NoCandidateReturnsNoSelection() => Assert.That(FairLootSpawnSelector.SelectIndex(Array.Empty<LootPoint>(), Array.Empty<LootPoint>()), Is.EqualTo(-1));
        [Test] public void NonfinitePointsRejected() => Assert.Throws<ArgumentOutOfRangeException>(() => new LootPoint(double.NaN, 0));
        [Test] public void NullCandidateInputRejected() => Assert.Throws<ArgumentNullException>(() => FairLootSpawnSelector.SelectIndex(null, Array.Empty<LootPoint>()));
        [Test] public void DefaultConfigHasThirtySecondWeaponDelay() => Assert.That(new GameplayConfiguration().WeaponRespawnDelaySeconds, Is.EqualTo(30));
    }
}
