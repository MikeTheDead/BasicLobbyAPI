using LobbyAPI.Interfaces;
using LobbyAPI.Models;
using LobbyAPI.MongoCollectionControllers.Interface;

namespace LobbyAPI.Repositories;

public class ConnectionAddressRepository : IConnectionAddressRepository
{

    private readonly IMongoController<ConnectionAddress> connAddController;

    public ConnectionAddressRepository(IMongoController<ConnectionAddress> connAdd)
    {
        connAddController = connAdd;
    }
    
    
    public async Task CreatePair(string hostname, Lobby lobby)
    {
        var connAdd = new ConnectionAddress(hostname,lobby.ConnectionIdentifier);
        await connAddController.Set(connAdd);
    }

    public async Task<ConnectionAddress> GetPair(string connectionID)
    {
        return await connAddController.Get(connectionID);
    }
}