using FluentValidation;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Common.Models;
using SIGH.Application.Interfaces.Repositories.Disciplinary;
using SIGH.Domain.Exceptions;

namespace SIGH.Application.Disciplinary.InfractionTypes.DeactivateInfractionType;

public class DeactivateInfractionTypeUseCase : IDeactivateInfractionTypeUseCase
{
    private readonly IInfractionTypeRepository _infractionTypeRepository;
    private readonly IApplicationDbContext _context;
    private readonly IValidator<DeactivateInfractionTypeRequest> _validator;

    public DeactivateInfractionTypeUseCase(
        IInfractionTypeRepository infractionTypeRepository,
        IApplicationDbContext context,
        IValidator<DeactivateInfractionTypeRequest> validator)
    {
        _infractionTypeRepository = infractionTypeRepository;
        _context = context;
        _validator = validator;
    }

    public async Task<Result<DeactivateInfractionTypeResponse>> ExecuteAsync(DeactivateInfractionTypeRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Result<DeactivateInfractionTypeResponse>.Failure(string.Join("; ", errors));
        }

        var infractionType = await _infractionTypeRepository.GetByIdAsync(request.Id, cancellationToken);
        if (infractionType == null)
        {
            return Result<DeactivateInfractionTypeResponse>.Failure("Tipo de infração não encontrado.");
        }

        try
        {
            infractionType.Deactivate();
            await _context.SaveChangesAsync(cancellationToken);

            var response = new DeactivateInfractionTypeResponse(
                infractionType.Id,
                infractionType.IsActive);

            return Result<DeactivateInfractionTypeResponse>.Ok(response);
        }
        catch (BusinessRuleValidationException ex)
        {
            return Result<DeactivateInfractionTypeResponse>.Failure(ex.Message);
        }
    }
}
