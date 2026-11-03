namespace SmallArena.Protocol
{
    /// <summary>Local input adapter contract; transport adapters will translate network requests separately.</summary>
    public interface ICombatInputSource
    {
        bool IsInputReady { get; }
        bool ConsumePunchPressed();
        bool ConsumeInteractPressed();
    }

    /// <summary>Offline startup readiness, not proof that movement is authoritative over the network.</summary>
    public interface IMovementReadiness
    {
        bool IsMovementReady { get; }
    }
}
