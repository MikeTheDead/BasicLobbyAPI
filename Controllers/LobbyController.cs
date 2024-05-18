using LobbyAPI.Interfaces;
using LobbyAPI.Middlewares;
using LobbyAPI.Models;
using LobbyAPI.SignalRHubs;
using LobbyAPI.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace LobbyAPI.Controllers;

[ApiController]
[Route("lobby")]
[ValidateSession]
public class LobbyController : ControllerBase
{
    private readonly ILogger<LobbyController> _logger;
    private readonly SignalHubs _hubs;
    private readonly IHubOperations _hubOperations;
    
    #region Repos

    private readonly ILobbyRepository _lobbyRepo;
    private readonly IPasswordRepository _pwdRepo;
    private readonly IPlayerRepository _playerRepo;
    private readonly ISessionRepository _sessionRepo;
    private readonly IConnectionAddressRepository _connAddRepo;

    #endregion

    public LobbyController(ILogger<LobbyController> logger, ILobbyRepository lobbyRepo, IPasswordRepository pwdRepo, IPlayerRepository playerRepo,
        ISessionRepository sessionRepo, SignalHubs hubs, IHubOperations hubOperations,IConnectionAddressRepository connAddRepo)
    {
        _logger = logger;
        _lobbyRepo = lobbyRepo;
        _pwdRepo = pwdRepo;
        _playerRepo = playerRepo;
        _sessionRepo = sessionRepo;
        _hubs = hubs;
        _hubOperations = hubOperations;
        _connAddRepo = connAddRepo;
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
    [HttpGet("{name}")]
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
    
    [HttpPost("new")]
    public async Task<ActionResult> CreateLobby([FromBody] Lobby newLobby, [FromQuery] string? password = null)
    {
        Console.WriteLine($"CreateLobby {newLobby.LobbyName}");
        try
        {
            HttpContext.Request.Headers.TryGetValue("sessionId", out var sessionId);
        
            if (string.IsNullOrEmpty(sessionId))
            {
                return Unauthorized("Session ID not found.");
            }

            var session = await _sessionRepo.Get(sessionId);
            if (session == null)
            {
                return Unauthorized("Invalid session ID.");
            }

            newLobby.Host = session.player;
            if (newLobby.Host == null)
            {
                return BadRequest("Session has no associated player.");
            }

            Console.WriteLine(session.player.playerName);
            newLobby.ConnectionIdentifier = RandomStringGenerator.GenerateRandomString();
            string hostname = HttpContext.Connection.RemoteIpAddress.ToString();
            bool status = await _lobbyRepo.CreateLobbyAsync(newLobby, password);
            if (status)
            {
                Console.WriteLine("Lobby made");
                var _lobby = await _lobbyRepo.GetLobbyAsync(newLobby.ConnectionIdentifier);

                if (_lobby == null)
                {
                    Console.WriteLine("Failed to retrieve the created lobby.");
                    return StatusCode(500, "Failed to retrieve the created lobby.");
                }

                if (string.IsNullOrEmpty(session.ConnectionID))
                {
                    Console.WriteLine("ConnectionID is null or empty.");
                    return StatusCode(500, "ConnectionID is null or empty.");
                }

                await _hubs.ConHub.SendLobby(session.ConnectionID, _lobby);
                Console.WriteLine("SendLobby called successfully.");
                var waitForConnadd = _connAddRepo.CreatePair(hostname, _lobby);
                if (waitForConnadd.IsFaulted)
                {
                    return Conflict($"Lobby made but failed to set hostname.");
                }
                return Ok();
            }

            return Conflict($"The name {newLobby.LobbyName} is already in use.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating lobby");
            return StatusCode(500, "An error occurred while creating the lobby.");
        }
    }
    
    [HttpPost("{connectionId}/join")]
    public async Task<ActionResult> JoinLobby(string connectionId,[FromQuery] string? passkey = null)
    {
        try
        {
            var sess = await _sessionRepo.Get(CustomContexts.SessionId);
            Player player = sess.player;
            
            var lobby = await _lobbyRepo.GetLobbyAsync(connectionId);
            if (lobby == null)
            {
                return NotFound($"Lobby {connectionId} not found.");
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
                    bool passwordValid = await _pwdRepo.ValidPassword(connectionId, passkey);
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
                await _hubOperations.LobbyService.EnterLobby(sess.ConnectionID, lobby);
                return Ok();
            }

            return StatusCode(500, "Failed to join the lobby.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error trying to join the lobby.");
            return StatusCode(500, "An error occurred while trying to join the lobby.");
        }
    }
    
    [HttpPost("{connectionID}/leave")]
    public async Task<ActionResult> LeaveLobby(string connectionID, [FromQuery] string token)
    {
        try
        {
            var lobby = await _lobbyRepo.GetLobbyAsync(connectionID);
            if (lobby == null)
            {
                return NotFound($"Lobby {connectionID} not found.");
            }
            Session? session = await _playerRepo.GetPlayer(token);
            if (session == null)
            {
                return NotFound("Session not found.");
            }

            Player player = session.player;

            //Add ownership transfer later
            if (lobby.Host == player)
            {
                bool taskStatus = await _lobbyRepo.DeleteLobbyAsync(lobby.LobbyName);
                if (taskStatus)
                {
                    return Ok();
                }
            }
            
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
        bool isHost = await _sessionRepo.Valid(Token,Host);
        return isHost;
    }

    private async Task<bool> isHost(string Token, Lobby lobby)
    {
        bool isHost = await _sessionRepo.Valid(Token,lobby.Host);
        return isHost;
    }

}