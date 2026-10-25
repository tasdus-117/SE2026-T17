using ArenaServer.Models;
using Microsoft.Extensions.Logging;

namespace ArenaServer.Services;

public class MatchmakingService
{
    private readonly LobbyService _lobbyService;
    private readonly ArenaService _arenaService;
    private readonly ILogger<MatchmakingService> _logger;
    private readonly Timer _matchmakingTimer;
    private const int MIN_PLAYERS_TO_START = 4;

    public MatchmakingService(
        LobbyService lobbyService,
        ArenaService arenaService,
        ILogger<MatchmakingService> logger)
    {
        _lobbyService = lobbyService;
        _arenaService = arenaService;
        _logger = logger;

        // Run matchmaking every 2 seconds
        _matchmakingTimer = new Timer(TickMatchmaking, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
    }

    private void TickMatchmaking(object? state)
    {
        try
        {
            if (_lobbyService.CanStartMatch())
            {
                var matchPlayers = _lobbyService.GetMatchPlayers();
                if (matchPlayers.Count >= MIN_PLAYERS_TO_START)
                {
                    StartMatch(matchPlayers);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during matchmaking tick");
        }
    }

    private void StartMatch(List<int> playerIds)
    {
        var arenaId = _arenaService.CreateNewArena();

        foreach (var playerId in playerIds)
        {
            _arenaService.AddPlayerToArena(arenaId, playerId);
            _lobbyService.RemovePlayerFromLobby(playerId);
        }

        _logger.LogInformation("Match started: {ArenaId} with players {Players}",
            arenaId, string.Join(", ", playerIds));
    }

    public void Dispose()
    {
        _matchmakingTimer?.Dispose();
    }
}
