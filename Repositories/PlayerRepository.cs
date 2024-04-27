using LobbyAPI.Interfaces;
using LobbyAPI.Models;
using Raven.Client.Documents;

namespace LobbyAPI.Repositories;

/// <summary>
/// For creating, storing and comparing unique identifiers and ensuring the players arent being removed by random people
/// using insomnia or something
/// </summary>
public class PlayerRepository : IPlayerRepository
{

    private readonly IDocumentStore _playerStore;
    
    public PlayerRepository(IDocumentStore playerStore)
    {
        _playerStore = playerStore;
    }
    
    
    /// <summary>
    /// Create the player then send their security key for later requests
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    public string CreatePlayer(Player player)
    {
        using (var session = _playerStore.OpenSession())
        {
            Console.WriteLine(player.key);
            bool existingPlayer = session.Query<PlayerKey>().Any(p => p.Player.key == player.key);
            if (existingPlayer)
            {
                return string.Empty;
            }
            var token = Guid.NewGuid().ToString();
            session.Store(new PlayerKey(player, token), token);
            session.SaveChanges();
            return token;
        }
    }

    public async Task<bool> DisposePlayer(string token, Player player)
    {
        using (var session = _playerStore.OpenAsyncSession())
        {
            bool valid = await ValidPlayer(token, player);
            if (valid)
            {
                session.Delete(GetPlayerKVP(token));
                return true;
            }
        }

        return false;
    }
    public async Task<bool> ValidPlayer(string token)
    {
        using (var session = _playerStore.OpenAsyncSession())
        {
            var playerKVP = await session.LoadAsync<PlayerKey>(token);
            if (playerKVP != null)
            {
                return true;
            }
        }

        return false;
    }
    public async Task<bool> ValidPlayer(string token, Player player)
    {
        using (var session = _playerStore.OpenAsyncSession())
        {
            var playerKVP = await session.LoadAsync<PlayerKey>(token);
            if (playerKVP != null && playerKVP.Player.key == player.key)
            {
                return true;
            }
        }

        return false;
    }
    public async Task<PlayerKey> GetPlayerKVP(string token)
    {
        using (var session = _playerStore.OpenAsyncSession())
        {
            var playerKVP = await session.LoadAsync<PlayerKey>(token);
            return playerKVP;
        }
    }
    public async Task<Player> GetPlayer(string token)
    {
        using (var session = _playerStore.OpenAsyncSession())
        {
            var playerKVP = await session.LoadAsync<PlayerKey>(token);
            return playerKVP.Player;
        }
    }
}