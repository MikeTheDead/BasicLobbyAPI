using LobbyAPI.Models;
using LobbyAPI.MongoCollectionControllers.Interface;
using MongoDB.Driver;

namespace LobbyAPI.MongoCollectionControllers;

public class PlayerMongoController : IMongoController<Player>
{
    private readonly IMongoCollection<Player> _playerCollection;
    
    public PlayerMongoController(IMongoCollection<Player> playerCollection)
    {
        _playerCollection = playerCollection;
    }
    
    public async Task<Player?> Get(string value)
    {
        var builder = Builders<Player>.Filter;
        var filter = builder.Eq(p => p.key, value);

        var result = await _playerCollection.Find(filter).FirstOrDefaultAsync();
        return result;
    }

    public Task<List<Player>> GetAll()
    {
        throw new NotImplementedException();
    }


    public async Task Set(Player value)
    {
        await _playerCollection.InsertOneAsync(value);
    }


    public async Task Put(Player value)
    {
        var builder = Builders<Player>.Filter;
        var filter = builder.Eq(p => p.key, value.key);
        await _playerCollection.ReplaceOneAsync(filter, value);
    }

    public async Task Remove(Player value)
    {
        var builder = Builders<Player>.Filter;
        var filter = builder.Eq(p => p.key, value.key);
        await _playerCollection.DeleteOneAsync(filter);
    }
}