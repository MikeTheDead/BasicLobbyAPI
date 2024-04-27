public class Password
{
    public string LobbyName { get; set; }
    public string Hash { get; set; } // This should store the hashed password, not plaintext
    public byte[] Salt { get; set; } // This stores the salt used to generate the hash

    public Password(string lobbyName, string hash, byte[] salt)
    {
        LobbyName = lobbyName;
        Hash = hash;
        Salt = salt;
    }
}