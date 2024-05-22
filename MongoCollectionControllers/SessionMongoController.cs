using LobbyAPI.Interfaces;
using LobbyAPI.Models;
using LobbyAPI.MongoCollectionControllers.Interface;
using MongoDB.Driver;

namespace LobbyAPI.MongoCollectionControllers;

public class SessionMongoController : IMongoSessionExtension
{
    private readonly IMongoCollection<Session> sessionCollection;
    private readonly IMongoCollection<PlayerKey> KVPCollection;

    public SessionMongoController(IMongoCollection<Session> _sessionCollection, IMongoCollection<PlayerKey> _KVPCollection)
    {
        sessionCollection = _sessionCollection;
        KVPCollection = _KVPCollection;
    }
    
    public async Task<Session?> Get(string value)
    {
        var builder = Builders<Session>.Filter;
        var filter = builder.Eq(p => p.SessionID, value);

        var result = await sessionCollection.Find(filter).FirstOrDefaultAsync();
        return result;
    }

    public async Task<Session?> GetViaConnectionId(string value)
    {
        var builder = Builders<Session>.Filter;
        var filter = builder.Eq(p => p.ConnectionID, value);

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

    public async Task PutConnectionID(Session value)
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

            Console.WriteLine($"Matched Count: {updateResult.MatchedCount}, Modified Count: {updateResult.ModifiedCount}");

            if (updateResult.ModifiedCount == 0)
            {
                Console.WriteLine("No documents were modified. Checking if the session exists with the same ConnectionID.");
                var existingSession = await sessionCollection.Find(filter).FirstOrDefaultAsync();
                if (existingSession != null)
                {
                    if (existingSession.ConnectionID == value.ConnectionID)
                    {
                        await sessionCollection.UpdateOneAsync(filter, update);
                        Console.WriteLine($"Existing Session found with the same ConnectionID: {existingSession.ConnectionID}. No update necessary.");
                        return; 
                    }
                    else
                    {
                        Console.WriteLine($"Existing ConnectionID: {existingSession.ConnectionID}");
                    }
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

    public async Task PutPlayer(Session value)
    {
        try
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), "Session value cannot be null.");
            }

            Console.WriteLine($"Attempting to update SessionID: {value.SessionID} with new Player: {value.player.playerName}");

            var filter = Builders<Session>.Filter.Eq(l => l.SessionID, value.SessionID);
            var update = Builders<Session>.Update.Set(l => l.player, value.player);
            var updateResult = await sessionCollection.UpdateOneAsync(filter, update);
            Console.WriteLine($"Matched Count: {updateResult.MatchedCount}, Modified Count: {updateResult.ModifiedCount}");

            if (updateResult.ModifiedCount == 0)
            {
                Console.WriteLine("No documents were modified. Checking if the session exists with the same player.");

                var existingSession = await sessionCollection.Find(filter).FirstOrDefaultAsync();
                if (existingSession != null)
                {
                    if (existingSession.player.Equals(value.player))
                    {
                        Console.WriteLine($"Player is already named {value.player.playerName}. No update necessary.");
                        return;
                    }
                }
                else
                {
                    Console.WriteLine("Session not found.");
                }
                Console.WriteLine("Session not changed.");
                return;
            }
            await UpdatePlayerKey(value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            throw;
        }
    }



    public async Task UpdatePlayerKey(Session session)
    {
        try
        {
            var kvp = await GetKVPOfSession(session);
            if (kvp == null)
            {
                Console.WriteLine($"No PlayerKey found for SessionID: {session.SessionID}");
                return; 
            }

            var filter = Builders<PlayerKey>.Filter.Eq(l => l.Token, kvp.Token);
            var update = Builders<PlayerKey>.Update.Set(l => l.Player, session.player);
            var updateResult = await KVPCollection.UpdateOneAsync(filter, update);
        
            Console.WriteLine($"PlayerKey update - Matched Count: {updateResult.MatchedCount}, Modified Count: {updateResult.ModifiedCount}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred in UpdatePlayerKey: {ex.Message}");
            throw;
        }
    }

    
    public async Task<PlayerKey> GetKVPOfSession(Session session)
    {
        var filter = Builders<PlayerKey>.Filter.Eq(l => l.CurrentSession.SessionID, session.SessionID);
        var result = await KVPCollection.Find(filter).FirstOrDefaultAsync();

        if (result == null)
        {
            Console.WriteLine($"No PlayerKey found for SessionID: {session.SessionID}");
        }

        return result;
    }


    public async Task SubmitPlayerUpdate(Session session)
    {
        await PutPlayer(session);
        
    }

    public async Task SubmitUpdate(Session session)
    {
        await PutClientID(session);
    }
    public async Task PutClientID(Session value)
    {
        try
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), "Session value cannot be null.");
            }

            var filter = Builders<Session>.Filter.Eq(l => l.SessionID, value.SessionID);
            var update = Builders<Session>.Update.Set(l => l.NGOClientID, value.NGOClientID);
            var updateResult = await sessionCollection.UpdateOneAsync(filter, update);
            Console.WriteLine($"Matched Count: {updateResult.MatchedCount}, Modified Count: {updateResult.ModifiedCount}");

            if (updateResult.ModifiedCount == 0)
            {
                Console.WriteLine("No documents were modified. Checking if the session exists with the same player.");

                var existingSession = await sessionCollection.Find(filter).FirstOrDefaultAsync();
                if (existingSession != null)
                {
                    if (existingSession.player.Equals(value.player))
                    {
                        Console.WriteLine($"{value.player.playerName} already has ClientID set");
                        return;
                    }
                }
                else
                {
                    Console.WriteLine("Session not found.");
                }
                Console.WriteLine("Session not changed.");
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