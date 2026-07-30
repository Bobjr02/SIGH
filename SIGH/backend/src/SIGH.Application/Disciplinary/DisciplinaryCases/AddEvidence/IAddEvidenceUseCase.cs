using SIGH.Application.Common.Models;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.AddEvidence;

public interface IAddEvidenceUseCase
{
    Task<Result<AddEvidenceResponse>> ExecuteAsync(AddEvidenceRequest request, CancellationToken cancellationToken = default);
}
