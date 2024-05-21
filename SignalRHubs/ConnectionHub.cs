using System.Collections.Concurrent;
using LobbyAPI.Interfaces;
using LobbyAPI.Models;
using LobbyAPI.Repositories;
using LobbyAPI.Utilities;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace LobbyAPI.SignalRHubs;

/// <summary>
/// We use one entry hub since managing multiple connection ids is not optimal.
/// </summary>
public class ConnectionHub : Hub, IConnectionHub
{
    private readonly IHubOperations _hubOperations;
    private Repositories.Repositories Repos;
    private static ConcurrentDictionary<string, int> connectionAttempts = new ConcurrentDictionary<string, int>();

    public ConnectionHub(IHubOperations hubOperations)
    {
        _hubOperations = hubOperations;
        Repos = _hubOperations.RepoManager;
    }

    #region Connection Tasks


    public async Task Connect(string sessionId)
    {
        // This method might need implementation based on specific requirements.
    }

    public override async Task OnConnectedAsync()
    {
        var sessionId = Context.GetHttpContext().Request.Query["sessionId"];
        var session = _hubOperations.HubHandler.Queue.FirstOrDefault(s => s.SessionId == sessionId);
        if (session == null)
        {
            Console.WriteLine($"Not queued, player exists");
        }

        // Initialize or reset the connection attempts for this session.
        connectionAttempts[sessionId] = 0;

        await TryConnection(sessionId);
        await base.OnConnectedAsync();
    }

    async Task TryConnection(string sessionId)
    {
        await _hubOperations.TryConnection(sessionId, Clients, Context);
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var sessionId = Context.GetHttpContext().Request.Query["sessionId"];
        // Clean up the connection attempts for this session.
        connectionAttempts.TryRemove(sessionId, out _);

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendToken(string sessionId)
    {
        await _hubOperations.SendToken(Context.ConnectionId, sessionId);
    }
    public async Task UpdateDetails(Session session)
    {
        await _hubOperations.UpdateDetails(session);
    }

    #endregion


    #region Generic Tasks

    /// <summary>
    /// Confirm the completion/success of a request
    /// </summary>
    /// <param name="response"></param>
    public async Task ConfirmRequest(ClientResponse response)
    {
        await _hubOperations.ConfirmRequest(response);
    }
    /// <summary>
    /// Send a string
    /// </summary>
    /// <param name="sessionId"></param>
    /// <param name="message"></param>
    public async Task SendMessage(string sessionId, string message)
    {
        var user = await Repos.SessionRepo.Get(sessionId);
        await _hubOperations.SendMessage(user.ConnectionID, message);
    }

    #endregion

    #region Session Tasks

    public void QueueSession(string token, string sessionId)
    {
        Console.WriteLine($"Queue session {sessionId}:{token}");
        _hubOperations.HubHandler.Queue.Add(new SessionQueue
        {
            SessionId = sessionId,
            Token = token
        });
    }
    
    public async Task SessionUpdate(string sessionId)
    {
        await _hubOperations.SessionUpdate(sessionId, Context.ConnectionId);
    }

    public async Task InvokeRefresh(Lobby  lobby)
    {
        Console.WriteLine($"Refreshed {lobby.LobbyName}");
        await _hubOperations.LobbyService.RefreshLobby(lobby);
        
    }

    #endregion

    #region Lobby Tasks

    /// <summary>
    /// Grab list of current lobbies
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task GetLobbies()
    {
        await _hubOperations.GetLobbies(Context);
    }
    /// <summary>
    /// Callback for player lobby join
    /// </summary>
    /// <param name="player"></param>
    /// <param name="lobbyName"></param>
    public async Task LobbyJoined(Player player, string lobbyName)
    {
        await _hubOperations.LobbyJoined(player.connectionID, lobbyName);
    }
    /// <summary>
    /// Called when entering a lobby
    /// </summary>
    /// <param name="lobbyName"></param>
    public async Task EnterLobby(string lobbyName)
    {
        await _hubOperations.EnterLobby(lobbyName, Context);
    }
/// <summary>
/// Called when player leaves lobby
/// </summary>
/// <param name="lobby"></param>
/// <param name="token"></param>
    public async Task LeaveLobby(Lobby lobby, string token)
    {
        await _hubOperations.LeaveLobby(lobby, token, Context);
    }

/// <summary>
/// Called when host starts the Net-code for Game Objects connection
/// </summary>
/// <param name="lobbyName"></param>
/// <param name="hostToken"></param>
    public async Task BroadcastLobbyHostname(string lobbyName, string hostToken)
    {
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
/// <summary>
/// seemingly same as EnterLobby
/// Need to check and see if this is still used.
/// </summary>
/// <param name="connId"></param>
/// <param name="lobby"></param>
    public async Task SendLobby(string connId, Lobby lobby)
    {
        await _hubOperations.SendLobby(connId, lobby, Clients, Context);
    }

    #endregion

}
