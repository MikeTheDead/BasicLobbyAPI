using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LobbyAPI.Models;

/// <summary>
/// Represents a lobby object
/// </summary>
public class Lobby
{
    [BsonId] public ObjectId _id { get; set; } = ObjectId.GenerateNewId();
    [BsonElement("host")]
    public Player Host { get; set; }
    [BsonElement("lobbyName")]
    public string LobbyName { get; set; } = "Lobby";
    [BsonElement("locked")]
    public bool Locked { get; set; } = false;
    [BsonElement("players")]
    public List<Player> Players { get; set; }
    [BsonElement("connectionIdentifier")]
    public string? ConnectionIdentifier { get; set; }
    
}