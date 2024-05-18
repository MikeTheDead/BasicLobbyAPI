using LobbyAPI.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LobbyAPI.Models;

/// <summary>
///store IP address separately till host broadcasts it
/// (i plan to use a transport layer this is just easier rn)
/// </summary>
public class ConnectionAddress
{
    [BsonId] public ObjectId _id { get; set; } = ObjectId.GenerateNewId();
    //The actual IP
    [BsonElement("IPAddress")]
    public string IPAddress { get; set; }
    //The code used to connect
    [BsonElement("connectionIdentifier")]
    public string ConnectionIdentifier { get; set; }

    public ConnectionAddress(string _IPAddress, string connectionIdentifier)
    {
        IPAddress = _IPAddress;
        ConnectionIdentifier = connectionIdentifier;
    }
}