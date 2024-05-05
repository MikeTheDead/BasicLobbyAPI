using Newtonsoft.Json;

namespace LobbyAPI.Models;

public class Heartbeat
{
    [JsonProperty("sessionId")]
    public string sessionId { get; set; }
}