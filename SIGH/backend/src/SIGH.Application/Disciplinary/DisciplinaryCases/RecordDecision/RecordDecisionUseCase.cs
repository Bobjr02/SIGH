using SIGH.Application.Common.Interfaces;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.Common;
using SIGH.Application.Interfaces;
using SIGH.Application.Interfaces.Repositories.Disciplinary;
using SIGH.Domain.Disciplinary.Entities;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.RecordDecision;

public class RecordDecisionUseCase : IRecordDecisionUseCase
{
    private readonly IApplicationDbContext _context;
    private readonly IDisciplinaryCaseRepository _caseRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RecordDecisionUseCase(
        IApplicationDbContext context,
        IDisciplinaryCaseRepository caseRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _caseRepository = caseRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<RecordDecisionResponse>> ExecuteAsync(RecordDecisionRequest request, CancellationToken cancellationToken = default)
    {
        var caseObj = await _caseRepository.GetByIdAsync(request.DisciplinaryCaseId, cancellationToken);
        if (caseObj == null)
        {
            return Result<RecordDecisionResponse>.Failure("Processo disciplinar não encontrado.", DisciplinaryErrors.DisciplinaryCaseNotFound);
        }

        var now = _dateTimeProvider.UtcNow;

        var decision = DisciplinaryDecision.Create(
            caseObj.Id,
            request.Type,
            request.Justification,
            request.DecidedByUserId,
            now,
            request.ApprovedByUserId,
            request.ApprovedAt);

        caseObj.RecordDecision(decision);

        await _context.SaveChangesAsync(cancellationToken);

        var response = new RecordDecisionResponse(
            decision.Id,
            caseObj.Id,
            decision.Type,
            decision.Status,
            decision.DecidedAt);

        return Result<RecordDecisionResponse>.Ok(response, "Decisão registrada com sucesso.");
    }
}
