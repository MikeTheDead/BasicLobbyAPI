using LobbyAPI.Interfaces;
using LobbyAPI.Models;
using LobbyAPI.Repositories;
using LobbyAPI.Utilities;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace LobbyAPI.SignalRHubs;

/// <summary>
/// Entry hub for delegating callbacks to their respective operations
/// </summary>
public class ConnectionHub : Hub, IConnectionHub
{
    private readonly IHubOperations _hubOperations;
    private Repositories.Repositories Repos;
    private int tries = 0;

    public ConnectionHub(IHubOperations hubOperations)
    {
        _hubOperations = hubOperations;
        Repos = _hubOperations.RepoManager;
    }

    public async Task LobbyJoined(Player player, string lobbyName)
    {
        await _hubOperations.LobbyJoined(player.connectionID, lobbyName);
    }

    public async Task Connect(string sessionId)
    {
        // Your existing connect logic
    }

    public override async Task OnConnectedAsync()
    {
        tries = 0;
        var sessionId = Context.GetHttpContext().Request.Query["sessionId"];
        var session = _hubOperations.HubHandler.Queue.FirstOrDefault(s => s.SessionId == sessionId);
        if (session == null)
        {
            Console.WriteLine($"Not queued, player exists");
        }

        await TryConnection(sessionId);
        await base.OnConnectedAsync();
    }

    async Task TryConnection(string sessionId)
    {
        await _hubOperations.TryConnection(sessionId, Clients, Context);
    }

    public async Task SendToken(string sessionId)
    {
        await _hubOperations.SendToken(Context.ConnectionId, sessionId);
    }

    public async Task ConfirmRequest(ClientResponse response)
    {
        await _hubOperations.ConfirmRequest(response);
    }

    public async Task UpdateDetails(Session session)
    {
        await _hubOperations.UpdateDetails(session);
    }

    public async Task SendMessage(string sessionId, string message)
    {
        var user = await Repos.SessionRepo.Get(sessionId);
        await _hubOperations.SendMessage(user.ConnectionID, message);
    }

    public void QueueSession(string token, string sessionId)
    {
        Console.WriteLine($"Queue session {sessionId}:{token}");
        _hubOperations.HubHandler.Queue.Add(new SessionQueue
        {
            SessionId = sessionId,
            Token = token
        });
    }
    public async Task EnterLobby(string lobbyName)
    {
        await _hubOperations.EnterLobby(lobbyName, Context);
    }

    public async Task BroadcastLobbyHostname(string lobbyName, string hostToken)
    {
        //basic security checks
        var session = await Repos.PlayerRepo.GetPlayer(hostToken);
        var lobby = await Repos.LobbyRepo.GetLobbyAsync(lobbyName);
        if (session != null && lobby != null)
        {
            if (lobby.Host.key == session.player.key)
            {
                var address = await Repos.ConAddRepo.GetPair(lobby.ConnectionIdentifier);
                if (address != null)
                {
                    await _hubOperations.BroadcastLobbyHostname(address.IPAddress, lobby);
                }
            }
        }
        
        
    }
    public async Task LeaveLobby(string lobbyName)
    {
        Console.WriteLine(lobbyName);
        await _hubOperations.LeaveLobby(lobbyName, Context);
    }
    public async Task SendLobby(string connId, Lobby lobby)
    {
        await _hubOperations.SendLobby(connId, lobby, Clients,Context);
    }

    public async Task SessionUpdate(string sessionId)
    {
        await _hubOperations.SessionUpdate(sessionId, Context.ConnectionId);
    }
}
