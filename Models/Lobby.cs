namespace LobbyAPI.Models;

/// <summary>
/// Represents a lobby object
/// </summary>
public class Lobby
{
    public Player Host { get; set; }
    public string LobbyName { get; set; } = "Lobby";
    public bool Locked { get; set; } = false;
    public List<Player> Players { get; set; }
}