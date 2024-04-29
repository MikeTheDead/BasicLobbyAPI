namespace LobbyAPI.Interfaces;
using LobbyAPI.Models;

public interface IPlayerRepository
{
    Task<string> CreatePlayer(Player player);
    Task<bool> ValidPlayer(string token, Player player);
    Task<Session?> GetPlayer(string token);
}