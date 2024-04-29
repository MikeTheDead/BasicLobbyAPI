using LobbyAPI.Models;

namespace LobbyAPI.Interfaces;

public interface IConnectionAddressRepository
{
    Task CreatePair(string hostname, Lobby lobby);
}