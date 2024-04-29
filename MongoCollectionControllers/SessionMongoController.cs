using LobbyAPI.Models;
using LobbyAPI.MongoCollectionControllers.Interface;
using MongoDB.Driver;

namespace LobbyAPI.MongoCollectionControllers;

public class SessionMongoController : IMongoController<Session>
{
    private readonly IMongoCollection<Session> sessionCollection;

    public SessionMongoController(IMongoCollection<Session> _sessionCollection)
    {
        sessionCollection = _sessionCollection;
    }
    
    public async Task<Session?> Get(string value)
    {
        var builder = Builders<Session>.Filter;
        var filter = builder.Eq(p => p.SessionID, value);

        var result = await sessionCollection.Find(filter).FirstOrDefaultAsync();
        return result;
    }

    public Task<List<Session>> GetAll()
    {
        throw new NotImplementedException();
    }

    public async Task Set(Session value)
    {
        await sessionCollection.InsertOneAsync(value);
    }

    public Task Put(Session value)
    {
        throw new NotImplementedException();
    }

    public async Task Remove(Session value)
    {
        var builder = Builders<Session>.Filter;
        var filter = builder.Eq(p => p.SessionID, value.SessionID);
        await sessionCollection.DeleteOneAsync(filter);
    }
}