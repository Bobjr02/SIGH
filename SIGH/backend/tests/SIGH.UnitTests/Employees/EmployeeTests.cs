using FluentAssertions;
using SIGH.Domain.Employees.Entities;
using SIGH.Domain.Employees.Enums;
using SIGH.Domain.Exceptions;
using Xunit;

namespace SIGH.UnitTests.Employees;

public class EmployeeTests
{
    private readonly Guid _validCompanyId = Guid.NewGuid();
    private readonly Guid _validJobTitleId = Guid.NewGuid();
    private readonly Guid _validMgmtUnitId = Guid.NewGuid();
    private const string ValidCpf = "11144477735";
    private readonly DateOnly _validAdmissionDate = new(2025, 1, 15);

    [Fact]
    public void Create_WithValidParameters_PendingAdmission_ShouldSucceed()
    {
        var employee = Employee.Create(
            companyId: _validCompanyId,
            employeeNumber: " EMP-1001 ",
            fullName: " Carlos Eduardo Silva ",
            cpf: "111.444.777-35",
            admissionDate: _validAdmissionDate,
            jobTitleId: _validJobTitleId,
            managementUnitId: _validMgmtUnitId,
            initialStatus: EmployeeStatus.PendingAdmission,
            corporateEmail: "carlos.silva@sigh.com.br"
        );

        employee.Should().NotBeNull();
        employee.CompanyId.Should().Be(_validCompanyId);
        employee.EmployeeNumber.Should().Be("EMP-1001");
        employee.FullName.Should().Be("Carlos Eduardo Silva");
        employee.Cpf.Should().Be("11144477735");
        employee.AdmissionDate.Should().Be(_validAdmissionDate);
        employee.Status.Should().Be(EmployeeStatus.PendingAdmission);
        employee.CorporateEmail.Should().Be("carlos.silva@sigh.com.br");
        employee.HasSystemAccess.Should().BeFalse();
    }

    [Fact]
    public void Create_WithValidParameters_Active_ShouldSucceed()
    {
        var employee = Employee.Create(
            companyId: _validCompanyId,
            employeeNumber: "EMP-1002",
            fullName: "Ana Maria Souza",
            cpf: ValidCpf,
            admissionDate: _validAdmissionDate,
            jobTitleId: _validJobTitleId,
            managementUnitId: _validMgmtUnitId,
            initialStatus: EmployeeStatus.Active
        );

        employee.Status.Should().Be(EmployeeStatus.Active);
    }

    [Theory]
    [InlineData(EmployeeStatus.OnLeave)]
    [InlineData(EmployeeStatus.Inactive)]
    [InlineData(EmployeeStatus.Terminated)]
    public void Create_WithInvalidInitialStatus_ShouldThrowBusinessRuleValidationException(EmployeeStatus invalidStatus)
    {
        var act = () => Employee.Create(
            companyId: _validCompanyId,
            employeeNumber: "EMP-1003",
            fullName: "Roberto Lima",
            cpf: ValidCpf,
            admissionDate: _validAdmissionDate,
            jobTitleId: _validJobTitleId,
            managementUnitId: _validMgmtUnitId,
            initialStatus: invalidStatus
        );

        act.Should().Throw<BusinessRuleValidationException>()
           .WithMessage("Status inicial inválido para o cadastro do funcionário.");
    }

    [Fact]
    public void Create_WithEmptyCompanyId_ShouldThrowBusinessRuleValidationException()
    {
        var act = () => Employee.Create(
            companyId: Guid.Empty,
            employeeNumber: "EMP-1004",
            fullName: "Roberto Lima",
            cpf: ValidCpf,
            admissionDate: _validAdmissionDate,
            jobTitleId: _validJobTitleId,
            managementUnitId: _validMgmtUnitId
        );

        act.Should().Throw<BusinessRuleValidationException>()
           .WithMessage("O ID da empresa é obrigatório.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyEmployeeNumber_ShouldThrowBusinessRuleValidationException(string? invalidNumber)
    {
        var act = () => Employee.Create(
            companyId: _validCompanyId,
            employeeNumber: invalidNumber!,
            fullName: "Roberto Lima",
            cpf: ValidCpf,
            admissionDate: _validAdmissionDate,
            jobTitleId: _validJobTitleId,
            managementUnitId: _validMgmtUnitId
        );

        act.Should().Throw<BusinessRuleValidationException>()
           .WithMessage("A matrícula do funcionário é obrigatória.");
    }

    [Fact]
    public void Create_WithEmployeeNumberExceeding20Chars_ShouldThrowBusinessRuleValidationException()
    {
        var longNumber = new string('9', 21);

        var act = () => Employee.Create(
            companyId: _validCompanyId,
            employeeNumber: longNumber,
            fullName: "Roberto Lima",
            cpf: ValidCpf,
            admissionDate: _validAdmissionDate,
            jobTitleId: _validJobTitleId,
            managementUnitId: _validMgmtUnitId
        );

        act.Should().Throw<BusinessRuleValidationException>()
           .WithMessage("A matrícula deve ter no máximo 20 caracteres.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyFullName_ShouldThrowBusinessRuleValidationException(string? invalidName)
    {
        var act = () => Employee.Create(
            companyId: _validCompanyId,
            employeeNumber: "EMP-1005",
            fullName: invalidName!,
            cpf: ValidCpf,
            admissionDate: _validAdmissionDate,
            jobTitleId: _validJobTitleId,
            managementUnitId: _validMgmtUnitId
        );

        act.Should().Throw<BusinessRuleValidationException>()
           .WithMessage("O nome completo do funcionário é obrigatório.");
    }

    [Fact]
    public void Create_WithFullNameExceeding150Chars_ShouldThrowBusinessRuleValidationException()
    {
        var longName = new string('X', 151);

        var act = () => Employee.Create(
            companyId: _validCompanyId,
            employeeNumber: "EMP-1006",
            fullName: longName,
            cpf: ValidCpf,
            admissionDate: _validAdmissionDate,
            jobTitleId: _validJobTitleId,
            managementUnitId: _validMgmtUnitId
        );

        act.Should().Throw<BusinessRuleValidationException>()
           .WithMessage("O nome completo deve ter no máximo 150 caracteres.");
    }

    [Fact]
    public void Create_WithInvalidCpf_ShouldThrowBusinessRuleValidationException()
    {
        var act = () => Employee.Create(
            companyId: _validCompanyId,
            employeeNumber: "EMP-1007",
            fullName: "Fernanda Costa",
            cpf: "12345678900", // Invalid check digits
            admissionDate: _validAdmissionDate,
            jobTitleId: _validJobTitleId,
            managementUnitId: _validMgmtUnitId
        );

        act.Should().Throw<BusinessRuleValidationException>()
           .WithMessage("CPF inválido.");
    }

    [Fact]
    public void Create_WithAllDigitsEqualCpf_ShouldThrowBusinessRuleValidationException()
    {
        var act = () => Employee.Create(
            companyId: _validCompanyId,
            employeeNumber: "EMP-1008",
            fullName: "Fernanda Costa",
            cpf: "11111111111",
            admissionDate: _validAdmissionDate,
            jobTitleId: _validJobTitleId,
            managementUnitId: _validMgmtUnitId
        );

        act.Should().Throw<BusinessRuleValidationException>()
           .WithMessage("CPF inválido.");
    }

    [Fact]
    public void Create_WithInvalidAdmissionDate_ShouldThrowBusinessRuleValidationException()
    {
        var act = () => Employee.Create(
            companyId: _validCompanyId,
            employeeNumber: "EMP-1009",
            fullName: "Fernanda Costa",
            cpf: ValidCpf,
            admissionDate: DateOnly.MinValue,
            jobTitleId: _validJobTitleId,
            managementUnitId: _validMgmtUnitId
        );

        act.Should().Throw<BusinessRuleValidationException>()
           .WithMessage("A data de admissão é obrigatória.");
    }

    [Fact]
    public void Create_WithEmptyJobTitleId_ShouldThrowBusinessRuleValidationException()
    {
        var act = () => Employee.Create(
            companyId: _validCompanyId,
            employeeNumber: "EMP-1010",
            fullName: "Fernanda Costa",
            cpf: ValidCpf,
            admissionDate: _validAdmissionDate,
            jobTitleId: Guid.Empty,
            managementUnitId: _validMgmtUnitId
        );

        act.Should().Throw<BusinessRuleValidationException>()
           .WithMessage("O cargo é obrigatório.");
    }

    [Fact]
    public void Create_WithEmptyManagementUnitId_ShouldThrowBusinessRuleValidationException()
    {
        var act = () => Employee.Create(
            companyId: _validCompanyId,
            employeeNumber: "EMP-1011",
            fullName: "Fernanda Costa",
            cpf: ValidCpf,
            admissionDate: _validAdmissionDate,
            jobTitleId: _validJobTitleId,
            managementUnitId: Guid.Empty
        );

        act.Should().Throw<BusinessRuleValidationException>()
           .WithMessage("A unidade gestora é obrigatória.");
    }

    [Fact]
    public void Create_WithInvalidEmails_ShouldThrowBusinessRuleValidationException()
    {
        var actCorporate = () => Employee.Create(
            companyId: _validCompanyId,
            employeeNumber: "EMP-1012",
            fullName: "Fernanda Costa",
            cpf: ValidCpf,
            admissionDate: _validAdmissionDate,
            jobTitleId: _validJobTitleId,
            managementUnitId: _validMgmtUnitId,
            corporateEmail: "invalid-email"
        );

        actCorporate.Should().Throw<BusinessRuleValidationException>()
                    .WithMessage("E-mail corporativo em formato inválido.");

        var actPersonal = () => Employee.Create(
            companyId: _validCompanyId,
            employeeNumber: "EMP-1013",
            fullName: "Fernanda Costa",
            cpf: ValidCpf,
            admissionDate: _validAdmissionDate,
            jobTitleId: _validJobTitleId,
            managementUnitId: _validMgmtUnitId,
            personalEmail: "invalid-email"
        );

        actPersonal.Should().Throw<BusinessRuleValidationException>()
                   .WithMessage("E-mail pessoal em formato inválido.");
    }

    [Fact]
    public void LinkUser_And_UnlinkUser_ShouldWorkCorrectly()
    {
        var employee = Employee.Create(
            _validCompanyId, "EMP-2001", "Marcos Paulo", ValidCpf, _validAdmissionDate, _validJobTitleId, _validMgmtUnitId);

        var userId = Guid.NewGuid();

        employee.LinkUser(userId);

        employee.UserId.Should().Be(userId);
        employee.HasSystemAccess.Should().BeTrue();

        // Idempotent call with same userId
        employee.LinkUser(userId);
        employee.UserId.Should().Be(userId);

        // Attempting to overwrite with another userId without unlinking first should throw Option B exception
        var secondUserId = Guid.NewGuid();
        var actOverwrite = () => employee.LinkUser(secondUserId);
        actOverwrite.Should().Throw<BusinessRuleValidationException>()
                    .WithMessage("Para vincular outro usuário, é necessário desvincular o usuário atual primeiro.");

        // Unlink
        employee.UnlinkUser();
        employee.UserId.Should().BeNull();
        employee.HasSystemAccess.Should().BeFalse();
    }

    [Fact]
    public void LinkUser_WithEmptyGuid_ShouldThrowBusinessRuleValidationException()
    {
        var employee = Employee.Create(
            _validCompanyId, "EMP-2002", "Marcos Paulo", ValidCpf, _validAdmissionDate, _validJobTitleId, _validMgmtUnitId);

        var act = () => employee.LinkUser(Guid.Empty);

        act.Should().Throw<BusinessRuleValidationException>()
           .WithMessage("O ID do usuário é obrigatório para vínculo.");
    }

    [Fact]
    public void ChangeSupervisor_WithSelfId_ShouldThrowBusinessRuleValidationException()
    {
        var employeeId = Guid.NewGuid();
        var employee = new Employee(employeeId);

        var act = () => employee.ChangeSupervisor(employeeId);

        act.Should().Throw<BusinessRuleValidationException>()
           .WithMessage("O funcionário não pode ser gestor de si mesmo.");
    }

    [Fact]
    public void Transitions_PendingAdmission_To_Active_And_Inactive_ShouldSucceed()
    {
        var employee = Employee.Create(
            _validCompanyId, "EMP-3001", "Juliana Paes", ValidCpf, _validAdmissionDate, _validJobTitleId, _validMgmtUnitId, EmployeeStatus.PendingAdmission);

        employee.Activate();
        employee.Status.Should().Be(EmployeeStatus.Active);

        employee.SetInactive();
        employee.Status.Should().Be(EmployeeStatus.Inactive);
    }

    [Fact]
    public void Transitions_PendingAdmission_To_OnLeave_And_Terminated_ShouldFail()
    {
        var emp1 = Employee.Create(
            _validCompanyId, "EMP-3002", "Juliana Paes", ValidCpf, _validAdmissionDate, _validJobTitleId, _validMgmtUnitId, EmployeeStatus.PendingAdmission);

        var actOnLeave = () => emp1.SetOnLeave();
        actOnLeave.Should().Throw<BusinessRuleValidationException>()
                  .WithMessage("Apenas funcionários com status Ativo podem ser colocados em afastamento.");

        var emp2 = Employee.Create(
            _validCompanyId, "EMP-3003", "Juliana Paes", ValidCpf, _validAdmissionDate, _validJobTitleId, _validMgmtUnitId, EmployeeStatus.PendingAdmission);

        var actTerminate = () => emp2.Terminate(new DateOnly(2025, 2, 1), "Desistência");
        actTerminate.Should().Throw<BusinessRuleValidationException>()
                    .WithMessage("Funcionário pendente de admissão não pode ser desligado diretamente.");
    }

    [Fact]
    public void Transitions_Active_To_OnLeave_Inactive_And_Terminated_ShouldSucceed()
    {
        var emp1 = Employee.Create(
            _validCompanyId, "EMP-4001", "Lucas Mendes", ValidCpf, _validAdmissionDate, _validJobTitleId, _validMgmtUnitId, EmployeeStatus.Active);

        emp1.SetOnLeave();
        emp1.Status.Should().Be(EmployeeStatus.OnLeave);

        emp1.Activate();
        emp1.Status.Should().Be(EmployeeStatus.Active);

        emp1.SetInactive();
        emp1.Status.Should().Be(EmployeeStatus.Inactive);

        emp1.Activate();
        emp1.Status.Should().Be(EmployeeStatus.Active);

        var termDate = new DateOnly(2025, 6, 30);
        emp1.Terminate(termDate, "Pedido de demissão");
        emp1.Status.Should().Be(EmployeeStatus.Terminated);
        emp1.TerminationDate.Should().Be(termDate);
        emp1.TerminationReason.Should().Be("Pedido de demissão");
    }

    [Fact]
    public void Terminate_WithInvalidDateOrEmptyReason_ShouldThrowBusinessRuleValidationException()
    {
        var employee = Employee.Create(
            _validCompanyId, "EMP-5001", "Patricia Abravanel", ValidCpf, new DateOnly(2025, 5, 1), _validJobTitleId, _validMgmtUnitId, EmployeeStatus.Active);

        // TerminationDate prior to AdmissionDate
        var actPriorDate = () => employee.Terminate(new DateOnly(2025, 4, 30), "Motivo qualquer");
        actPriorDate.Should().Throw<BusinessRuleValidationException>()
                    .WithMessage("Data de desligamento não pode ser anterior à data de admissão.");

        // Empty reason
        var actEmptyReason = () => employee.Terminate(new DateOnly(2025, 5, 10), "   ");
        actEmptyReason.Should().Throw<BusinessRuleValidationException>()
                      .WithMessage("O motivo do desligamento é obrigatório.");

        // Reason exceeding 500 chars
        var longReason = new string('R', 501);
        var actLongReason = () => employee.Terminate(new DateOnly(2025, 5, 10), longReason);
        actLongReason.Should().Throw<BusinessRuleValidationException>()
                     .WithMessage("O motivo do desligamento deve ter no máximo 500 caracteres.");
    }

    [Fact]
    public void Terminated_CannotUseActivate_Or_SetOnLeave_Or_SetInactive()
    {
        var employee = Employee.Create(
            _validCompanyId, "EMP-6001", "Tiago Leifert", ValidCpf, _validAdmissionDate, _validJobTitleId, _validMgmtUnitId, EmployeeStatus.Active);

        employee.Terminate(new DateOnly(2025, 3, 1), "Acordo das partes");

        var actActivate = () => employee.Activate();
        actActivate.Should().Throw<BusinessRuleValidationException>()
                   .WithMessage("Para reativar um funcionário desligado, utilize o método Reativar.");

        var actOnLeave = () => employee.SetOnLeave();
        actOnLeave.Should().Throw<BusinessRuleValidationException>()
                  .WithMessage("Apenas funcionários com status Ativo podem ser colocados em afastamento.");

        var actInactive = () => employee.SetInactive();
        actInactive.Should().Throw<BusinessRuleValidationException>()
                   .WithMessage("Funcionário desligado não pode ser alterado para Inativo.");
    }

    [Fact]
    public void Reactivate_TerminatedEmployee_ShouldSucceedAndClearTerminationData()
    {
        var employee = Employee.Create(
            _validCompanyId, "EMP-7001", "William Bonner", ValidCpf, new DateOnly(2020, 1, 1), _validJobTitleId, _validMgmtUnitId, EmployeeStatus.Active);

        employee.Terminate(new DateOnly(2024, 12, 31), "Aposentadoria");

        var newAdmissionDate = new DateOnly(2025, 2, 1);
        employee.Reactivate(newAdmissionDate);

        employee.Status.Should().Be(EmployeeStatus.Active);
        employee.AdmissionDate.Should().Be(newAdmissionDate);
        employee.TerminationDate.Should().BeNull();
        employee.TerminationReason.Should().BeNull();
    }

    [Fact]
    public void Reactivate_NonTerminatedEmployee_ShouldThrowBusinessRuleValidationException()
    {
        var employee = Employee.Create(
            _validCompanyId, "EMP-7002", "Renata Vasconcellos", ValidCpf, _validAdmissionDate, _validJobTitleId, _validMgmtUnitId, EmployeeStatus.Active);

        var act = () => employee.Reactivate(new DateOnly(2025, 6, 1));

        act.Should().Throw<BusinessRuleValidationException>()
           .WithMessage("Apenas funcionários desligados podem ser reativados.");
    }

    [Fact]
    public void Reactivate_WithNewAdmissionDatePriorToOldAdmissionDate_ShouldThrowBusinessRuleValidationException()
    {
        var employee = Employee.Create(
            _validCompanyId, "EMP-7003", "César Tralli", ValidCpf, new DateOnly(2022, 1, 1), _validJobTitleId, _validMgmtUnitId, EmployeeStatus.Active);

        employee.Terminate(new DateOnly(2023, 1, 1), "Rescisão");

        var act = () => employee.Reactivate(new DateOnly(2021, 1, 1));

        act.Should().Throw<BusinessRuleValidationException>()
           .WithMessage("A nova data de admissão não pode ser anterior à admissão anterior.");
    }

    [Fact]
    public void UpdatePersonalData_And_UpdateCorporateData_ShouldSucceed()
    {
        var employee = Employee.Create(
            _validCompanyId, "EMP-8001", "Gabriel Medina", ValidCpf, _validAdmissionDate, _validJobTitleId, _validMgmtUnitId);

        employee.UpdatePersonalData(
            fullName: "Gabriel Medina Pinto Ferreira",
            socialName: "Medina",
            birthDate: new DateOnly(1993, 12, 22),
            personalEmail: "gabriel.medina@email.com",
            mobileNumber: "11999998888",
            notes: "Atleta de surf"
        );

        employee.FullName.Should().Be("Gabriel Medina Pinto Ferreira");
        employee.SocialName.Should().Be("Medina");
        employee.BirthDate.Should().Be(new DateOnly(1993, 12, 22));
        employee.PersonalEmail.Should().Be("gabriel.medina@email.com");
        employee.MobileNumber.Should().Be("11999998888");
        employee.Notes.Should().Be("Atleta de surf");

        var newJobTitleId = Guid.NewGuid();
        var newMgmtUnitId = Guid.NewGuid();
        var newDeptId = Guid.NewGuid();

        employee.UpdateCorporateData("gabriel.medina@sigh.com.br", newJobTitleId, newMgmtUnitId, newDeptId);

        employee.CorporateEmail.Should().Be("gabriel.medina@sigh.com.br");
        employee.JobTitleId.Should().Be(newJobTitleId);
        employee.ManagementUnitId.Should().Be(newMgmtUnitId);
        employee.DepartmentId.Should().Be(newDeptId);
    }
}
