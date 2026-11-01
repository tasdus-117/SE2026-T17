using System;

namespace SmallArena.Domain
{
    /// <summary>Single-threaded match rules; scene validation remains the caller's responsibility.</summary>
    public sealed class MatchStateMachine
    {
        private bool isNotifying;

        public MatchState CurrentState { get; private set; } = MatchState.Initializing;

        public bool IsPlaying => CurrentState == MatchState.Playing;

        public event Action<MatchState> OnStateChanged;

        public bool StartMatch(MatchReadinessResult readiness)
        {
            if (isNotifying || CurrentState != MatchState.Initializing || !readiness.IsReady)
            {
                return false;
            }

            TransitionTo(MatchState.Playing);
            return true;
        }

        public bool TriggerVictory()
        {
            if (isNotifying || CurrentState != MatchState.Playing)
            {
                return false;
            }

            TransitionTo(MatchState.Victory);
            return true;
        }

        private void TransitionTo(MatchState nextState)
        {
            CurrentState = nextState;
            isNotifying = true;
            try
            {
                OnStateChanged?.Invoke(nextState);
            }
            finally
            {
                // Subscriber exceptions propagate; the committed state is retained.
                isNotifying = false;
            }
        }
    }
}
