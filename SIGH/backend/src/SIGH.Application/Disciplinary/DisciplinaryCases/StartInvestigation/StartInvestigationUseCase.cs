using FluentValidation;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Common.Models;
using SIGH.Application.Interfaces.Repositories.Disciplinary;
using SIGH.Domain.Exceptions;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.StartInvestigation;

public class StartInvestigationUseCase : IStartInvestigationUseCase
{
    private readonly IDisciplinaryCaseRepository _caseRepository;
    private readonly IApplicationDbContext _context;
    private readonly IValidator<StartInvestigationRequest> _validator;

    public StartInvestigationUseCase(
        IDisciplinaryCaseRepository caseRepository,
        IApplicationDbContext context,
        IValidator<StartInvestigationRequest> validator)
    {
        _caseRepository = caseRepository;
        _context = context;
        _validator = validator;
    }

    public async Task<Result<StartInvestigationResponse>> ExecuteAsync(StartInvestigationRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Result<StartInvestigationResponse>.Failure(string.Join("; ", errors));
        }

        var disciplinaryCase = await _caseRepository.GetByIdWithDetailsAsync(request.DisciplinaryCaseId, cancellationToken);
        if (disciplinaryCase == null)
        {
            return Result<StartInvestigationResponse>.Failure("Processo disciplinar não encontrado.");
        }

        try
        {
            disciplinaryCase.StartInvestigation();
            await _context.SaveChangesAsync(cancellationToken);

            var response = new StartInvestigationResponse(
                disciplinaryCase.Id,
                disciplinaryCase.Status);

            return Result<StartInvestigationResponse>.Ok(response);
        }
        catch (BusinessRuleValidationException ex)
        {
            return Result<StartInvestigationResponse>.Failure(ex.Message);
        }
    }
}
