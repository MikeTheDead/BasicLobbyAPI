using LobbyAPI.Models;
using LobbyAPI.MongoCollectionControllers.Interface;
using MongoDB.Driver;

namespace LobbyAPI.MongoCollectionControllers;

public class LobbyMongoController : IMongoController<Lobby>
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

    public async Task<List<Lobby>> GetAll()
    {
        // Assuming _lobbyCollection is of type IMongoCollection<Lobby>
        return await _lobbyCollection.Find(_ => true).ToListAsync();
    }


    public async Task Set(Lobby value)
    {
        await _lobbyCollection.InsertOneAsync(value);
    }

    public async Task Put(Lobby value)
    {
        var filter = Builders<Lobby>.Filter.Eq(l => l.ConnectionIdentifier, value.ConnectionIdentifier);
        var update = Builders<Lobby>.Update.Set(l => l.Players, value.Players);
        await _lobbyCollection.UpdateOneAsync(filter, update);
        await CheckForPlayers(value);
    }

    public async Task Remove(Lobby value)
    {
        var builder = Builders<Lobby>.Filter;
        var filter = builder.Eq(l => l.ConnectionIdentifier, value.ConnectionIdentifier);
        await _lobbyCollection.DeleteOneAsync(filter);
    }
//check if the host left and if the lobby is empty
    async Task CheckForPlayers(Lobby value)
    {
        Console.WriteLine("CheckForPlayers");
        if (!value.Players.Contains(value.Host))
        {
            Console.WriteLine("Host left!");

            Player? newHost = value.Players.FirstOrDefault();
            //host is not in the lobby
            if (value.Players.Count > 0&&newHost!=null)
            {
                Console.WriteLine($"{newHost.playerName} is the new host");
                value.Host = value.Players.FirstOrDefault();
            }
            else
            {
                Console.WriteLine($"No one in Lobby, removing it");
                await Remove(value);
                
            }
        }
    }
}