namespace LobbyAPI.Interfaces;
using LobbyAPI.Models;

public interface IPlayerRepository
{
    string CreatePlayer(Player player);
    Task<bool> DisposePlayer(string token, Player player);
    Task<bool> ValidPlayer(string token);
    Task<bool> ValidPlayer(string token, Player player);
    Task<PlayerKey> GetPlayerKVP(string token);
    Task<Player> GetPlayer(string token);
}