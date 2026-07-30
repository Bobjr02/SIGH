using SIGH.Application.Common.Models;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.AddEmployee;

public interface IAddEmployeeToCaseUseCase
{
    Task<Result<AddEmployeeToCaseResponse>> ExecuteAsync(AddEmployeeToCaseRequest request, CancellationToken cancellationToken = default);
}
