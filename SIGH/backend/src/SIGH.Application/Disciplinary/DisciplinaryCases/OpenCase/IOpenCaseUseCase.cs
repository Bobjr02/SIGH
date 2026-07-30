using SIGH.Application.Common.Models;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.OpenCase;

public interface IOpenCaseUseCase
{
    Task<Result<OpenCaseResponse>> ExecuteAsync(OpenCaseRequest request, CancellationToken cancellationToken = default);
}
