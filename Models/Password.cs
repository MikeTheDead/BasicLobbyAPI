using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class Password
{
    [BsonId] public ObjectId _id { get; set; } = ObjectId.GenerateNewId();
    [BsonElement("lobbyName")]
    public string LobbyName { get; set; }
    [BsonElement("hash")]
    public string Hash { get; set; }
    [BsonElement("salt")]
    public byte[] Salt { get; set; }

    public Password(string lobbyName, string hash, byte[] salt)
    {
        LobbyName = lobbyName;
        Hash = hash;
        Salt = salt;
    }
}