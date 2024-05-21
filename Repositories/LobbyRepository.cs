using LobbyAPI.Models;
using LobbyAPI.MongoCollectionControllers;
using LobbyAPI.MongoCollectionControllers.Interface;
using Microsoft.AspNetCore.Mvc;

namespace LobbyAPI.Repositories;
using LobbyAPI.Interfaces;

public class LobbyRepository : ILobbyRepository
{
    private readonly IMongoController<Lobby> lobbyMongoController;
    private readonly IPasswordRepository passwordRepository;

    public LobbyRepository(IMongoController<Lobby> lobby, IPasswordRepository _passwordRepository)
    {
        lobbyMongoController = lobby;
        passwordRepository = _passwordRepository;
    }


    public Repositories repo { get; set; }

    public async Task<Lobby> GetLobbyAsync(string connectionIdentifier)
    {
        return await lobbyMongoController.Get(connectionIdentifier);
    }

    public async Task<List<Lobby>> GetLobbiesAsync()
    {
        return await lobbyMongoController.GetAll();

    }

   

    public async Task<bool> CreateLobbyAsync(Lobby newLobby, string passkey = null)
    {
        Console.WriteLine("CreateLobbyAsync");
        Lobby? existingLobby = await lobbyMongoController.Get(newLobby.ConnectionIdentifier);
        if (existingLobby == null)
        {
            newLobby.Players = new List<Player>();
            newLobby.Players.Add(newLobby.Host);
            newLobby.Locked = false;
            if (!string.IsNullOrEmpty(passkey))
            {
                newLobby.Locked = true;
                await passwordRepository.SetPassword(newLobby.LobbyName, passkey);
            }

            await lobbyMongoController.Set(newLobby);
            return true;
        }

        return false;

    }

    public async Task<bool> UpdateLobbyAsync(Lobby newLobby)
    {
        Lobby? oldLobby = await lobbyMongoController.Get(newLobby.ConnectionIdentifier);
        if (oldLobby != null)
        {
            Console.WriteLine($"found {oldLobby.LobbyName} : {oldLobby.ConnectionIdentifier}");
            await lobbyMongoController.Put(newLobby);
            await repo.hubOps.LobbyService.RefreshLobby(newLobby);
            return true;
        }
        return false;
    }
    public async Task<bool> DeleteLobbyAsync(string lobbyID)
    {
        var lobby = await lobbyMongoController.Get(lobbyID);
        if (lobby != null)
        {
            await lobbyMongoController.Remove(lobby);
            return true;
        }
        return false;
    }

}