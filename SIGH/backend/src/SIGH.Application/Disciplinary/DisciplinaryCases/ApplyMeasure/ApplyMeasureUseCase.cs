using SIGH.Application.Common.Interfaces;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.Common;
using SIGH.Application.Interfaces;
using SIGH.Application.Interfaces.Repositories.Disciplinary;
using SIGH.Domain.Disciplinary.Entities;
using SIGH.Domain.Repositories;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.ApplyMeasure;

public class ApplyMeasureUseCase : IApplyMeasureUseCase
{
    private readonly IApplicationDbContext _context;
    private readonly IDisciplinaryCaseRepository _caseRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ApplyMeasureUseCase(
        IApplicationDbContext context,
        IDisciplinaryCaseRepository caseRepository,
        IEmployeeRepository employeeRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _caseRepository = caseRepository;
        _employeeRepository = employeeRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<ApplyMeasureResponse>> ExecuteAsync(ApplyMeasureRequest request, CancellationToken cancellationToken = default)
    {
        var caseObj = await _caseRepository.GetByIdWithDetailsAsync(request.DisciplinaryCaseId, cancellationToken);
        if (caseObj == null)
        {
            return Result<ApplyMeasureResponse>.Failure("Processo disciplinar não encontrado.", DisciplinaryErrors.DisciplinaryCaseNotFound);
        }

        var decisionExists = caseObj.Decisions.Any(d => d.Id == request.DecisionId);
        if (!decisionExists)
        {
            return Result<ApplyMeasureResponse>.Failure("Decisão não encontrada para este processo disciplinar.", DisciplinaryErrors.DecisionNotFound);
        }

        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee == null)
        {
            return Result<ApplyMeasureResponse>.Failure("Funcionário não encontrado.", DisciplinaryErrors.EmployeeNotFound);
        }

        var now = _dateTimeProvider.UtcNow;

        var measure = DisciplinaryMeasure.Create(
            caseObj.Id,
            request.DecisionId,
            employee.Id,
            request.Type,
            request.Description,
            request.EffectiveFrom,
            request.AppliedByUserId,
            now,
            request.EffectiveUntil);

        caseObj.ApplyMeasure(measure);

        await _context.SaveChangesAsync(cancellationToken);

        var response = new ApplyMeasureResponse(
            measure.Id,
            caseObj.Id,
            request.DecisionId,
            employee.Id,
            measure.Type,
            measure.Status,
            measure.AppliedAt);

        return Result<ApplyMeasureResponse>.Ok(response, "Medida disciplinar aplicada com sucesso.");
    }
}
