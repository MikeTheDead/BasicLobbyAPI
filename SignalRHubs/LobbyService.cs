using LobbyAPI.Interfaces;
using LobbyAPI.Models;
using LobbyAPI.Utilities;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace LobbyAPI.SignalRHubs
{
    public interface ILobbyService
    {
        Task EnterLobby(string connectionId, Lobby lobby);
        Task LeaveLobby(string connectionId, Lobby lobby);
        Task EndLobby(Lobby lobby);
        Task RefreshLobby(Lobby lobby);
    }

    public class LobbyService : ILobbyService
    {
        private readonly IHubContext<ConnectionHub> _connectionHubContext;
        private readonly ILobbyRepository lobbyRepo;

        public LobbyService(IHubContext<ConnectionHub> connectionHubContext, ILobbyRepository _lobbyRepo)
        {
            _connectionHubContext = connectionHubContext;
            lobbyRepo = _lobbyRepo;
        }

        public async Task EnterLobby(string connectionId, Lobby lobby)
        {
            await _connectionHubContext.Groups.AddToGroupAsync(connectionId, lobby.LobbyName);
            Console.WriteLine($"Added {connectionId} to {lobby.LobbyName} group");
            await _connectionHubContext.Clients.Client(connectionId).SendAsync("EnterLobby", lobby);
            Console.WriteLine("sending lobby refresh");
            await RefreshLobby(lobby);
        }

        public async Task EndLobby(Lobby lobby)
        {
            await _connectionHubContext.Clients.Group(lobby.LobbyName).SendAsync("LobbyLeft", lobby);
        }

        public async Task RefreshLobby(Lobby lobby)
        {
            await _connectionHubContext.Clients.Group(lobby.LobbyName).SendAsync("RefreshLobby",lobby);
        }
        public async Task LeaveLobby(string connectionId, Lobby lobby)
        {
            await _connectionHubContext.Groups.RemoveFromGroupAsync(connectionId, lobby.LobbyName);
            Console.WriteLine($"Removed {connectionId} from {lobby.LobbyName} group");
            //await _connectionHubContext.Clients.Client(connectionId).SendAsync("LobbyLeft", lobby);
            await RefreshLobby(lobby);
        }
    }

}