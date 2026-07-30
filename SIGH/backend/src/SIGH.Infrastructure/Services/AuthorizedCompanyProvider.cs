using SIGH.Application.Interfaces;

namespace SIGH.Infrastructure.Services;

public class AuthorizedCompanyProvider : IAuthorizedCompanyProvider
{
    private readonly ICurrentUserService _currentUserService;

    public AuthorizedCompanyProvider(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public Task<IReadOnlyList<Guid>> GetAuthorizedCompanyIdsAsync(CancellationToken cancellationToken = default)
    {
        var defaultCompanyId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        IReadOnlyList<Guid> authorizedCompanies = new List<Guid> { defaultCompanyId };
        return Task.FromResult(authorizedCompanies);
    }

    public async Task<bool> IsCompanyAuthorizedAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty) return false;
        var authorizedIds = await GetAuthorizedCompanyIdsAsync(cancellationToken);
        return authorizedIds.Contains(companyId);
    }

    public async Task<Guid> GetAuthorizedCompanyIdAsync(Guid? requestedCompanyId, CancellationToken cancellationToken = default)
    {
        var authorizedIds = await GetAuthorizedCompanyIdsAsync(cancellationToken);
        if (authorizedIds.Count == 0)
        {
            throw new UnauthorizedAccessException("Usuário não possui empresas autorizadas.");
        }

        if (requestedCompanyId.HasValue && requestedCompanyId.Value != Guid.Empty)
        {
            if (!authorizedIds.Contains(requestedCompanyId.Value))
            {
                throw new UnauthorizedAccessException($"Acesso não autorizado à empresa {requestedCompanyId.Value}.");
            }
            return requestedCompanyId.Value;
        }

        return authorizedIds[0];
    }
}
