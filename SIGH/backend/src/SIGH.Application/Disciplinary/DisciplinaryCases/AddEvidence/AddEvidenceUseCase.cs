using SIGH.Application.Common.Interfaces;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.Common;
using SIGH.Application.Interfaces.Repositories.Disciplinary;
using SIGH.Domain.Disciplinary.Entities;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.AddEvidence;

public class AddEvidenceUseCase : IAddEvidenceUseCase
{
    private readonly IApplicationDbContext _context;
    private readonly IDisciplinaryCaseRepository _caseRepository;

    public AddEvidenceUseCase(
        IApplicationDbContext context,
        IDisciplinaryCaseRepository caseRepository)
    {
        _context = context;
        _caseRepository = caseRepository;
    }

    public async Task<Result<AddEvidenceResponse>> ExecuteAsync(AddEvidenceRequest request, CancellationToken cancellationToken = default)
    {
        var caseObj = await _caseRepository.GetByIdWithDetailsAsync(request.DisciplinaryCaseId, cancellationToken);
        if (caseObj == null)
        {
            return Result<AddEvidenceResponse>.Failure("Processo disciplinar não encontrado.", DisciplinaryErrors.DisciplinaryCaseNotFound);
        }

        if (request.OccurrenceId.HasValue)
        {
            var occurrenceExists = caseObj.Occurrences.Any(o => o.Id == request.OccurrenceId.Value);
            if (!occurrenceExists)
            {
                return Result<AddEvidenceResponse>.Failure("A ocorrência informada não pertence a este processo disciplinar.", DisciplinaryErrors.OccurrenceNotFound);
            }
        }

        var evidence = DisciplinaryEvidence.Create(
            caseObj.Id,
            request.Type,
            request.Description,
            request.CollectedByUserId,
            request.CollectedAt,
            request.OccurrenceId,
            request.ReferenceCode,
            request.Location);

        caseObj.AddEvidence(evidence);

        await _context.SaveChangesAsync(cancellationToken);

        var response = new AddEvidenceResponse(
            evidence.Id,
            caseObj.Id,
            evidence.Type,
            evidence.Status,
            evidence.CollectedAt);

        return Result<AddEvidenceResponse>.Ok(response, "Evidência registrada com sucesso.");
    }
}
