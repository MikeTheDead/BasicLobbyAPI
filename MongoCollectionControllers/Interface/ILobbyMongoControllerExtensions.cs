using LobbyAPI.Models;

namespace LobbyAPI.MongoCollectionControllers.Interface;

public interface ILobbyMongoControllerExtensions
{
    Task<Lobby?> GetLobbyOfPlayer(string value);
}