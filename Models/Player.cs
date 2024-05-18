using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization; // Import for JsonPropertyName

namespace LobbyAPI.Models;

public class Player
{
    [BsonId]
    [JsonPropertyName("_id")] // Ensure this matches with JSON field names if necessary
    public ObjectId _id { get; set; } = ObjectId.GenerateNewId();

    [BsonElement("playerName")]
    [JsonPropertyName("playerName")]
    public string playerName { get; set; }

    [BsonElement("connectionID")]
    [JsonPropertyName("connectionID")]
    public string? connectionID { get; set; }
    
    [BsonElement("key")]
    [JsonPropertyName("key")]
    public string key { get; set; }

    public Player(string playerName = "player", string key = null)
    {
        this.playerName = playerName;
        this.key = key ?? Guid.NewGuid().ToString();
    }
}