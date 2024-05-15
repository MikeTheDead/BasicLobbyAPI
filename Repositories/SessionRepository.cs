using LobbyAPI.Interfaces;
using LobbyAPI.Models;
using LobbyAPI.MongoCollectionControllers;
using LobbyAPI.MongoCollectionControllers.Interface;

namespace LobbyAPI.Repositories;

public class SessionRepository : ISessionRepository
{
    private readonly IMongoSessionExtension sessionMongoController;
    public SessionRepository(IMongoSessionExtension _sessionMongoController)
    {
        sessionMongoController = _sessionMongoController;
    }
    
    
    public async Task<bool> Valid(string sessionId)
    {
        var session = await sessionMongoController.Get(sessionId);
        return session != null;
    }
    public async Task<bool> Valid(string sessionId,Player player)
    {
        var session = await sessionMongoController.Get(sessionId);
        if (session == null)
        {
            return false;
        }
        return session.player == player;
    }

    public async Task<Session> Get(string sessionId)
    {
        Session? session = await sessionMongoController.Get(sessionId);
        if (session == null)
        {
            Console.WriteLine($"failed: {sessionId}");
            return null;
        }
        return session;
    }
    public async Task<Session?> SetConnectionID(string sessionId,string connectionId)
    {
        Console.WriteLine("SettConnID");
        Session? session = await sessionMongoController.Get(sessionId);
        if (session != null)
        {
            Console.WriteLine("SettConnID");
            session.ConnectionID = connectionId;
            session.player.connectionID = session.ConnectionID;
            await sessionMongoController.SubmitPlayerUpdate(session);
            Console.WriteLine($"{session.player.connectionID}");
            await sessionMongoController.PutConnectionID(session);
        }

        return await sessionMongoController.Get(sessionId);
    }
    public async Task UpdatePlayerName(Session _session)
    {
        Session session = await Get(_session.SessionID);
        session.player.playerName = _session.player.playerName;
        session.player.connectionID = _session.ConnectionID;
        await sessionMongoController.SubmitPlayerUpdate(session);
    }

    public async Task SetSession(Session session)
    {
        await sessionMongoController.Set(session);
    }

    public async Task EndSession(Session session)
    {
        await sessionMongoController.Remove(session);
    }
}