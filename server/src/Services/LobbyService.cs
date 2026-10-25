using ArenaServer.Database;
using ArenaServer.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace ArenaServer.Services;

public class LobbyService
{
    private readonly ILogger<LobbyService> _logger;
    private readonly ConcurrentDictionary<int, LobbyPlayer> _waitingPlayers;

    public LobbyService(ILogger<LobbyService> logger)
    {
        _logger = logger;
        _waitingPlayers = new ConcurrentDictionary<int, LobbyPlayer>();
    }

    public void AddPlayerToLobby(int playerId, string playerName)
    {
        var lobbyPlayer = new LobbyPlayer
        {
            PlayerId = playerId,
            PlayerName = playerName,
            JoinedAt = DateTime.UtcNow,
            IsReady = false
        };

        _waitingPlayers.TryAdd(playerId, lobbyPlayer);
        _logger.LogInformation("Player {PlayerId} ({PlayerName}) joined lobby. Total waiting: {Count}",
            playerId, playerName, _waitingPlayers.Count);
    }

    public bool RemovePlayerFromLobby(int playerId)
    {
        var removed = _waitingPlayers.TryRemove(playerId, out _);
        if (removed)
        {
            _logger.LogInformation("Player {PlayerId} removed from lobby. Total waiting: {Count}",
                playerId, _waitingPlayers.Count);
        }
        return removed;
    }

    public void MarkPlayerReady(int playerId)
    {
        if (_waitingPlayers.TryGetValue(playerId, out var player))
        {
            player.IsReady = true;
            _logger.LogInformation("Player {PlayerId} marked ready", playerId);
        }
    }

    public void MarkPlayerNotReady(int playerId)
    {
        if (_waitingPlayers.TryGetValue(playerId, out var player))
        {
            player.IsReady = false;
            _logger.LogInformation("Player {PlayerId} marked not ready", playerId);
        }
    }

    public List<LobbyPlayer> GetWaitingPlayers() => _waitingPlayers.Values.ToList();

    public int GetWaitingPlayerCount() => _waitingPlayers.Count;

    public List<int> GetReadyPlayerIds() => _waitingPlayers
        .Where(kvp => kvp.Value.IsReady)
        .Select(kvp => kvp.Key)
        .ToList();

    public bool CanStartMatch()
    {
        var readyCount = _waitingPlayers.Count(kvp => kvp.Value.IsReady);
        return readyCount >= 4; // Minimum 4 players to start
    }

    public List<int> GetMatchPlayers()
    {
        if (!CanStartMatch())
            return new List<int>();

        // Take 4 ready players
        return _waitingPlayers
            .Where(kvp => kvp.Value.IsReady)
            .OrderBy(kvp => kvp.Value.JoinedAt)
            .Take(4)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    public void ClearWaitingPlayers()
    {
        _waitingPlayers.Clear();
        _logger.LogInformation("Lobby cleared");
    }
}

public class LobbyPlayer
{
    public int PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public bool IsReady { get; set; }
    public int Ping { get; set; } = 0;
}
