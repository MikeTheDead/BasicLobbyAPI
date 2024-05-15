using LobbyAPI.Interfaces;
using LobbyAPI.Models;
using LobbyAPI.Utilities;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace LobbyAPI.SignalRHubs
{
    public class LobbyHub : Hub, ILobbyHub
    {
        private readonly ISessionRepository _sessionRepository;
        private readonly ConnectionHub connHub;
        
        public LobbyHub(ISessionRepository sessionRepository, ConnectionHub _connHub)
        {
            _sessionRepository = sessionRepository;
            connHub = _connHub;
        }
        
        public async Task LobbyJoin(Lobby lobby, string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                Console.WriteLine("LobbyJoin: sessionId is null or empty");
                return;
            }

            var session = await _sessionRepository.Get(sessionId);
            if (session == null)
            {
                Console.WriteLine($"LobbyJoin: No session found for sessionId {sessionId}");
                return;
            }

            var connId = session.ConnectionID;
            if (string.IsNullOrEmpty(connId))
            {
                Console.WriteLine($"LobbyJoin: ConnectionID is null or empty for sessionId {sessionId}");
                return;
            }

            Console.WriteLine($"Sending Lobby Join to {session.player.playerName} with connection Id {connId}");
            await connHub.SendLobby(connId,lobby);
            //await Clients.Client(connId).SendAsync("EnterLobby", lobby);
        }
    }
}