using LobbyAPI.Models;
using Microsoft.AspNetCore.SignalR;

namespace LobbyAPI.SignalRHubs;

public class ConnectionHub : Hub
{
    public async Task SendMessage(string connectionID, Lobby lobby)
        => await Clients.All.SendAsync("ReceiveMessage", connectionID, lobby);
}