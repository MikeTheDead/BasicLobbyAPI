namespace LobbyAPI.SignalRHubs;

public class SignalHubs
{

    public readonly ConnectionHub ConnectionHub;
    public readonly HeartbeatHub HeartbeatHub;
    
    
    public SignalHubs(ConnectionHub _connectionHub, HeartbeatHub _heartbeatHub)
    {
        ConnectionHub = _connectionHub;
        HeartbeatHub = _heartbeatHub;
    }
}