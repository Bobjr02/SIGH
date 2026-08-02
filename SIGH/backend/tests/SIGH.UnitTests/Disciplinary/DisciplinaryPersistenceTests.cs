using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SIGH.Application.Interfaces.Repositories.Disciplinary;
using SIGH.Domain.Disciplinary.Constants;
using SIGH.Domain.Disciplinary.Entities;
using SIGH.Domain.Disciplinary.Enums;
using SIGH.Persistence.Context;
using SIGH.Persistence.Repositories.Disciplinary;
using Xunit;

namespace SIGH.UnitTests.Disciplinary;

public class DisciplinaryPersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<SighDbContext> _options;

    public DisciplinaryPersistenceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<SighDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new SighDbContext(_options);
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }

    private SighDbContext CreateDbContext()
    {
        return new SighDbContext(_options);
    }

    [Fact]
    public void Model_ShouldRegisterAllSevenDisciplinaryEntities()
    {
        using var context = CreateDbContext();
        var model = context.Model;

        model.FindEntityType(typeof(DisciplinaryCase)).Should().NotBeNull();
        model.FindEntityType(typeof(DisciplinaryOccurrence)).Should().NotBeNull();
        model.FindEntityType(typeof(DisciplinaryCaseEmployee)).Should().NotBeNull();
        model.FindEntityType(typeof(DisciplinaryEvidence)).Should().NotBeNull();
        model.FindEntityType(typeof(DisciplinaryDecision)).Should().NotBeNull();
        model.FindEntityType(typeof(DisciplinaryMeasure)).Should().NotBeNull();
        model.FindEntityType(typeof(InfractionType)).Should().NotBeNull();
    }

    [Fact]
    public void Model_ShouldConfigureCorrectTableNames()
    {
        using var context = CreateDbContext();
        var model = context.Model;

        model.FindEntityType(typeof(DisciplinaryCase))!.GetTableName().Should().Be("DisciplinaryCases");
        model.FindEntityType(typeof(DisciplinaryOccurrence))!.GetTableName().Should().Be("DisciplinaryOccurrences");
        model.FindEntityType(typeof(DisciplinaryCaseEmployee))!.GetTableName().Should().Be("DisciplinaryCaseEmployees");
        model.FindEntityType(typeof(DisciplinaryEvidence))!.GetTableName().Should().Be("DisciplinaryEvidences");
        model.FindEntityType(typeof(DisciplinaryDecision))!.GetTableName().Should().Be("DisciplinaryDecisions");
        model.FindEntityType(typeof(DisciplinaryMeasure))!.GetTableName().Should().Be("DisciplinaryMeasures");
        model.FindEntityType(typeof(InfractionType))!.GetTableName().Should().Be("InfractionTypes");
    }

    [Fact]
    public void DisciplinaryCase_ShouldIgnoreCalculatedProperties()
    {
        using var context = CreateDbContext();
        var entityType = context.Model.FindEntityType(typeof(DisciplinaryCase))!;

        entityType.FindProperty(nameof(DisciplinaryCase.Decision)).Should().BeNull();
        entityType.FindProperty(nameof(DisciplinaryCase.DecisionId)).Should().BeNull();
    }

    [Fact]
    public void DisciplinaryCase_ShouldHaveCorrectPropertyConfigurations()
    {
        using var context = CreateDbContext();
        var entityType = context.Model.FindEntityType(typeof(DisciplinaryCase))!;

        var caseNumberProp = entityType.FindProperty(nameof(DisciplinaryCase.CaseNumber))!;
        caseNumberProp.IsNullable.Should().BeFalse();
        caseNumberProp.GetMaxLength().Should().Be(DisciplinaryDomainConstants.CaseNumberMaxLength);

        var titleProp = entityType.FindProperty(nameof(DisciplinaryCase.Title))!;
        titleProp.IsNullable.Should().BeFalse();
        titleProp.GetMaxLength().Should().Be(DisciplinaryDomainConstants.TitleMaxLength);

        var descriptionProp = entityType.FindProperty(nameof(DisciplinaryCase.Description))!;
        descriptionProp.IsNullable.Should().BeFalse();
        descriptionProp.GetMaxLength().Should().Be(DisciplinaryDomainConstants.DescriptionMaxLength);
    }

    [Fact]
    public void Model_ShouldConfigureBackingFieldsForDisciplinaryCaseCollections()
    {
        using var context = CreateDbContext();
        var entityType = context.Model.FindEntityType(typeof(DisciplinaryCase))!;

        entityType.FindNavigation(nameof(DisciplinaryCase.Occurrences))!.PropertyAccessMode.Should().Be(PropertyAccessMode.Field);
        entityType.FindNavigation(nameof(DisciplinaryCase.Employees))!.PropertyAccessMode.Should().Be(PropertyAccessMode.Field);
        entityType.FindNavigation(nameof(DisciplinaryCase.Evidences))!.PropertyAccessMode.Should().Be(PropertyAccessMode.Field);
        entityType.FindNavigation(nameof(DisciplinaryCase.Decisions))!.PropertyAccessMode.Should().Be(PropertyAccessMode.Field);
        entityType.FindNavigation(nameof(DisciplinaryCase.Measures))!.PropertyAccessMode.Should().Be(PropertyAccessMode.Field);
    }

    [Fact]
    public void Model_ShouldConfigureEnumConversionsAsInt()
    {
        using var context = CreateDbContext();
        var caseType = context.Model.FindEntityType(typeof(DisciplinaryCase))!;

        var statusProp = caseType.FindProperty(nameof(DisciplinaryCase.Status))!;
        statusProp.GetValueConverter()!.ProviderClrType.Should().Be(typeof(int));

        var priorityProp = caseType.FindProperty(nameof(DisciplinaryCase.Priority))!;
        priorityProp.GetValueConverter()!.ProviderClrType.Should().Be(typeof(int));
    }

    [Fact]
    public async Task DisciplinaryCase_ShouldPersistAndRetrieveCorrectly()
    {
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var caseObj = DisciplinaryCase.Create("PROC-2026-001", companyId, "Título do Processo", "Descrição detalhada do caso", userId, now);

        using (var context = CreateDbContext())
        {
            var repo = new DisciplinaryCaseRepository(context);
            await repo.AddAsync(caseObj);
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext())
        {
            var repo = new DisciplinaryCaseRepository(context);
            var retrieved = await repo.GetByIdAsync(caseObj.Id);

            retrieved.Should().NotBeNull();
            retrieved!.CaseNumber.Should().Be("PROC-2026-001");
            retrieved.CompanyId.Should().Be(companyId);
            retrieved.Title.Should().Be("Título do Processo");
            retrieved.Status.Should().Be(DisciplinaryCaseStatus.Draft);
        }
    }

    [Fact]
    public async Task DisciplinaryCase_SameCaseNumberInDifferentCompanies_ShouldBeAllowed()
    {
        var company1 = Guid.NewGuid();
        var company2 = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var case1 = DisciplinaryCase.Create("PROC-DUP-01", company1, "Caso Empresa 1", "Descrição 1", userId, now);
        var case2 = DisciplinaryCase.Create("PROC-DUP-01", company2, "Caso Empresa 2", "Descrição 2", userId, now);

        using (var context = CreateDbContext())
        {
            var repo = new DisciplinaryCaseRepository(context);
            await repo.AddAsync(case1);
            await repo.AddAsync(case2);
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext())
        {
            var repo = new DisciplinaryCaseRepository(context);
            var exists1 = await repo.ExistsByCaseNumberAsync(company1, "PROC-DUP-01");
            var exists2 = await repo.ExistsByCaseNumberAsync(company2, "PROC-DUP-01");

            exists1.Should().BeTrue();
            exists2.Should().BeTrue();
        }
    }

    [Fact]
    public async Task InfractionType_ShouldPersistAndQueryByCodeNormalized()
    {
        var infraction = InfractionType.Create("INF-101", "Atraso Injustificado", InfractionSeverity.Low, "Atraso sem justificativa médica");

        using (var context = CreateDbContext())
        {
            var repo = new InfractionTypeRepository(context);
            await repo.AddAsync(infraction);
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext())
        {
            var repo = new InfractionTypeRepository(context);
            var existsUpper = await repo.ExistsByCodeAsync("INF-101");
            var existsLower = await repo.ExistsByCodeAsync("inf-101");

            existsUpper.Should().BeTrue();
            existsLower.Should().BeTrue();
        }
    }

    [Fact]
    public async Task SoftDelete_GlobalFilter_ShouldHideDeletedEntities()
    {
        var infraction = InfractionType.Create("INF-DEL", "Infração Deletada", InfractionSeverity.Moderate, "Descrição");

        using (var context = CreateDbContext())
        {
            context.InfractionTypes.Add(infraction);
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext())
        {
            var item = await context.InfractionTypes.FindAsync(infraction.Id);
            item!.MarkAsDeleted(Guid.NewGuid());
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext())
        {
            var visibleCount = await context.InfractionTypes.CountAsync(i => i.Id == infraction.Id);
            var totalWithDeleted = await context.InfractionTypes.IgnoreQueryFilters().CountAsync(i => i.Id == infraction.Id);

            visibleCount.Should().Be(0);
            totalWithDeleted.Should().Be(1);
        }
    }

    [Fact]
    public async Task DisciplinaryCase_GetByIdWithDetailsAsync_ShouldLoadAllCollections()
    {
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var caseObj = DisciplinaryCase.Create("PROC-DET-01", companyId, "Processo Detalhado", "Descrição completa", userId, now);
        var infraction = InfractionType.Create("INF-DET", "Infração Exemplo", InfractionSeverity.High, "Infração");
        
        var occurrence = DisciplinaryOccurrence.Create(caseObj.Id, now, now, "Ocorrência 1", userId, infraction.Id, InfractionSeverity.High);
        var caseEmp = DisciplinaryCaseEmployee.Create(caseObj.Id, employeeId, CaseEmployeeRole.Accused, true);
        var evidence = DisciplinaryEvidence.CreateFileEvidence(
            caseObj.Id,
            EvidenceType.Document,
            "Documento 1",
            "evidences/documento-1.pdf",
            "documento-1.pdf",
            "application/pdf",
            1024,
            now,
            userId,
            disciplinaryOccurrenceId: occurrence.Id);

        caseObj.AddOccurrence(occurrence);
        caseObj.AddEmployee(caseEmp);
        caseObj.AddEvidence(evidence);

        using (var context = CreateDbContext())
        {
            context.InfractionTypes.Add(infraction);
            var repo = new DisciplinaryCaseRepository(context);
            await repo.AddAsync(caseObj);
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext())
        {
            var repo = new DisciplinaryCaseRepository(context);
            var loaded = await repo.GetByIdWithDetailsAsync(caseObj.Id);

            loaded.Should().NotBeNull();
            loaded!.Occurrences.Should().HaveCount(1);
            loaded.Employees.Should().HaveCount(1);
            loaded.Evidences.Should().HaveCount(1);
            loaded.Evidences.First().DisciplinaryOccurrenceId.Should().Be(occurrence.Id);
        }
    }

    [Theory]
    [InlineData("  PROC-TRIM-01  ", "PROC-TRIM-01")]
    [InlineData("proc-trim-02", "proc-trim-02")]
    public async Task ExistsByCaseNumberAsync_ShouldHandleSpacesCorrectly(string inputNumber, string expectedNumber)
    {
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var caseObj = DisciplinaryCase.Create(expectedNumber, companyId, "Título", "Descrição", userId, now);

        using (var context = CreateDbContext())
        {
            var repo = new DisciplinaryCaseRepository(context);
            await repo.AddAsync(caseObj);
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext())
        {
            var repo = new DisciplinaryCaseRepository(context);
            var exists = await repo.ExistsByCaseNumberAsync(companyId, inputNumber);

            exists.Should().BeTrue();
        }
    }

    [Theory]
    [InlineData("  inf-code-01  ")]
    [InlineData("INF-CODE-01")]
    public async Task ExistsByCodeAsync_ShouldNormalizeInputCode(string inputCode)
    {
        var infraction = InfractionType.Create("INF-CODE-01", "Tipo de Infração", InfractionSeverity.Low);

        using (var context = CreateDbContext())
        {
            var repo = new InfractionTypeRepository(context);
            await repo.AddAsync(infraction);
            await context.SaveChangesAsync();
        }

        using (var context = CreateDbContext())
        {
            var repo = new InfractionTypeRepository(context);
            var exists = await repo.ExistsByCodeAsync(inputCode);

            exists.Should().BeTrue();
        }
    }
}
