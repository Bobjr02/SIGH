using SIGH.Application.Common.Interfaces;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.Common;
using SIGH.Application.Interfaces;
using SIGH.Application.Interfaces.Repositories.Disciplinary;
using SIGH.Domain.Disciplinary.Entities;
using SIGH.Domain.Disciplinary.Enums;

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
            request.DecisionType,
            request.Summary,
            request.Reasoning,
            now,
            request.DecidedByUserId,
            DecisionStatus.Draft);

        caseObj.RegisterDecision(decision);

        await _context.SaveChangesAsync(cancellationToken);

        var response = new RecordDecisionResponse(
            decision.Id,
            caseObj.Id,
            decision.DecisionType,
            decision.Summary,
            decision.Reasoning,
            decision.Status,
            decision.DecidedAt,
            decision.DecidedByUserId,
            decision.ApprovedByUserId,
            decision.ApprovedAt);

        return Result<RecordDecisionResponse>.Ok(response, "Decisão registrada com sucesso.");
    }
}
