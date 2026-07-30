using FluentValidation;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Common.Models;
using SIGH.Application.Interfaces.Repositories.Disciplinary;
using SIGH.Domain.Exceptions;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.SubmitCaseForDecision;

public class SubmitCaseForDecisionUseCase : ISubmitCaseForDecisionUseCase
{
    private readonly IDisciplinaryCaseRepository _caseRepository;
    private readonly IApplicationDbContext _context;
    private readonly IValidator<SubmitCaseForDecisionRequest> _validator;

    public SubmitCaseForDecisionUseCase(
        IDisciplinaryCaseRepository caseRepository,
        IApplicationDbContext context,
        IValidator<SubmitCaseForDecisionRequest> validator)
    {
        _caseRepository = caseRepository;
        _context = context;
        _validator = validator;
    }

    public async Task<Result<SubmitCaseForDecisionResponse>> ExecuteAsync(SubmitCaseForDecisionRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Result<SubmitCaseForDecisionResponse>.Failure(errors);
        }

        var disciplinaryCase = await _caseRepository.GetByIdWithDetailsAsync(request.DisciplinaryCaseId, cancellationToken);
        if (disciplinaryCase == null)
        {
            return Result<SubmitCaseForDecisionResponse>.Failure("Processo disciplinar não encontrado.");
        }

        try
        {
            disciplinaryCase.SubmitForDecision();
            await _context.SaveChangesAsync(cancellationToken);

            var response = new SubmitCaseForDecisionResponse(
                disciplinaryCase.Id,
                disciplinaryCase.Status);

            return Result<SubmitCaseForDecisionResponse>.Ok(response);
        }
        catch (BusinessRuleValidationException ex)
        {
            return Result<SubmitCaseForDecisionResponse>.Failure(ex.Message);
        }
    }
}
