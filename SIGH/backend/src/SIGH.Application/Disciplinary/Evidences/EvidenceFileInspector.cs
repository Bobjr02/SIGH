using System.IO.Compression;
using System.Text;
using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.Evidences;

/// <summary>Tipo de arquivo reconhecido pela assinatura (magic bytes).</summary>
public sealed record DetectedFileType(string Extension, string ContentType, EvidenceType Kind, string Description);

/// <summary>
/// Validação de arquivos de evidência pela ASSINATURA do conteúdo. A extensão do nome original só
/// precisa estar na lista e ser coerente com o que o conteúdo é; o Content-Type enviado pelo cliente
/// é ignorado (o tipo gravado é o detectado).
/// </summary>
public static class EvidenceFileInspector
{
    private static readonly DetectedFileType Pdf = new("pdf", "application/pdf", EvidenceType.Document, "PDF");
    private static readonly DetectedFileType Jpeg = new("jpg", "image/jpeg", EvidenceType.Image, "JPEG");
    private static readonly DetectedFileType Png = new("png", "image/png", EvidenceType.Image, "PNG");
    private static readonly DetectedFileType Webp = new("webp", "image/webp", EvidenceType.Image, "WebP");
    private static readonly DetectedFileType Mp4 = new("mp4", "video/mp4", EvidenceType.Video, "MP4");
    private static readonly DetectedFileType M4a = new("m4a", "audio/mp4", EvidenceType.Audio, "M4A");
    private static readonly DetectedFileType Mp3 = new("mp3", "audio/mpeg", EvidenceType.Audio, "MP3");
    private static readonly DetectedFileType Docx = new("docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", EvidenceType.Document, "DOCX");
    private static readonly DetectedFileType Xlsx = new("xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", EvidenceType.Document, "XLSX");

    /// <summary>Extensões aceitas e a extensão canônica de cada uma.</summary>
    public static readonly IReadOnlyDictionary<string, string> AllowedExtensions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["pdf"] = "pdf",
        ["jpg"] = "jpg",
        ["jpeg"] = "jpg",
        ["png"] = "png",
        ["webp"] = "webp",
        ["mp4"] = "mp4",
        ["mp3"] = "mp3",
        ["m4a"] = "m4a",
        ["docx"] = "docx",
        ["xlsx"] = "xlsx",
    };

    // Brands ISO-BMFF ("ftyp") aceitos como vídeo MP4. QuickTime (.mov, brand "qt  ") e outros ficam de fora.
    private static readonly HashSet<string> Mp4VideoBrands = new(StringComparer.Ordinal)
    {
        "isom", "iso2", "iso3", "iso4", "iso5", "iso6", "mp41", "mp42", "avc1", "dash", "M4V ", "MSNV"
    };

    private static readonly HashSet<string> M4aBrands = new(StringComparer.Ordinal) { "M4A ", "M4B " };

    private const int HeaderLength = 16;

    /// <summary>
    /// Identifica o tipo pelo conteúdo. O stream precisa ser seekable; a posição volta ao início.
    /// Devolve null para conteúdo não reconhecido (inclusive executáveis, ZIPs genéricos etc.).
    /// </summary>
    public static DetectedFileType? Detect(Stream content)
    {
        if (!content.CanSeek)
            throw new ArgumentException("O stream precisa permitir seek.", nameof(content));

        content.Position = 0;
        var header = new byte[HeaderLength];
        var read = ReadAtLeast(content, header);
        content.Position = 0;

        if (read < 4)
            return null;

        var h = header.AsSpan(0, read);

        if (StartsWith(h, "%PDF-"u8)) return Pdf;
        if (h.Length >= 3 && h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF) return Jpeg;
        if (StartsWith(h, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A])) return Png;
        if (h.Length >= 12 && StartsWith(h, "RIFF"u8) && h.Slice(8, 4).SequenceEqual("WEBP"u8)) return Webp;

        if (h.Length >= 12 && h.Slice(4, 4).SequenceEqual("ftyp"u8))
        {
            var brand = Encoding.ASCII.GetString(h.Slice(8, 4));
            if (M4aBrands.Contains(brand)) return M4a;
            if (Mp4VideoBrands.Contains(brand)) return Mp4;
            return null;
        }

        if (StartsWith(h, "ID3"u8)) return Mp3;
        // Sincronismo de frame MPEG de áudio (11 bits em 1), layer III.
        if (h[0] == 0xFF && (h[1] & 0xE0) == 0xE0 && (h[1] & 0x06) == 0x02) return Mp3;

        if (StartsWith(h, [0x50, 0x4B, 0x03, 0x04]))
            return DetectOfficeOpenXml(content);

        return null;
    }

    /// <summary>
    /// Valida o arquivo: extensão permitida, conteúdo reconhecido e coerente com a extensão.
    /// Devolve o tipo detectado ou a mensagem de erro.
    /// </summary>
    public static (DetectedFileType? Type, string? Error) Validate(Stream content, string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName).TrimStart('.');
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.TryGetValue(extension, out var canonical))
        {
            return (null, $"Formato não permitido. Envie um destes: {string.Join(", ", AllowedExtensions.Keys)}.");
        }

        var detected = Detect(content);
        if (detected is null)
        {
            return (null, "O conteúdo do arquivo não corresponde a nenhum formato permitido.");
        }

        if (!string.Equals(detected.Extension, canonical, StringComparison.Ordinal))
        {
            return (null, $"O conteúdo do arquivo ({detected.Description}) não corresponde à extensão .{extension.ToLowerInvariant()}.");
        }

        return (detected, null);
    }

    private static DetectedFileType? DetectOfficeOpenXml(Stream content)
    {
        try
        {
            using var zip = new ZipArchive(content, ZipArchiveMode.Read, leaveOpen: true);
            var names = zip.Entries.Select(e => e.FullName).ToHashSet(StringComparer.Ordinal);

            if (!names.Contains("[Content_Types].xml"))
                return null;
            if (names.Contains("word/document.xml"))
                return Docx;
            if (names.Contains("xl/workbook.xml"))
                return Xlsx;
            return null;
        }
        catch (InvalidDataException)
        {
            return null;
        }
        finally
        {
            content.Position = 0;
        }
    }

    private static bool StartsWith(ReadOnlySpan<byte> data, ReadOnlySpan<byte> prefix) =>
        data.Length >= prefix.Length && data[..prefix.Length].SequenceEqual(prefix);

    private static int ReadAtLeast(Stream stream, byte[] buffer)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var n = stream.Read(buffer, total, buffer.Length - total);
            if (n == 0) break;
            total += n;
        }
        return total;
    }
}
