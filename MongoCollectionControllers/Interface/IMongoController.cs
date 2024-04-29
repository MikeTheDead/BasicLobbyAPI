using LobbyAPI.Models;

namespace LobbyAPI.MongoCollectionControllers.Interface;

public interface IMongoController<T>
{
    Task<T?> Get(string value);
    Task<List<T>> GetAll();
    Task Set(T value);
    Task Put(T value);
    Task Remove(T value);
}