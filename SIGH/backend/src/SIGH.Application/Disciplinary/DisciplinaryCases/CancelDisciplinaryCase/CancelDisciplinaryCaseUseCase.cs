using SIGH.Application.Common.Interfaces;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.Common;
using SIGH.Application.Interfaces;
using SIGH.Application.Interfaces.Repositories.Disciplinary;
using SIGH.Domain.Exceptions;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.CancelDisciplinaryCase;

public class CancelDisciplinaryCaseUseCase : ICancelDisciplinaryCaseUseCase
{
    private readonly IApplicationDbContext _context;
    private readonly IDisciplinaryCaseRepository _caseRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CancelDisciplinaryCaseUseCase(
        IApplicationDbContext context,
        IDisciplinaryCaseRepository caseRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _caseRepository = caseRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<CancelDisciplinaryCaseResponse>> ExecuteAsync(CancelDisciplinaryCaseRequest request, CancellationToken cancellationToken = default)
    {
        var caseObj = await _caseRepository.GetByIdAsync(request.DisciplinaryCaseId, cancellationToken);
        if (caseObj == null)
        {
            return Result<CancelDisciplinaryCaseResponse>.Failure("Processo disciplinar não encontrado.", DisciplinaryErrors.DisciplinaryCaseNotFound);
        }

        var now = _dateTimeProvider.UtcNow;

        try
        {
            caseObj.Cancel(request.CancelledByUserId, request.Reason, now);
        }
        catch (DomainException ex)
        {
            return Result<CancelDisciplinaryCaseResponse>.Failure(ex.Message, DisciplinaryErrors.InvalidStatusTransition);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var response = new CancelDisciplinaryCaseResponse(
            caseObj.Id,
            caseObj.Status,
            caseObj.CancelledAt!.Value,
            caseObj.CancellationReason!);

        return Result<CancelDisciplinaryCaseResponse>.Ok(response, "Processo disciplinar cancelado com sucesso.");
    }
}
