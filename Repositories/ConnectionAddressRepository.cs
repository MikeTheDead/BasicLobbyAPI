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
        var connAdd = new ConnectionAddress(hostname);
        await connAddController.Set(connAdd);
        lobby.ConnectionIdentifier = connAdd.ConnectionIdentifier;
    }
}