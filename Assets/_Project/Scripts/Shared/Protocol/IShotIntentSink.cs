using System;
namespace SmallArena.Protocol
{
    /// <summary>NEW local adapter boundary, not a wire message. Never route this to legacy melee.</summary>
    public interface IShotIntentSink
    {
        bool CanSubmit { get; }
        double RequestIntervalSeconds { get; }
        // Synchronous, including revoke/restore between frames; reset on life/binding/baseline changes.
        event Action Invalidated;
        void SubmitAim(float worldX, float worldZ);
    }
}
