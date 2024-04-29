using LobbyAPI.Models;
using MongoDB.Driver;

namespace LobbyAPI.Utilities;

public class HandleShutdown
{
    private readonly IHostApplicationLifetime IHAL;
    private readonly IMongoCollection<Session> session;
    public HandleShutdown(IHostApplicationLifetime _IHAL,IMongoCollection<Session> _session)
    {
        IHAL = _IHAL;
        session = _session;
        IHAL.ApplicationStopping.Register(ClearSessions);
    }

    public async void ClearSessions()
    {
        await session.DeleteManyAsync(FilterDefinition<Session>.Empty);
    }
}