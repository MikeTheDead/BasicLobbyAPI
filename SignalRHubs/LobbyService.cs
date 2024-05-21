using LobbyAPI.Interfaces;
using LobbyAPI.Models;
using LobbyAPI.Utilities;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace LobbyAPI.SignalRHubs
{
    public interface ILobbyService
    {
        IHubContext<ConnectionHub> HubContext { get; set; }
        Task EnterLobby(string connectionId, Lobby lobby);
        Task HandleLobbyLeave(Lobby VerifiedLobby, Session VerifiedSession);
        Task RefreshLobby(Lobby  lobby);
    }

    public class LobbyService : ILobbyService
    {
        private IHubContext<ConnectionHub> _connectionHubContext;
        private readonly ILobbyRepository lobbyRepo;

        public LobbyService( ILobbyRepository _lobbyRepo)
        {
            lobbyRepo = _lobbyRepo;
        }

        public IHubContext<ConnectionHub> HubContext
        {
            get => _connectionHubContext;
            set
            {
                _connectionHubContext = value;
            }
        }


        public async Task EnterLobby(string connectionId, Lobby lobby)
        {
            await _connectionHubContext.Groups.AddToGroupAsync(connectionId, lobby.LobbyName);
            Console.WriteLine($"Added {connectionId} to {lobby.LobbyName} group");
            await _connectionHubContext.Clients.Client(connectionId).SendAsync("EnterLobby", lobby);
            Console.WriteLine("sending lobby refresh");
            await RefreshLobby(lobby);
        }
        
        public async Task HandleLobbyLeave(Lobby VerifiedLobby, Session VerifiedSession)
        {
            
            Console.WriteLine($"Send {VerifiedSession.player.playerName} the 'LobbyLeft' callback");
            await _connectionHubContext.Clients.Client(VerifiedSession.ConnectionID).SendAsync("LobbyLeft");
            Console.WriteLine($"Remove {VerifiedSession.player.playerName} from the signalR group.");
            await _connectionHubContext.Groups.RemoveFromGroupAsync(VerifiedSession.SessionID, VerifiedLobby.LobbyName);
            Console.WriteLine($"Finally send everyone else the refresh callback.");
            await _connectionHubContext.Clients.Group(VerifiedLobby.LobbyName).SendAsync("RefreshLobby");
        }

        public async Task EndLobby(Lobby lobby)
        {
            await _connectionHubContext.Clients.Group(lobby.LobbyName).SendAsync("LobbyLeft", lobby);
        }

        public async Task RefreshLobby(Lobby  lobby)
        {
            Lobby lobbyData = await lobbyRepo.GetLobbyAsync(lobby.ConnectionIdentifier);
            await _connectionHubContext.Clients.Group(lobby.LobbyName).SendAsync("RefreshLobby",lobbyData);
        }
        public async Task LeaveLobby(string connectionId, Lobby lobby)
        {
            await _connectionHubContext.Groups.RemoveFromGroupAsync(connectionId, lobby.LobbyName);
            Console.WriteLine($"Removed {connectionId} from {lobby.LobbyName} group");
            await _connectionHubContext.Clients.Client(connectionId).SendAsync("LobbyLeft", lobby);
            await RefreshLobby(lobby);
        }
    }

}