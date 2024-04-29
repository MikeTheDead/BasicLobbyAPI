using LobbyAPI.Models;
using LobbyAPI.MongoCollectionControllers.Interface;
using MongoDB.Driver;

namespace LobbyAPI.MongoCollectionControllers;

public class PasswordMongoController : IMongoController<Password>
{

    private readonly IMongoCollection<Password> _passwordCollection;
    public PasswordMongoController(IMongoCollection<Password> passwordCollection)
    {
        _passwordCollection = passwordCollection;
    }
    
    
    /// <summary>
    /// Lobbyname=>password
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public async Task<Password?> Get(string value)
    {
        var builder = Builders<Password>.Filter;
        var filter = builder.Eq(p => p.LobbyName, value);

        var result = await _passwordCollection.Find(filter).FirstOrDefaultAsync();
        return result;
    }

    public Task<List<Password>> GetAll()
    {
        throw new NotImplementedException();
    }

    public async Task Set(Password value)
    {
        await _passwordCollection.InsertOneAsync(value);
    }

    public Task Put(Password value)
    {
        throw new NotImplementedException();
    }

    public async Task Remove(Password value)
    {
        var builder = Builders<Password>.Filter;
        var filter = builder.Eq(p => p, value);
        await _passwordCollection.DeleteOneAsync(filter);
    }
}