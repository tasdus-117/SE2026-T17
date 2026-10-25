using ArenaServer.Models;
using Microsoft.Extensions.Logging;

namespace ArenaServer.Services;

public class ArenaService
{
    private readonly GameStateService _gameStateService;
    private readonly ILogger<ArenaService> _logger;
    private int _arenaIdCounter = 0;

    public ArenaService(GameStateService gameStateService, ILogger<ArenaService> logger)
    {
        _gameStateService = gameStateService;
        _logger = logger;
    }

    public string CreateNewArena()
    {
        var arenaId = $"arena_{Interlocked.Increment(ref _arenaIdCounter)}";
        _gameStateService.CreateArena(arenaId, maxPlayers: 4);
        _logger.LogInformation("New arena created: {ArenaId}", arenaId);
        return arenaId;
    }

    public bool AddPlayerToArena(string arenaId, int playerId)
    {
        try
        {
            var playerController = new PlayerController
            {
                PlayerId = playerId,
                Position = GetSpawnPosition(playerId),
                Health = 100f,
                MaxHealth = 100f,
                IsAlive = true
            };

            _gameStateService.AddPlayerToArena(arenaId, playerId, playerController);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add player {PlayerId} to arena {ArenaId}", playerId, arenaId);
            return false;
        }
    }

    public void RemovePlayerFromArena(string arenaId, int playerId)
    {
        _gameStateService.RemovePlayerFromArena(arenaId, playerId);
    }

    public ArenaState? GetArenaState(string arenaId)
    {
        try
        {
            return _gameStateService.GetArenaState(arenaId);
        }
        catch
        {
            return null;
        }
    }

    public void ProcessPlayerMovement(string arenaId, int playerId, PlayerInput input)
    {
        _gameStateService.ProcessPlayerInput(arenaId, playerId, input);
    }

    public HitValidation? ValidateHit(string arenaId, int attackerId, CombatInput combatInput)
    {
        var arena = _gameStateService.GetArena(arenaId);
        if (arena == null)
        {
            return new HitValidation
            {
                AttackerId = attackerId,
                TargetId = combatInput.TargetPlayerId,
                IsValid = false,
                InvalidReason = "Arena not found"
            };
        }

        if (!arena.Players.TryGetValue(attackerId, out var attacker) ||
            !arena.Players.TryGetValue(combatInput.TargetPlayerId, out var target))
        {
            return new HitValidation
            {
                AttackerId = attackerId,
                TargetId = combatInput.TargetPlayerId,
                IsValid = false,
                InvalidReason = "Player not found"
            };
        }

        // Validate distance
        var distance = Vector3.Distance(attacker.Position, target.Position);
        if (distance > 5f)
        {
            return new HitValidation
            {
                AttackerId = attackerId,
                TargetId = combatInput.TargetPlayerId,
                IsValid = false,
                InvalidReason = $"Target out of range (distance: {distance:F2}m)"
            };
        }

        // Validate target is alive
        if (!target.IsAlive)
        {
            return new HitValidation
            {
                AttackerId = attackerId,
                TargetId = combatInput.TargetPlayerId,
                IsValid = false,
                InvalidReason = "Target is dead"
            };
        }

        // Validate attacker is alive
        if (!attacker.IsAlive)
        {
            return new HitValidation
            {
                AttackerId = attackerId,
                TargetId = combatInput.TargetPlayerId,
                IsValid = false,
                InvalidReason = "Attacker is dead"
            };
        }

        // Hit is valid
        _gameStateService.ProcessCombatInput(arenaId, attackerId, combatInput);

        var damage = 20f + Random.Shared.Next(-5, 6);
        return new HitValidation
        {
            AttackerId = attackerId,
            TargetId = combatInput.TargetPlayerId,
            Damage = damage,
            IsValid = true,
            HitType = distance < 2f ? "critical" : "normal",
            ServerTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }

    private Vector3 GetSpawnPosition(int playerId)
    {
        // 4 spawn positions in a circle
        var angle = (playerId % 4) * MathF.PI / 2;
        var radius = 10f;
        return new Vector3(
            MathF.Cos(angle) * radius,
            1f,
            MathF.Sin(angle) * radius
        );
    }
}
