namespace SIGH.Application.Interfaces;

public interface ITokenHasher
{
    string HashToken(string token);
}
