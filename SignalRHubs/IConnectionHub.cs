using LobbyAPI.Models;
using LobbyAPI.Utilities;

namespace LobbyAPI.SignalRHubs;

public interface IConnectionHub
{
    Task SendLobbyJoin(Lobby lobby);
    Task SendToken(string sessionId);
    Task ConfirmRequest(ClientResponse response);
    Task UpdateDetails(Session session);
}