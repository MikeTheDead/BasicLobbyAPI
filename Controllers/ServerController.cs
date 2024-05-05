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
    private readonly ISessionRepository _sessionRepo;
    private readonly SignalHubs _signals;
    

    public ServerController(IPlayerRepository playerRepo, ISessionRepository sessionRepo,
        SignalHubs signals)
    {
        _playerRepo = playerRepo;
        _sessionRepo = sessionRepo;
        _signals = signals;
    }
    
    
    [HttpGet("login")]
    public async Task<ActionResult<Session>> Login([FromQuery]string token = null)
    {
        Console.WriteLine("Login");
        Session newSession = null;
        try
        {
            bool newplayer = false;
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("IsNullOrEmpty");
                var newPlayer = new Player($"player {_globalRandom.NextInt64(1000, 9999)}");
                token = await _playerRepo.CreatePlayer(newPlayer);
                newplayer = true;
                newSession = await _playerRepo.GetPlayer(token);
            }
            else
            {
                try
                {
                    newSession = await _playerRepo.GetPlayer(token);
                }
                catch (Exception e)
                {
                    
                }

                if (newSession == null)
                {
                    return NotFound();
                }
            }
            Console.WriteLine("GetPlayer");
            if (newplayer)
            {
                _signals.ConnectionHub.QueueSession(token, newSession.SessionID);
            }
            await _sessionRepo.SetSession(newSession);
            return newSession;
        }
        catch (Exception ex)
        {
            
            return StatusCode(500, "An error occurred while logging in.");
        }
    }
    [HttpGet("logout/{token}")]
    public async Task<ActionResult> Logout(string token)
    {
        Console.WriteLine("Logout");
        try
        {
            if (!string.IsNullOrEmpty(token))
            {
                await _sessionRepo.EndSession(await _playerRepo.GetPlayer(token));
                Console.WriteLine("logged out");
            }
        }
        catch (Exception ex)
        {
            
            return StatusCode(500, "An error occurred while logging in.");
        }

        return Ok();
    }

    [HttpGet("heartbeat")]
    public async Task<ActionResult> Heartbeat(Heartbeat heartbeat)
    {
        var session = await _sessionRepo.Valid(heartbeat.sessionId);
        if (!session)
        {
            return StatusCode(500);
        }
        
        await Task.Delay(1000);
        await _signals.HeartbeatHub.SendHeartbeat(heartbeat.sessionId, heartbeat);
        Console.WriteLine($"send heartbeat {heartbeat.sessionId}");
        return Ok();
    }
}