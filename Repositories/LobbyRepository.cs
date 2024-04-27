using LobbyAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Raven.Client.Documents;

namespace LobbyAPI.Repositories;
using LobbyAPI.Interfaces;

public class LobbyRepository : ILobbyRepository
{

    private readonly IDocumentStore _store;
    private readonly IPasswordRepository _pwdRepo;
    private readonly IPlayerRepository _playerKVP;

    public LobbyRepository(IDocumentStore store, IPasswordRepository pwdRepo, IPlayerRepository playerKVP)
    {
        _store = store;
        _pwdRepo = pwdRepo;
        _playerKVP = playerKVP;
    }
    
    
    public async Task<Lobby> GetLobbyAsync(string name)
    {
        using (var session = _store.OpenAsyncSession())
        {
            return await session.LoadAsync<Lobby>(name);
        }
    }

    public async Task<List<Lobby>> GetLobbiesAsync()
    {
        using (var session = _store.OpenAsyncSession())
        {
            var lobbies = await session.Query<Lobby>().ToListAsync();
            return lobbies;
        }
       
    }

   

    public async Task<bool> CreateLobbyAsync(Lobby newLobby, string passkey = null)
    {
        using (var session = _store.OpenAsyncSession())
        {
            var existingLobby = await session.LoadAsync<Lobby>(newLobby.LobbyName);
            if (existingLobby == null)
            {
                newLobby.Players = new List<Player>();
                newLobby.Players.Add(newLobby.Host);
                newLobby.Locked = false;
                if (!string.IsNullOrEmpty(passkey))
                {
                    newLobby.Locked = true;
                    await _pwdRepo.SetPassword(newLobby.LobbyName, passkey);
                }
                await session.StoreAsync(newLobby, newLobby.LobbyName);
                await session.SaveChangesAsync();
                return true;
            }
        }

        return false;

    }

    public async Task<bool> UpdateLobbyAsync(Lobby newLobby)
    {
        using (var session = _store.OpenAsyncSession())
        {
            Lobby oldLobby = await session.LoadAsync<Lobby>(newLobby.LobbyName);
            if (oldLobby != null)
            {
                oldLobby.Host = newLobby.Host;
                oldLobby.LobbyName = newLobby.LobbyName;
                oldLobby.Locked = newLobby.Locked;
                oldLobby.Players = newLobby.Players;
                
                await session.SaveChangesAsync();
                return true;
            }
        }
        return false;
    }
    public async Task<bool> DeleteLobbyAsync(string lobbyName)
    {
        using (var session = _store.OpenAsyncSession())
        {
            var lobby = await session.LoadAsync<Lobby>(lobbyName);
            if (lobby != null)
            {
                session.Delete(lobby);
                await session.SaveChangesAsync();
                return true;
            }
        }
        return false;
    }

}