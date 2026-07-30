using SIGH.Application.Common.Models;
using SIGH.Application.Disciplinary.Common;
using SIGH.Application.Interfaces.Repositories.Disciplinary;

namespace SIGH.Application.Disciplinary.InfractionTypes.GetInfractionTypeById;

public class GetInfractionTypeByIdUseCase : IGetInfractionTypeByIdUseCase
{
    private readonly IInfractionTypeRepository _repository;

    public GetInfractionTypeByIdUseCase(IInfractionTypeRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<GetInfractionTypeByIdResponse>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetByIdAsync(id, cancellationToken);
        if (item == null)
        {
            return Result<GetInfractionTypeByIdResponse>.Failure("Tipo de infração não encontrado.", DisciplinaryErrors.InfractionTypeNotFound);
        }

        var response = new GetInfractionTypeByIdResponse(
            item.Id,
            item.Code,
            item.Name,
            item.DefaultSeverity,
            item.Description,
            item.LegalReference,
            item.IsActive);

        return Result<GetInfractionTypeByIdResponse>.Ok(response);
    }
}
