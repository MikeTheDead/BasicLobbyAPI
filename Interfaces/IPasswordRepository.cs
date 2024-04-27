namespace LobbyAPI.Interfaces;

public interface IPasswordRepository
{
    public Task<bool> ValidPassword(string lobbyName, string password);
    public Task SetPassword(string lobbyName, string password);
}