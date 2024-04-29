using LobbyAPI.Interfaces;
using LobbyAPI.Models;
using LobbyAPI.MongoCollectionControllers;
using LobbyAPI.MongoCollectionControllers.Interface;
using MongoDB.Driver;

namespace LobbyAPI.Repositories;

/// <summary>
/// For creating, storing and comparing unique identifiers and ensuring the players arent being removed by random people
/// using insomnia or something
/// </summary>
public class PlayerRepository : IPlayerRepository
{
    private readonly IMongoController<Player> playerMongoController;
    private readonly IMongoController<PlayerKey> playerKeyMongoController;
    
    public PlayerRepository(IMongoController<Player> _playerMongoController,IMongoController<PlayerKey> _playerKeyMongoController)
    {
        playerMongoController = _playerMongoController;
        playerKeyMongoController = _playerKeyMongoController;
    }
    
    
    /// <summary>
    /// Create the player then send their player token.
    /// the player token is called to retrieve the account at the start, then the session is used to make further requests
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    public async Task<string> CreatePlayer(Player player)
    {
        Console.WriteLine(player.key);
        try
        {
            Player? existingPlayer = await playerMongoController.Get(player.key);
            if (existingPlayer != null)
            {
                if (!string.IsNullOrEmpty(existingPlayer.key))
                {
                    return existingPlayer.key;
                }
            }
            var token = Guid.NewGuid().ToString();
            var playerKey = new PlayerKey(player, token);
            await playerKeyMongoController.Set(playerKey);
            return token;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
    /// <summary>
    /// Validate the player
    /// (fix later)
    /// </summary>
    /// <param name="token"></param>
    /// <param name="player"></param>
    /// <returns></returns>
    public async Task<bool> ValidPlayer(string token, Player player)
    {
        var playerKVP = await playerKeyMongoController.Get(token);
        if (playerKVP != null && playerKVP.Player.key == player.key)
        {
            
        }

        return false;
    }
    
    /// <summary>
    /// Once Login, retrieve player and turn it into a session
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task<Session?> GetPlayer(string token)
    {
        Console.WriteLine("playerKeyMongoController");
        try
        {
            var playerKVP = await playerKeyMongoController.Get(token);
            var session = new Session(playerKVP.Player);
            if (session != null)
            {
                Console.WriteLine("session");
                return session;
            }
            return null;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}