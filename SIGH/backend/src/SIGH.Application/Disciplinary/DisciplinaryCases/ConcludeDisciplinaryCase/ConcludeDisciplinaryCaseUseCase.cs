using SIGH.Application.Common.Interfaces;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.Common;
using SIGH.Application.Interfaces;
using SIGH.Application.Interfaces.Repositories.Disciplinary;
using SIGH.Domain.Exceptions;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.ConcludeDisciplinaryCase;

public class ConcludeDisciplinaryCaseUseCase : IConcludeDisciplinaryCaseUseCase
{
    private readonly IApplicationDbContext _context;
    private readonly IDisciplinaryCaseRepository _caseRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ConcludeDisciplinaryCaseUseCase(
        IApplicationDbContext context,
        IDisciplinaryCaseRepository caseRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _caseRepository = caseRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<ConcludeDisciplinaryCaseResponse>> ExecuteAsync(ConcludeDisciplinaryCaseRequest request, CancellationToken cancellationToken = default)
    {
        var caseObj = await _caseRepository.GetByIdAsync(request.DisciplinaryCaseId, cancellationToken);
        if (caseObj == null)
        {
            return Result<ConcludeDisciplinaryCaseResponse>.Failure("Processo disciplinar não encontrado.", DisciplinaryErrors.DisciplinaryCaseNotFound);
        }

        var closedAt = _dateTimeProvider.UtcNow;

        try
        {
            caseObj.Complete(
                conclusionSummary: request.ConclusionSummary,
                closedAt: closedAt);
        }
        catch (BusinessRuleValidationException ex)
        {
            return Result<ConcludeDisciplinaryCaseResponse>.Failure(ex.Message, DisciplinaryErrors.InvalidStatusTransition);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var response = new ConcludeDisciplinaryCaseResponse(
            caseObj.Id,
            caseObj.Status,
            caseObj.ConclusionSummary!,
            caseObj.ClosedAt!.Value);

        return Result<ConcludeDisciplinaryCaseResponse>.Ok(response, "Processo disciplinar concluído com sucesso.");
    }
}
