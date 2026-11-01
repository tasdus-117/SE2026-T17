using SmallArena.Domain;

namespace SmallArena.Core
{
    /// <summary>
    /// Reports real initialization of Health, player stats, pickup state and HUD.
    /// Entity presence alone must not satisfy this contract. No M1 production
    /// implementation exists; later components supply it through explicit wiring.
    /// </summary>
    public interface IMatchReadinessProbe
    {
        MatchReadinessResult ValidateReadiness();
    }
}
