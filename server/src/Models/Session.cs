namespace ArenaServer.Models;

public class Session
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public required string Token { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ConnectionId { get; set; }
    public string? LastKnownArenaId { get; set; }
    public DateTime? LastHeartbeat { get; set; }

    // Relationships
    public Player? Player { get; set; }
}
