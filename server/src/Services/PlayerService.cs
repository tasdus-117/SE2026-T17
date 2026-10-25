using ArenaServer.Database;
using ArenaServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ArenaServer.Services;

public class PlayerService
{
    private readonly GameDbContext _context;
    private readonly ILogger<PlayerService> _logger;

    public PlayerService(GameDbContext context, ILogger<PlayerService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Player?> GetPlayerByIdAsync(int playerId)
    {
        return await _context.Players.FirstOrDefaultAsync(p => p.Id == playerId);
    }

    public async Task<Player?> GetPlayerByUsernameAsync(string username)
    {
        return await _context.Players.FirstOrDefaultAsync(p => p.Username == username);
    }

    public async Task<Player> CreatePlayerAsync(string username, string? email = null)
    {
        // Check if player exists
        var existing = await GetPlayerByUsernameAsync(username);
        if (existing != null)
            throw new InvalidOperationException($"Player {username} already exists");

        var player = new Player
        {
            Username = username,
            Email = email,
            CreatedAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow
        };

        _context.Players.Add(player);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Player created: {Username} (ID: {PlayerId})", username, player.Id);
        return player;
    }

    public async Task<Session> CreateSessionAsync(int playerId, int expirationMinutes = 60)
    {
        var player = await GetPlayerByIdAsync(playerId);
        if (player == null)
            throw new InvalidOperationException($"Player {playerId} not found");

        var token = GenerateSessionToken();
        var session = new Session
        {
            PlayerId = playerId,
            Token = token,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes),
            IsActive = true
        };

        _context.Sessions.Add(session);
        player.Status = PlayerStatus.Online;
        player.LastSeenAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Session created for player {PlayerId}", playerId);
        return session;
    }

    public async Task<Session?> GetSessionByTokenAsync(string token)
    {
        var session = await _context.Sessions
            .Include(s => s.Player)
            .FirstOrDefaultAsync(s => s.Token == token);

        if (session != null && session.ExpiresAt < DateTime.UtcNow)
        {
            session.IsActive = false;
            await _context.SaveChangesAsync();
            return null;
        }

        return session;
    }

    public async Task RevokeSessionAsync(string token)
    {
        var session = await _context.Sessions.FirstOrDefaultAsync(s => s.Token == token);
        if (session != null)
        {
            session.IsActive = false;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Session revoked: {Token}", token);
        }
    }

    public async Task UpdatePlayerStatusAsync(int playerId, PlayerStatus status)
    {
        var player = await GetPlayerByIdAsync(playerId);
        if (player != null)
        {
            player.Status = status;
            player.LastSeenAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdatePlayerStatsAsync(int playerId, int kills, int deaths, int damage, bool isWinner)
    {
        var player = await GetPlayerByIdAsync(playerId);
        if (player == null)
            return;

        var totalMatches = player.Wins + player.Losses + 1;
        if (isWinner)
            player.Wins++;
        else
            player.Losses++;

        player.WinRate = (double)player.Wins / totalMatches;
        player.Experience += kills * 10 + damage;

        // Level up every 1000 exp
        var newLevel = 1 + player.Experience / 1000;
        if (newLevel > player.Level)
        {
            player.Level = newLevel;
            _logger.LogInformation("Player {PlayerId} leveled up to {Level}", playerId, player.Level);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<Player>> GetTopPlayersAsync(int limit = 10)
    {
        return await _context.Players
            .OrderByDescending(p => p.WinRate)
            .ThenByDescending(p => p.Level)
            .Take(limit)
            .ToListAsync();
    }

    private static string GenerateSessionToken()
    {
        var random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Range(0, 32)
            .Select(_ => chars[random.Next(chars.Length)])
            .ToArray());
    }
}
