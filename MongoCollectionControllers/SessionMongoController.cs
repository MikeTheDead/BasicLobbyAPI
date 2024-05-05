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

    public async Task Put(Session value)
    {
        try
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), "Session value cannot be null.");
            }

            Console.WriteLine($"Attempting to update SessionID: {value.SessionID} with new ConnectionID: {value.ConnectionID}");

            var filter = Builders<Session>.Filter.Eq(l => l.SessionID, value.SessionID);
            var update = Builders<Session>.Update.Set(l => l.ConnectionID, value.ConnectionID);
            var updateResult = await sessionCollection.UpdateOneAsync(filter, update);

            if (updateResult.ModifiedCount == 0)
            {
                Console.WriteLine("No documents were modified. Checking if the session exists with the same ConnectionID.");
                var existingSession = await sessionCollection.Find(filter).FirstOrDefaultAsync();
                if (existingSession != null)
                {
                    Console.WriteLine($"Existing ConnectionID: {existingSession.ConnectionID}");
                }
                else
                {
                    Console.WriteLine("Session not found.");
                }
                throw new InvalidOperationException("Session not found or not modified.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            throw;
        }
    }



    public async Task Remove(Session value)
    {
        var builder = Builders<Session>.Filter;
        var filter = builder.Eq(p => p.SessionID, value.SessionID);
        await sessionCollection.DeleteOneAsync(filter);
    }
}