using SIGH.Application.Interfaces;

namespace SIGH.Infrastructure.Storage;

public class LocalStorageService : IStorageService
{
    public Task<string> SaveFileAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        // Implementação mínima zerada
        return Task.FromResult(string.Empty);
    }

    public Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
