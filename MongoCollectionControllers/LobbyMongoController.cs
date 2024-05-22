using LobbyAPI.Models;
using LobbyAPI.MongoCollectionControllers.Interface;
using MongoDB.Driver;

namespace LobbyAPI.MongoCollectionControllers;

public class LobbyMongoController : IMongoController<Lobby>, ILobbyMongoControllerExtensions
{
    
    private readonly IMongoCollection<Lobby> _lobbyCollection;
    
    public LobbyMongoController(IMongoCollection<Lobby> lobbyCollection)
    {
        _lobbyCollection = lobbyCollection;
    }
    public async Task<Lobby?> Get(string value)
    {
        var builder = Builders<Lobby>.Filter;
        var filter = builder.Eq(lob => lob.ConnectionIdentifier, value);
        return await _lobbyCollection.Find(filter).FirstOrDefaultAsync();
    }


    public async Task<Lobby?> GetLobbyOfPlayer(string value)
    {
        var builder = Builders<Lobby>.Filter;
        var filter = builder.ElemMatch(lob => lob.Players, player => player.connectionID == value);

        return await _lobbyCollection.Find(filter).FirstOrDefaultAsync();
    }
    public async Task<List<Lobby>> GetAll()
    {
        // Assuming _lobbyCollection is of type IMongoCollection<Lobby>
        return await _lobbyCollection.Find(_ => true).ToListAsync();
    }


    public async Task Set(Lobby value)
    {
        await _lobbyCollection.InsertOneAsync(value);
    }

    /// <summary>
    /// Update Lobby
    /// Always check to see if lobby is active
    /// </summary>
    /// <param name="value"></param>
    public async Task Put(Lobby value)
    {
        bool isActive = HostCheck(value);
        if (!isActive)
        {
            await Remove(value);
            return;
        }

        var filter = Builders<Lobby>.Filter.Eq(l => l.ConnectionIdentifier, value.ConnectionIdentifier);
        var update = Builders<Lobby>.Update.Set(l => l.Players, value.Players).Set(lo=>lo.Host, value.Host);
        await _lobbyCollection.UpdateOneAsync(filter, update);
    }

    public async Task Remove(Lobby value)
    {
        var builder = Builders<Lobby>.Filter;
        var filter = builder.Eq(l => l.ConnectionIdentifier, value.ConnectionIdentifier);
        await _lobbyCollection.DeleteOneAsync(filter);
    }
//check if the host left and if the lobby is empty
    bool HostCheck(Lobby value)
    {
        Console.WriteLine("CheckForPlayers");
        if (value.Players.FirstOrDefault(P=>P.key == value.Host.key) == null)
        {
            Console.WriteLine("Host left!");
            //host is not in the lobby
            if (value.Players.Count > 0)
            {
                Player? newHost = value.Players.FirstOrDefault();
                Console.WriteLine($"{newHost.playerName} is the new host");
                value.Host = newHost;
            }
            else
            {
                Console.WriteLine($"No one in Lobby, removing it");
                return false;
            }
        }

        return true;
    }
}