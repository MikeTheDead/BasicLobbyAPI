using LobbyAPI.Models;
using LobbyAPI.MongoCollectionControllers.Interface;
using MongoDB.Driver;

namespace LobbyAPI.MongoCollectionControllers;

public class PlayerKeyMongoController : IMongoController<PlayerKey>
{
    
    private readonly IMongoCollection<PlayerKey> _playerKeyCollection;
    
    public PlayerKeyMongoController(IMongoCollection<PlayerKey> playerKeyCollection)
    {
        _playerKeyCollection = playerKeyCollection;
    }
    
    public async Task<PlayerKey?> Get(string value)
    {
        var builder = Builders<PlayerKey>.Filter;
        var filter = builder.Eq(p => p.Token, value);

        var result = await _playerKeyCollection.Find(filter).FirstOrDefaultAsync();
        return result;
    }

    public Task<List<PlayerKey>> GetAll()
    {
        throw new NotImplementedException();
    }

    public async Task Set(PlayerKey value)
    {
        await _playerKeyCollection.InsertOneAsync(value);
    }

    public async Task Put(PlayerKey value)
    {
        var builder = Builders<PlayerKey>.Filter;
        var filter = builder.Eq(p => p.Token, value.Token);
        await _playerKeyCollection.ReplaceOneAsync(filter, value);
    }

    public async Task Remove(PlayerKey value)
    {
        var builder = Builders<PlayerKey>.Filter;
        var filter = builder.Eq(p => p.Token, value.Token);
        await _playerKeyCollection.DeleteOneAsync(filter);
    }
}