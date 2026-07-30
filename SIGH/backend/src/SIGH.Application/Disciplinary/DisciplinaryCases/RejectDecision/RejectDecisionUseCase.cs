using FluentValidation;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Common.Models;
using SIGH.Application.Interfaces.Repositories.Disciplinary;
using SIGH.Domain.Exceptions;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.RejectDecision;

public class RejectDecisionUseCase : IRejectDecisionUseCase
{
    private readonly IDisciplinaryCaseRepository _caseRepository;
    private readonly IApplicationDbContext _context;
    private readonly IValidator<RejectDecisionRequest> _validator;

    public RejectDecisionUseCase(
        IDisciplinaryCaseRepository caseRepository,
        IApplicationDbContext context,
        IValidator<RejectDecisionRequest> validator)
    {
        _caseRepository = caseRepository;
        _context = context;
        _validator = validator;
    }

    public async Task<Result<RejectDecisionResponse>> ExecuteAsync(RejectDecisionRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Result<RejectDecisionResponse>.Failure(errors);
        }

        var disciplinaryCase = await _caseRepository.GetByIdWithDetailsAsync(request.DisciplinaryCaseId, cancellationToken);
        if (disciplinaryCase == null)
        {
            return Result<RejectDecisionResponse>.Failure("Processo disciplinar não encontrado.");
        }

        if (disciplinaryCase.Decision == null)
        {
            return Result<RejectDecisionResponse>.Failure("Nenhuma decisão encontrada para o processo disciplinar.");
        }

        try
        {
            disciplinaryCase.RejectDecision(request.Reason);
            await _context.SaveChangesAsync(cancellationToken);

            var decision = disciplinaryCase.Decision!;
            var response = new RejectDecisionResponse(
                disciplinaryCase.Id,
                decision.Id,
                disciplinaryCase.Status,
                decision.Status,
                request.Reason);

            return Result<RejectDecisionResponse>.Ok(response);
        }
        catch (BusinessRuleValidationException ex)
        {
            return Result<RejectDecisionResponse>.Failure(ex.Message);
        }
    }
}
