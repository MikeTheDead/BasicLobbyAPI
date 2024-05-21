namespace LobbyAPI.Interfaces;
using LobbyAPI.Models;

public interface ILobbyRepository
{
    Repositories.Repositories repo { get; set; }
    Task<Lobby?> GetLobbyAsync(string connectionIdentifier);
    Task<List<Lobby>> GetLobbiesAsync();
    Task<bool> CreateLobbyAsync(Lobby newLobby, string? passkey = null);
    Task<bool> UpdateLobbyAsync(Lobby newLobby);
    Task<bool> DeleteLobbyAsync(string lobbyName);
}