namespace SIGH.Application.Interfaces.Storage;

/// <summary>
/// Armazenamento de arquivos binários (evidências). Abstração pensada para trocar o disco local
/// por S3 ou Azure Blob sem tocar no domínio: o domínio só conhece a chave (string) devolvida aqui.
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Grava o conteúdo sob uma chave NOVA, sempre "GUID + extensão" (o nome original nunca vira
    /// caminho). Nunca sobrescreve um arquivo existente. Devolve a chave.
    /// </summary>
    Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken = default);

    /// <summary>Abre o arquivo para leitura em stream. Lança <see cref="FileNotFoundException"/> se não existir.</summary>
    Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}
