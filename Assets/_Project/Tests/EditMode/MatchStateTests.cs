using System;
using System.Collections.Generic;
using NUnit.Framework;
using SmallArena.Domain;

namespace SmallArena.Tests.EditMode
{
    // Domain coverage for AC-001 / TC-029, AC-015 / TC-024 and AC-017 / TC-026.
    // Scene validation, Health defeat and combat cancellation need later integration tests.
    [TestFixture]
    public sealed class MatchStateTests
    {
        private static MatchReadinessResult Ready => new MatchReadinessResult(true);

        [Test]
        public void NewMachine_IsInitializing_AndNotPlaying()
        {
            var machine = new MatchStateMachine();

            Assert.That(machine.CurrentState, Is.EqualTo(MatchState.Initializing));
            Assert.That(machine.IsPlaying, Is.False);
        }

        [Test]
        public void CurrentState_HasNoPublicSetter()
        {
            var property = typeof(MatchStateMachine).GetProperty(nameof(MatchStateMachine.CurrentState));

            Assert.That(property.GetSetMethod(), Is.Null);
        }

        [TestCase(false, null)]
        [TestCase(false, "")]
        [TestCase(false, "Missing bot reference")]
        [TestCase(true, null)]
        [TestCase(true, "")]
        [TestCase(true, "Validation complete")]
        public void Readiness_PreservesExplicitFlag_AndNormalizesDiagnostic(bool isReady, string diagnostic)
        {
            var readiness = new MatchReadinessResult(isReady, diagnostic);

            Assert.That(readiness.IsReady, Is.EqualTo(isReady));
            Assert.That(readiness.DiagnosticMessage, Is.EqualTo(diagnostic ?? string.Empty));
            Assert.That(typeof(MatchReadinessResult).GetProperty(nameof(MatchReadinessResult.IsReady)).CanWrite, Is.False);
            Assert.That(typeof(MatchReadinessResult).GetProperty(nameof(MatchReadinessResult.DiagnosticMessage)).CanWrite, Is.False);
        }

        [Test]
        public void DefaultReadiness_IsNotReady_AndCannotStart()
        {
            var readiness = default(MatchReadinessResult);
            var machine = new MatchStateMachine();
            var notifications = new List<MatchState>();
            machine.OnStateChanged += notifications.Add;

            Assert.That(readiness.IsReady, Is.False);
            Assert.That(readiness.DiagnosticMessage, Is.Empty);
            Assert.That(machine.StartMatch(readiness), Is.False);
            Assert.That(machine.CurrentState, Is.EqualTo(MatchState.Initializing));
            Assert.That(notifications, Is.Empty);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Missing bot reference")]
        public void StartMatch_FailedReadiness_RejectsWithoutNotification(string diagnostic)
        {
            var machine = new MatchStateMachine();
            var notifications = new List<MatchState>();
            machine.OnStateChanged += notifications.Add;

            Assert.That(machine.StartMatch(new MatchReadinessResult(false, diagnostic)), Is.False);
            Assert.That(machine.CurrentState, Is.EqualTo(MatchState.Initializing));
            Assert.That(machine.IsPlaying, Is.False);
            Assert.That(notifications, Is.Empty);
        }

        [Test]
        public void StartMatch_Ready_CommitsPlayingBeforeExactlyOneNotification()
        {
            var machine = new MatchStateMachine();
            var notifications = new List<MatchState>();
            machine.OnStateChanged += state =>
            {
                Assert.That(state, Is.EqualTo(MatchState.Playing));
                Assert.That(machine.CurrentState, Is.EqualTo(state));
                Assert.That(machine.IsPlaying, Is.True);
                notifications.Add(state);
            };

            Assert.That(machine.StartMatch(Ready), Is.True);
            Assert.That(machine.CurrentState, Is.EqualTo(MatchState.Playing));
            Assert.That(notifications, Is.EqualTo(new[] { MatchState.Playing }));
        }

        [Test]
        public void StartMatch_AfterFailedReadiness_CanRetrySuccessfully()
        {
            var machine = new MatchStateMachine();
            var notifications = new List<MatchState>();
            machine.OnStateChanged += notifications.Add;

            Assert.That(machine.StartMatch(new MatchReadinessResult(false, "Not initialized")), Is.False);
            Assert.That(machine.StartMatch(Ready), Is.True);
            Assert.That(machine.IsPlaying, Is.True);
            Assert.That(notifications, Is.EqualTo(new[] { MatchState.Playing }));
        }

        [TestCase(MatchState.Playing, false)]
        [TestCase(MatchState.Playing, true)]
        [TestCase(MatchState.Victory, false)]
        [TestCase(MatchState.Victory, true)]
        public void StartMatch_OutsideInitializing_RejectsWithoutNotification(MatchState state, bool isReady)
        {
            var machine = CreateAt(state);
            var notifications = new List<MatchState>();
            machine.OnStateChanged += notifications.Add;

            Assert.That(machine.StartMatch(new MatchReadinessResult(isReady)), Is.False);
            Assert.That(machine.CurrentState, Is.EqualTo(state));
            Assert.That(machine.IsPlaying, Is.EqualTo(state == MatchState.Playing));
            Assert.That(notifications, Is.Empty);
        }

        [Test]
        public void TriggerVictory_WhileInitializing_RejectsWithoutNotification()
        {
            var machine = new MatchStateMachine();
            var notifications = new List<MatchState>();
            machine.OnStateChanged += notifications.Add;

            Assert.That(machine.TriggerVictory(), Is.False);
            Assert.That(machine.CurrentState, Is.EqualTo(MatchState.Initializing));
            Assert.That(machine.IsPlaying, Is.False);
            Assert.That(notifications, Is.Empty);
        }

        [Test]
        public void TriggerVictory_WhilePlaying_CommitsVictoryBeforeExactlyOneNotification()
        {
            var machine = CreateAt(MatchState.Playing);
            var notifications = new List<MatchState>();
            machine.OnStateChanged += state =>
            {
                Assert.That(state, Is.EqualTo(MatchState.Victory));
                Assert.That(machine.CurrentState, Is.EqualTo(state));
                Assert.That(machine.IsPlaying, Is.False);
                notifications.Add(state);
            };

            Assert.That(machine.TriggerVictory(), Is.True);
            Assert.That(machine.CurrentState, Is.EqualTo(MatchState.Victory));
            Assert.That(notifications, Is.EqualTo(new[] { MatchState.Victory }));
        }

        [TestCase(1)]
        [TestCase(10)]
        public void TriggerVictory_RepeatedAfterWinning_RejectsWithoutDuplicateNotifications(int repeats)
        {
            var machine = CreateAt(MatchState.Playing);
            var notifications = new List<MatchState>();
            machine.OnStateChanged += notifications.Add;
            Assert.That(machine.TriggerVictory(), Is.True);

            for (var attempt = 0; attempt < repeats; attempt++)
            {
                Assert.That(machine.TriggerVictory(), Is.False);
                Assert.That(machine.StartMatch(Ready), Is.False);
            }

            Assert.That(machine.CurrentState, Is.EqualTo(MatchState.Victory));
            Assert.That(machine.IsPlaying, Is.False);
            Assert.That(notifications, Is.EqualTo(new[] { MatchState.Victory }));
        }

        [Test]
        public void FullLifecycle_EmitsOnlyPlayingThenVictory()
        {
            var machine = new MatchStateMachine();
            var notifications = new List<MatchState>();
            machine.OnStateChanged += notifications.Add;

            Assert.That(machine.StartMatch(Ready), Is.True);
            Assert.That(machine.TriggerVictory(), Is.True);
            Assert.That(notifications, Is.EqualTo(new[] { MatchState.Playing, MatchState.Victory }));
        }

        [Test]
        public void FullLifecycle_WithoutListeners_CompletesNormally()
        {
            var machine = new MatchStateMachine();

            Assert.That(machine.StartMatch(Ready), Is.True);
            Assert.That(machine.TriggerVictory(), Is.True);
            Assert.That(machine.CurrentState, Is.EqualTo(MatchState.Victory));
        }

        [Test]
        public void UnsubscribedListener_ReceivesNoFurtherNotifications()
        {
            var machine = new MatchStateMachine();
            var notifications = new List<MatchState>();
            Action<MatchState> listener = notifications.Add;
            machine.OnStateChanged += listener;
            Assert.That(machine.StartMatch(Ready), Is.True);
            machine.OnStateChanged -= listener;

            Assert.That(machine.TriggerVictory(), Is.True);
            Assert.That(notifications, Is.EqualTo(new[] { MatchState.Playing }));
        }

        [Test]
        public void SeparateMachines_DoNotShareStateOrEvents()
        {
            var first = new MatchStateMachine();
            var second = new MatchStateMachine();
            var secondNotifications = new List<MatchState>();
            second.OnStateChanged += secondNotifications.Add;

            Assert.That(first.StartMatch(Ready), Is.True);
            Assert.That(first.TriggerVictory(), Is.True);
            Assert.That(second.CurrentState, Is.EqualTo(MatchState.Initializing));
            Assert.That(secondNotifications, Is.Empty);
        }

        [Test]
        public void PlayingNotification_RejectsReentrantTransitions_AndKeepsListenersConsistent()
        {
            var machine = new MatchStateMachine();
            var notifications = new List<MatchState>();
            machine.OnStateChanged += state =>
            {
                if (state == MatchState.Playing)
                {
                    Assert.That(machine.StartMatch(Ready), Is.False);
                    Assert.That(machine.TriggerVictory(), Is.False);
                }
            };
            machine.OnStateChanged += state =>
            {
                Assert.That(machine.CurrentState, Is.EqualTo(state));
                notifications.Add(state);
            };

            Assert.That(machine.StartMatch(Ready), Is.True);
            Assert.That(machine.CurrentState, Is.EqualTo(MatchState.Playing));
            Assert.That(notifications, Is.EqualTo(new[] { MatchState.Playing }));
            Assert.That(machine.TriggerVictory(), Is.True);
            Assert.That(notifications, Is.EqualTo(new[] { MatchState.Playing, MatchState.Victory }));
        }

        [Test]
        public void VictoryNotification_RejectsReentrantVictoryAndRestart()
        {
            var machine = CreateAt(MatchState.Playing);
            var notifications = new List<MatchState>();
            machine.OnStateChanged += state =>
            {
                Assert.That(machine.TriggerVictory(), Is.False);
                Assert.That(machine.StartMatch(Ready), Is.False);
                Assert.That(machine.CurrentState, Is.EqualTo(MatchState.Victory));
            };
            machine.OnStateChanged += notifications.Add;

            Assert.That(machine.TriggerVictory(), Is.True);
            Assert.That(notifications, Is.EqualTo(new[] { MatchState.Victory }));
        }

        [Test]
        public void ThrowingPlayingListener_PropagatesWithoutRollback_AndReleasesNotificationGuard()
        {
            var machine = new MatchStateMachine();
            var expected = new InvalidOperationException("Listener failure");
            Action<MatchState> listener = state => throw expected;
            machine.OnStateChanged += listener;

            Assert.That(Assert.Throws<InvalidOperationException>(() => machine.StartMatch(Ready)), Is.SameAs(expected));
            Assert.That(machine.CurrentState, Is.EqualTo(MatchState.Playing));
            Assert.That(machine.IsPlaying, Is.True);
            machine.OnStateChanged -= listener;
            var notifications = new List<MatchState>();
            machine.OnStateChanged += notifications.Add;

            Assert.That(machine.StartMatch(Ready), Is.False);
            Assert.That(machine.TriggerVictory(), Is.True);
            Assert.That(notifications, Is.EqualTo(new[] { MatchState.Victory }));
        }

        [Test]
        public void ThrowingVictoryListener_PropagatesWithoutRollback_AndVictoryRemainsTerminal()
        {
            var machine = CreateAt(MatchState.Playing);
            var notificationCount = 0;
            machine.OnStateChanged += state =>
            {
                notificationCount++;
                throw new InvalidOperationException("Listener failure");
            };

            Assert.Throws<InvalidOperationException>(() => machine.TriggerVictory());
            Assert.That(machine.CurrentState, Is.EqualTo(MatchState.Victory));
            Assert.That(machine.TriggerVictory(), Is.False);
            Assert.That(machine.StartMatch(Ready), Is.False);
            Assert.That(notificationCount, Is.EqualTo(1));
        }

        private static MatchStateMachine CreateAt(MatchState state)
        {
            var machine = new MatchStateMachine();
            if (state != MatchState.Initializing)
            {
                Assert.That(machine.StartMatch(Ready), Is.True);
            }

            if (state == MatchState.Victory)
            {
                Assert.That(machine.TriggerVictory(), Is.True);
            }

            return machine;
        }
    }
}
