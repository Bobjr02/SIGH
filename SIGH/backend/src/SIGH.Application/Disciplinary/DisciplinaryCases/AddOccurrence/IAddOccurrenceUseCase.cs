using SIGH.Application.Common.Models;

namespace SIGH.Application.Disciplinary.DisciplinaryCases.AddOccurrence;

public interface IAddOccurrenceUseCase
{
    Task<Result<AddOccurrenceResponse>> ExecuteAsync(AddOccurrenceRequest request, CancellationToken cancellationToken = default);
}
