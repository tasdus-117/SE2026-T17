using ArenaServer.Models;
using Microsoft.Extensions.Logging;

namespace ArenaServer.Services;

public class CombatService
{
    private readonly ILogger<CombatService> _logger;

    public CombatService(ILogger<CombatService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validate and process an attack on the server.
    /// Server is authoritative - client attacks are only suggestions.
    /// </summary>
    public CombatResult ValidateAndProcessAttack(
        PlayerController attacker,
        PlayerController target,
        CombatInput input,
        float clientDistance)
    {
        var result = new CombatResult();

        // Check if attacker is alive
        if (!attacker.IsAlive)
        {
            result.IsValid = false;
            result.Reason = "Attacker is dead";
            return result;
        }

        // Check if target is alive
        if (!target.IsAlive)
        {
            result.IsValid = false;
            result.Reason = "Target is dead";
            return result;
        }

        // Server-side distance validation
        var serverDistance = Vector3.Distance(attacker.Position, target.Position);
        const float MAX_ATTACK_RANGE = 5f;

        if (serverDistance > MAX_ATTACK_RANGE)
        {
            result.IsValid = false;
            result.Reason = $"Out of range (server distance: {serverDistance:F2}m, client reported: {clientDistance:F2}m)";
            result.WasCheatAttempt = true;
            return result;
        }

        // Check for distance desync (anti-cheat)
        const float MAX_DISTANCE_DESYNC = 2f;
        var distanceDesync = MathF.Abs(serverDistance - clientDistance);
        if (distanceDesync > MAX_DISTANCE_DESYNC)
        {
            result.IsValid = false;
            result.Reason = $"Distance desynchronization detected (desync: {distanceDesync:F2}m)";
            result.WasCheatAttempt = true;

            _logger.LogWarning(
                "Potential cheat attempt: Player {AttackerId} reported distance {ClientDistance:F2}m but server measured {ServerDistance:F2}m",
                attacker.PlayerId, clientDistance, serverDistance);

            return result;
        }

        // Calculate damage
        var baseDamage = 20f;
        var damageVariation = Random.Shared.Next(-5, 6);
        var finalDamage = baseDamage + damageVariation;

        // Critical hit if very close
        var isCritical = serverDistance < 2f;
        if (isCritical)
        {
            finalDamage *= 1.5f;
        }

        // Apply damage
        target.TakeDamage(finalDamage);
        attacker.Kills++;

        result.IsValid = true;
        result.Damage = finalDamage;
        result.IsCritical = isCritical;
        result.TargetHealth = target.Health;
        result.TargetDied = !target.IsAlive;

        _logger.LogInformation(
            "Valid attack: {AttackerId} hit {TargetId} for {Damage:F2} damage (critical: {IsCritical}, distance: {Distance:F2}m)",
            attacker.PlayerId, target.PlayerId, finalDamage, isCritical, serverDistance);

        return result;
    }

    /// <summary>
    /// Detect common cheating patterns
    /// </summary>
    public CheatDetectionResult DetectCheat(PlayerController player, PlayerInput input)
    {
        var result = new CheatDetectionResult();

        // Check for impossible speeds
        if (input.IsSprinting)
        {
            const float MAX_SPRINT_SPEED = 20f;
            var speed = new Vector3(input.MoveX, 0, input.MoveZ).Length();
            if (speed > MAX_SPRINT_SPEED)
            {
                result.IsCheatDetected = true;
                result.CheatType = "Speed hack";
                _logger.LogWarning("Speed hack detected: Player {PlayerId} speed {Speed:F2}m/s", player.PlayerId, speed);
                return result;
            }
        }

        // Check for angle anomalies
        if (MathF.Abs(input.Pitch) > 120) // Beyond normal view angles
        {
            result.IsCheatDetected = true;
            result.CheatType = "Invalid pitch angle";
            _logger.LogWarning("Pitch anomaly detected: Player {PlayerId} pitch {Pitch:F2}°", player.PlayerId, input.Pitch);
            return result;
        }

        return result;
    }
}

public class CombatResult
{
    public bool IsValid { get; set; }
    public string Reason { get; set; } = string.Empty;
    public float Damage { get; set; }
    public bool IsCritical { get; set; }
    public float TargetHealth { get; set; }
    public bool TargetDied { get; set; }
    public bool WasCheatAttempt { get; set; }
}

public class CheatDetectionResult
{
    public bool IsCheatDetected { get; set; }
    public string CheatType { get; set; } = string.Empty;
}
