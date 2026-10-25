using Microsoft.AspNetCore.SignalR;
using ArenaServer.Models;
using ArenaServer.Services;

namespace ArenaServer.Hubs;

public class LobbyHub : Hub
{
    private readonly LobbyService _lobbyService;
    private readonly PlayerService _playerService;
    private readonly ILogger<LobbyHub> _logger;

    public LobbyHub(
        LobbyService lobbyService,
        PlayerService playerService,
        ILogger<LobbyHub> logger)
    {
        _lobbyService = lobbyService;
        _playerService = playerService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected to lobby: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected from lobby: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinLobby(int playerId, string playerName)
    {
        try
        {
            var player = await _playerService.GetPlayerByIdAsync(playerId);
            if (player == null)
            {
                await Clients.Caller.SendAsync("Error", "Player not found");
                return;
            }

            _lobbyService.AddPlayerToLobby(playerId, playerName);
            await _playerService.UpdatePlayerStatusAsync(playerId, PlayerStatus.InLobby);

            // Notify all clients in lobby
            await Clients.All.SendAsync("PlayerJoinedLobby", playerId, playerName, _lobbyService.GetWaitingPlayerCount());

            _logger.LogInformation("Player {PlayerId} joined lobby", playerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining lobby");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task LeaveLobby(int playerId)
    {
        try
        {
            _lobbyService.RemovePlayerFromLobby(playerId);
            await _playerService.UpdatePlayerStatusAsync(playerId, PlayerStatus.Online);

            // Notify all clients
            await Clients.All.SendAsync("PlayerLeftLobby", playerId, _lobbyService.GetWaitingPlayerCount());

            _logger.LogInformation("Player {PlayerId} left lobby", playerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving lobby");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task ReadyUp(int playerId)
    {
        try
        {
            _lobbyService.MarkPlayerReady(playerId);
            var readyCount = _lobbyService.GetReadyPlayerIds().Count;

            await Clients.All.SendAsync("PlayerReadyStatusChanged", playerId, true, readyCount);

            if (_lobbyService.CanStartMatch())
            {
                await Clients.All.SendAsync("MatchCanStart");
            }

            _logger.LogInformation("Player {PlayerId} ready up", playerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking player ready");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task NotReady(int playerId)
    {
        try
        {
            _lobbyService.MarkPlayerNotReady(playerId);
            var readyCount = _lobbyService.GetReadyPlayerIds().Count;

            await Clients.All.SendAsync("PlayerReadyStatusChanged", playerId, false, readyCount);

            _logger.LogInformation("Player {PlayerId} marked not ready", playerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking player not ready");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task GetLobbyStatus()
    {
        try
        {
            var players = _lobbyService.GetWaitingPlayers();
            var readyCount = _lobbyService.GetReadyPlayerIds().Count;

            var status = new
            {
                TotalPlayers = players.Count,
                ReadyPlayers = readyCount,
                CanStart = _lobbyService.CanStartMatch(),
                Players = players.Select(p => new { p.PlayerId, p.PlayerName, p.IsReady })
            };

            await Clients.Caller.SendAsync("LobbyStatus", status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lobby status");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }
}
