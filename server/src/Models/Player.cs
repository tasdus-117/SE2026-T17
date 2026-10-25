namespace ArenaServer.Models;

public class Player
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public string? Email { get; set; }
    public int Level { get; set; } = 1;
    public int Experience { get; set; } = 0;
    public int Wins { get; set; } = 0;
    public int Losses { get; set; } = 0;
    public double WinRate { get; set; } = 0.0;
    public PlayerStatus Status { get; set; } = PlayerStatus.Offline;
    public DateTime CreatedAt { get; set; }
    public DateTime LastSeenAt { get; set; }

    // Relationships
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
    public ICollection<MatchResult> Matches { get; set; } = new List<MatchResult>();
}

public enum PlayerStatus
{
    Offline,
    Online,
    InLobby,
    InArena,
    InQueue
}
