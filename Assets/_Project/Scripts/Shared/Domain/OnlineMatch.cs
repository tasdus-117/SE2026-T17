using System;

namespace SmallArena.Domain
{
    public enum OnlineMatchState { Waiting, Playing, Finished, Aborted }

    /// <summary>
    /// Single-threaded server lifecycle, distinct from the offline Victory flow.
    /// The caller owns roster/ready/load checks, automatic start, result selection and scheduling.
    /// Supply monotonic server seconds from one clock domain, never client timestamps.
    /// Gameplay is allowed in [StartedAtSeconds, DeadlineSeconds); reaching the deadline
    /// disables gameplay even before the caller commits Finished. No automatic orchestration.
    /// </summary>
    public sealed class OnlineMatch
    {
        public OnlineMatch(GameplayConfiguration configuration)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public GameplayConfiguration Configuration { get; }
        public OnlineMatchState State { get; private set; } = OnlineMatchState.Waiting;
        public double? StartedAtSeconds { get; private set; }
        public double? DeadlineSeconds { get; private set; }
        public bool IsTerminal => State == OnlineMatchState.Finished || State == OnlineMatchState.Aborted;

        public bool TryStart(double serverTimeSeconds, bool startConditionsSatisfied)
        {
            RequireTime(serverTimeSeconds);
            if (State != OnlineMatchState.Waiting || !startConditionsSatisfied) return false;
            var deadline = AddDuration(serverTimeSeconds, Configuration.MatchDurationSeconds);
            StartedAtSeconds = serverTimeSeconds;
            DeadlineSeconds = deadline;
            State = OnlineMatchState.Playing;
            return true;
        }

        public bool IsGameplayActive(double serverTimeSeconds)
        {
            RequireTime(serverTimeSeconds);
            return State == OnlineMatchState.Playing && serverTimeSeconds >= StartedAtSeconds.Value
                && serverTimeSeconds < DeadlineSeconds.Value;
        }

        public bool TryFinish(double serverTimeSeconds)
        {
            RequireTime(serverTimeSeconds);
            if (State != OnlineMatchState.Playing || serverTimeSeconds < DeadlineSeconds.Value) return false;
            State = OnlineMatchState.Finished;
            return true;
        }

        public bool TryAbort()
        {
            if (IsTerminal) return false;
            State = OnlineMatchState.Aborted;
            return true;
        }

        internal static void RequireTime(double time)
        {
            if (double.IsNaN(time) || double.IsInfinity(time) || time < 0)
                throw new ArgumentOutOfRangeException(nameof(time), "Server seconds must be finite and nonnegative.");
        }

        internal static double AddDuration(double time, double duration)
        {
            var deadline = time + duration;
            if (double.IsInfinity(deadline) || deadline <= time)
                throw new ArgumentOutOfRangeException(nameof(time), "Deadline must be finite and later than its start.");
            return deadline;
        }
    }
}
