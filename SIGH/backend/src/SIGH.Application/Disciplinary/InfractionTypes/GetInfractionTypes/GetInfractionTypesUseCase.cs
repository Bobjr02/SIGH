using FluentValidation;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.DTOs;
using SIGH.Application.Interfaces.Repositories.Disciplinary;

namespace SIGH.Application.Disciplinary.InfractionTypes.GetInfractionTypes;

public class GetInfractionTypesUseCase : IGetInfractionTypesUseCase
{
    private readonly IInfractionTypeRepository _infractionTypeRepository;
    private readonly IValidator<GetInfractionTypesQuery> _validator;

    public GetInfractionTypesUseCase(
        IInfractionTypeRepository infractionTypeRepository,
        IValidator<GetInfractionTypesQuery> validator)
    {
        _infractionTypeRepository = infractionTypeRepository;
        _validator = validator;
    }

    public async Task<Result<PagedResult<InfractionTypeDto>>> ExecuteAsync(GetInfractionTypesQuery query, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Result<PagedResult<InfractionTypeDto>>.Failure(string.Join("; ", errors));
        }

        var pagedResult = await _infractionTypeRepository.GetPagedAsync(query, cancellationToken);
        return Result<PagedResult<InfractionTypeDto>>.Ok(pagedResult);
    }
}
