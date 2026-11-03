using SmallArena.Domain;

namespace SmallArena.Core
{
    /// <summary>
    /// Reports real initialization of Health, player stats, pickup state and HUD.
    /// Entity presence alone must not satisfy this contract. The offline arena
    /// supplies its production implementation through explicit scene wiring.
    /// </summary>
    public interface IMatchReadinessProbe
    {
        MatchReadinessResult ValidateReadiness();
    }
}
