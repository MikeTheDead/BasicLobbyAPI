namespace LobbyAPI.Models;

public class PlayerKey
{
    public string Token { get; set; }
    public Player Player { get; set; }

    public PlayerKey(Player player, string token)
    {
        Token = token;
        Player = player;
    }
}