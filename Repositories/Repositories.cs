using LobbyAPI.Interfaces;
using LobbyAPI.SignalRHubs;

namespace LobbyAPI.Repositories
{
    public class Repositories
    {

        public ISessionRepository SessionRepo { get; }
        public IPlayerRepository PlayerRepo { get; }
        public ILobbyRepository LobbyRepo { get; }
        public IConnectionAddressRepository ConAddRepo { get; }

        public Repositories(ISessionRepository sessionRepo, IPlayerRepository playerRepo, ILobbyRepository lobbyRepo, IConnectionAddressRepository connAddRepo)
        {
            SessionRepo = sessionRepo;
            PlayerRepo = playerRepo;
            LobbyRepo = lobbyRepo;
            ConAddRepo = connAddRepo;
            
            //send this to repos
            lobbyRepo.repo = this;
        }
        public HubOperations hubOps { get; set; }
    }
}