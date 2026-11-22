using System;
using SmallArena.Domain;

namespace SmallArena.Client.UI
{
    /// <summary>Own one per view lifetime. Dispose on disable/despawn; rebind only to a full baseline.</summary>
    public sealed class PlayerPresentationBinding : IDisposable
    {
        private IPlayerPresentationSource source;
        private Action<PlayerPresentationSnapshot> listener;
        private ulong generation;
        public PlayerPresentationSnapshot State { get; private set; }
        public event Action<PlayerPresentationSnapshot> Changed;
        public void Bind(IPlayerPresentationSource next)
        {
            if (ReferenceEquals(source, next)) return;
            var bindingGeneration = ++generation;
            if (source != null) source.Changed -= listener;
            source = next;
            State = null;
            Changed?.Invoke(null); // clear life-bound effects before the new baseline
            if (bindingGeneration != generation) return;
            if (source == null) { listener = null; return; }
            var current = generation;
            listener = state =>
            {
                if (current != generation) return;
                if (state == null)
                {
                    if (source.Current == null) { State = null; Changed?.Invoke(null); }
                    return;
                }
                if (!ReferenceEquals(state.Context, source.Current?.Context)) return;
                if (State != null && ReferenceEquals(State.Context, state.Context) && state.Revision <= State.Revision) return;
                if (State != null && !ReferenceEquals(State.Context, state.Context))
                {
                    State = null;
                    Changed?.Invoke(null);
                    if (current != generation) return;
                }
                State = state;
                Changed?.Invoke(state);
            };
            source.Changed += listener;
            listener(source.Current);
        }
        public void Dispose() => Bind(null);
    }
}
