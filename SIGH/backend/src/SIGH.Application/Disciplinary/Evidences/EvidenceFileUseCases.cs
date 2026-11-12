using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Common.Models;
using SIGH.Application.Interfaces;
using SIGH.Application.Interfaces.Repositories.Disciplinary;
using SIGH.Application.Interfaces.Storage;
using SIGH.Application.Options;
using SIGH.Application.Security;
using SIGH.Domain.Disciplinary.Enums;

namespace SIGH.Application.Disciplinary.Evidences;

public static class EvidenceErrors
{
    public const string CaseNotFound = "Evidence.Case.NotFound";
    public const string EvidenceNotFound = "Evidence.NotFound";
    public const string FileNotFound = "Evidence.File.NotFound";
    public const string FileTooLarge = "Evidence.File.TooLarge";
    public const string UnsupportedFile = "Evidence.File.Unsupported";
    public const string Malware = "Evidence.File.Malware";
    public const string BusinessRule = "Evidence.BusinessRule";
}

public record EvidenceFileDto(
    Guid EvidenceId,
    string OriginalFileName,
    string ContentType,
    long FileSize,
    string Sha256Hash,
    DateTimeOffset UploadedAt,
    Guid UploadedByUserId);

public record UploadEvidenceFileRequest(Guid CaseId, Guid EvidenceId, Stream Content, string FileName, long Length, Guid ActorUserId);

public record EvidenceDownload(Stream Content, string ContentType, string FileName, long FileSize, string Sha256Hash);

public interface IUploadEvidenceFileUseCase
{
    Task<Result<EvidenceFileDto>> ExecuteAsync(UploadEvidenceFileRequest request, CancellationToken cancellationToken = default);
}

public interface IDownloadEvidenceFileUseCase
{
    Task<Result<EvidenceDownload>> ExecuteAsync(Guid caseId, Guid evidenceId, Guid actorUserId, CancellationToken cancellationToken = default);
}

public interface IRemoveEvidenceUseCase
{
    Task<Result<bool>> ExecuteAsync(Guid caseId, Guid evidenceId, string reason, Guid actorUserId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Upload do arquivo de uma evidência já cadastrada. Ordem: regras do domínio (processo aberto,
/// evidência sem arquivo) → tamanho → assinatura → antivírus → SHA-256 → storage → domínio.
/// </summary>
public sealed class UploadEvidenceFileUseCase : IUploadEvidenceFileUseCase
{
    private readonly IApplicationDbContext _context;
    private readonly IDisciplinaryCaseRepository _caseRepository;
    private readonly IFileStorage _storage;
    private readonly IMalwareScanner _malwareScanner;
    private readonly IDateTimeProvider _clock;
    private readonly StorageOptions _options;
    private readonly ILogger<UploadEvidenceFileUseCase> _logger;

    public UploadEvidenceFileUseCase(
        IApplicationDbContext context,
        IDisciplinaryCaseRepository caseRepository,
        IFileStorage storage,
        IMalwareScanner malwareScanner,
        IDateTimeProvider clock,
        IOptions<StorageOptions> options,
        ILogger<UploadEvidenceFileUseCase> logger)
    {
        _context = context;
        _caseRepository = caseRepository;
        _storage = storage;
        _malwareScanner = malwareScanner;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result<EvidenceFileDto>> ExecuteAsync(UploadEvidenceFileRequest request, CancellationToken cancellationToken = default)
    {
        var caseObj = await _caseRepository.GetByIdWithDetailsAsync(request.CaseId, cancellationToken);
        if (caseObj is null)
            return Result<EvidenceFileDto>.Failure("Processo disciplinar não encontrado.", EvidenceErrors.CaseNotFound);

        var evidence = caseObj.Evidences.FirstOrDefault(e => e.Id == request.EvidenceId);
        if (evidence is null)
            return Result<EvidenceFileDto>.Failure("Evidência não encontrada.", EvidenceErrors.EvidenceNotFound);

        // Regras do domínio primeiro: nada vai para o disco se a evidência não pode receber arquivo.
        caseObj.EnsureEvidenceAcceptsFile(evidence.Id);

        if (request.Length <= 0 || request.Content.Length == 0)
            return Result<EvidenceFileDto>.Failure("O arquivo está vazio.", EvidenceErrors.UnsupportedFile);

        if (request.Length > _options.MaxUploadSizeBytes || request.Content.Length > _options.MaxUploadSizeBytes)
            return Result<EvidenceFileDto>.Failure(
                $"O arquivo excede o tamanho máximo de {_options.MaxUploadSizeBytes / (1024 * 1024)} MB.", EvidenceErrors.FileTooLarge);

        var (detected, error) = EvidenceFileInspector.Validate(request.Content, request.FileName);
        if (detected is null)
            return Result<EvidenceFileDto>.Failure(error!, EvidenceErrors.UnsupportedFile);

        if (evidence.EvidenceType != EvidenceType.Other && evidence.EvidenceType != detected.Kind)
            return Result<EvidenceFileDto>.Failure(
                $"O arquivo ({detected.Description}) não corresponde ao tipo desta evidência ({evidence.EvidenceType}).", EvidenceErrors.UnsupportedFile);

        var fileName = EvidenceFileName.Sanitize(request.FileName, detected.Extension);

        request.Content.Position = 0;
        var scan = await _malwareScanner.ScanAsync(request.Content, fileName, cancellationToken);
        if (!scan.IsClean)
        {
            _logger.LogWarning("Upload de evidência {EvidenceId} rejeitado pelo antivírus: {Threat}", evidence.Id, scan.ThreatName);
            return Result<EvidenceFileDto>.Failure("O arquivo foi rejeitado pela verificação de segurança.", EvidenceErrors.Malware);
        }

        request.Content.Position = 0;
        var sha256 = Convert.ToHexString(await SHA256.HashDataAsync(request.Content, cancellationToken)).ToLowerInvariant();
        var size = request.Content.Length;

        request.Content.Position = 0;
        var key = await _storage.SaveAsync(request.Content, detected.Extension, cancellationToken);

        var now = _clock.UtcNow;
        caseObj.AttachEvidenceFile(evidence.Id, key, fileName, detected.ContentType, size, sha256, request.ActorUserId, now);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // O arquivo gravado fica órfão (o storage não apaga: imutabilidade). Registrado para limpeza manual.
            _logger.LogError(ex, "Falha ao gravar metadados da evidência {EvidenceId}; arquivo órfão no storage: {Key}", evidence.Id, key);
            throw;
        }

        return Result<EvidenceFileDto>.Ok(
            new EvidenceFileDto(evidence.Id, fileName, detected.ContentType, size, sha256, now, request.ActorUserId),
            "Arquivo anexado à evidência.");
    }
}

public sealed class DownloadEvidenceFileUseCase : IDownloadEvidenceFileUseCase
{
    private readonly IDisciplinaryCaseRepository _caseRepository;
    private readonly IFileStorage _storage;
    private readonly ISecurityAuditLogger _securityAudit;
    private readonly ILogger<DownloadEvidenceFileUseCase> _logger;

    public DownloadEvidenceFileUseCase(
        IDisciplinaryCaseRepository caseRepository,
        IFileStorage storage,
        ISecurityAuditLogger securityAudit,
        ILogger<DownloadEvidenceFileUseCase> logger)
    {
        _caseRepository = caseRepository;
        _storage = storage;
        _securityAudit = securityAudit;
        _logger = logger;
    }

    public async Task<Result<EvidenceDownload>> ExecuteAsync(Guid caseId, Guid evidenceId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var caseObj = await _caseRepository.GetByIdWithDetailsAsync(caseId, cancellationToken);
        if (caseObj is null)
            return Result<EvidenceDownload>.Failure("Processo disciplinar não encontrado.", EvidenceErrors.CaseNotFound);

        // Evidências removidas não aparecem (filtro de exclusão lógica) e não são baixáveis.
        var evidence = caseObj.Evidences.FirstOrDefault(e => e.Id == evidenceId);
        if (evidence is null)
            return Result<EvidenceDownload>.Failure("Evidência não encontrada.", EvidenceErrors.EvidenceNotFound);

        if (!evidence.HasFile)
            return Result<EvidenceDownload>.Failure("Esta evidência não possui arquivo.", EvidenceErrors.FileNotFound);

        Stream stream;
        try
        {
            stream = await _storage.OpenReadAsync(evidence.StorageReference!, cancellationToken);
        }
        catch (FileNotFoundException)
        {
            _logger.LogError("Arquivo da evidência {EvidenceId} não encontrado no storage ({Key}).", evidence.Id, evidence.StorageReference);
            return Result<EvidenceDownload>.Failure("Arquivo da evidência não encontrado.", EvidenceErrors.FileNotFound);
        }

        await _securityAudit.RecordAsync(
            SecurityAuditActions.EvidenceDownloaded,
            actorUserId,
            new { caseId, evidenceId, fileName = evidence.OriginalFileName, sha256 = evidence.IntegrityHash, size = evidence.FileSize },
            cancellationToken,
            entityName: "DisciplinaryEvidence",
            entityId: evidence.Id.ToString());

        return Result<EvidenceDownload>.Ok(new EvidenceDownload(
            stream, evidence.ContentType!, evidence.OriginalFileName!, evidence.FileSize ?? stream.Length, evidence.IntegrityHash!));
    }
}

public sealed class RemoveEvidenceUseCase : IRemoveEvidenceUseCase
{
    private readonly IApplicationDbContext _context;
    private readonly IDisciplinaryCaseRepository _caseRepository;
    private readonly IDateTimeProvider _clock;

    public RemoveEvidenceUseCase(IApplicationDbContext context, IDisciplinaryCaseRepository caseRepository, IDateTimeProvider clock)
    {
        _context = context;
        _caseRepository = caseRepository;
        _clock = clock;
    }

    public async Task<Result<bool>> ExecuteAsync(Guid caseId, Guid evidenceId, string reason, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var caseObj = await _caseRepository.GetByIdWithDetailsAsync(caseId, cancellationToken);
        if (caseObj is null)
            return Result<bool>.Failure("Processo disciplinar não encontrado.", EvidenceErrors.CaseNotFound);

        if (caseObj.Evidences.All(e => e.Id != evidenceId))
            return Result<bool>.Failure("Evidência não encontrada.", EvidenceErrors.EvidenceNotFound);

        caseObj.RemoveEvidence(evidenceId, reason, actorUserId, _clock.UtcNow);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Ok(true, "Evidência removida. O registro e o arquivo continuam preservados para auditoria.");
    }
}
