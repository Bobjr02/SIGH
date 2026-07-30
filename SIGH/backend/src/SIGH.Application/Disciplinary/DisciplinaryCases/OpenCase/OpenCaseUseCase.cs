using FluentValidation;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Common.Models;
using SIGH.Application.Interfaces.Repositories.Disciplinary;
using SIGH.Domain.Exceptions;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.OpenCase;

public class OpenCaseUseCase : IOpenCaseUseCase
{
    private readonly IDisciplinaryCaseRepository _caseRepository;
    private readonly IApplicationDbContext _context;
    private readonly IValidator<OpenCaseRequest> _validator;

    public OpenCaseUseCase(
        IDisciplinaryCaseRepository caseRepository,
        IApplicationDbContext context,
        IValidator<OpenCaseRequest> validator)
    {
        _caseRepository = caseRepository;
        _context = context;
        _validator = validator;
    }

    public async Task<Result<OpenCaseResponse>> ExecuteAsync(OpenCaseRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Result<OpenCaseResponse>.Failure(errors);
        }

        var disciplinaryCase = await _caseRepository.GetByIdAsync(request.DisciplinaryCaseId, cancellationToken);
        if (disciplinaryCase == null)
        {
            return Result<OpenCaseResponse>.Failure("Processo disciplinar não encontrado.");
        }

        try
        {
            disciplinaryCase.Open();
            await _context.SaveChangesAsync(cancellationToken);

            var response = new OpenCaseResponse(
                disciplinaryCase.Id,
                disciplinaryCase.Status,
                disciplinaryCase.OpenedAt);

            return Result<OpenCaseResponse>.Ok(response);
        }
        catch (BusinessRuleValidationException ex)
        {
            return Result<OpenCaseResponse>.Failure(ex.Message);
        }
    }
}
