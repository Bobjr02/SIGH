using System.IO.Compression;
using System.Text;
using FluentAssertions;
using SIGH.Application.Disciplinary.Evidences;
using SIGH.Domain.Disciplinary.Enums;
using Xunit;

namespace SIGH.UnitTests.Evidences;

public class EvidenceFileInspectorTests
{
    private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.7\n%âãÏÓ\n1 0 obj<<>>endobj\n%%EOF");
    private static readonly byte[] Exe = [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00, 0xB8];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52];
    private static readonly byte[] Webp = Concat("RIFF"u8.ToArray(), [0x24, 0x00, 0x00, 0x00], "WEBPVP8 "u8.ToArray());
    private static readonly byte[] Mp4 = Concat([0x00, 0x00, 0x00, 0x20], "ftypisom"u8.ToArray(), [0x00, 0x00, 0x02, 0x00]);
    private static readonly byte[] M4a = Concat([0x00, 0x00, 0x00, 0x20], "ftypM4A "u8.ToArray(), [0x00, 0x00, 0x00, 0x00]);
    private static readonly byte[] QuickTime = Concat([0x00, 0x00, 0x00, 0x14], "ftypqt  "u8.ToArray(), [0x00, 0x00, 0x00, 0x00]);
    private static readonly byte[] Mp3Id3 = Concat("ID3"u8.ToArray(), [0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);
    private static readonly byte[] Mp3Frame = [0xFF, 0xFB, 0x90, 0x64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];

    [Theory]
    [InlineData("contrato.pdf")]
    [InlineData("CONTRATO.PDF")]
    public void Pdf_IsAccepted(string fileName)
    {
        var (type, error) = Validate(Pdf, fileName);

        error.Should().BeNull();
        type!.ContentType.Should().Be("application/pdf");
        type.Kind.Should().Be(EvidenceType.Document);
    }

    [Fact]
    public void ExeRenamedToPdf_IsRejected()
    {
        var (type, error) = Validate(Exe, "contrato.pdf");

        type.Should().BeNull();
        error.Should().Contain("não corresponde a nenhum formato permitido");
    }

    [Fact]
    public void Exe_IsRejectedByExtension()
    {
        Validate(Exe, "setup.exe").Type.Should().BeNull();
    }

    [Fact]
    public void PngRenamedToPdf_IsRejectedBecauseContentAndExtensionDisagree()
    {
        var (type, error) = Validate(Png, "foto.pdf");

        type.Should().BeNull();
        error.Should().Contain("PNG").And.Contain(".pdf");
    }

    [Fact]
    public void PlainTextRenamedToPng_IsRejected()
    {
        Validate(Encoding.UTF8.GetBytes("isto é só texto, não imagem"), "imagem.png").Type.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(AllowedSamples))]
    public void AllowedFormats_AreRecognizedByTheirSignature(byte[] content, string fileName, string expectedContentType, EvidenceType expectedKind)
    {
        var (type, error) = Validate(content, fileName);

        error.Should().BeNull();
        type!.ContentType.Should().Be(expectedContentType);
        type.Kind.Should().Be(expectedKind);
    }

    public static IEnumerable<object[]> AllowedSamples() =>
    [
        [Jpeg, "foto.jpg", "image/jpeg", EvidenceType.Image],
        [Jpeg, "foto.jpeg", "image/jpeg", EvidenceType.Image],
        [Png, "print.png", "image/png", EvidenceType.Image],
        [Webp, "print.webp", "image/webp", EvidenceType.Image],
        [Mp4, "video.mp4", "video/mp4", EvidenceType.Video],
        [M4a, "audio.m4a", "audio/mp4", EvidenceType.Audio],
        [Mp3Id3, "audio.mp3", "audio/mpeg", EvidenceType.Audio],
        [Mp3Frame, "audio.mp3", "audio/mpeg", EvidenceType.Audio],
        [OfficeZip("word/document.xml"), "ata.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", EvidenceType.Document],
        [OfficeZip("xl/workbook.xml"), "planilha.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", EvidenceType.Document],
    ];

    [Fact]
    public void GenericZipRenamedToDocx_IsRejected()
    {
        var zip = Zip(("leia-me.txt", "conteúdo"), ("payload.exe", "MZ"));

        Validate(zip, "documento.docx").Type.Should().BeNull();
    }

    [Fact]
    public void XlsxRenamedToDocx_IsRejected()
    {
        Validate(OfficeZip("xl/workbook.xml"), "documento.docx").Type.Should().BeNull();
    }

    [Fact]
    public void M4aRenamedToMp4_IsRejected()
    {
        Validate(M4a, "video.mp4").Type.Should().BeNull();
    }

    [Fact]
    public void QuickTimeMov_IsRejectedEvenWithMp4Extension()
    {
        Validate(QuickTime, "video.mp4").Type.Should().BeNull();
    }

    [Fact]
    public void EmptyOrTinyFile_IsRejected()
    {
        Validate([], "vazio.pdf").Type.Should().BeNull();
        Validate([0x25, 0x50], "curto.pdf").Type.Should().BeNull();
    }

    [Fact]
    public void Detect_LeavesTheStreamAtTheBeginning()
    {
        using var stream = new MemoryStream(OfficeZip("word/document.xml"));

        EvidenceFileInspector.Detect(stream);

        stream.Position.Should().Be(0);
    }

    private static (DetectedFileType? Type, string? Error) Validate(byte[] content, string fileName)
    {
        using var stream = new MemoryStream(content);
        return EvidenceFileInspector.Validate(stream, fileName);
    }

    private static byte[] OfficeZip(string mainPart) =>
        Zip(("[Content_Types].xml", "<Types/>"), (mainPart, "<root/>"));

    private static byte[] Zip(params (string Name, string Content)[] entries)
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                using var writer = new StreamWriter(zip.CreateEntry(name).Open());
                writer.Write(content);
            }
        }
        return buffer.ToArray();
    }

    private static byte[] Concat(params byte[][] parts) => parts.SelectMany(p => p).ToArray();
}

public class EvidenceTypeCompatibilityTests
{
    private static DetectedFileType Detect(byte[] content, string name)
    {
        using var stream = new MemoryStream(content);
        return EvidenceFileInspector.Validate(stream, name).Type!;
    }

    private static readonly byte[] Pdf = System.Text.Encoding.ASCII.GetBytes("%PDF-1.7\n%%EOF");
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52];
    private static readonly byte[] Mp4 = [0x00, 0x00, 0x00, 0x20, .. "ftypisom"u8.ToArray(), 0x00, 0x00, 0x02, 0x00];

    [Theory]
    [InlineData(EvidenceType.Document, "pdf", true)]
    [InlineData(EvidenceType.Document, "jpg", true)]
    [InlineData(EvidenceType.Document, "png", true)]
    [InlineData(EvidenceType.Document, "mp4", false)]
    [InlineData(EvidenceType.Image, "jpg", true)]
    [InlineData(EvidenceType.Image, "pdf", false)]
    [InlineData(EvidenceType.Video, "mp4", true)]
    [InlineData(EvidenceType.Video, "jpg", false)]
    [InlineData(EvidenceType.Other, "pdf", true)]
    [InlineData(EvidenceType.Other, "mp4", true)]
    [InlineData(EvidenceType.Text, "pdf", false)]
    [InlineData(EvidenceType.Text, "jpg", false)]
    public void IsAllowedFor_FollowsTheEvidenceTypeRules(EvidenceType evidenceType, string sample, bool expected)
    {
        var detected = sample switch
        {
            "pdf" => Detect(Pdf, "a.pdf"),
            "jpg" => Detect(Jpeg, "a.jpg"),
            "png" => Detect(Png, "a.png"),
            _ => Detect(Mp4, "a.mp4"),
        };

        EvidenceFileInspector.IsAllowedFor(evidenceType, detected).Should().Be(expected);
    }
}
