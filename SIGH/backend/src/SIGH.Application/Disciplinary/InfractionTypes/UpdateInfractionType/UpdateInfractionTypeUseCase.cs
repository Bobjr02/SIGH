using FluentValidation;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Common.Models;
using SIGH.Application.Interfaces.Repositories.Disciplinary;
using SIGH.Domain.Exceptions;

namespace SIGH.Application.Disciplinary.InfractionTypes.UpdateInfractionType;

public class UpdateInfractionTypeUseCase : IUpdateInfractionTypeUseCase
{
    private readonly IInfractionTypeRepository _infractionTypeRepository;
    private readonly IApplicationDbContext _context;
    private readonly IValidator<UpdateInfractionTypeRequest> _validator;

    public UpdateInfractionTypeUseCase(
        IInfractionTypeRepository infractionTypeRepository,
        IApplicationDbContext context,
        IValidator<UpdateInfractionTypeRequest> validator)
    {
        _infractionTypeRepository = infractionTypeRepository;
        _context = context;
        _validator = validator;
    }

    public async Task<Result<UpdateInfractionTypeResponse>> ExecuteAsync(UpdateInfractionTypeRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Result<UpdateInfractionTypeResponse>.Failure(errors);
        }

        var infractionType = await _infractionTypeRepository.GetByIdAsync(request.Id, cancellationToken);
        if (infractionType == null)
        {
            return Result<UpdateInfractionTypeResponse>.Failure("Tipo de infração não encontrado.");
        }

        try
        {
            infractionType.UpdateDetails(
                request.Name,
                request.DefaultSeverity,
                request.RequiresFormalInvestigation,
                request.AllowsTerminationRecommendation,
                request.Description,
                request.LegalReference);

            await _context.SaveChangesAsync(cancellationToken);

            var response = new UpdateInfractionTypeResponse(
                infractionType.Id,
                infractionType.Code,
                infractionType.Name,
                infractionType.DefaultSeverity,
                infractionType.RequiresFormalInvestigation,
                infractionType.AllowsTerminationRecommendation,
                infractionType.Description,
                infractionType.LegalReference,
                infractionType.IsActive);

            return Result<UpdateInfractionTypeResponse>.Ok(response);
        }
        catch (BusinessRuleValidationException ex)
        {
            return Result<UpdateInfractionTypeResponse>.Failure(ex.Message);
        }
    }
}
