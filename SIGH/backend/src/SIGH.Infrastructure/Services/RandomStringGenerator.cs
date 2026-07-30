using System.Security.Cryptography;
using System.Text;
using SIGH.Application.Interfaces;

namespace SIGH.Infrastructure.Services;

public class RandomStringGenerator : IRandomStringGenerator
{
    private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public string GenerateRandomString(int length = 32)
    {
        var result = new StringBuilder(length);
        var buffer = new byte[sizeof(uint)];

        using var rng = RandomNumberGenerator.Create();

        while (result.Length < length)
        {
            rng.GetBytes(buffer);
            var num = BitConverter.ToUInt32(buffer, 0);
            result.Append(Chars[(int)(num % (uint)Chars.Length)]);
        }

        return result.ToString();
    }
}
