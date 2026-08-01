using FluentValidation;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Common.Models;
using SIGH.Application.Interfaces.Repositories.Disciplinary;
using SIGH.Domain.Exceptions;

namespace SIGH.Application.Disciplinary.InfractionTypes.ActivateInfractionType;

public class ActivateInfractionTypeUseCase : IActivateInfractionTypeUseCase
{
    private readonly IInfractionTypeRepository _infractionTypeRepository;
    private readonly IApplicationDbContext _context;
    private readonly IValidator<ActivateInfractionTypeRequest> _validator;

    public ActivateInfractionTypeUseCase(
        IInfractionTypeRepository infractionTypeRepository,
        IApplicationDbContext context,
        IValidator<ActivateInfractionTypeRequest> validator)
    {
        _infractionTypeRepository = infractionTypeRepository;
        _context = context;
        _validator = validator;
    }

    public async Task<Result<ActivateInfractionTypeResponse>> ExecuteAsync(ActivateInfractionTypeRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Result<ActivateInfractionTypeResponse>.Failure(string.Join("; ", errors));
        }

        var infractionType = await _infractionTypeRepository.GetByIdAsync(request.Id, cancellationToken);
        if (infractionType == null)
        {
            return Result<ActivateInfractionTypeResponse>.Failure("Tipo de infração não encontrado.");
        }

        try
        {
            infractionType.Activate();
            await _context.SaveChangesAsync(cancellationToken);

            var response = new ActivateInfractionTypeResponse(
                infractionType.Id,
                infractionType.IsActive);

            return Result<ActivateInfractionTypeResponse>.Ok(response);
        }
        catch (BusinessRuleValidationException ex)
        {
            return Result<ActivateInfractionTypeResponse>.Failure(ex.Message);
        }
    }
}
