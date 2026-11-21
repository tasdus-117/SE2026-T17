using System;
using NUnit.Framework;
using SmallArena.Client.UI;
using SmallArena.Domain;
namespace SmallArena.Tests.PlayMode
{
    public sealed class Tv3PresentationTests
    {
        private sealed class Source : IPlayerPresentationSource
        {
            public PlayerPresentationSnapshot Current { get; set; }
            public event Action<PlayerPresentationSnapshot> Changed;
            public Action<PlayerPresentationSnapshot> Capture() => Changed;
            public void Publish(PlayerPresentationSnapshot state) { Current = state; Changed?.Invoke(state); }
        }
        private static PlayerPresentationSnapshot Snapshot(object context, ulong revision, int hp = 75,
            int score = 4, double time = 23) => new PlayerPresentationSnapshot(context, revision, hp, 100, score, time, "Playing", "Cooldown");
        [Test] public void Bind_RefreshesAuthoritativeValuesAndDoesNotDuplicateListeners()
        {
            var source = new Source { Current = Snapshot(new object(), 2) };
            using (var binding = new PlayerPresentationBinding())
            {
                var changes = 0;
                binding.Changed += _ => changes++;
                binding.Bind(source);
                Assert.That(binding.State, Is.SameAs(source.Current));
                binding.Bind(source);
                changes = 0;
                source.Publish(Snapshot(source.Current.Context, 3, 50, 5, 12));
                Assert.That(changes, Is.EqualTo(1));
                Assert.That(binding.State.Health, Is.EqualTo(50));
                Assert.That(binding.State.Score, Is.EqualTo(5));
                Assert.That(binding.State.RemainingSeconds, Is.EqualTo(12));
                Assert.That(binding.State.Rejection, Is.EqualTo("Cooldown"));
            }
        }
        [Test] public void RebindAndDispose_RejectCapturedCallbacksEvenFromSameSource()
        {
            var source = new Source { Current = Snapshot(new object(), 1) };
            var binding = new PlayerPresentationBinding();
            binding.Bind(source);
            var old = source.Capture();
            binding.Bind(null);
            binding.Bind(source);
            old(Snapshot(source.Current.Context, 100, 1));
            Assert.That(binding.State.Health, Is.EqualTo(75));
            var delayed = source.Capture();
            binding.Dispose();
            delayed(Snapshot(source.Current.Context, 101, 1));
            Assert.That(binding.State, Is.Null);
        }
        [Test] public void LifeChange_ClearsReadableStateBeforeNotifyingViews()
        {
            var source = new Source { Current = Snapshot(new object(), 1) };
            using (var binding = new PlayerPresentationBinding())
            {
                binding.Bind(source);
                var cleared = false;
                binding.Changed += state =>
                {
                    if (state != null) return;
                    cleared = true;
                    Assert.That(binding.State, Is.Null);
                };
                source.Publish(Snapshot(new object(), 1, 40));
                Assert.That(cleared, Is.True);
            }
        }
        [Test] public void NewLife_ClearsOldStateAndRejectsOldLifeAndRevision()
        {
            var context = new object();
            var source = new Source { Current = Snapshot(context, 10) };
            using (var binding = new PlayerPresentationBinding())
            {
                binding.Bind(source);
                source.Publish(Snapshot(context, 9, 100));
                Assert.That(binding.State.Health, Is.EqualTo(75));
                source.Publish(null);
                Assert.That(binding.State, Is.Null);
                var next = Snapshot(new object(), 1, 40, 7, 8);
                source.Publish(next);
                source.Capture()(Snapshot(context, 99, 100));
                Assert.That(binding.State, Is.SameAs(next));
                Assert.That(binding.State.RemainingSeconds, Is.EqualTo(8));
            }
        }
    }
}
