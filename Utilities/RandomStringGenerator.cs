namespace LobbyAPI.Utilities;

using System;

public class RandomStringGenerator
{
    private static Random random = new Random();

    public static string GenerateRandomString(int length = 5)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        char[] stringChars = new char[length];
        for (int i = 0; i < length; i++)
        {
            stringChars[i] = chars[random.Next(chars.Length)];
        }
        return new string(stringChars);
    }
}
