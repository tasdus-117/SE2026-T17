namespace ArenaServer.Models;

public class Match
{
    public int Id { get; set; }
    public required string ArenaId { get; set; }
    public MatchState State { get; set; } = MatchState.Waiting;
    public int MaxPlayers { get; set; } = 4;
    public int CurrentPlayerCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int DurationSeconds { get; set; } = 600;

    // Relationships
    public ICollection<Player> Players { get; set; } = new List<Player>();
    public ICollection<MatchResult> Results { get; set; } = new List<MatchResult>();
}

public class MatchResult
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public int PlayerId { get; set; }
    public int Kills { get; set; } = 0;
    public int Deaths { get; set; } = 0;
    public int Damage { get; set; } = 0;
    public int DamageTaken { get; set; } = 0;
    public PlayerRank Rank { get; set; } = PlayerRank.None;
    public bool IsWinner { get; set; } = false;
    public int Score { get; set; } = 0;
    public DateTime CreatedAt { get; set; }

    // Relationships
    public Match? Match { get; set; }
    public Player? Player { get; set; }
}

public enum MatchState
{
    Waiting,
    Starting,
    InProgress,
    Ended,
    Cancelled
}

public enum PlayerRank
{
    None,
    First,
    Second,
    Third,
    Fourth
}
