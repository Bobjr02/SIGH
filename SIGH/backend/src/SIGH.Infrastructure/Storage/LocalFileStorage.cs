using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SIGH.Application.Interfaces.Storage;
using SIGH.Application.Options;

namespace SIGH.Infrastructure.Storage;

/// <summary>
/// Arquivos em disco local, sob Storage:Local:RootPath. Nome físico = GUID ("N") + extensão;
/// o nome original nunca vira caminho. Nenhum arquivo é sobrescrito (FileMode.CreateNew).
/// </summary>
public sealed partial class LocalFileStorage : IFileStorage
{
    // Chave: 32 hex minúsculos + "." + extensão alfanumérica curta. Qualquer outra coisa
    // (barras, "..", caminhos absolutos, unicode) é recusada antes de tocar no disco.
    [GeneratedRegex("^[0-9a-f]{32}\\.[a-z0-9]{1,8}$", RegexOptions.CultureInvariant)]
    private static partial Regex KeyPattern();

    [GeneratedRegex("^[a-z0-9]{1,8}$", RegexOptions.CultureInvariant)]
    private static partial Regex ExtensionPattern();

    private readonly string _root;

    public LocalFileStorage(IOptions<StorageOptions> options, IHostEnvironment environment)
    {
        _root = ResolveRoot(options.Value.Local.RootPath, environment.ContentRootPath);
        Directory.CreateDirectory(_root);
    }

    public string RootPath => _root;

    /// <summary>
    /// Resolve a raiz e garante que ela fica fora do diretório da aplicação (binários e ContentRoot),
    /// onde poderia ser servida ou sobrescrita por um deploy.
    /// </summary>
    public static string ResolveRoot(string configuredRoot, string contentRootPath)
    {
        if (string.IsNullOrWhiteSpace(configuredRoot))
            throw new InvalidOperationException(
                "Storage:Local:RootPath não configurado. Defina um diretório fora da aplicação " +
                "(ex.: variável de ambiente Storage__Local__RootPath=/var/lib/sigh/evidences).");

        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(configuredRoot, contentRootPath));

        foreach (var appDir in new[] { contentRootPath, AppContext.BaseDirectory })
        {
            var full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(appDir));
            if (IsSameOrInside(root, full))
                throw new InvalidOperationException(
                    $"Storage:Local:RootPath ({root}) não pode ficar dentro do diretório da aplicação ({full}).");
        }

        return root;
    }

    public async Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken = default)
    {
        var ext = (extension ?? string.Empty).TrimStart('.').ToLowerInvariant();
        if (!ExtensionPattern().IsMatch(ext))
            throw new ArgumentException("Extensão inválida.", nameof(extension));

        var key = $"{Guid.NewGuid():N}.{ext}";
        var path = ResolvePath(key);

        try
        {
            await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            await content.CopyToAsync(file, cancellationToken);
            await file.FlushAsync(cancellationToken);
        }
        catch
        {
            // Gravação parcial não pode virar evidência: remove o que tiver sido escrito.
            TryDelete(path);
            throw;
        }

        return key;
    }

    public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(key);
        if (!File.Exists(path))
            throw new FileNotFoundException("Arquivo não encontrado no storage.", key);

        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Task.FromResult(stream);
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(File.Exists(ResolvePath(key)));

    /// <summary>Converte a chave em caminho, recusando qualquer tentativa de sair da raiz.</summary>
    public string ResolvePath(string key)
    {
        if (string.IsNullOrEmpty(key) || !KeyPattern().IsMatch(key))
            throw new ArgumentException("Chave de arquivo inválida.", nameof(key));

        var path = Path.GetFullPath(Path.Combine(_root, key));
        if (!string.Equals(Path.GetDirectoryName(path), _root, StringComparison.Ordinal))
            throw new ArgumentException("Chave de arquivo inválida.", nameof(key));

        return path;
    }

    private static bool IsSameOrInside(string path, string directory) =>
        string.Equals(path, directory, StringComparison.Ordinal)
        || path.StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.Ordinal);

    private static void TryDelete(string path)
    {
        try { File.Delete(path); } catch { /* melhor esforço */ }
    }
}
