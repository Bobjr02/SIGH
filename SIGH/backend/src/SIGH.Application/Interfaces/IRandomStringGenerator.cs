namespace SIGH.Application.Interfaces;

public interface IRandomStringGenerator
{
    string GenerateRandomString(int length = 32);
}
