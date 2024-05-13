using LobbyAPI.Interfaces;
using LobbyAPI.Models;
using LobbyAPI.Repositories;
using LobbyAPI.Utilities;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;

namespace LobbyAPI.SignalRHubs;

/// <summary>
/// Signal hub that handles login and 
/// </summary>
public class ConnectionHub : Hub, IConnectionHub
{
    private readonly HubHandlerService _hubHandlerService;
    private readonly ISessionRepository _sessionRepository;
    private int tries = 0;
    public ConnectionHub(HubHandlerService hubHandlerService, ISessionRepository sessionRepository)
    {
        _hubHandlerService = hubHandlerService;
        _sessionRepository = sessionRepository;
    }

    
    
    
    public async Task LobbyJoined(Player player,string lobbyName)
    {
        await Groups.AddToGroupAsync(player.connectionID, lobbyName);
        Console.WriteLine("");
    }

    public async Task Connect(string sessionId)
    {
       
    }
    
    public override async Task OnConnectedAsync()
    {
        tries = 0;
        var sessionId = Context.GetHttpContext().Request.Query["sessionId"];
        var session = _hubHandlerService.Queue.FirstOrDefault(s => s.SessionId == sessionId);
        if (session == null)
        {
            Console.WriteLine($"Not queued, player exists");
        }
        
        await _sessionRepository.SetConnectionID(sessionId, Context.ConnectionId);
        await TryConnection(sessionId);
        await base.OnConnectedAsync();
    }

    async Task TryConnection(string sessionId)
    {
        if (tries <= 5)
        {
            tries++;
            await Clients.Client(Context.ConnectionId).SendAsync("Connected", "You are connected.");
            await EnsureRequest("Connected", sessionId, 
                async () => await SendMessage(sessionId, "Ping"), 
                async () => await TryConnection(sessionId),
                1);
        }
        else
        {
            Console.WriteLine("TryConnection called too many times");
        }
        
    }
    
    
    public async Task SendToken(string sessionId)
    {
        Console.WriteLine($"SendToken:{sessionId}");
        try
        {
            var session = _hubHandlerService.Queue.FirstOrDefault(s => s.SessionId == sessionId);
            if (session != null)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("Token", session.Token);
            }
            else
            {
                Console.WriteLine($"No session found for ID: {sessionId}");
                await Clients.Client(Context.ConnectionId).SendAsync("Error", "Session not found.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in SendToken: {ex.Message}");
            await Clients.Client(Context.ConnectionId).SendAsync("Error", "An error occurred on the server.");
        }
    }

    public async Task ConfirmRequest(ClientResponse response)
    {
        if (!await _hubHandlerService.TryExecuteConfirmation(response))
        {
            Console.WriteLine("Confirmation failed or no action found.");
        }
        var clientResponses =
            _hubHandlerService.RequestConfirmations.Where(r => r.Key.SessionId == response.SessionId);
        if (clientResponses.Any())
        {
            var Response = clientResponses.FirstOrDefault(r
                => r.Key.Method == response.Method);
            if (_hubHandlerService.RequestConfirmations.TryGetValue(Response.Key, out Func<Task> action))
            {
                await action();
            }
            else
            {
                Console.WriteLine("Response not found.");
            }
        }
        else
        {
            
            Console.WriteLine("player has no responses");
        }
    }
    
    public async Task UpdateDetails(Session session)
    {
        await _sessionRepository.UpdatePlayerName(session);
        await Clients.Client(Context.ConnectionId).SendAsync("RefreshAccount", session.player);
    }

    public async Task SendMessage(string sessionId, string message)
    {
        Console.WriteLine($"SendMessage {sessionId}");
        Session user = await _sessionRepository.Get(sessionId);
        
        await Clients.Client(user.ConnectionID).SendAsync("ReceiveMessage", message);
    }




    public void QueueSession(string token,string sessionId)
    {
        Console.WriteLine($"Queue session {sessionId}:{token}");
        _hubHandlerService.Queue.Add(new SessionQueue
        {
            SessionId = sessionId,
            Token = token
        });
    }

    public async Task EnsureRequest(string method, string sessionId, Func<Task> onSucceeded, Func<Task> onTimeout, int timeoutSeconds = 1)
    {
        Console.WriteLine("EnsureRequest");
        var clientResponse = new ClientResponse { Method = method, SessionId = sessionId };
        var cts = new CancellationTokenSource();

        if (!_hubHandlerService.RequestConfirmations.ContainsKey(clientResponse))
        {
            _hubHandlerService.RequestConfirmations.Add(clientResponse, async () =>
            {
                if (!cts.IsCancellationRequested)
                {
                    cts.Cancel();  // Cancel the timeout task
                    await onSucceeded();
                }
            });

            // Schedule the timeout task
            Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), cts.Token)
                .ContinueWith(async t =>
                {
                    if (!t.IsCanceled)
                    {
                        _hubHandlerService.RequestConfirmations.Remove(clientResponse);
                        await onTimeout();
                    }
                }, TaskScheduler.Default);
        }
        else
        {
            // Handle the case where the key already exists
            _hubHandlerService.RequestConfirmations[clientResponse] = async () =>
            {
                if (!cts.IsCancellationRequested)
                {
                    cts.Cancel();  // Cancel the timeout task
                    await onSucceeded();
                }
            };
            Console.WriteLine("Warning: Overwriting an existing request confirmation action.");
        }
    }


    
    
    public Task SendLobbyJoin(Lobby lobby)
    {
        throw new NotImplementedException();
    }

}