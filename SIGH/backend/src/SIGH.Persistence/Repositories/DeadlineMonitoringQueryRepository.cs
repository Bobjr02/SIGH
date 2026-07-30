namespace SIGH.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using SIGH.Application.Interfaces.Repositories;
using SIGH.Application.Notifications.DTOs;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.Persistence.Context;

public class DeadlineMonitoringQueryRepository : IDeadlineMonitoringQueryRepository
{
    private readonly SighDbContext _context;

    public DeadlineMonitoringQueryRepository(SighDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DeadlineItemDto>> GetActiveItemsWithDeadlinesAsync(
        Guid? companyId = null,
        CancellationToken cancellationToken = default)
    {
        var activeStatuses = new[]
        {
            DisciplinaryCaseStatus.Draft,
            DisciplinaryCaseStatus.Open,
            DisciplinaryCaseStatus.UnderInvestigation,
            DisciplinaryCaseStatus.AwaitingDecision
        };

        var query = _context.DisciplinaryCases
            .AsNoTracking()
            .Where(c => c.DueDate.HasValue && activeStatuses.Contains(c.Status));

        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(c => c.CompanyId == companyId.Value);
        }

        var cases = await query.ToListAsync(cancellationToken);
        var result = new List<DeadlineItemDto>();

        foreach (var c in cases)
        {
            Guid? responsibleUserId = null;
            Guid? directSupervisorUserId = null;
            Guid? higherSupervisorUserId = null;

            if (c.ResponsibleEmployeeId.HasValue)
            {
                var employee = await _context.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == c.ResponsibleEmployeeId.Value, cancellationToken);

                if (employee != null)
                {
                    responsibleUserId = employee.UserId;

                    if (employee.SupervisorId.HasValue)
                    {
                        var supervisor = await _context.Employees
                            .AsNoTracking()
                            .FirstOrDefaultAsync(e => e.Id == employee.SupervisorId.Value, cancellationToken);

                        if (supervisor != null)
                        {
                            directSupervisorUserId = supervisor.UserId;

                            if (supervisor.SupervisorId.HasValue)
                            {
                                var higherSupervisor = await _context.Employees
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(e => e.Id == supervisor.SupervisorId.Value, cancellationToken);

                                higherSupervisorUserId = higherSupervisor?.UserId;
                            }
                        }
                    }
                }
            }

            // Se responsável não estiver definido ou não possuir UserId vinculado, atribui OpenedByUserId como responsável principal
            responsibleUserId ??= c.OpenedByUserId;

            result.Add(new DeadlineItemDto
            {
                Id = c.Id,
                CompanyId = c.CompanyId,
                SourceEntity = "DisciplinaryCase",
                ReferenceNumber = c.CaseNumber,
                Title = c.Title,
                Status = c.Status.ToString(),
                DueDate = c.DueDate!.Value,
                OpenedByUserId = c.OpenedByUserId,
                ResponsibleEmployeeId = c.ResponsibleEmployeeId,
                ResponsibleUserId = responsibleUserId,
                DirectSupervisorUserId = directSupervisorUserId,
                HigherSupervisorUserId = higherSupervisorUserId
            });
        }

        return result;
    }

    public async Task<Guid?> GetSupervisorUserIdAsync(Guid employeeOrUserId, CancellationToken cancellationToken = default)
    {
        // Tenta buscar o funcionário pelo Id ou pelo UserId
        var employee = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeOrUserId || e.UserId == employeeOrUserId, cancellationToken);

        if (employee?.SupervisorId != null)
        {
            var supervisor = await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == employee.SupervisorId.Value, cancellationToken);

            if (supervisor?.UserId != null)
            {
                return supervisor.UserId;
            }
        }

        return null;
    }
}
