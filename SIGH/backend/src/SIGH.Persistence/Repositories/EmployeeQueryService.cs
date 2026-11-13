using SIGH.Application.Common.Authorization;
using Microsoft.EntityFrameworkCore;
using SIGH.Application.Common.Models;
using SIGH.Application.Employees.Common;
using SIGH.Application.Employees.GetEmployeeById;
using SIGH.Application.Employees.GetEmployees;
using SIGH.Domain.Employees.Entities;
using SIGH.Persistence.Context;

namespace SIGH.Persistence.Repositories;

public class EmployeeQueryService : IEmployeeQueryService
{
    private readonly SighDbContext _context;
    private readonly IAccessScope _scope;

    public EmployeeQueryService(SighDbContext context, IAccessScope scope)
    {
        _context = context;
        _scope = scope;
    }

    /// <summary>Funcionários no escopo do usuário (gerências vigentes, ou todos com ViewAll).</summary>
    private async Task<IQueryable<Employee>> ScopedEmployeesAsync(CancellationToken cancellationToken) =>
        _context.Employees.InScope(await _scope.GetFilterAsync(cancellationToken));

    public async Task<GetEmployeeByIdResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var scoped = await ScopedEmployeesAsync(cancellationToken);
        var employee = await (from e in scoped
                              where e.Id == id
                              join c in _context.Companies on e.CompanyId equals c.Id into compJoin
                              from c in compJoin.DefaultIfEmpty()
                              join j in _context.JobTitles on e.JobTitleId equals j.Id into jobJoin
                              from j in jobJoin.DefaultIfEmpty()
                              join m in _context.ManagementUnits on e.ManagementUnitId equals m.Id into muJoin
                              from m in muJoin.DefaultIfEmpty()
                              join d in _context.Departments on e.DepartmentId equals d.Id into deptJoin
                              from d in deptJoin.DefaultIfEmpty()
                              join s in _context.Employees on e.SupervisorId equals s.Id into supJoin
                              from s in supJoin.DefaultIfEmpty()
                              select new
                              {
                                  e.Id,
                                  e.CompanyId,
                                  CompanyName = c != null ? c.Name : null,
                                  e.EmployeeNumber,
                                  e.FullName,
                                  e.SocialName,
                                  e.Cpf,
                                  e.AdmissionDate,
                                  e.BirthDate,
                                  e.Status,
                                  e.JobTitleId,
                                  JobTitleName = j != null ? j.Name : null,
                                  e.ManagementUnitId,
                                  ManagementUnitName = m != null ? m.Name : null,
                                  ManagementUnitCode = m != null ? m.Code : null,
                                  e.DepartmentId,
                                  DepartmentName = d != null ? d.Name : null,
                                  DepartmentCode = d != null ? d.Code : null,
                                  JobTitleCode = j != null ? j.Code : null,
                                  e.Group,
                                  e.Team,
                                  e.SupervisorId,
                                  SupervisorName = s != null ? s.FullName : null,
                                  e.UserId,
                                  e.HasSystemAccess,
                                  e.CorporateEmail,
                                  e.PersonalEmail,
                                  e.MobileNumber,
                                  e.TerminationDate,
                                  e.TerminationReason,
                                  e.Notes,
                                  e.CreatedAt,
                                  e.UpdatedAt
                              })
                              .AsNoTracking()
                              .FirstOrDefaultAsync(cancellationToken);

        if (employee == null)
            return null;

        return new GetEmployeeByIdResponse(
            employee.Id,
            employee.CompanyId,
            employee.CompanyName,
            employee.EmployeeNumber,
            employee.FullName,
            employee.SocialName,
            CpfMasker.Mask(employee.Cpf),
            employee.AdmissionDate,
            employee.BirthDate,
            employee.Status,
            employee.JobTitleId,
            employee.JobTitleName,
            employee.ManagementUnitId,
            employee.ManagementUnitName,
            employee.DepartmentId,
            employee.DepartmentName,
            employee.SupervisorId,
            employee.SupervisorName,
            employee.UserId,
            employee.HasSystemAccess,
            employee.CorporateEmail,
            employee.PersonalEmail,
            employee.MobileNumber,
            employee.TerminationDate,
            employee.TerminationReason,
            employee.Notes,
            employee.CreatedAt,
            employee.UpdatedAt,
            employee.Cpf,
            employee.Group,
            employee.Team,
            employee.ManagementUnitCode,
            employee.DepartmentCode,
            employee.JobTitleCode
        );
    }

    public async Task<PagedResult<EmployeeListItemResponse>> SearchAsync(GetEmployeesQuery query, CancellationToken cancellationToken = default)
    {
        var scoped = await ScopedEmployeesAsync(cancellationToken);
        IQueryable<EmployeeQueryRow> baseQuery =
            from e in scoped
            join c in _context.Companies on e.CompanyId equals c.Id into compJoin
            from c in compJoin.DefaultIfEmpty()
            join j in _context.JobTitles on e.JobTitleId equals j.Id into jobJoin
            from j in jobJoin.DefaultIfEmpty()
            join m in _context.ManagementUnits on e.ManagementUnitId equals m.Id into muJoin
            from m in muJoin.DefaultIfEmpty()
            join d in _context.Departments on e.DepartmentId equals d.Id into deptJoin
            from d in deptJoin.DefaultIfEmpty()
            join s in _context.Employees on e.SupervisorId equals s.Id into supJoin
            from s in supJoin.DefaultIfEmpty()
            // Inicializador de objeto (não construtor): o EF só traduz filtros e ordenações sobre
            // membros atribuídos assim.
            select new EmployeeQueryRow
            {
                Employee = e,
                CompanyName = c != null ? c.Name : null,
                JobTitleName = j != null ? j.Name : null,
                ManagementUnitName = m != null ? m.Name : null,
                DepartmentName = d != null ? d.Name : null,
                SupervisorName = s != null ? s.FullName : null,
                ManagementUnitCode = m != null ? m.Code : null,
                DepartmentCode = d != null ? d.Code : null
            };

        if (query.CompanyId.HasValue)
            baseQuery = baseQuery.Where(x => x.Employee.CompanyId == query.CompanyId.Value);

        if (query.ManagementUnitId.HasValue)
            baseQuery = baseQuery.Where(x => x.Employee.ManagementUnitId == query.ManagementUnitId.Value);

        if (query.DepartmentId.HasValue)
            baseQuery = baseQuery.Where(x => x.Employee.DepartmentId == query.DepartmentId.Value);

        if (query.JobTitleId.HasValue)
            baseQuery = baseQuery.Where(x => x.Employee.JobTitleId == query.JobTitleId.Value);

        if (query.Group.HasValue)
            baseQuery = baseQuery.Where(x => x.Employee.Group == query.Group.Value);

        if (query.Status.HasValue)
            baseQuery = baseQuery.Where(x => x.Employee.Status == query.Status.Value);

        if (query.HasSystemAccess.HasValue)
            baseQuery = baseQuery.Where(x => (x.Employee.UserId != null) == query.HasSystemAccess.Value);

        if (query.AdmissionDateFrom.HasValue)
            baseQuery = baseQuery.Where(x => x.Employee.AdmissionDate >= query.AdmissionDateFrom.Value);

        if (query.AdmissionDateTo.HasValue)
            baseQuery = baseQuery.Where(x => x.Employee.AdmissionDate <= query.AdmissionDateTo.Value);

        if (query.SupervisorId.HasValue)
            baseQuery = baseQuery.Where(x => x.Employee.SupervisorId == query.SupervisorId.Value);

        var searchTerm = query.EffectiveSearchTerm;
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            baseQuery = baseQuery.Where(x =>
                x.Employee.FullName.ToLower().Contains(term) ||
                (x.Employee.SocialName != null && x.Employee.SocialName.ToLower().Contains(term)) ||
                x.Employee.EmployeeNumber.ToLower().Contains(term) ||
                (x.Employee.CorporateEmail != null && x.Employee.CorporateEmail.ToLower().Contains(term))
            );
        }

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        // Sorting
        var sortBy = query.SortBy?.Trim().ToLowerInvariant();
        var isDescending = query.SortDescending;

        baseQuery = (sortBy, isDescending) switch
        {
            ("employeenumber", false) => baseQuery.OrderBy(x => x.Employee.EmployeeNumber),
            ("employeenumber", true) => baseQuery.OrderByDescending(x => x.Employee.EmployeeNumber),
            ("admissiondate", false) => baseQuery.OrderBy(x => x.Employee.AdmissionDate),
            ("admissiondate", true) => baseQuery.OrderByDescending(x => x.Employee.AdmissionDate),
            ("status", false) => baseQuery.OrderBy(x => x.Employee.Status),
            ("status", true) => baseQuery.OrderByDescending(x => x.Employee.Status),
            ("createdat", false) => baseQuery.OrderBy(x => x.Employee.CreatedAt),
            ("createdat", true) => baseQuery.OrderByDescending(x => x.Employee.CreatedAt),
            ("managementunitname", false) => baseQuery.OrderBy(x => x.ManagementUnitName),
            ("managementunitname", true) => baseQuery.OrderByDescending(x => x.ManagementUnitName),
            ("jobtitlename", false) => baseQuery.OrderBy(x => x.JobTitleName),
            ("jobtitlename", true) => baseQuery.OrderByDescending(x => x.JobTitleName),
            (_, true) => baseQuery.OrderByDescending(x => x.Employee.FullName),
            _ => baseQuery.OrderBy(x => x.Employee.FullName)
        };

        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize < 1 ? 20 : (query.PageSize > 100 ? 100 : query.PageSize);

        var items = await baseQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .Select(x => new EmployeeListItemResponse(
                x.Employee.Id,
                x.Employee.CompanyId,
                x.CompanyName,
                x.Employee.EmployeeNumber,
                x.Employee.FullName,
                x.Employee.SocialName,
                CpfMasker.Mask(x.Employee.Cpf),
                x.Employee.AdmissionDate,
                x.Employee.Status,
                x.Employee.JobTitleId,
                x.JobTitleName,
                x.Employee.ManagementUnitId,
                x.ManagementUnitName,
                x.Employee.DepartmentId,
                x.DepartmentName,
                x.Employee.SupervisorId,
                x.SupervisorName,
                x.Employee.UserId,
                x.Employee.HasSystemAccess,
                x.Employee.CorporateEmail,
                x.Employee.CreatedAt,
                x.Employee.Group,
                x.Employee.Team,
                x.ManagementUnitCode,
                x.DepartmentCode
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<EmployeeListItemResponse>(items, pageNumber, pageSize, totalCount);
    }

    public async Task<EmployeeFilterOptionsDto> GetFilterOptionsAsync(CancellationToken cancellationToken = default)
    {
        // Máscara do escopo: o gestor não vê as gerências (nem os setores) fora do escopo nos filtros.
        var scope = await _scope.GetFilterAsync(cancellationToken);
        var unitIds = scope.ManagementUnitIds.ToList();
        var units = await _context.ManagementUnits.AsNoTracking().Where(m => m.IsActive && (scope.All || unitIds.Contains(m.Id)))
            .OrderBy(m => m.Code).ThenBy(m => m.Name)
            .Select(m => new OrganizationOptionDto(m.Id, m.Code, m.Name, null))
            .ToListAsync(cancellationToken);
        var departments = await _context.Departments.AsNoTracking().Where(d => d.IsActive && (scope.All || unitIds.Contains(d.ManagementUnitId)))
            .OrderBy(d => d.Name)
            .Select(d => new OrganizationOptionDto(d.Id, d.Code, d.Name, d.ManagementUnitId))
            .ToListAsync(cancellationToken);
        return new EmployeeFilterOptionsDto(units, departments);
    }

    private sealed class EmployeeQueryRow
    {
        public Employee Employee { get; init; } = null!;
        public string? CompanyName { get; init; }
        public string? JobTitleName { get; init; }
        public string? ManagementUnitName { get; init; }
        public string? DepartmentName { get; init; }
        public string? SupervisorName { get; init; }
        public string? ManagementUnitCode { get; init; }
        public string? DepartmentCode { get; init; }
    }
}
