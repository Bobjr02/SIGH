using System.Text;
using SIGH.Domain.Disciplinary.Constants;

namespace SIGH.Application.Disciplinary.Evidences;

/// <summary>
/// Higieniza o nome original do arquivo para exibição e para o Content-Disposition do download.
/// Ele nunca é usado como caminho no disco (o nome físico é GUID + extensão).
/// </summary>
public static class EvidenceFileName
{
    private static readonly HashSet<char> Forbidden = new("<>:\"/\\|?*");

    public static string Sanitize(string? originalFileName, string canonicalExtension)
    {
        // Descarta qualquer diretório (inclusive no estilo Windows) que o cliente tenha mandado.
        var name = (originalFileName ?? string.Empty).Replace('\\', '/');
        name = name[(name.LastIndexOf('/') + 1)..];

        var builder = new StringBuilder(name.Length);
        foreach (var ch in name.Normalize(NormalizationForm.FormC))
        {
            if (char.IsControl(ch) || Forbidden.Contains(ch))
                builder.Append('_');
            else
                builder.Append(ch);
        }

        // A extensão é sempre a canônica do tipo detectado; pontos nas pontas do nome são descartados.
        var stem = Path.GetFileNameWithoutExtension(builder.ToString().Trim()).Trim().Trim('.').Trim();
        if (string.IsNullOrWhiteSpace(stem) || stem.All(c => c == '_'))
            stem = "arquivo";

        var suffix = "." + canonicalExtension;
        var maxStem = DisciplinaryDomainConstants.OriginalFileNameMaxLength - suffix.Length;
        if (stem.Length > maxStem)
            stem = stem[..maxStem];

        return stem + suffix;
    }
}
