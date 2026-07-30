using SIGH.Application.Common.Interfaces;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.Common;
using SIGH.Application.Interfaces;
using SIGH.Application.Interfaces.Repositories.Disciplinary;
using SIGH.Domain.Disciplinary.Entities;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.AddOccurrence;

public class AddOccurrenceUseCase : IAddOccurrenceUseCase
{
    private readonly IApplicationDbContext _context;
    private readonly IDisciplinaryCaseRepository _caseRepository;
    private readonly IInfractionTypeRepository _infractionTypeRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AddOccurrenceUseCase(
        IApplicationDbContext context,
        IDisciplinaryCaseRepository caseRepository,
        IInfractionTypeRepository infractionTypeRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _caseRepository = caseRepository;
        _infractionTypeRepository = infractionTypeRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<AddOccurrenceResponse>> ExecuteAsync(AddOccurrenceRequest request, CancellationToken cancellationToken = default)
    {
        var caseObj = await _caseRepository.GetByIdAsync(request.DisciplinaryCaseId, cancellationToken);
        if (caseObj == null)
        {
            return Result<AddOccurrenceResponse>.Failure("Processo disciplinar não encontrado.", DisciplinaryErrors.DisciplinaryCaseNotFound);
        }

        var infractionType = await _infractionTypeRepository.GetByIdAsync(request.InfractionTypeId, cancellationToken);
        if (infractionType == null)
        {
            return Result<AddOccurrenceResponse>.Failure("Tipo de infração não encontrado.", DisciplinaryErrors.InfractionTypeNotFound);
        }

        if (!infractionType.IsActive)
        {
            return Result<AddOccurrenceResponse>.Failure("O tipo de infração está inativo.", DisciplinaryErrors.InactiveInfractionType);
        }

        var now = _dateTimeProvider.UtcNow;

        var occurrence = DisciplinaryOccurrence.Create(
            caseObj.Id,
            request.OccurrenceDate,
            now,
            request.Description,
            request.ReportedByUserId,
            infractionType.Id,
            request.Severity,
            request.Location);

        caseObj.AddOccurrence(occurrence);

        await _context.SaveChangesAsync(cancellationToken);

        var response = new AddOccurrenceResponse(
            occurrence.Id,
            caseObj.Id,
            infractionType.Id,
            occurrence.Severity,
            occurrence.Status);

        return Result<AddOccurrenceResponse>.Ok(response, "Ocorrência adicionada ao processo com sucesso.");
    }
}
