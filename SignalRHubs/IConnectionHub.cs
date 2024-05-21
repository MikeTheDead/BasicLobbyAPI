using LobbyAPI.Models;
using LobbyAPI.Utilities;

namespace LobbyAPI.SignalRHubs;

public interface IConnectionHub
{
    //Task SendLobbyJoin(Lobby lobby);
    Task SendToken(string sessionId);
    Task ConfirmRequest(ClientResponse response);
    Task UpdateDetails(Session session);
    Task SessionUpdate(string sessionId);
    Task GetLobbies();
    Task SendLobby(string connId, Lobby lobby);
    Task EnterLobby(string lobbyName);
    Task BroadcastLobbyHostname(string lobbyName, string hostToken);
    Task LeaveLobby(Lobby lobby, string token);
    Task InvokeRefresh(Lobby  lobby);
}