namespace SmallArena.Protocol
{
    /// <summary>Intent only. The server owns position, velocity and simulation time.</summary>
    public struct MovementInput
    {
        public uint Sequence;
        public float MoveX;
        public float MoveZ;
        // Repeated in heartbeats so a lost unreliable packet does not lose a fresh jump edge.
        public uint JumpSequence;
    }
}
