using LobbyAPI.Interfaces;
using LobbyAPI.Models;
using LobbyAPI.SignalRHubs;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace LobbyAPI.Controllers;
[ApiController]
[Route("server")]
public class ServerController : ControllerBase
{
    private static Random _globalRandom = new Random();
    private readonly ILobbyRepository _lobbyRepo;
    private readonly IPlayerRepository _playerRepo;
    private readonly ISessionRepository sessionRepo;
    private readonly IHubOperations HubOp;
    
    

    public ServerController(IPlayerRepository playerRepo,
        IHubOperations _HubOp)
    {
        _playerRepo = playerRepo;
        HubOp = _HubOp;
        sessionRepo = HubOp.RepoManager.SessionRepo;
    }
    
    
    [HttpGet("login")]
    public async Task<ActionResult<Session>> Login([FromQuery] string token = null)
    {
        Console.WriteLine("Login");
        Session newSession = null;
        try
        {
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("Non existent, making new player");
                token = await newPlayer();
            }

            // Attempt to retrieve the session using the token
            newSession = await _playerRepo.GetPlayer(token);
            if (newSession == null)
            {
                Console.WriteLine("Non existent, making new player");
                token = await newPlayer();
                newSession = await _playerRepo.GetPlayer(token);
            }
        
            Console.WriteLine($"{token} {newSession.SessionID}");
            HubOp.HubHandler.QueueSession(token, newSession.SessionID);
            await sessionRepo.SetSession(newSession);
            return newSession;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex}");
            return StatusCode(500, "An error occurred while logging in.");
        }
    }

    async Task<string> newPlayer()
    {
        Console.WriteLine("IsNullOrEmpty");
        var newPlayer = new Player($"player {_globalRandom.NextInt64(1000, 9999)}");
        return await _playerRepo.CreatePlayer(newPlayer);
    }

    [HttpGet("logout/{token}")]
    public async Task<ActionResult> Logout(string token)
    {
        Console.WriteLine("Logout");
        try
        {
            if (!string.IsNullOrEmpty(token))
            {
                await sessionRepo.EndSession(await _playerRepo.GetPlayer(token));
                Console.WriteLine("logged out");
            }
        }
        catch (Exception ex)
        {
            
            return StatusCode(500, "An error occurred while logging in.");
        }

        return Ok();
    }

    // [HttpGet("heartbeat")]
    // public async Task<ActionResult> Heartbeat(Heartbeat heartbeat)
    // {
    //     var session = await _sessionRepo.Valid(heartbeat.sessionId);
    //     if (!session)
    //     {
    //         return StatusCode(500);
    //     }
    //     
    //     await Task.Delay(1000);
    //     await _signals.HeartbeatHub.SendHeartbeat(heartbeat.sessionId, heartbeat);
    //     Console.WriteLine($"send heartbeat {heartbeat.sessionId}");
    //     return Ok();
    // }
}