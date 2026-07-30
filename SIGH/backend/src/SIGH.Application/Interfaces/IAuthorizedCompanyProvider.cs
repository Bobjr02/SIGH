namespace SIGH.Application.Interfaces;

public interface IAuthorizedCompanyProvider
{
    Task<IReadOnlyList<Guid>> GetAuthorizedCompanyIdsAsync(CancellationToken cancellationToken = default);
    Task<bool> IsCompanyAuthorizedAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<Guid> GetAuthorizedCompanyIdAsync(Guid? requestedCompanyId, CancellationToken cancellationToken = default);
}
