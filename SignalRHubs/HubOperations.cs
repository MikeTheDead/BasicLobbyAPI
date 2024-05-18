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
    Task TryConnection(string sessionId, IHubCallerClients Clients, HubCallerContext Context);
    Task LobbyJoined(string connectionId, string lobbyName);
    Task EnterLobby(string lobbyName, HubCallerContext Context);
    Task BroadcastLobbyHostname(string address, Lobby lobby);
    Task LeaveLobby(string lobbyName, HubCallerContext Context);
    Task SendToken(string connectionId, string sessionId);
    Task ConfirmRequest(ClientResponse response);
    Task UpdateDetails(Session session);
    Task SendMessage(string connectionId, string message);
    Task SendLobby(string connectionId, Lobby lobby, IHubCallerClients Clients, HubCallerContext Context);
    Task SessionUpdate(string sessionId, string connId);
    Task EnsureRequest(string method, string sessionId, Func<Task> onSucceeded, Func<Task> onTimeout, int timeoutSeconds = 1);
}

public class HubOperations : IHubOperations
{
    private readonly Repositories.Repositories Repos;
    private readonly HubHandlerService _hubHandlerService;
    private readonly IHubContext<ConnectionHub> _connectionHubContext;
    private readonly ILobbyService _lobbyService;

    public HubOperations( 
        HubHandlerService hubHandlerService, IHubContext<ConnectionHub> connectionHubContext,Repositories.Repositories _repo, ILobbyService lobbyService)
    {
        _lobbyService = lobbyService;
        Repos = _repo;
        Repos.hubOps = this;
        _hubHandlerService = hubHandlerService;
        _connectionHubContext = connectionHubContext;
    }

    #region Exposed Properties

    public Repositories.Repositories RepoManager => Repos;

    public HubHandlerService HubHandler => _hubHandlerService;
    public ILobbyService LobbyService => _lobbyService;

    #endregion



    #region Player op

    public async Task SessionUpdate(string sessionId, string connectionId)
    {
        Console.WriteLine($"{sessionId} Session Update at {connectionId}");
        var session = await Repos.SessionRepo.SetConnectionID(sessionId, connectionId);
        string json = JsonConvert.SerializeObject(session);
        await _connectionHubContext.Clients.Client(connectionId).SendAsync("UpdateSession", json);
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
    public async Task TryConnection(string sessionId, IHubCallerClients Clients, HubCallerContext Context)
    {
        var tries = 0;
        if (tries <= 5)
        {
            tries++;
            Console.WriteLine("con = "+Context.ConnectionId);
            await Clients.Client(Context.ConnectionId).SendAsync("Connected", "You are connected.");
            await EnsureRequest("Connected", sessionId,
                async () => await SendMessage(sessionId, "Ping"),
                async () => await TryConnection(sessionId,Clients,Context), 1);
        }
        else
        {
            Console.WriteLine("TryConnection called too many times");
        }
    }
    

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

    #region LobbyOps
    public async Task EnterLobby(string lobbyName, HubCallerContext Context)
    {
        var lobby = await Repos.LobbyRepo.GetLobbyAsync(lobbyName);
        if (lobby != null)
        {
            var sessionId = Context.GetHttpContext().Request.Query["sessionId"];
            var session = await Repos.SessionRepo.Get(sessionId);
            if (session != null)
            {
                await _lobbyService.EnterLobby(Context.ConnectionId, lobby);
            }
        }
        
    }
    public async Task BroadcastLobbyHostname(string address, Lobby lobby)
    {
        await _connectionHubContext.Clients.Group(lobby.LobbyName).SendAsync("ConnectToHost", address);

    }

    public async Task LeaveLobby(string lobbyName,HubCallerContext Context)
    {
        var sessionId = Context.GetHttpContext().Request.Query["sessionId"];
        var session = await Repos.SessionRepo.Get(sessionId);
        if (session != null)
        {
            var lobby = new Lobby { LobbyName = lobbyName, Host = session.player }; // Assuming you construct Lobby this way
            await _lobbyService.LeaveLobby(Context.ConnectionId, lobby);
        }
    }
    public async Task LobbyJoined(string connectionId, string lobbyName)
    {
        //await _connectionHubContext.Groups.AddToGroupAsync(connectionId, lobbyName);
    }

    public async Task SendLobby(string connectionId, Lobby lobby,IHubCallerClients Clients, HubCallerContext Context)
    {
        await _lobbyService.EnterLobby(connectionId, lobby);
        Console.WriteLine($"SendLobby @ {connectionId}"); 
        //await Clients.Client(connectionId).SendAsync("EnterLobby", lobby);
    }

    #endregion

    #region General Ops

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
}

