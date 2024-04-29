using LobbyAPI.Models;
using LobbyAPI.MongoCollectionControllers.Interface;
using MongoDB.Driver;

namespace LobbyAPI.MongoCollectionControllers;

public class ConnectionAddressMongoController : IMongoController<ConnectionAddress>
{
    private readonly IMongoCollection<ConnectionAddress> connAddCollection;

    public ConnectionAddressMongoController(IMongoCollection<ConnectionAddress> _connAddress)
    {
        connAddCollection = _connAddress;
    }
    
    
    public async Task<ConnectionAddress?> Get(string value)
    {
        var builder = Builders<ConnectionAddress>.Filter;
        var filter = builder.Eq(conn => conn.ConnectionIdentifier, value);
        return await connAddCollection.Find(filter).FirstOrDefaultAsync();
    }

    public Task<List<ConnectionAddress>> GetAll()
    {
        throw new NotImplementedException();
    }

    public async Task Set(ConnectionAddress value)
    {
        await connAddCollection.InsertOneAsync(value);
    }

    public Task Put(ConnectionAddress value)
    {
        throw new NotImplementedException();
    }

    public async Task Remove(ConnectionAddress value)
    {
        var builder = Builders<ConnectionAddress>.Filter;
        var filter = builder.Eq(conn => conn, value);
        await connAddCollection.DeleteOneAsync(filter);
    }
}