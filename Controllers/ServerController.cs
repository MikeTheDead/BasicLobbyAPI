using LobbyAPI.Interfaces;
using LobbyAPI.Models;
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
    

    public ServerController(IPlayerRepository playerRepo, ISessionRepository sessionRepo)
    {
        _playerRepo = playerRepo;
        _sessionRepo = sessionRepo;
    }
    
    
    [HttpGet("login")]
    public async Task<ActionResult<Session>> Login([FromQuery]string token = null)
    {
        Console.WriteLine("Login");
        try
        {
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("IsNullOrEmpty");
                var newPlayer = new Player($"player {_globalRandom.NextInt64(1000, 9999)}");
                token = await _playerRepo.CreatePlayer(newPlayer);
            }
            Console.WriteLine("GetPlayer");
            var newSession = await _playerRepo.GetPlayer(token);
            await _sessionRepo.SetSession(newSession);
            return newSession;
        }
        catch (Exception ex)
        {
            
            return StatusCode(500, "An error occurred while logging in.");
        }
    }
    
    
    
    
}