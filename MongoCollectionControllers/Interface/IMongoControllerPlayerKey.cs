using LobbyAPI.Models;
using MongoDB.Driver;

namespace LobbyAPI.Interfaces;

public interface IMongoControllerPlayerKey<T>
{
    Task<T?> Get(string value);
    Task<List<T>> GetAll();
    Task Set(T value);
    Task Put(T value);
    Task Remove(T value);
    public Task UpdatePlayer(Session verifiedSession);

}