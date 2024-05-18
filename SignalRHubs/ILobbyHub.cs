using LobbyAPI.Models;

namespace LobbyAPI.SignalRHubs;

public interface ILobbyHub
{
    Task LobbyJoin(Lobby lobby, string sessionId);
}