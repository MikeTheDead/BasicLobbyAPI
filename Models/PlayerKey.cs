using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LobbyAPI.Models;

public class PlayerKey
{
    [BsonId] public ObjectId _id { get; set; } = ObjectId.GenerateNewId();
    [BsonElement("token")]
    public string Token { get; set; }
    [BsonElement("Player")]
    public Player Player { get; set; }

    public PlayerKey(Player player, string token)
    {
        Token = token;
        Player = player;
    }
}