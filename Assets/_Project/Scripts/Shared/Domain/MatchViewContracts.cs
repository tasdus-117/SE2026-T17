using System;

namespace SmallArena.Domain
{
    // Read-only contracts keep presentation independent of Runtime and Server implementations.
    public interface IMatchStateSource
    {
        MatchState CurrentState { get; }
        event Action<MatchState> OnMatchStateChanged;
    }

    public interface IHealthSource
    {
        int CurrentHealth { get; }
        int MaxHealth { get; }
        event Action<int, int> OnHealthChanged;
    }

    public interface IDamageSource
    {
        int CurrentDamage { get; }
        event Action<int> OnDamageChanged;
    }

    public interface IMatchEndParticipant
    {
        void EndMatch();
    }

    public interface IMatchHudReadiness
    {
        bool IsReady { get; }
        bool IsBoundTo(IMatchStateSource manager, IHealthSource health, IDamageSource combat);
    }
}
