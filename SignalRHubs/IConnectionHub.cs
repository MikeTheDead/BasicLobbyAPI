using LobbyAPI.Models;
using LobbyAPI.Utilities;

namespace LobbyAPI.SignalRHubs;

public interface IConnectionHub
{
    
    //Task SendLobbyJoin(Lobby lobby);

    #region Connection

    Task SendToken(string sessionId);
    Task ConfirmRequest(ClientResponse response);
    Task InvokeRefresh(Lobby  lobby);
    Task UpdateDetails(Session session);
    Task SessionUpdate(string sessionId);

        #endregion

    #region Lobbies

    Task GetLobbies();
    Task SendLobby(string connId, Lobby lobby);
    Task EnterLobby(string connId);
    Task BroadcastLobbyHostname(string lobbyConnID, string hostToken);
    Task LeaveLobby(Lobby lobby, string token);

    #endregion

    #region Netcode for Gameobjects

    Task NGO_ClientConnected(string clientId);
    Task NGO_ClientDisconnected(string clientId);
    #endregion
        
    
}