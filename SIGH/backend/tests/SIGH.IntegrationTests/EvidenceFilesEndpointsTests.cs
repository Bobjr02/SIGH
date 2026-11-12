using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIGH.Api.Contracts.Disciplinary;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.DisciplinaryCases.AddEvidence;
using SIGH.Application.Disciplinary.DisciplinaryCases.CancelDisciplinaryCase;
using SIGH.Application.Disciplinary.DisciplinaryCases.CreateDisciplinaryCase;
using SIGH.Application.Disciplinary.DisciplinaryCases.GetDisciplinaryCaseById;
using SIGH.Application.Disciplinary.Evidences;
using SIGH.Application.Security;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.IntegrationTests.Infrastructure;
using SIGH.Persistence.Auditing;
using SIGH.Persistence.Context;
using Xunit;

namespace SIGH.IntegrationTests;

/// <summary>Upload, download e imutabilidade dos arquivos de evidência, contra SQL Server e disco reais.</summary>
public class EvidenceFilesEndpointsTests : IntegrationTestBase
{
    // PDF mínimo, mas real (abre em leitores de PDF).
    private static readonly byte[] PdfBytes = Encoding.ASCII.GetBytes(
        "%PDF-1.4\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n" +
        "3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 200 200]>>endobj\ntrailer<</Root 1 0 R>>\n%%EOF\n");

    // Cabeçalho de executável Windows (MZ ... PE).
    private static readonly byte[] ExeBytes = [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00, 0xB8, 0x00, 0x50, 0x45];

    public EvidenceFilesEndpointsTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task UploadPdf_ThenDownload_ReturnsSameBytesAndTheRecordedHashMatches()
    {
        using var admin = await CreateClientAsync("admin@sigh.com");
        var caseId = await CreateCaseAsync(admin);
        var evidenceId = await CreateFileEvidenceAsync(admin, caseId, EvidenceType.Document);

        var upload = await UploadAsync(admin, caseId, evidenceId, PdfBytes, "../../etc/Relatório final.pdf");
        upload.StatusCode.Should().Be(HttpStatusCode.OK);
        var uploaded = (await upload.Content.ReadFromJsonAsync<Result<EvidenceFileDto>>())!.Data!;

        var expectedHash = Convert.ToHexString(SHA256.HashData(PdfBytes)).ToLowerInvariant();
        uploaded.Sha256Hash.Should().Be(expectedHash, "o hash é calculado no servidor");
        uploaded.ContentType.Should().Be("application/pdf");
        uploaded.FileSize.Should().Be(PdfBytes.Length);
        uploaded.OriginalFileName.Should().Be("Relatório final.pdf", "o caminho enviado pelo cliente é descartado");
        uploaded.UploadedByUserId.Should().Be(CustomWebApplicationFactory.AdminUserId);

        // O nome físico é GUID + extensão; o nome original não vira caminho.
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
            var row = await db.DisciplinaryEvidences.AsNoTracking().SingleAsync(e => e.Id == evidenceId);
            row.StorageReference.Should().MatchRegex("^[0-9a-f]{32}\\.pdf$");
            File.Exists(Path.Combine(Factory.StorageRoot, row.StorageReference!)).Should().BeTrue();
        }

        var download = await admin.GetAsync($"/api/v1/disciplinary-cases/{caseId}/evidences/{evidenceId}/file");
        download.StatusCode.Should().Be(HttpStatusCode.OK);
        download.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        download.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
        download.Content.Headers.ContentDisposition.FileNameStar.Should().Be("Relatório final.pdf");
        download.Headers.GetValues("X-Content-Type-Options").Should().ContainSingle().Which.Should().Be("nosniff");

        var downloadedBytes = await download.Content.ReadAsByteArrayAsync();
        downloadedBytes.Should().Equal(PdfBytes);
        Convert.ToHexString(SHA256.HashData(downloadedBytes)).ToLowerInvariant().Should().Be(uploaded.Sha256Hash);

        var detail = await GetCaseAsync(admin, caseId);
        var evidence = detail.Evidences.Should().ContainSingle().Subject;
        evidence.HasFile.Should().BeTrue();
        evidence.IntegrityHash.Should().Be(expectedHash);
        evidence.FileUploadedByName.Should().Be("Administrador do Sistema");
    }

    [Fact]
    public async Task ExeRenamedToPdf_IsRejectedAndNothingIsStored()
    {
        using var admin = await CreateClientAsync("admin@sigh.com");
        var caseId = await CreateCaseAsync(admin);
        var evidenceId = await CreateFileEvidenceAsync(admin, caseId, EvidenceType.Document);
        var filesBefore = CountStoredFiles();

        var response = await UploadAsync(admin, caseId, evidenceId, ExeBytes, "contrato.pdf", "application/pdf");

        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
        CountStoredFiles().Should().Be(filesBefore);
        (await GetCaseAsync(admin, caseId)).Evidences.Single().HasFile.Should().BeFalse();
    }

    [Fact]
    public async Task UploadedFile_CannotBeReplaced()
    {
        using var admin = await CreateClientAsync("admin@sigh.com");
        var caseId = await CreateCaseAsync(admin);
        var evidenceId = await CreateFileEvidenceAsync(admin, caseId, EvidenceType.Document);
        (await UploadAsync(admin, caseId, evidenceId, PdfBytes, "original.pdf")).StatusCode.Should().Be(HttpStatusCode.OK);

        var other = Encoding.ASCII.GetBytes("%PDF-1.7\noutro conteudo\n%%EOF\n");
        var second = await UploadAsync(admin, caseId, evidenceId, other, "substituto.pdf");

        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var downloaded = await admin.GetByteArrayAsync($"/api/v1/disciplinary-cases/{caseId}/evidences/{evidenceId}/file");
        downloaded.Should().Equal(PdfBytes);
    }

    [Fact]
    public async Task FileLargerThanTheLimit_Returns413()
    {
        using var admin = await CreateClientAsync("admin@sigh.com");
        var caseId = await CreateCaseAsync(admin);
        var evidenceId = await CreateFileEvidenceAsync(admin, caseId, EvidenceType.Document);

        var big = new byte[25 * 1024 * 1024 + 1];
        PdfBytes.CopyTo(big, 0);

        var response = await UploadAsync(admin, caseId, evidenceId, big, "grande.pdf");

        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task CancelledCase_AcceptsNoNewEvidenceAndNoFile()
    {
        using var admin = await CreateClientAsync("admin@sigh.com");
        var caseId = await CreateCaseAsync(admin);
        var evidenceId = await CreateFileEvidenceAsync(admin, caseId, EvidenceType.Document);

        (await admin.PostAsJsonAsync($"/api/v1/disciplinary-cases/{caseId}/cancel",
            new CancelDisciplinaryCaseRequest(caseId, "Cancelado no teste de imutabilidade.")))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await admin.PostAsJsonAsync($"/api/v1/disciplinary-cases/{caseId}/evidences",
            new AddEvidenceHttpRequest(EvidenceType.Document, "Nova evidência", DateTimeOffset.UtcNow)))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);

        (await UploadAsync(admin, caseId, evidenceId, PdfBytes, "tarde-demais.pdf"))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);

        (await admin.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/disciplinary-cases/{caseId}/evidences/{evidenceId}")
        {
            Content = JsonContent.Create(new { reason = "Tentativa de remover em processo cancelado" })
        })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RemovingAnEvidence_IsLogicalRequiresAReasonAndRecordsWhoRemoved()
    {
        using var admin = await CreateClientAsync("admin@sigh.com");
        var caseId = await CreateCaseAsync(admin);
        var evidenceId = await CreateFileEvidenceAsync(admin, caseId, EvidenceType.Document);
        (await UploadAsync(admin, caseId, evidenceId, PdfBytes, "removivel.pdf")).StatusCode.Should().Be(HttpStatusCode.OK);

        (await DeleteAsync(admin, caseId, evidenceId, "  ")).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        (await DeleteAsync(admin, caseId, evidenceId, "Anexada ao processo errado")).StatusCode.Should().Be(HttpStatusCode.OK);

        (await GetCaseAsync(admin, caseId)).Evidences.Should().BeEmpty();
        (await admin.GetAsync($"/api/v1/disciplinary-cases/{caseId}/evidences/{evidenceId}/file")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
        var row = await db.DisciplinaryEvidences.IgnoreQueryFilters().AsNoTracking().SingleAsync(e => e.Id == evidenceId);
        row.IsDeleted.Should().BeTrue();
        row.RemovalReason.Should().Be("Anexada ao processo errado");
        row.DeletedBy.Should().Be(CustomWebApplicationFactory.AdminUserId);
        File.Exists(Path.Combine(Factory.StorageRoot, row.StorageReference!)).Should().BeTrue("o arquivo é preservado");

        (await db.AuditLogs.AnyAsync(a => a.Action == AuditActions.SoftDeleted && a.EntityId == evidenceId.ToString())).Should().BeTrue();
    }

    [Fact]
    public async Task UnprivilegedUser_CannotUploadDownloadOrRemove()
    {
        using var admin = await CreateClientAsync("admin@sigh.com");
        using var unprivileged = await CreateClientAsync("unprivileged@sigh.com");
        var caseId = await CreateCaseAsync(admin);
        var evidenceId = await CreateFileEvidenceAsync(admin, caseId, EvidenceType.Document);
        (await UploadAsync(admin, caseId, evidenceId, PdfBytes, "a.pdf")).StatusCode.Should().Be(HttpStatusCode.OK);

        (await UploadAsync(unprivileged, caseId, evidenceId, PdfBytes, "b.pdf")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await unprivileged.GetAsync($"/api/v1/disciplinary-cases/{caseId}/evidences/{evidenceId}/file")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await DeleteAsync(unprivileged, caseId, evidenceId, "sem permissão")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateCaseAttachAndDownload_ProduceTheExpectedAuditRecordsWithoutSensitiveData()
    {
        using var admin = await CreateClientAsync("admin@sigh.com");
        var caseId = await CreateCaseAsync(admin);
        var evidenceId = await CreateFileEvidenceAsync(admin, caseId, EvidenceType.Document);
        (await UploadAsync(admin, caseId, evidenceId, PdfBytes, "auditado.pdf")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await admin.GetAsync($"/api/v1/disciplinary-cases/{caseId}/evidences/{evidenceId}/file")).StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SighDbContext>();
        var logs = await db.AuditLogs.AsNoTracking().ToListAsync();
        var storageKey = (await db.DisciplinaryEvidences.AsNoTracking().SingleAsync(e => e.Id == evidenceId)).StorageReference!;

        logs.Should().Contain(a => a.Entity == "DisciplinaryCase" && a.EntityId == caseId.ToString() && a.Action == AuditActions.Created);
        logs.Should().Contain(a => a.Entity == "DisciplinaryEvidence" && a.EntityId == evidenceId.ToString() && a.Action == AuditActions.Created);
        var attach = logs.Should().ContainSingle(a => a.Entity == "DisciplinaryEvidence" && a.EntityId == evidenceId.ToString() && a.Action == AuditActions.Updated).Subject;
        attach.NewValues.Should().Contain(Convert.ToHexString(SHA256.HashData(PdfBytes)).ToLowerInvariant());
        var download = logs.Should().ContainSingle(a => a.Action == SecurityAuditActions.EvidenceDownloaded).Subject;
        download.EntityId.Should().Be(evidenceId.ToString());
        download.UserId.Should().Be(CustomWebApplicationFactory.AdminUserId);

        var everything = string.Join("\n", logs.Select(a => a.OldValues + a.NewValues));
        everything.Should().NotContain(storageKey, "o caminho físico do arquivo não vai para a auditoria");
        everything.Should().NotContain("%PDF", "o conteúdo do arquivo nunca vai para a auditoria");
        everything.Should().NotContain("PasswordHash\":\"$", "hashes de senha nunca vão para a auditoria");
    }

    // ---- helpers ----

    private int CountStoredFiles() =>
        Directory.Exists(Factory.StorageRoot) ? Directory.GetFiles(Factory.StorageRoot).Length : 0;

    private static async Task<HttpResponseMessage> UploadAsync(HttpClient client, Guid caseId, Guid evidenceId, byte[] bytes, string fileName, string contentType = "application/octet-stream")
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);
        return await client.PostAsync($"/api/v1/disciplinary-cases/{caseId}/evidences/{evidenceId}/file", form);
    }

    private static Task<HttpResponseMessage> DeleteAsync(HttpClient client, Guid caseId, Guid evidenceId, string reason) =>
        client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/disciplinary-cases/{caseId}/evidences/{evidenceId}")
        {
            Content = JsonContent.Create(new { reason })
        });

    private static async Task<Guid> CreateCaseAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/disciplinary-cases", new CreateDisciplinaryCaseHttpRequest(
            CaseNumber: $"EVD-{Guid.NewGuid():N}"[..12],
            CompanyId: Guid.NewGuid(),
            Title: "Processo com evidências",
            Description: "Processo usado nos testes de evidência."));
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<Result<CreateDisciplinaryCaseResponse>>())!.Data!.Id;
    }

    private static async Task<Guid> CreateFileEvidenceAsync(HttpClient client, Guid caseId, EvidenceType type)
    {
        var response = await client.PostAsJsonAsync($"/api/v1/disciplinary-cases/{caseId}/evidences",
            new AddEvidenceHttpRequest(type, "Documento da evidência", DateTimeOffset.UtcNow));
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<Result<AddEvidenceResponse>>())!.Data!.Id;
    }

    private static async Task<GetDisciplinaryCaseByIdResponse> GetCaseAsync(HttpClient client, Guid caseId) =>
        (await client.GetFromJsonAsync<Result<GetDisciplinaryCaseByIdResponse>>($"/api/v1/disciplinary-cases/{caseId}"))!.Data!;

    private async Task<HttpClient> CreateClientAsync(string email)
    {
        var client = Factory.CreateClient();
        var (login, _) = await AuthTestHelpers.LoginAsync(client, email, CustomWebApplicationFactory.DefaultPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return client;
    }
}
