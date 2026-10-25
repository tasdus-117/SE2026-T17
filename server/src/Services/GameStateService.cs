using ArenaServer.Models;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace ArenaServer.Services;

/// <summary>
/// Manages the authoritative game state.
/// Server is the single source of truth.
/// </summary>
public class GameStateService
{
    private readonly ILogger<GameStateService> _logger;
    private readonly ConcurrentDictionary<string, Arena> _arenas;
    private readonly Timer _gameLoopTimer;
    private const int TICK_RATE = 60; // 60 updates per second

    public GameStateService(ILogger<GameStateService> logger)
    {
        _logger = logger;
        _arenas = new ConcurrentDictionary<string, Arena>();
        _gameLoopTimer = new Timer(GameLoop, null, TimeSpan.FromMilliseconds(1000 / TICK_RATE), TimeSpan.FromMilliseconds(1000 / TICK_RATE));
    }

    public Arena CreateArena(string arenaId, int maxPlayers = 4)
    {
        var arena = new Arena
        {
            Id = arenaId,
            MaxPlayers = maxPlayers,
            CreatedAt = DateTime.UtcNow,
            State = MatchState.Waiting
        };

        if (_arenas.TryAdd(arenaId, arena))
        {
            _logger.LogInformation("Arena created: {ArenaId}", arenaId);
            return arena;
        }

        throw new InvalidOperationException($"Arena {arenaId} already exists");
    }

    public Arena? GetArena(string arenaId)
    {
        _arenas.TryGetValue(arenaId, out var arena);
        return arena;
    }

    public void AddPlayerToArena(string arenaId, int playerId, PlayerController player)
    {
        if (_arenas.TryGetValue(arenaId, out var arena))
        {
            arena.AddPlayer(playerId, player);
            _logger.LogInformation("Player {PlayerId} added to arena {ArenaId}", playerId, arenaId);
        }
    }

    public void RemovePlayerFromArena(string arenaId, int playerId)
    {
        if (_arenas.TryGetValue(arenaId, out var arena))
        {
            arena.RemovePlayer(playerId);
            _logger.LogInformation("Player {PlayerId} removed from arena {ArenaId}", playerId, arenaId);

            if (arena.Players.Count == 0)
            {
                _arenas.TryRemove(arenaId, out _);
                _logger.LogInformation("Arena {ArenaId} destroyed (empty)", arenaId);
            }
        }
    }

    public void ProcessPlayerInput(string arenaId, int playerId, PlayerInput input)
    {
        if (_arenas.TryGetValue(arenaId, out var arena))
        {
            arena.ProcessPlayerInput(playerId, input);
        }
    }

    public void ProcessCombatInput(string arenaId, int attackerId, CombatInput input)
    {
        if (_arenas.TryGetValue(arenaId, out var arena))
        {
            arena.ProcessCombatInput(attackerId, input);
        }
    }

    public ArenaState GetArenaState(string arenaId)
    {
        var arena = GetArena(arenaId);
        if (arena == null)
            throw new InvalidOperationException($"Arena {arenaId} not found");

        return new ArenaState
        {
            ArenaId = arenaId,
            State = arena.State,
            Players = arena.Players.Values
                .Select(p => p.GetState())
                .ToList(),
            TimeRemaining = (float)arena.GetTimeRemaining().TotalSeconds,
            ServerTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }

    public List<PlayerState> GetAllPlayerStates(string arenaId)
    {
        var arena = GetArena(arenaId);
        if (arena == null)
            return new List<PlayerState>();

        return arena.Players.Values
            .Select(p => p.GetState())
            .ToList();
    }

    private void GameLoop(object? state)
    {
        try
        {
            foreach (var arena in _arenas.Values)
            {
                arena.Update(1.0 / TICK_RATE);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in game loop");
        }
    }

    public void Dispose()
    {
        _gameLoopTimer?.Dispose();
    }
}

/// <summary>
/// Represents a single arena instance
/// </summary>
public class Arena
{
    public string Id { get; set; } = string.Empty;
    public int MaxPlayers { get; set; }
    public Dictionary<int, PlayerController> Players { get; set; } = new();
    public MatchState State { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public int DurationSeconds { get; set; } = 600;

    public void AddPlayer(int playerId, PlayerController player)
    {
        if (Players.Count < MaxPlayers)
        {
            Players[playerId] = player;
        }
    }

    public void RemovePlayer(int playerId)
    {
        Players.Remove(playerId);
    }

    public void ProcessPlayerInput(int playerId, PlayerInput input)
    {
        if (Players.TryGetValue(playerId, out var player))
        {
            player.ApplyInput(input);
        }
    }

    public void ProcessCombatInput(int attackerId, CombatInput input)
    {
        if (Players.TryGetValue(attackerId, out var attacker) && 
            Players.TryGetValue(input.TargetPlayerId, out var target))
        {
            attacker.ProcessCombatAction(input, target);
        }
    }

    public void Update(double deltaTime)
    {
        foreach (var player in Players.Values)
        {
            player.Update(deltaTime);
        }

        // Check match end conditions
        if (State == MatchState.InProgress && HasRemainingTime() == false)
        {
            State = MatchState.Ended;
        }
    }

    public TimeSpan GetTimeRemaining()
    {
        if (StartedAt == null)
            return TimeSpan.FromSeconds(DurationSeconds);

        var elapsed = DateTime.UtcNow - StartedAt.Value;
        var remaining = TimeSpan.FromSeconds(DurationSeconds) - elapsed;
        return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
    }

    private bool HasRemainingTime() => GetTimeRemaining().TotalSeconds > 0;
}

/// <summary>
/// Server-side player controller (authoritative)
/// </summary>
public class PlayerController
{
    public int PlayerId { get; set; }
    public Vector3 Position { get; set; }
    public float Yaw { get; set; }
    public float Pitch { get; set; }
    public float Health { get; set; } = 100f;
    public float MaxHealth { get; set; } = 100f;
    public bool IsAlive { get; set; } = true;
    public int Kills { get; set; } = 0;
    public int Deaths { get; set; } = 0;

    private Vector3 _velocity = Vector3.Zero;
    private PlayerInput? _lastInput;

    private const float MOVE_SPEED = 10f;
    private const float SPRINT_SPEED = 15f;
    private const float JUMP_FORCE = 5f;
    private const float GRAVITY = 9.81f;

    public void ApplyInput(PlayerInput input)
    {
        if (!IsAlive)
            return;

        _lastInput = input;
        Yaw = input.Yaw;
        Pitch = input.Pitch;

        // Calculate movement
        var moveDirection = new Vector3(input.MoveX, 0, input.MoveZ).Normalized();
        var speed = input.IsSprinting ? SPRINT_SPEED : MOVE_SPEED;

        // Apply movement
        if (moveDirection.Length() > 0)
        {
            _velocity.X = moveDirection.X * speed;
            _velocity.Z = moveDirection.Z * speed;
        }
        else
        {
            _velocity.X = 0;
            _velocity.Z = 0;
        }

        // Jump
        if (input.IsJumping && IsOnGround())
        {
            _velocity.Y = JUMP_FORCE;
        }
    }

    public void ProcessCombatAction(CombatInput input, PlayerController target)
    {
        if (input.ActionType == "attack")
        {
            PerformAttack(target);
        }
    }

    private void PerformAttack(PlayerController target)
    {
        if (!IsAlive || !target.IsAlive)
            return;

        // Validate distance
        var distance = Vector3.Distance(Position, target.Position);
        if (distance > 5f) // Max attack range
            return;

        // Calculate damage
        var damage = 20f; // Base damage
        var damageVariation = Random.Shared.Next(-5, 6);
        var finalDamage = damage + damageVariation;

        target.TakeDamage(finalDamage);
        Kills++;

        if (!target.IsAlive)
        {
            Kills++;
        }
    }

    public void TakeDamage(float damage)
    {
        Health -= damage;
        if (Health <= 0)
        {
            Health = 0;
            IsAlive = false;
            Deaths++;
        }
    }

    public void Update(double deltaTime)
    {
        if (!IsAlive)
            return;

        // Apply gravity
        _velocity.Y -= (float)(GRAVITY * deltaTime);

        // Update position
        Position += _velocity * (float)deltaTime;

        // Keep in bounds (simple arena bounds)
        var arenaSize = 50f;
        Position.X = Math.Clamp(Position.X, -arenaSize, arenaSize);
        Position.Z = Math.Clamp(Position.Z, -arenaSize, arenaSize);

        // Ground collision
        if (Position.Y < 0)
        {
            Position.Y = 0;
            _velocity.Y = 0;
        }
    }

    private bool IsOnGround() => Position.Y <= 0.1f;

    public PlayerState GetState()
    {
        return new PlayerState
        {
            PlayerId = PlayerId,
            PositionX = Position.X,
            PositionY = Position.Y,
            PositionZ = Position.Z,
            Yaw = Yaw,
            Pitch = Pitch,
            Health = Health,
            MaxHealth = MaxHealth,
            IsAlive = IsAlive,
            Kills = Kills,
            Deaths = Deaths,
            ServerTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }
}

public struct Vector3
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public Vector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static Vector3 Zero => new(0, 0, 0);

    public float Length() => MathF.Sqrt(X * X + Y * Y + Z * Z);

    public Vector3 Normalized()
    {
        var len = Length();
        if (len == 0) return Zero;
        return new Vector3(X / len, Y / len, Z / len);
    }

    public static float Distance(Vector3 a, Vector3 b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        var dz = a.Z - b.Z;
        return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vector3 operator *(Vector3 a, float b) => new(a.X * b, a.Y * b, a.Z * b);
}
