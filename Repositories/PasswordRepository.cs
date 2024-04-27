using System.Security.Cryptography;
using System.Text;
using LobbyAPI.Interfaces;
using LobbyAPI.Models;
using Raven.Client.Documents;

namespace LobbyAPI.Repositories;

public class PasswordRepository : IPasswordRepository
{
    private readonly IDocumentStore _passwordStore;
    
    public PasswordRepository(IDocumentStore passwordStore)
    {
        _passwordStore = passwordStore;
    }
    
    
    public async Task<bool> ValidPassword(string lobbyName, string password)
    {
        using (var session = _passwordStore.OpenAsyncSession())
        {
            var passwordEntry = await session.LoadAsync<Password>($"Password/{lobbyName}");
            if (passwordEntry == null)
            {
                return false; // No password entry found for the lobby.
            }

            // Re-hash the input password using the same salt and compare
            string inputHash = HashPassword(password, passwordEntry.Salt);
            return inputHash == passwordEntry.Hash; // Compare the hashes
        }
    }

    public async Task SetPassword(string lobbyName, string password)
    {
        using (var session = _passwordStore.OpenAsyncSession())
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
            await session.StoreAsync(passwordEntry, $"Password/{lobbyName}");
            await session.SaveChangesAsync();
        }
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