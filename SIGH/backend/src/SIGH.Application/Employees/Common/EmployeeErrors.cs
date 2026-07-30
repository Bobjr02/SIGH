namespace SIGH.Application.Employees.Common;

public static class EmployeeErrors
{
    public const string NotFound = "Employee.NotFound";
    public const string CpfAlreadyExists = "Employee.CpfAlreadyExists";
    public const string EmployeeNumberAlreadyExists = "Employee.EmployeeNumberAlreadyExists";
    public const string CorporateEmailAlreadyExists = "Employee.CorporateEmailAlreadyExists";
    public const string UserAlreadyLinked = "Employee.UserAlreadyLinked";
    public const string InvalidStatusTransition = "Employee.InvalidStatusTransition";
    public const string InvalidData = "Employee.InvalidData";
    public const string CompanyNotFound = "Employee.CompanyNotFound";
    public const string JobTitleNotFound = "Employee.JobTitleNotFound";
    public const string ManagementUnitNotFound = "Employee.ManagementUnitNotFound";
    public const string DepartmentNotFound = "Employee.DepartmentNotFound";
    public const string SupervisorNotFound = "Employee.SupervisorNotFound";
    public const string SupervisorFromAnotherCompany = "Employee.SupervisorFromAnotherCompany";
    public const string UserNotFound = "Employee.UserNotFound";
    public const string InactiveCompany = "Employee.InactiveCompany";
    public const string InactiveJobTitle = "Employee.InactiveJobTitle";
    public const string InactiveManagementUnit = "Employee.InactiveManagementUnit";
    public const string InactiveDepartment = "Employee.InactiveDepartment";
}
