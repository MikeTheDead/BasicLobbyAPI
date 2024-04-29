using System.Security.Cryptography;
using System.Text;
using LobbyAPI.Interfaces;
using LobbyAPI.Models;
using LobbyAPI.MongoCollectionControllers.Interface;

namespace LobbyAPI.Repositories;

public class PasswordRepository : IPasswordRepository
{
    private readonly IMongoController<Password> passwordMongoController;

    public PasswordRepository(IMongoController<Password> _passwordMongoController)
    {
        passwordMongoController = _passwordMongoController;
    }
    
    
    public async Task<bool> ValidPassword(string lobbyName, string password)
    {
        var passwordEntry = await passwordMongoController.Get(lobbyName);
        if (passwordEntry == null)
        {
            return false;
            
        }
        string inputHash = HashPassword(password, passwordEntry.Salt);
        return inputHash == passwordEntry.Hash; // Compare the hashes
    }

    public async Task SetPassword(string lobbyName, string password)
    {
        // Generate salt
        byte[] salt = new byte[16];
        using (var rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(salt);
        }

        // Generate hash with the salt
        string hash = HashPassword(password, salt);

        // Store the hashed password and salt
        var passwordEntry = new Password(lobbyName, hash, salt);
        await passwordMongoController.Set(passwordEntry);
    }

    public static string HashPassword(string input, byte[] salt)
    {
        using (var pbkdf2 = new Rfc2898DeriveBytes(input, salt, 10000))
        {
            byte[] hash = pbkdf2.GetBytes(20);
            return Convert.ToBase64String(hash);
        }
    }

}