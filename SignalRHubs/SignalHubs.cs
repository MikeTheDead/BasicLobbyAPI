namespace LobbyAPI.SignalRHubs;

public class SignalHubs
{

    public readonly ConnectionHub ConnectionHub;
    public readonly HeartbeatHub HeartbeatHub;
    public readonly LobbyHub LobbyHub;
    
    
    public SignalHubs(ConnectionHub _connectionHub, HeartbeatHub _heartbeatHub, LobbyHub _lobbyHub)
    {
        ConnectionHub = _connectionHub;
        HeartbeatHub = _heartbeatHub;
        LobbyHub = _lobbyHub;
    }
}