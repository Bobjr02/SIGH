using FluentValidation;
using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.DTOs;
using SIGH.Application.Interfaces.Repositories.Disciplinary;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.GetDisciplinaryCases;

public class GetDisciplinaryCasesUseCase : IGetDisciplinaryCasesUseCase
{
    private readonly IDisciplinaryCaseRepository _caseRepository;
    private readonly IValidator<GetDisciplinaryCasesQuery> _validator;

    public GetDisciplinaryCasesUseCase(
        IDisciplinaryCaseRepository caseRepository,
        IValidator<GetDisciplinaryCasesQuery> validator)
    {
        _caseRepository = caseRepository;
        _validator = validator;
    }

    public async Task<Result<PagedResult<DisciplinaryCaseSummaryDto>>> ExecuteAsync(GetDisciplinaryCasesQuery query, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Result<PagedResult<DisciplinaryCaseSummaryDto>>.Failure(string.Join("; ", errors));
        }

        var pagedResult = await _caseRepository.GetPagedAsync(query, cancellationToken);
        return Result<PagedResult<DisciplinaryCaseSummaryDto>>.Ok(pagedResult);
    }
}
