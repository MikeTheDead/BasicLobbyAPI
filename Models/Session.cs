using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LobbyAPI.Models;

/// <summary>
/// Responsible for everything after the login
/// </summary>
public class Session
{
    [BsonId] public ObjectId _id { get; set; } = ObjectId.GenerateNewId();
    [BsonElement("sessionID")]
    public string SessionID { get; set; }
    [BsonElement("player")]
    public Player player { get; set; }

    public Session()
    {
        SessionID = Guid.NewGuid().ToString();
        player = new Player();
    }
    public Session(Player _player)
    {
        Console.WriteLine($"{_player.playerName}");
        SessionID = Guid.NewGuid().ToString();
        player = _player;
    }
}
