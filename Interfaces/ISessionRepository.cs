namespace LobbyAPI.Interfaces;
using LobbyAPI.Models;

public interface ISessionRepository
{
    public Task<bool> Valid(string sessionId);
    public Task<bool> Valid(string sessionId, Player player);
    public Task<Session> Get(string sessionId);
    public Task SetSession(Session session);
    public Task EndSession(Session session);
    public Task SetConnectionID(string sessionId, string connectionId);
    public Task UpdatePlayerName(Session session);
}