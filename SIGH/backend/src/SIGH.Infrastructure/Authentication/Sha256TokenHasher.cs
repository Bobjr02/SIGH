using System.Security.Cryptography;
using System.Text;
using SIGH.Application.Interfaces;

namespace SIGH.Infrastructure.Authentication;

public class Sha256TokenHasher : ITokenHasher
{
    public string HashToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return string.Empty;

        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
