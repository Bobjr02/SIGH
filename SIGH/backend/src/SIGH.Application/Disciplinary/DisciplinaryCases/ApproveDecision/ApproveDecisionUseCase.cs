using FluentValidation;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Common.Models;
using SIGH.Application.Interfaces.Repositories.Disciplinary;
using SIGH.Domain.Exceptions;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.ApproveDecision;

public class ApproveDecisionUseCase : IApproveDecisionUseCase
{
    private readonly IDisciplinaryCaseRepository _caseRepository;
    private readonly IApplicationDbContext _context;
    private readonly IValidator<ApproveDecisionRequest> _validator;

    public ApproveDecisionUseCase(
        IDisciplinaryCaseRepository caseRepository,
        IApplicationDbContext context,
        IValidator<ApproveDecisionRequest> validator)
    {
        _caseRepository = caseRepository;
        _context = context;
        _validator = validator;
    }

    public async Task<Result<ApproveDecisionResponse>> ExecuteAsync(ApproveDecisionRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Result<ApproveDecisionResponse>.Failure(errors);
        }

        var disciplinaryCase = await _caseRepository.GetByIdWithDetailsAsync(request.DisciplinaryCaseId, cancellationToken);
        if (disciplinaryCase == null)
        {
            return Result<ApproveDecisionResponse>.Failure("Processo disciplinar não encontrado.");
        }

        if (disciplinaryCase.Decision == null)
        {
            return Result<ApproveDecisionResponse>.Failure("Nenhuma decisão encontrada para o processo disciplinar.");
        }

        try
        {
            var approvedAt = DateTimeOffset.UtcNow;
            disciplinaryCase.ApproveDecision(request.ApprovedByUserId, approvedAt);
            await _context.SaveChangesAsync(cancellationToken);

            var decision = disciplinaryCase.Decision!;
            var response = new ApproveDecisionResponse(
                disciplinaryCase.Id,
                decision.Id,
                disciplinaryCase.Status,
                decision.Status,
                approvedAt);

            return Result<ApproveDecisionResponse>.Ok(response);
        }
        catch (BusinessRuleValidationException ex)
        {
            return Result<ApproveDecisionResponse>.Failure(ex.Message);
        }
    }
}
