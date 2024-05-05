using LobbyAPI.Models;
using Microsoft.AspNetCore.SignalR;

namespace LobbyAPI.SignalRHubs;

public class HeartbeatHub :Hub, IHeartbeatHub
{
    public async Task SendHeartbeat(string connectionId, Heartbeat heartbeat)
    {
        var user = Clients.User(connectionId);
        object[] args = new[] { heartbeat };
        await user.SendCoreAsync("Heartbeat", args);
    }
}