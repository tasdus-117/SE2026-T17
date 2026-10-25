using Microsoft.AspNetCore.Mvc;
using ArenaServer.Services;
using ArenaServer.Models;

namespace ArenaServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlayersController : ControllerBase
{
    private readonly PlayerService _playerService;
    private readonly ILogger<PlayersController> _logger;

    public PlayersController(PlayerService playerService, ILogger<PlayersController> logger)
    {
        _playerService = playerService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var player = await _playerService.CreatePlayerAsync(request.Username, request.Email);
            return Ok(new { playerId = player.Id, username = player.Username });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var player = await _playerService.GetPlayerByUsernameAsync(request.Username);
            if (player == null)
                return Unauthorized(new { error = "Player not found" });

            var session = await _playerService.CreateSessionAsync(player.Id);
            return Ok(new { playerId = player.Id, token = session.Token });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromHeader] string authorization)
    {
        try
        {
            var token = authorization.Replace("Bearer ", "");
            await _playerService.RevokeSessionAsync(token);
            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout failed");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{playerId}")]
    public async Task<IActionResult> GetPlayer(int playerId)
    {
        try
        {
            var player = await _playerService.GetPlayerByIdAsync(playerId);
            if (player == null)
                return NotFound();

            return Ok(new
            {
                player.Id,
                player.Username,
                player.Level,
                player.Experience,
                player.Wins,
                player.Losses,
                player.WinRate,
                player.Status,
                player.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting player");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("leaderboard")]
    public async Task<IActionResult> GetLeaderboard([FromQuery] int limit = 10)
    {
        try
        {
            var topPlayers = await _playerService.GetTopPlayersAsync(limit);
            return Ok(topPlayers.Select((p, i) => new
            {
                rank = i + 1,
                p.Id,
                p.Username,
                p.Level,
                p.Wins,
                p.Losses,
                p.WinRate
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting leaderboard");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("validate-session")]
    public async Task<IActionResult> ValidateSession([FromHeader] string authorization)
    {
        try
        {
            var token = authorization.Replace("Bearer ", "");
            var session = await _playerService.GetSessionByTokenAsync(token);
            if (session == null)
                return Unauthorized(new { valid = false });

            return Ok(new { valid = true, playerId = session.PlayerId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating session");
            return BadRequest(new { error = ex.Message });
        }
    }
}

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
}
