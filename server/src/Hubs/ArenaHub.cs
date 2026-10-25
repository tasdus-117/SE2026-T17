using Microsoft.AspNetCore.SignalR;
using ArenaServer.Models;
using ArenaServer.Services;

namespace ArenaServer.Hubs;

public class ArenaHub : Hub
{
    private readonly ArenaService _arenaService;
    private readonly PlayerService _playerService;
    private readonly CombatService _combatService;
    private readonly GameStateService _gameStateService;
    private readonly ILogger<ArenaHub> _logger;

    public ArenaHub(
        ArenaService arenaService,
        PlayerService playerService,
        CombatService combatService,
        GameStateService gameStateService,
        ILogger<ArenaHub> logger)
    {
        _arenaService = arenaService;
        _playerService = playerService;
        _combatService = combatService;
        _gameStateService = gameStateService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected to arena: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected from arena: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join a specific arena
    /// </summary>
    public async Task JoinArena(string arenaId, int playerId)
    {
        try
        {
            var player = await _playerService.GetPlayerByIdAsync(playerId);
            if (player == null)
            {
                await Clients.Caller.SendAsync("Error", "Player not found");
                return;
            }

            // Add to SignalR group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"arena_{arenaId}");

            // Join game
            var success = _arenaService.AddPlayerToArena(arenaId, playerId);
            if (!success)
            {
                await Clients.Caller.SendAsync("Error", "Failed to join arena");
                return;
            }

            await _playerService.UpdatePlayerStatusAsync(playerId, PlayerStatus.InArena);

            // Get current arena state
            var arenaState = _arenaService.GetArenaState(arenaId);
            if (arenaState != null)
            {
                await Clients.Caller.SendAsync("ArenaState", arenaState);
                await Clients.GroupExcept($"arena_{arenaId}", Context.ConnectionId)
                    .SendAsync("PlayerJoinedArena", playerId, arenaState);
            }

            _logger.LogInformation("Player {PlayerId} joined arena {ArenaId}", playerId, arenaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining arena");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// Handle player movement input
    /// </summary>
    public async Task SendPlayerInput(string arenaId, int playerId, PlayerInput input)
    {
        try
        {
            // Validate session
            var player = await _playerService.GetPlayerByIdAsync(playerId);
            if (player == null)
            {
                await Clients.Caller.SendAsync("Error", "Player not found");
                return;
            }

            // Cheat detection
            var arena = _gameStateService.GetArena(arenaId);
            if (arena != null && arena.Players.TryGetValue(playerId, out var controller))
            {
                var cheatResult = _combatService.DetectCheat(controller, input);
                if (cheatResult.IsCheatDetected)
                {
                    _logger.LogWarning(
                        "Cheat detected: Player {PlayerId} - {CheatType}",
                        playerId, cheatResult.CheatType);

                    // TODO: Apply penalty or kick
                    return;
                }
            }

            // Process movement
            _arenaService.ProcessPlayerMovement(arenaId, playerId, input);

            // Broadcast to all clients in arena
            var arenaState = _arenaService.GetArenaState(arenaId);
            if (arenaState != null)
            {
                await Clients.Group($"arena_{arenaId}").SendAsync("ArenaStateUpdate", arenaState);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing player input");
        }
    }

    /// <summary>
    /// Handle combat action (attack)
    /// </summary>
    public async Task SendCombatInput(string arenaId, int playerId, CombatInput combatInput, float clientDistance)
    {
        try
        {
            var player = await _playerService.GetPlayerByIdAsync(playerId);
            if (player == null)
            {
                await Clients.Caller.SendAsync("Error", "Player not found");
                return;
            }

            // Authoritative hit validation on server
            var arena = _gameStateService.GetArena(arenaId);
            if (arena == null)
            {
                await Clients.Caller.SendAsync("Error", "Arena not found");
                return;
            }

            if (!arena.Players.TryGetValue(playerId, out var attacker))
            {
                await Clients.Caller.SendAsync("Error", "Attacker not found");
                return;
            }

            if (!arena.Players.TryGetValue(combatInput.TargetPlayerId, out var target))
            {
                await Clients.Caller.SendAsync("Error", "Target not found");
                return;
            }

            // Validate and process attack
            var combatResult = _combatService.ValidateAndProcessAttack(
                attacker, target, combatInput, clientDistance);

            var validation = new HitValidation
            {
                AttackerId = playerId,
                TargetId = combatInput.TargetPlayerId,
                Damage = combatResult.Damage,
                IsValid = combatResult.IsValid,
                HitType = combatResult.IsCritical ? "critical" : "normal",
                InvalidReason = combatResult.Reason,
                ServerTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            // Send validation result to attacker
            await Clients.Caller.SendAsync("HitValidation", validation);

            // If valid hit, broadcast state update
            if (combatResult.IsValid)
            {
                var arenaState = _arenaService.GetArenaState(arenaId);
                if (arenaState != null)
                {
                    await Clients.Group($"arena_{arenaId}").SendAsync("ArenaStateUpdate", arenaState);
                }

                // Check if target died
                if (combatResult.TargetDied)
                {
                    await Clients.Group($"arena_{arenaId}")
                        .SendAsync("PlayerDied", combatInput.TargetPlayerId, playerId);
                }
            }

            _logger.LogInformation(
                "Combat processed: {AttackerId} → {TargetId}, Valid: {IsValid}",
                playerId, combatInput.TargetPlayerId, combatResult.IsValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing combat input");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// Handle player disconnect (may rejoin)
    /// </summary>
    public async Task PlayerDisconnected(string arenaId, int playerId)
    {
        try
        {
            _arenaService.RemovePlayerFromArena(arenaId, playerId);
            await _playerService.UpdatePlayerStatusAsync(playerId, PlayerStatus.Online);

            await Clients.Group($"arena_{arenaId}")
                .SendAsync("PlayerLeftArena", playerId);

            _logger.LogInformation("Player {PlayerId} disconnected from arena {ArenaId}", playerId, arenaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling player disconnect");
        }
    }

    /// <summary>
    /// Handle reconnection
    /// </summary>
    public async Task Reconnect(string arenaId, int playerId)
    {
        try
        {
            await JoinArena(arenaId, playerId);
            await Clients.Caller.SendAsync("ReconnectSuccess", true);

            _logger.LogInformation("Player {PlayerId} reconnected to arena {ArenaId}", playerId, arenaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reconnecting player");
            await Clients.Caller.SendAsync("ReconnectSuccess", false);
        }
    }
}
