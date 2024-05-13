using LobbyAPI.Models;

namespace LobbyAPI.Interfaces;

public interface IMongoSessionExtension
{
    Task<Session?> Get(string value);
    Task<List<Session>> GetAll();
    Task Set(Session value);
    Task Put(Session value);
    Task Remove(Session value);
    public Task<PlayerKey> GetKVPOfSession(Session value);
    public Task SubmitPlayerUpdate(Session session);
}