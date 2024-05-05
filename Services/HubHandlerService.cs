using LobbyAPI.SignalRHubs;

namespace LobbyAPI.Utilities;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;

public class HubHandlerService
{
    public List<SessionQueue> Queue { get; } = new List<SessionQueue>();
    public Dictionary<ClientResponse, Func<Task>> RequestConfirmations = new Dictionary<ClientResponse, Func<Task>>(new ClientResponseComparer());



    public void QueueSession(string token, string sessionId)
    {
        Queue.Add(new SessionQueue
        {
            SessionId = sessionId,
            Token = token
        });
    }

    public void AddConfirmationRequest(ClientResponse response, Func<Task> onSuccess)
    {
        RequestConfirmations.Add(response, onSuccess);
    }

    public async Task<bool> TryExecuteConfirmation(ClientResponse response)
    {
        
        Console.WriteLine(response.SessionId +" "+response.Method);
        if (RequestConfirmations.TryGetValue(response, out Func<Task> action))
        {
            await action();
            return true;
        }
        return false;
    }
}



public class SessionQueue
{
    public string Token { get; set; }
    public string SessionId { get; set; }
    public string ConnectionId { get; set; }
    
}

public class ClientResponse
{
    public string SessionId { get; set; }
    public string Method { get; set; }
    public Dictionary<string, Object>? AdditionalArgs = new();
}
public class ClientResponseComparer : IEqualityComparer<ClientResponse>
{
    public bool Equals(ClientResponse x, ClientResponse y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x == null || y == null) return false;
        return x.SessionId == y.SessionId && x.Method == y.Method;
    }

    public int GetHashCode(ClientResponse obj)
    {
        unchecked 
        {
            int hash = 17;
            hash = hash * 23 + (obj.SessionId?.GetHashCode() ?? 0);
            hash = hash * 23 + (obj.Method?.GetHashCode() ?? 0);
            return hash;
        }
    }
}


