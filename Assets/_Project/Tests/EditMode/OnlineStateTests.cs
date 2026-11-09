using System;
using NUnit.Framework;
using SmallArena.Domain;

namespace SmallArena.Tests.EditMode
{
    // Focused Domain slices: FR/AC/TC-202 (eligibility), 205 (HP/life),
    // 206 (respawn/protection), 207 (match/terminal). No physics/network acceptance claim.
    [TestFixture]
    public sealed class OnlineStateTests
    {
        private static OnlineMatch Playing(GameplayConfiguration configuration = null)
        {
            var match = new OnlineMatch(configuration ?? new GameplayConfiguration());
            Assert.That(match.TryStart(0, true), Is.True);
            return match;
        }

        private static OnlineCharacterState Snapshot(OnlineMatch match, CharacterLifeState life = CharacterLifeState.Alive,
            CharacterConnectionState connection = CharacterConnectionState.Connected, int hp = 100,
            long lifeId = 7, double cooldown = 20, double protection = 1, double? respawn = null,
            int score = 4, int deaths = 2, string participant = "participant-a") =>
            new OnlineCharacterState(match, participant, lifeId, life, connection, hp, score, deaths, cooldown, protection, respawn);

        [Test]
        public void InitialRosterSpawn_IsFullHealth_WithProtectionAndZeroLedger()
        {
            var match = new OnlineMatch(new GameplayConfiguration(maxHealth: 120));
            var state = OnlineCharacterState.CreateInitial(match, "a", 10);
            Assert.That(state.Health, Is.EqualTo(120));
            Assert.That(state.LifeId, Is.EqualTo(1));
            Assert.That(state.Score, Is.Zero);
            Assert.That(state.Deaths, Is.Zero);
            Assert.That(state.NextShotAtSeconds, Is.Zero);
            Assert.That(state.ProtectionUntilSeconds, Is.EqualTo(11));
            Assert.That(state.RespawnAtSeconds, Is.Null);
            Assert.That(state.CanReceiveControl(10), Is.False);
            Assert.That(match.TryStart(10, true), Is.True);
            Assert.That(state.CanReceiveControl(10), Is.True);
            Assert.That(state.CanFire(10.999), Is.False);
            Assert.That(state.CanFire(11), Is.True);
            Assert.Throws<InvalidOperationException>(() => OnlineCharacterState.CreateInitial(match, "b", 11));
        }

        [Test]
        public void WaitingPlayingFinished_OnlyAllowedTransitionsSucceed()
        {
            var match = new OnlineMatch(new GameplayConfiguration());
            Assert.That(match.State, Is.EqualTo(OnlineMatchState.Waiting));
            Assert.That(match.IsGameplayActive(0), Is.False);
            Assert.That(match.TryFinish(190), Is.False);
            Assert.That(match.TryStart(10, false), Is.False);
            Assert.That(match.StartedAtSeconds, Is.Null);
            Assert.That(match.DeadlineSeconds, Is.Null);
            Assert.That(match.TryStart(10, true), Is.True);
            Assert.That(match.StartedAtSeconds, Is.EqualTo(10));
            Assert.That(match.DeadlineSeconds, Is.EqualTo(190));
            Assert.That(match.IsGameplayActive(9.999), Is.False);
            Assert.That(match.IsGameplayActive(10), Is.True);
            Assert.That(match.IsGameplayActive(189.999), Is.True);
            Assert.That(match.IsGameplayActive(190), Is.False);
            Assert.That(match.IsGameplayActive(190.001), Is.False);
            Assert.That(match.TryStart(11, true), Is.False);
            Assert.That(match.TryFinish(190), Is.True);
            Assert.That(match.State, Is.EqualTo(OnlineMatchState.Finished));
            Assert.That(match.TryFinish(190), Is.False);
            Assert.That(match.TryAbort(), Is.False);
            Assert.That(match.TryStart(200, true), Is.False);
        }

        [TestCase(179.999, false)]
        [TestCase(180.0, true)]
        [TestCase(180.001, true)]
        public void FinishRequiresMatchDeadline_WithoutChoosingResults(double time, bool expected)
        {
            var match = Playing();
            Assert.That(match.TryFinish(time), Is.EqualTo(expected));
            Assert.That(match.State, Is.EqualTo(expected ? OnlineMatchState.Finished : OnlineMatchState.Playing));
        }
        [TestCase(false)]
        [TestCase(true)]
        public void Abort_IsAllowedFromWaitingOrPlaying_AndTerminal(bool playing)
        {
            var match = playing ? Playing() : new OnlineMatch(new GameplayConfiguration());
            Assert.That(match.TryAbort(), Is.True);
            Assert.That(match.State, Is.EqualTo(OnlineMatchState.Aborted));
            Assert.That(match.TryAbort(), Is.False);
            Assert.That(match.TryFinish(190), Is.False);
            Assert.That(match.TryStart(1, true), Is.False);
            Assert.That(match.IsGameplayActive(1), Is.False);
        }

        [TestCase(-1.0)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void InvalidServerTime_IsRejectedWithoutMutation(double time)
        {
            var match = new OnlineMatch(new GameplayConfiguration());
            Assert.Throws<ArgumentOutOfRangeException>(() => match.TryStart(time, true));
            Assert.Throws<ArgumentOutOfRangeException>(() => match.IsGameplayActive(time));
            Assert.That(match.State, Is.EqualTo(OnlineMatchState.Waiting));
            Assert.That(match.DeadlineSeconds, Is.Null);
        }

        [Test]
        public void UnrepresentableDeadlines_RejectWithoutChangingState()
        {
            var match = new OnlineMatch(new GameplayConfiguration(matchDurationSeconds: double.MaxValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => match.TryStart(double.MaxValue, true));
            Assert.That(match.State, Is.EqualTo(OnlineMatchState.Waiting));
            var tiny = new OnlineMatch(new GameplayConfiguration(matchDurationSeconds: double.Epsilon));
            Assert.Throws<ArgumentOutOfRangeException>(() => tiny.TryStart(1, true));
            Assert.That(tiny.State, Is.EqualTo(OnlineMatchState.Waiting));
        }

        [TestCase(CharacterLifeState.Alive, 0, false)]
        [TestCase(CharacterLifeState.Alive, -1, false)]
        [TestCase(CharacterLifeState.Alive, 101, false)]
        [TestCase(CharacterLifeState.Alive, 100, true)]
        [TestCase(CharacterLifeState.Dead, 1, true)]
        [TestCase(CharacterLifeState.Dead, 0, false)]
        [TestCase(CharacterLifeState.RespawnPending, 1, true)]
        [TestCase(CharacterLifeState.RespawnPending, 0, false)]
        public void IncoherentHpOrRespawnDeadline_IsRejected(CharacterLifeState life, int hp, bool hasDeadline)
        {
            Assert.That(() => Snapshot(Playing(), life: life, hp: hp, respawn: hasDeadline ? 5.0 : (double?)null), Throws.InstanceOf<ArgumentException>());
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void MissingParticipantIdentity_IsRejected(string id) =>
            Assert.Throws<ArgumentException>(() => Snapshot(Playing(), participant: id));

        [Test]
        public void InvalidSnapshotValues_AreRejected()
        {
            var match = Playing();
            Assert.Throws<ArgumentNullException>(() => Snapshot(null));
            Assert.Throws<ArgumentOutOfRangeException>(() => Snapshot(match, lifeId: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Snapshot(match, lifeId: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Snapshot(match, life: (CharacterLifeState)99));
            Assert.Throws<ArgumentOutOfRangeException>(() => Snapshot(match, connection: (CharacterConnectionState)99));
            Assert.Throws<ArgumentOutOfRangeException>(() => Snapshot(match, score: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Snapshot(match, deaths: -1));
            foreach (var time in new[] { -1.0, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => Snapshot(match, cooldown: time));
                Assert.Throws<ArgumentOutOfRangeException>(() => Snapshot(match, protection: time));
                Assert.Throws<ArgumentOutOfRangeException>(() => Snapshot(match, life: CharacterLifeState.Dead, hp: 0, respawn: time));
            }
        }

        [Test]
        public void DisconnectAlive_RemainsVulnerable_ReconnectPreservesAllLifeData()
        {
            var original = Snapshot(Playing(), hp: 75);
            Assert.That(original.TrySetConnection(CharacterConnectionState.Disconnected, out var offline), Is.True);
            Assert.That(offline.LifeState, Is.EqualTo(CharacterLifeState.Alive));
            Assert.That(offline.CanReceiveControl(2), Is.False);
            Assert.That(offline.CanFire(20), Is.False);
            Assert.That(offline.CanReceiveDamage(2), Is.True);
            Assert.That(offline.TryReduceHealth(7, 50, 2, out var hurt), Is.True);
            Assert.That(hurt.TrySetConnection(CharacterConnectionState.Connected, out var resumed), Is.True);
            Assert.That(resumed.Health, Is.EqualTo(50));
            Assert.That(resumed.LifeId, Is.EqualTo(7));
            Assert.That(resumed.ParticipantId, Is.EqualTo(original.ParticipantId));
            Assert.That(resumed.Score, Is.EqualTo(4));
            Assert.That(resumed.Deaths, Is.EqualTo(2));
            Assert.That(resumed.ProtectionUntilSeconds, Is.EqualTo(1));
            Assert.That(resumed.NextShotAtSeconds, Is.EqualTo(20));
            Assert.That(resumed.CanFire(19.999), Is.False);
            Assert.That(resumed.CanFire(20), Is.True);
            Assert.That(resumed.TrySetConnection(CharacterConnectionState.Connected, out var unchanged), Is.False);
            Assert.That(unchanged, Is.SameAs(resumed));
            Assert.That(original.Health, Is.EqualTo(75), "Snapshots are immutable.");
        }

        [Test]
        public void HealthReduction_EnforcesBounds_NoHealing_AndOneDeathPerCurrentLife()
        {
            var state = Snapshot(Playing());
            Assert.Throws<ArgumentOutOfRangeException>(() => state.TryReduceHealth(7, -1, 2, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => state.TryReduceHealth(7, 101, 2, out _));
            Assert.That(state.TryReduceHealth(7, 100, 2, out _), Is.False);
            Assert.That(state.TryReduceHealth(6, 0, 2, out _), Is.False);
            foreach (var hp in new[] { 75, 50, 25, 0 })
            {
                Assert.That(state.TryReduceHealth(7, hp, 2, out var next), Is.True);
                state = next;
                Assert.That(state.Health, Is.EqualTo(hp));
            }
            Assert.That(state.LifeState, Is.EqualTo(CharacterLifeState.Dead));
            Assert.That(state.RespawnAtSeconds, Is.EqualTo(5));
            Assert.That(state.TryReduceHealth(7, 0, 2, out _), Is.False);
            Assert.That(state.TryReduceHealth(7, 100, 3, out _), Is.False);
            Assert.That(state.CanReceiveControl(3), Is.False);
            Assert.That(state.CanFire(20), Is.False);
            Assert.That(state.CanReceiveDamage(3), Is.False);
        }

        [TestCase(4.999, false)]
        [TestCase(5.0, true)]
        [TestCase(5.001, true)]
        public void RespawnPending_UsesInclusiveDeadline_EvenWhileDisconnected(double now, bool expected)
        {
            var state = Snapshot(Playing(), life: CharacterLifeState.Dead, connection: CharacterConnectionState.Disconnected, hp: 0, respawn: 5);
            Assert.That(state.TryMarkRespawnPending(now, out var pending), Is.EqualTo(expected));
            Assert.That(pending.LifeState, Is.EqualTo(expected ? CharacterLifeState.RespawnPending : CharacterLifeState.Dead));
            Assert.That(pending.TryRespawn(7, now, true, out _), Is.False);
            Assert.That(pending.Health, Is.Zero);
            Assert.That(pending.RespawnAtSeconds, Is.EqualTo(5));
        }

        [Test]
        public void OfflineDeath_ResumePending_SafeSpawnCreatesDistinctFullLifeAndRetainsLedgerAndCooldown()
        {
            var state = Snapshot(Playing(), connection: CharacterConnectionState.Disconnected);
            Assert.That(state.TryReduceHealth(7, 0, 2, out var dead), Is.True);
            Assert.That(dead.TryRespawn(7, 5, true, out _), Is.False, "Dead must first become pending.");
            Assert.That(dead.TryMarkRespawnPending(5, out var pending), Is.True);
            Assert.That(pending.TryMarkRespawnPending(6, out _), Is.False);
            Assert.That(pending.TrySetConnection(CharacterConnectionState.Connected, out var connected), Is.True);
            Assert.That(connected.RespawnAtSeconds, Is.EqualTo(5));
            Assert.That(connected.TryRespawn(7, 4.999, true, out _), Is.False);
            Assert.That(connected.TryRespawn(7, 5, false, out var blocked), Is.False);
            Assert.That(blocked, Is.SameAs(connected));
            Assert.That(connected.TryRespawn(6, 5, true, out _), Is.False);
            Assert.That(connected.TryRespawn(7, 5, true, out var spawned), Is.True);
            Assert.That(spawned.LifeId, Is.EqualTo(8));
            Assert.That(spawned.Health, Is.EqualTo(100));
            Assert.That(spawned.LifeState, Is.EqualTo(CharacterLifeState.Alive));
            Assert.That(spawned.RespawnAtSeconds, Is.Null);
            Assert.That(spawned.ParticipantId, Is.EqualTo(state.ParticipantId));
            Assert.That(spawned.Score, Is.EqualTo(4));
            Assert.That(spawned.Deaths, Is.EqualTo(2));
            Assert.That(spawned.NextShotAtSeconds, Is.EqualTo(20));
            Assert.That(spawned.ProtectionUntilSeconds, Is.EqualTo(6));
            Assert.That(spawned.CanReceiveControl(5), Is.True);
            Assert.That(spawned.CanReceiveDamage(5.999), Is.False);
            Assert.That(spawned.CanReceiveDamage(6), Is.True);
            Assert.That(spawned.CanReceiveDamage(6.001), Is.True);
            Assert.That(spawned.CanFire(6), Is.False, "Old cooldown survives protection expiry.");
            Assert.That(spawned.CanFire(20), Is.True);
            Assert.That(spawned.TryReduceHealth(7, 0, 20, out _), Is.False);
            Assert.That(spawned.IsCurrentLife(state.ParticipantId, 7), Is.False);
            Assert.That(spawned.IsCurrentLife("other", 8), Is.False);
            Assert.That(spawned.IsCurrentLife(state.ParticipantId, 8), Is.True);
            Assert.That(spawned.TryRespawn(8, 20, true, out _), Is.False);
        }

        [TestCase(5.999, false)]
        [TestCase(6.0, true)]
        [TestCase(6.001, true)]
        public void ProtectionDeadline_BlocksDamageAndFire_ButAllowsMovement(double now, bool expected)
        {
            var state = Snapshot(Playing(), cooldown: 0, protection: 6);
            Assert.That(state.CanReceiveControl(now), Is.True);
            Assert.That(state.CanFire(now), Is.EqualTo(expected));
            Assert.That(state.TryReduceHealth(7, 25, now, out _), Is.EqualTo(expected));
        }

        [Test]
        public void PendingLifeCounterOverflow_IsRejected()
        {
            var state = Snapshot(Playing(), life: CharacterLifeState.RespawnPending, hp: 0, lifeId: long.MaxValue, respawn: 5);
            Assert.That(state.TryRespawn(long.MaxValue, 5, true, out var next), Is.False);
            Assert.That(next, Is.SameAs(state));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TerminalMatch_RejectsAllCharacterTransitions_EvenForEarlierServerTimes(bool abort)
        {
            var match = Playing();
            var alive = Snapshot(match);
            var dead = Snapshot(match, life: CharacterLifeState.Dead, hp: 0, respawn: 5);
            var pending = Snapshot(match, life: CharacterLifeState.RespawnPending, hp: 0, respawn: 5);
            Assert.That(abort ? match.TryAbort() : match.TryFinish(190), Is.True);
            Assert.That(alive.CanReceiveControl(2), Is.False);
            Assert.That(alive.CanFire(20), Is.False);
            Assert.That(alive.CanReceiveDamage(2), Is.False);
            Assert.That(alive.TryReduceHealth(7, 0, 2, out var next), Is.False);
            Assert.That(next, Is.SameAs(alive));
            Assert.That(alive.TrySetConnection(CharacterConnectionState.Disconnected, out _), Is.False);
            Assert.That(dead.TryMarkRespawnPending(5, out _), Is.False);
            Assert.That(pending.TryRespawn(7, 5, true, out _), Is.False);
        }

        [Test]
        public void WaitingMatch_RejectsHealthAndRespawnTransitions()
        {
            var match = new OnlineMatch(new GameplayConfiguration());
            var alive = Snapshot(match);
            var dead = Snapshot(match, life: CharacterLifeState.Dead, hp: 0, respawn: 5);
            var pending = Snapshot(match, life: CharacterLifeState.RespawnPending, hp: 0, respawn: 5);
            Assert.That(alive.TryReduceHealth(7, 0, 5, out _), Is.False);
            Assert.That(dead.TryMarkRespawnPending(5, out _), Is.False);
            Assert.That(pending.TryRespawn(7, 5, true, out _), Is.False);
            Assert.That(alive.CanFire(20), Is.False);
        }

        [TestCase(CharacterLifeState.Alive, 1, false)]
        [TestCase(CharacterLifeState.Alive, 100, false)]
        [TestCase(CharacterLifeState.Dead, 0, true)]
        [TestCase(CharacterLifeState.RespawnPending, 0, true)]
        public void CoherentHpAndLifeBoundaries_AreAccepted(CharacterLifeState life, int hp, bool hasDeadline)
        {
            var state = Snapshot(Playing(), life: life, hp: hp, respawn: hasDeadline ? 5.0 : (double?)null);
            Assert.That(state.Health, Is.EqualTo(hp));
            Assert.That(state.LifeState, Is.EqualTo(life));
            foreach (var property in typeof(OnlineCharacterState).GetProperties())
                Assert.That(property.GetSetMethod(true), Is.Null, property.Name);
        }

        [Test]
        public void HealthCannotIncreaseDuringAnExistingLife()
        {
            var state = Snapshot(Playing(), hp: 25);
            Assert.That(state.TryReduceHealth(7, 50, 2, out var next), Is.False);
            Assert.That(next, Is.SameAs(state));
            Assert.That(state.TryMarkRespawnPending(5, out _), Is.False);
            Assert.That(state.TryRespawn(7, 5, true, out _), Is.False);
        }

        [Test]
        public void UnrepresentableLifeDeadlines_RejectWithoutMutatingSnapshot()
        {
            var match = Playing(new GameplayConfiguration(respawnDelaySeconds: double.Epsilon));
            var alive = Snapshot(match);
            Assert.Throws<ArgumentOutOfRangeException>(() => alive.TryReduceHealth(7, 0, 2, out _));
            Assert.That(alive.Health, Is.EqualTo(100));
            match = Playing(new GameplayConfiguration(spawnProtectionSeconds: double.Epsilon));
            var pending = Snapshot(match, life: CharacterLifeState.RespawnPending, hp: 0, respawn: 5);
            Assert.Throws<ArgumentOutOfRangeException>(() => pending.TryRespawn(7, 5, true, out _));
            Assert.That(pending.LifeState, Is.EqualTo(CharacterLifeState.RespawnPending));
        }

        [TestCase(180.0)]
        [TestCase(180.001)]
        public void MatchDeadline_RejectsGameplayBeforeExplicitFinish(double now)
        {
            var match = Playing();
            var alive = Snapshot(match);
            var dead = Snapshot(match, life: CharacterLifeState.Dead, hp: 0, respawn: 180);
            var pending = Snapshot(match, life: CharacterLifeState.RespawnPending, hp: 0, respawn: 180);
            Assert.That(alive.TryReduceHealth(7, 0, now, out _), Is.False);
            Assert.That(dead.TryMarkRespawnPending(now, out _), Is.False);
            Assert.That(pending.TryRespawn(7, now, true, out _), Is.False);
            Assert.That(alive.CanReceiveControl(now), Is.False);
            Assert.That(alive.CanFire(now), Is.False);
            Assert.That(match.State, Is.EqualTo(OnlineMatchState.Playing), "The orchestrator commits the result.");
        }
    }
}
