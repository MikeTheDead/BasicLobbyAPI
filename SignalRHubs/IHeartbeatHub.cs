using LobbyAPI.Models;

namespace LobbyAPI.SignalRHubs;

public interface IHeartbeatHub
{
    Task SendHeartbeat(string connectionId, Heartbeat heartbeat);
}