namespace LobbyAPI.Interfaces;
using LobbyAPI.Models;

public interface ILobbyRepository
{
    Task<Lobby> GetLobbyAsync(string name);
    Task<List<Lobby>> GetLobbiesAsync();
    Task<bool> CreateLobbyAsync(Lobby newLobby, string? passkey = null);
    Task<bool> UpdateLobbyAsync(Lobby newLobby);
    Task<bool> DeleteLobbyAsync(string lobbyName);
}