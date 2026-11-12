using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using SIGH.Application.Disciplinary.Evidences;
using SIGH.Application.Options;
using SIGH.Infrastructure.Storage;
using Xunit;

namespace SIGH.UnitTests.Evidences;

public sealed class LocalFileStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "sigh-unit-storage", Guid.NewGuid().ToString("N"));
    private readonly string _contentRoot = Path.Combine(Path.GetTempPath(), "sigh-unit-app", Guid.NewGuid().ToString("N"));

    private LocalFileStorage CreateStorage(string? root = null)
    {
        var env = new Mock<IHostEnvironment>();
        env.SetupGet(e => e.ContentRootPath).Returns(_contentRoot);
        return new LocalFileStorage(Options.Create(new StorageOptions { Local = new LocalStorageOptions { RootPath = root ?? _root } }), env.Object);
    }

    [Fact]
    public async Task Save_UsesGuidPlusExtension_AndRoundTrips()
    {
        var storage = CreateStorage();
        var content = Encoding.UTF8.GetBytes("%PDF-1.4 conteúdo");

        var key = await storage.SaveAsync(new MemoryStream(content), "pdf");

        key.Should().MatchRegex("^[0-9a-f]{32}\\.pdf$");
        (await storage.ExistsAsync(key)).Should().BeTrue();
        await using var read = await storage.OpenReadAsync(key);
        using var copy = new MemoryStream();
        await read.CopyToAsync(copy);
        copy.ToArray().Should().Equal(content);
    }

    [Fact]
    public async Task Save_NeverReusesAKey()
    {
        var storage = CreateStorage();

        var first = await storage.SaveAsync(new MemoryStream([1]), "pdf");
        var second = await storage.SaveAsync(new MemoryStream([2]), "pdf");

        second.Should().NotBe(first);
        Directory.GetFiles(_root).Should().HaveCount(2);
    }

    [Theory]
    [InlineData("../segredo.pdf")]
    [InlineData("..\\..\\windows\\system32.pdf")]
    [InlineData("/etc/passwd")]
    [InlineData("subdir/0123456789abcdef0123456789abcdef.pdf")]
    [InlineData("0123456789abcdef0123456789abcdef.pdf/../../x")]
    [InlineData("0123456789ABCDEF0123456789ABCDEF.pdf")]
    [InlineData("0123456789abcdef0123456789abcdef")]
    [InlineData("")]
    [InlineData("%2e%2e%2fetc%2fpasswd")]
    public async Task PathTraversalAndMalformedKeys_AreRejected(string key)
    {
        var storage = CreateStorage();

        storage.Invoking(s => s.ResolvePath(key)).Should().Throw<ArgumentException>();
        await storage.Invoking(s => s.OpenReadAsync(key)).Should().ThrowAsync<ArgumentException>();
        await storage.Invoking(s => s.ExistsAsync(key)).Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("../pdf")]
    [InlineData("p/df")]
    [InlineData("")]
    [InlineData("toolongextension")]
    public async Task Save_RejectsInvalidExtensions(string extension)
    {
        var storage = CreateStorage();

        await storage.Invoking(s => s.SaveAsync(new MemoryStream([1]), extension)).Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void Root_InsideTheApplicationDirectory_IsRejected()
    {
        var inside = Path.Combine(_contentRoot, "wwwroot", "uploads");

        FluentActions.Invoking(() => CreateStorage(inside)).Should().Throw<InvalidOperationException>().WithMessage("*diretório da aplicação*");
        FluentActions.Invoking(() => CreateStorage(Path.Combine(AppContext.BaseDirectory, "uploads"))).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Root_NotConfigured_IsRejected()
    {
        FluentActions.Invoking(() => CreateStorage(" ")).Should().Throw<InvalidOperationException>().WithMessage("*Storage:Local:RootPath*");
    }

    [Theory]
    [InlineData("../../etc/passwd.pdf", "pdf", "passwd.pdf")]
    [InlineData("C:\\Users\\joao\\Desktop\\laudo.pdf", "pdf", "laudo.pdf")]
    [InlineData("foto\u0000\u0007.JPEG", "jpg", "foto__.jpg")]
    [InlineData("a<b>c:d|e?f*.png", "png", "a_b_c_d_e_f_.png")]
    [InlineData("....pdf", "pdf", "arquivo.pdf")]
    [InlineData("", "pdf", "arquivo.pdf")]
    public void OriginalFileName_IsSanitized(string original, string extension, string expected)
    {
        EvidenceFileName.Sanitize(original, extension).Should().Be(expected);
    }

    [Fact]
    public void OriginalFileName_IsTruncatedKeepingTheExtension()
    {
        var sanitized = EvidenceFileName.Sanitize(new string('a', 400) + ".pdf", "pdf");

        sanitized.Length.Should().Be(255);
        sanitized.Should().EndWith(".pdf");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}
