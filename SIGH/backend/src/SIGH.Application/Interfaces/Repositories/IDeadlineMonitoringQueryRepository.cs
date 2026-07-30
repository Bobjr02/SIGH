namespace SIGH.Application.Interfaces.Repositories;

using SIGH.Application.Notifications.DTOs;

public interface IDeadlineMonitoringQueryRepository
{
    /// <summary>
    /// Consulta todos os itens ativos (processos disciplinares) que possuem data de vencimento preenchida,
    /// retornando os dados necessários para verificação de lembretes e escalonamento.
    /// </summary>
    Task<IEnumerable<DeadlineItemDto>> GetActiveItemsWithDeadlinesAsync(Guid? companyId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tenta obter o UserId do gestor/supervisor de um funcionário ou usuário.
    /// </summary>
    Task<Guid?> GetSupervisorUserIdAsync(Guid employeeOrUserId, CancellationToken cancellationToken = default);
}
