namespace SIGH.Application.Options;

/// <summary>Armazenamento de arquivos de evidência (seção "Storage").</summary>
public class StorageOptions
{
    public const string SectionName = "Storage";

    public const long DefaultMaxUploadSizeBytes = 25L * 1024 * 1024;

    /// <summary>Tamanho máximo de um arquivo enviado. Também limita o corpo da requisição (Kestrel e FormOptions).</summary>
    public long MaxUploadSizeBytes { get; set; } = DefaultMaxUploadSizeBytes;

    public LocalStorageOptions Local { get; set; } = new();
}

public class LocalStorageOptions
{
    /// <summary>
    /// Raiz dos arquivos. Deve ficar FORA do diretório da aplicação e de qualquer pasta servida
    /// publicamente (a API valida isso no startup). Caminho relativo é resolvido a partir do ContentRoot.
    /// </summary>
    public string RootPath { get; set; } = string.Empty;
}
