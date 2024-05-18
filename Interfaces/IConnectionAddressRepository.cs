using LobbyAPI.Models;

namespace LobbyAPI.Interfaces;

public interface IConnectionAddressRepository
{
    Task CreatePair(string hostname, Lobby lobby);
    Task<ConnectionAddress> GetPair(string lobbyName);
}