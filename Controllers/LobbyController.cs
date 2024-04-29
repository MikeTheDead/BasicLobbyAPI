using LobbyAPI.Interfaces;
using LobbyAPI.Middlewares;
using LobbyAPI.Models;
using LobbyAPI.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace LobbyAPI.Controllers;

[ApiController]
[Route("lobby")]
[ValidateSession]
public class LobbyController : ControllerBase
{
    private readonly ILogger<LobbyController> _logger;
    private readonly ILobbyRepository _lobbyRepo;
    private readonly IPasswordRepository _pwdRepo;
    private readonly IPlayerRepository playerRepo;
    private readonly ISessionRepository sessionRepo;

    public LobbyController(ILogger<LobbyController> logger, ILobbyRepository lobbyRepo, IPasswordRepository pwdRepo, IPlayerRepository _playerRepo,
        ISessionRepository _sessionRepo)
    {
        _logger = logger;
        _lobbyRepo = lobbyRepo;
        _pwdRepo = pwdRepo;
        playerRepo = _playerRepo;
        sessionRepo = _sessionRepo;
    }


    [HttpGet("lobbies")]
    public async Task<ActionResult<List<Lobby>>> GetLobbies()
    {
        try
        {
            return Ok(await _lobbyRepo.GetLobbiesAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving lobbies.");
            return StatusCode(500, "An error occurred while retrieving lobbies.");
        }
    }
    [HttpGet("lobby/{name}")]
    public async Task<ActionResult<Lobby>> GetLobby(string name)
    {
        try
        {
            return Ok(await _lobbyRepo.GetLobbyAsync(name));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving lobby.");
            return StatusCode(500, $"An error occurred while retrieving '{name}' lobby.");
        }
    }
    
    [HttpPost("lobby")]
    public async Task<ActionResult> CreateLobby([FromBody] Lobby NewLobby, [FromQuery] string? password = null)
    {
        try
        {
            HttpContext.Request.Headers.TryGetValue("sessionId", out var sessionId);
            
            if (string.IsNullOrEmpty(sessionId))
            {
                return Unauthorized("Session ID not found.");
            }

            Session session = await sessionRepo.Get(sessionId);
            NewLobby.Host = session.player;
            Console.WriteLine(session.player.playerName);
            NewLobby.ConnectionIdentifier = RandomStringGenerator.GenerateRandomString();
            bool status = await _lobbyRepo.CreateLobbyAsync(NewLobby,password);
            if (status)
            {
                //NewLobby.Players.Add(NewLobby.Host);
                return Ok();
            }

            return Conflict($"The name {NewLobby.LobbyName} is already in-use.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving lobbies.");
            return StatusCode(500, "An error occurred while retrieving lobbies.");
        }
    }
    
    [HttpPost("lobby/{lobbyName}/join")]
    public async Task<ActionResult> JoinLobby(string lobbyName, [FromBody] Player player,[FromQuery] string? passkey = null)
    {
        try
        {
            var lobby = await _lobbyRepo.GetLobbyAsync(lobbyName);
            if (lobby == null)
            {
                return NotFound($"Lobby {lobbyName} not found.");
            }
            //no duplicates
            if (lobby.Players.Any(p => p.playerName == player.playerName))
            {
                return BadRequest("Player already in the lobby.");
            }
            //check password
            if (lobby.Locked)
            {
                if (!string.IsNullOrEmpty(passkey))
                {
                    bool passwordValid = await _pwdRepo.ValidPassword(lobbyName, passkey);
                    if (!passwordValid)
                    {
                        return BadRequest("You got the password wrong nerd");
                    }
                }
                else
                {
                    return BadRequest("This lobby is currently locked and cannot be joined.");
                }
            }
            player.key = Guid.NewGuid().ToString();
            lobby.Players.Add(player);
            bool updateStatus = await _lobbyRepo.UpdateLobbyAsync(lobby);
            if (updateStatus)
            {
                return Ok(playerRepo.CreatePlayer(player));
            }

            return StatusCode(500, "Failed to join the lobby.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error trying to join the lobby.");
            return StatusCode(500, "An error occurred while trying to join the lobby.");
        }
    }
    
    [HttpPost("lobby/{lobbyName}/leave")]
    public async Task<ActionResult> LeaveLobby(string lobbyName, [FromQuery] string token)
    {
        try
        {
            var lobby = await _lobbyRepo.GetLobbyAsync(lobbyName);
            if (lobby == null)
            {
                return NotFound($"Lobby {lobbyName} not found.");
            }
            Session? session = await playerRepo.GetPlayer(token);
            if (session == null)
            {
                return NotFound("Session not found.");
            }

            Player player = session.player;
            if (lobby.Players.Any(p => p.key == player.key))
            {
                var updatedPlayers = lobby.Players.Where(p => p.key != player.key).ToList();
                lobby.Players = updatedPlayers;  // Assigning a new list might help
                Console.WriteLine($"removed {player.playerName}, updating lobby");
                bool updateStatus = await _lobbyRepo.UpdateLobbyAsync(lobby);
                if (updateStatus)
                {
                    return Ok();
                }
            }


            return StatusCode(500, "You're not in that lobby");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error trying to join the lobby.");
            return StatusCode(500, "An error occurred while trying to join the lobby.");
        }
    }
    
    
    
    [HttpDelete("lobby/{lobbyName}")]
    public async Task<ActionResult> DeleteLobby(string lobbyName, string token)
    {
        try
        {
            var lobby = await _lobbyRepo.GetLobbyAsync(lobbyName);
            if (!await isHost(token,lobby))
            {
                return BadRequest("You are not the host.");
            }
            
            await _lobbyRepo.DeleteLobbyAsync(lobbyName);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving lobbies.");
            return StatusCode(500, "An error occurred while retrieving lobbies.");
        }
    }
    [HttpPut("lobby")]
    public async Task<ActionResult> UpdateLobby([FromBody] Lobby newLobby, [FromQuery] string token)
    {
        try
        {
            
            if (!await isHost(token,newLobby))
            {
                return BadRequest("You are not the host.");
            }

            await _lobbyRepo.UpdateLobbyAsync(newLobby);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving lobbies.");
            return StatusCode(500, "An error occurred while retrieving lobbies.");
        }
    }

    private async Task<bool> isHost(string Token, Player Host)
    {
        bool isHost = await sessionRepo.Valid(Token,Host);
        return isHost;
    }

    private async Task<bool> isHost(string Token, Lobby lobby)
    {
        bool isHost = await sessionRepo.Valid(Token,lobby.Host);
        return isHost;
    }

}