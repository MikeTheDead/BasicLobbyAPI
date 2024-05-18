using LobbyAPI.Models;
using Microsoft.AspNetCore.SignalR;

namespace LobbyAPI.SignalRHubs;

public class SignalHubs
{

    public IHubContext<ConnectionHub> ConnectionHubContext { get; }
    public IConnectionHub ConHub { get; }

    public SignalHubs(IHubContext<ConnectionHub> connectionHubContext,IConnectionHub conHub)
    {
        ConnectionHubContext = connectionHubContext;
        ConHub = conHub;
    }
    
    
    
}