namespace SmallArena.Protocol
{
    // Direction-only intent. The server derives origin, range and damage from its own state.
    public struct ShotIntent
    {
        public float AimX, AimZ;
        public double Timestamp;
        public ulong Sequence;
        public long LifeId;
    }
}
