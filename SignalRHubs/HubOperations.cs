using System.Collections.Concurrent;
using LobbyAPI.Interfaces;
using LobbyAPI.Models;
using LobbyAPI.Utilities;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace LobbyAPI.SignalRHubs;

public interface IHubOperations
{
    ILobbyService LobbyService { get; }
    Repositories.Repositories RepoManager { get; }
    HubHandlerService HubHandler { get; }
    

    #region Connection
    Task TryConnection(string sessionId, IHubCallerClients Clients, HubCallerContext Context);
    Task SendToken(string connectionId, string sessionId);
    Task UpdateDetails(Session session);
    Task SessionUpdate(string sessionId, string connId);
        #endregion
    #region Lobby

    Task GetLobbies(HubCallerContext Context);
    Task SendLobby(string connectionId, Lobby lobby, IHubCallerClients Clients, HubCallerContext Context);
    Task LobbyJoined(string connectionId, string lobbyName);
    Task EnterLobby(string _connId, HubCallerContext Context);
    Task BroadcastLobbyHostname(string lobbyConnID, string hostToken);
    Task LeaveLobby(Lobby _lobby, string token, HubCallerContext Context);
    #endregion

    #region Netcode for Gameobjects

    Task NGO_ClientConnected(string clientId, HubCallerContext ctx);
        
    Task NGO_ClientDisconnected(string clientId, HubCallerContext ctx);
        

    #endregion
    #region General

    Task SendMessage(string connectionId, string message);
    Task ConfirmRequest(ClientResponse response);
    Task EnsureRequest(string method, string sessionId, Func<Task> onSucceeded, Func<Task> onTimeout, int timeoutSeconds = 1);
    

    #endregion
}

public class HubOperations : IHubOperations
{
    private readonly Repositories.Repositories Repos;
    private readonly HubHandlerService _hubHandlerService;
    private readonly IHubContext<ConnectionHub> _connectionHubContext;
    private readonly ILobbyService _lobbyService;
    private static ConcurrentDictionary<string, int> connectionAttempts = new ConcurrentDictionary<string, int>();

    public HubOperations(
        HubHandlerService hubHandlerService, IHubContext<ConnectionHub> connectionHubContext, Repositories.Repositories repo, ILobbyService lobbyService)
    {
        _lobbyService = lobbyService;
        Repos = repo;
        Repos.hubOps = this;
        _hubHandlerService = hubHandlerService;
        _connectionHubContext = connectionHubContext;
        _lobbyService.HubContext = connectionHubContext;
    }

    public Repositories.Repositories RepoManager => Repos;
    public HubHandlerService HubHandler => _hubHandlerService;
    public ILobbyService LobbyService => _lobbyService;

    #region Connection Operations

    public async Task TryConnection(string sessionId, IHubCallerClients Clients, HubCallerContext Context)
    {
        if (connectionAttempts.TryGetValue(sessionId, out int tries) && tries >= 5)
        {
            Console.WriteLine("TryConnection called too many times");
            return;
        }

        tries = connectionAttempts.AddOrUpdate(sessionId, 1, (key, oldValue) => oldValue + 1);
        Console.WriteLine($"con = {Context.ConnectionId}, tries = {tries}");
        
        await Clients.Client(Context.ConnectionId).SendAsync("Connected", "You are connected.");
        await EnsureRequest("Connected", sessionId,
            async () => await SendMessage(Context.ConnectionId, "Ping"),
            async () => await TryConnection(sessionId, Clients, Context), 1);
    }

    public async Task SendToken(string connectionId, string sessionId)
    {
        var session = _hubHandlerService.Queue.FirstOrDefault(s => s.SessionId == sessionId);
        if (session != null)
        {
            await _connectionHubContext.Clients.Client(connectionId).SendAsync("Token", session.Token);
        }
        else
        {
            await _connectionHubContext.Clients.Client(connectionId).SendAsync("Error", "Session not found.");
        }
    }

    #endregion
    
    #region General Operations

    public async Task ConfirmRequest(ClientResponse response)
    {
        if (!await _hubHandlerService.TryExecuteConfirmation(response))
        {
            Console.WriteLine("Confirmation failed or no action found.");
        }
        var clientResponses = _hubHandlerService.RequestConfirmations.Where(r => r.Key.SessionId == response.SessionId);
        if (clientResponses.Any())
        {
            var responseEntry = clientResponses.FirstOrDefault(r => r.Key.Method == response.Method);
            if (_hubHandlerService.RequestConfirmations.TryGetValue(responseEntry.Key, out Func<Task> action))
            {
                await action();
            }
        }
    }

    public async Task UpdateDetails(Session session)
    {
        await Repos.SessionRepo.UpdatePlayerName(session);
        await _connectionHubContext.Clients.Client(session.ConnectionID).SendAsync("RefreshAccount", session.player);
    }

    #endregion
    
    #region Session Operations

    public async Task SessionUpdate(string sessionId, string connectionId)
    {
        Console.WriteLine($"{sessionId} Session Update at {connectionId}");
        var session = await Repos.SessionRepo.SetConnectionID(sessionId, connectionId);
        string json = JsonConvert.SerializeObject(session);
        await _connectionHubContext.Clients.Client(connectionId).SendAsync("UpdateSession", json);
    }


    public async Task SendMessage(string connectionId, string message)
    {
        await _connectionHubContext.Clients.Client(connectionId).SendAsync("ReceiveMessage", message);
    }

    public async Task EnsureRequest(string method, string sessionId, Func<Task> onSucceeded, Func<Task> onTimeout, int timeoutSeconds = 1)
    {
        var clientResponse = new ClientResponse { Method = method, SessionId = sessionId };
        var cts = new CancellationTokenSource();

        if (_hubHandlerService.RequestConfirmations.TryAdd(clientResponse, async () =>
            {
                if (!cts.IsCancellationRequested)
                {
                    cts.Cancel();  // Cancel the timeout task
                    await onSucceeded();
                }
            }))
        {
            // Schedule the timeout task
            Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), cts.Token)
                .ContinueWith(async t =>
                {
                    if (!t.IsCanceled)
                    {
                        _hubHandlerService.RequestConfirmations.TryRemove(clientResponse, out _);
                        await onTimeout();
                    }
                }, TaskScheduler.Default);
        }
        else
        {
            _hubHandlerService.RequestConfirmations[clientResponse] = async () =>
            {
                if (!cts.IsCancellationRequested)
                {
                    cts.Cancel();  // Cancel the timeout task
                    await onSucceeded();
                }
            };
        }
    }
    

    #endregion
    
    #region Lobby Operations

    public async Task GetLobbies(HubCallerContext Context)
    {
        //get
        List<Lobby> lobbies = new List<Lobby>();
        lobbies = await Repos.LobbyRepo.GetLobbiesAsync();
        //convert
        string json = JsonConvert.SerializeObject(lobbies);
        
        //send
        await _connectionHubContext.Clients.Client(Context.ConnectionId).SendAsync("LobbyList",json);
    }
    public async Task EnterLobby(string _connId, HubCallerContext Context)
    {
        var lobby = await Repos.LobbyRepo.GetLobbyAsync(_connId);
        if (lobby == null)
        {
            return;
        }
        var sessionId = Context.GetHttpContext().Request.Query["sessionId"];
        var session = await Repos.SessionRepo.Get(sessionId);
        if (session == null)
        {
            return;
        }
        await _lobbyService.EnterLobby(Context.ConnectionId, lobby);
    }

    public async Task BroadcastLobbyHostname(string lobbyConnID, string hostToken)
    {
        Console.WriteLine("Host submitted a connection request");
        var session = await Repos.PlayerRepo.GetPlayer(hostToken);
        if (session == null)
        {
            Console.WriteLine("Token invalid!");
            return;
        }
        var lobby = await Repos.LobbyRepo.GetLobbyAsync(lobbyConnID);
        if (lobby == null)
        {
            Console.WriteLine("Lobby invalid!");
            return;
        }
        
        Console.WriteLine("Verify host and lobby");
        if (lobby.Host.key != session.player.key)
        {
            Console.WriteLine("Not the host!");
            return;
        }
        
        Console.WriteLine("Verified! Broadcast address");
        var address = await Repos.ConAddRepo.GetPair(lobby.ConnectionIdentifier);
        if (string.IsNullOrEmpty(address.IPAddress))
        {
            Console.WriteLine("Host address invalid!");
            return;
        }
        await _connectionHubContext.Clients.Group(lobby.LobbyName).SendAsync("ConnectToHost", address.IPAddress);
    }


    public async Task LeaveLobby(Lobby _lobby,string token, HubCallerContext Context)
    {
        Console.WriteLine("Lobby leave requested. Verifying authenticity");
        Session? VerifiedSession = await RepoManager.PlayerRepo.GetPlayer(token);
        if (VerifiedSession == null)
        {
            Console.WriteLine($"Player not found. {Context.ConnectionId} is bad actor or critical error has occurred");
            return;
        }
        VerifiedSession.ConnectionID = Context.ConnectionId;
        Lobby? VerifiedLobby = await RepoManager.LobbyRepo.GetLobbyAsync(_lobby.ConnectionIdentifier);
        
        if (VerifiedLobby == null)
        {
            Console.WriteLine($"Lobby not found. {Context.ConnectionId} is bad actor or critical error has occurred");
            return;
        }

        Player? lobbyPlayer = VerifiedLobby.Players.FirstOrDefault(p => p.key == VerifiedSession.player.key);
        if (lobbyPlayer == null)
        {
            Console.WriteLine($"{Context.ConnectionId} is not in this lobby ({VerifiedSession.ConnectionID})");
            return;
        }
        Console.WriteLine($"Authentication complete, removing {VerifiedSession.player.playerName} from {VerifiedLobby.LobbyName}");
        VerifiedLobby.Players.Remove(lobbyPlayer);
        if (VerifiedLobby.Host.key == lobbyPlayer.key)
        {
            Console.WriteLine($"Player is Host, check for other players");
            Player? transfer = VerifiedLobby.Players.FirstOrDefault();
            if (transfer != null)
            {
                Console.WriteLine($"Lobby is not empty. {transfer.playerName} is the new host");
                VerifiedLobby.Host = transfer;
            }
            else
            {
                Console.WriteLine($"No one else in lobby, deleting.");
                await RepoManager.LobbyRepo.DeleteLobbyAsync(VerifiedLobby.ConnectionIdentifier);
                
                await LobbyService.HandleLobbyLeave(VerifiedLobby, VerifiedSession);
                return;
            }
        }
        await RepoManager.LobbyRepo.UpdateLobbyAsync(VerifiedLobby);
        await LobbyService.HandleLobbyLeave(VerifiedLobby, VerifiedSession);
    }
    public async Task LobbyJoined(string connectionId, string lobbyName)
    {
        await _connectionHubContext.Groups.AddToGroupAsync(connectionId, lobbyName);
    }

    public async Task SendLobby(string connectionId, Lobby lobby, IHubCallerClients Clients, HubCallerContext Context)
    {
        await _lobbyService.EnterLobby(connectionId, lobby);
        Console.WriteLine($"SendLobby @ {connectionId}");
        // await Clients.Client(connectionId).SendAsync("EnterLobby", lobby);
    }

    #endregion

    #region Netcode for Gameobjects

    
    public async Task NGO_ClientConnected(string clientId, HubCallerContext ctx)
    {
        Session session = await Repos.SessionRepo.GetCID(ctx.ConnectionId);
        if (string.IsNullOrEmpty(session.SessionID))
        {
            return;
        }

        session.NGOClientID = clientId;

        await Repos.SessionRepo.UpdateCid(session);
        var lobby = await Repos.LobbyRepo.GetPlayersLobby(ctx.ConnectionId);
        if (lobby == null)
        {
            Console.WriteLine("Lobby error");
            return;
        }

        await _connectionHubContext.Clients.Group(lobby.LobbyName).SendAsync("NGO_ClientConnectedCallback", session.player);

    }
    
    public async Task NGO_ClientDisconnected(string clientId, HubCallerContext ctx)
    {
        
    }

    #endregion
}


