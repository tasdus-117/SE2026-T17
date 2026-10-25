namespace ArenaServer.Models;

// Client → Server messages

public class PlayerInput
{
    public float MoveX { get; set; }
    public float MoveZ { get; set; }
    public float Yaw { get; set; }
    public float Pitch { get; set; }
    public bool IsJumping { get; set; }
    public bool IsSprinting { get; set; }
    public long Timestamp { get; set; }
}

public class CombatInput
{
    public int TargetPlayerId { get; set; }
    public string ActionType { get; set; } = "attack"; // attack, ability, dodge
    public long Timestamp { get; set; }
}

// Server → Client messages

public class PlayerState
{
    public int PlayerId { get; set; }
    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public float PositionZ { get; set; }
    public float Yaw { get; set; }
    public float Pitch { get; set; }
    public float Health { get; set; }
    public float MaxHealth { get; set; }
    public bool IsAlive { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public long ServerTimestamp { get; set; }
}

public class ArenaState
{
    public string ArenaId { get; set; } = string.Empty;
    public List<PlayerState> Players { get; set; } = new();
    public MatchState State { get; set; }
    public float TimeRemaining { get; set; }
    public long ServerTimestamp { get; set; }
}

public class HitValidation
{
    public int AttackerId { get; set; }
    public int TargetId { get; set; }
    public float Damage { get; set; }
    public string HitType { get; set; } = "normal"; // normal, critical, miss
    public bool IsValid { get; set; }
    public string? InvalidReason { get; set; }
    public long ServerTimestamp { get; set; }
}

public class ReconnectResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ArenaId { get; set; }
    public PlayerState? CurrentState { get; set; }
    public List<PlayerState>? ArenaPlayers { get; set; }
}

public class MatchStarted
{
    public int MatchId { get; set; }
    public string ArenaId { get; set; } = string.Empty;
    public List<int> PlayerIds { get; set; } = new();
    public int DurationSeconds { get; set; }
    public long ServerTimestamp { get; set; }
}

public class MatchEnded
{
    public int MatchId { get; set; }
    public Dictionary<int, MatchResultData> Results { get; set; } = new();
    public long ServerTimestamp { get; set; }
}

public class MatchResultData
{
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Damage { get; set; }
    public int Score { get; set; }
    public PlayerRank Rank { get; set; }
}
