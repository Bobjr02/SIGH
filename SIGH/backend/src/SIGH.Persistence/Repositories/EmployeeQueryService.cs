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

    public EmployeeQueryService(SighDbContext context)
    {
        _context = context;
    }

    public async Task<GetEmployeeByIdResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var employee = await (from e in _context.Employees
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
                                  e.DepartmentId,
                                  DepartmentName = d != null ? d.Name : null,
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
            employee.UpdatedAt
        );
    }

    public async Task<PagedResult<EmployeeListItemResponse>> SearchAsync(GetEmployeesQuery query, CancellationToken cancellationToken = default)
    {
        IQueryable<EmployeeQueryRow> baseQuery =
            from e in _context.Employees
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
            select new EmployeeQueryRow(
                e,
                c != null ? c.Name : null,
                j != null ? j.Name : null,
                m != null ? m.Name : null,
                d != null ? d.Name : null,
                s != null ? s.FullName : null);

        if (query.CompanyId.HasValue)
            baseQuery = baseQuery.Where(x => x.Employee.CompanyId == query.CompanyId.Value);

        if (query.ManagementUnitId.HasValue)
            baseQuery = baseQuery.Where(x => x.Employee.ManagementUnitId == query.ManagementUnitId.Value);

        if (query.DepartmentId.HasValue)
            baseQuery = baseQuery.Where(x => x.Employee.DepartmentId == query.DepartmentId.Value);

        if (query.JobTitleId.HasValue)
            baseQuery = baseQuery.Where(x => x.Employee.JobTitleId == query.JobTitleId.Value);

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
                x.Employee.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<EmployeeListItemResponse>(items, pageNumber, pageSize, totalCount);
    }

    private sealed record EmployeeQueryRow(
        Employee Employee,
        string? CompanyName,
        string? JobTitleName,
        string? ManagementUnitName,
        string? DepartmentName,
        string? SupervisorName);
}
