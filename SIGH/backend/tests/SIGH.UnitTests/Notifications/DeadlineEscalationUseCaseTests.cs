namespace SIGH.UnitTests.Notifications;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SIGH.Application.Common.Interfaces;
using SIGH.Application.Interfaces;
using SIGH.Application.Interfaces.Repositories;
using SIGH.Application.Notifications.DTOs;
using SIGH.Application.Notifications.UseCases;
using SIGH.Application.Options;
using SIGH.Domain.Notifications.Entities;
using Xunit;

public class DeadlineEscalationUseCaseTests
{
    private readonly Mock<IDeadlineMonitoringQueryRepository> _queryRepoMock;
    private readonly Mock<INotificationRepository> _notificationRepoMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly IOptions<NotificationReminderOptions> _options;
    private readonly Mock<ILogger<ProcessDeadlineEscalationsUseCase>> _loggerMock;
    private readonly ProcessDeadlineEscalationsUseCase _useCase;
    private readonly DateTimeOffset _now;

    public DeadlineEscalationUseCaseTests()
    {
        _queryRepoMock = new Mock<IDeadlineMonitoringQueryRepository>();
        _notificationRepoMock = new Mock<INotificationRepository>();
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _contextMock = new Mock<IApplicationDbContext>();
        _loggerMock = new Mock<ILogger<ProcessDeadlineEscalationsUseCase>>();

        _now = new DateTimeOffset(2026, 7, 28, 12, 0, 0, TimeSpan.Zero);
        _dateTimeProviderMock.Setup(d => d.UtcNow).Returns(_now);

        _options = Options.Create(new NotificationReminderOptions
        {
            FirstAlertDaysBeforeDue = 2,
            ReminderIntervalHours = 24,
            MaxRemindersCount = 3,
            EscalationDelayDays = 2,
            MaxEscalationLevels = 3
        });

        _useCase = new ProcessDeadlineEscalationsUseCase(
            _queryRepoMock.Object,
            _notificationRepoMock.Object,
            _dateTimeProviderMock.Object,
            _contextMock.Object,
            _options,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_OverduePastDelayThreshold_ShouldEscalateToLevel1()
    {
        var companyId = Guid.NewGuid();
        var item = new DeadlineItemDto
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            SourceEntity = "DisciplinaryCase",
            ReferenceNumber = "PROC-ESC-001",
            Title = "Caso Vencido Há 3 Dias",
            DueDate = _now.AddDays(-3), // Vencido há 3 dias (atraso mínimo = 2 dias)
            ResponsibleUserId = Guid.NewGuid()
        };

        _queryRepoMock
            .Setup(q => q.GetActiveItemsWithDeadlinesAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { item });

        _notificationRepoMock
            .Setup(n => n.GetLogsByEntityAsync(companyId, "DisciplinaryCase", item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NotificationLog>());

        var result = await _useCase.ExecuteAsync(companyId);

        result.Success.Should().BeTrue();
        result.Data!.EscalationsExecuted.Should().Be(1);
        _notificationRepoMock.Verify(n => n.AddAsync(It.Is<Notification>(notif => notif.Priority == "High"), It.IsAny<CancellationToken>()), Times.Once);
        _notificationRepoMock.Verify(n => n.AddLogAsync(It.Is<NotificationLog>(log => log.EscalationLevel == 1), It.IsAny<CancellationToken>()), Times.Once);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenLevel1AlreadySent_ShouldEscalateToLevel2DirectSupervisor()
    {
        var companyId = Guid.NewGuid();
        var responsibleUserId = Guid.NewGuid();
        var supervisorUserId = Guid.NewGuid();

        var item = new DeadlineItemDto
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            SourceEntity = "DisciplinaryCase",
            ReferenceNumber = "PROC-ESC-002",
            Title = "Caso para Nível 2",
            DueDate = _now.AddDays(-4),
            ResponsibleUserId = responsibleUserId,
            DirectSupervisorUserId = supervisorUserId
        };

        var level1Log = NotificationLog.Create(
            companyId,
            "DisciplinaryCase",
            item.Id,
            "DeadlineEscalation",
            responsibleUserId,
            sentAt: _now.AddDays(-2),
            remindersSentCount: 1,
            escalationLevel: 1);

        _queryRepoMock
            .Setup(q => q.GetActiveItemsWithDeadlinesAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { item });

        _notificationRepoMock
            .Setup(n => n.GetLogsByEntityAsync(companyId, "DisciplinaryCase", item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { level1Log });

        var result = await _useCase.ExecuteAsync(companyId);

        result.Success.Should().BeTrue();
        result.Data!.EscalationsExecuted.Should().Be(1);
        _notificationRepoMock.Verify(n => n.AddAsync(It.Is<Notification>(notif => notif.UserId == supervisorUserId && notif.Priority == "Urgent"), It.IsAny<CancellationToken>()), Times.Once);
        _notificationRepoMock.Verify(n => n.AddLogAsync(It.Is<NotificationLog>(log => log.RecipientUserId == supervisorUserId && log.EscalationLevel == 2), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenMaxEscalationLevelReached_ShouldSkip()
    {
        var companyId = Guid.NewGuid();
        var item = new DeadlineItemDto
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            SourceEntity = "DisciplinaryCase",
            ReferenceNumber = "PROC-ESC-003",
            Title = "Caso com Escalonamento Máximo",
            DueDate = _now.AddDays(-10),
            ResponsibleUserId = Guid.NewGuid()
        };

        var maxLog = NotificationLog.Create(
            companyId,
            "DisciplinaryCase",
            item.Id,
            "DeadlineEscalation",
            Guid.NewGuid(),
            sentAt: _now.AddDays(-3),
            remindersSentCount: 1,
            escalationLevel: 3); // Atingiu nível máximo 3

        _queryRepoMock
            .Setup(q => q.GetActiveItemsWithDeadlinesAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { item });

        _notificationRepoMock
            .Setup(n => n.GetLogsByEntityAsync(companyId, "DisciplinaryCase", item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { maxLog });

        var result = await _useCase.ExecuteAsync(companyId);

        result.Success.Should().BeTrue();
        result.Data!.EscalationsExecuted.Should().Be(0);
        result.Data!.ItemsSkippedDueToDuplicity.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoSupervisorDefined_ShouldFallbackToResponsibleOrOpener()
    {
        var companyId = Guid.NewGuid();
        var openerUserId = Guid.NewGuid();

        var item = new DeadlineItemDto
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            SourceEntity = "DisciplinaryCase",
            ReferenceNumber = "PROC-ESC-004",
            Title = "Caso sem Gestor",
            DueDate = _now.AddDays(-5),
            OpenedByUserId = openerUserId,
            ResponsibleUserId = null,
            DirectSupervisorUserId = null
        };

        _queryRepoMock
            .Setup(q => q.GetActiveItemsWithDeadlinesAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { item });

        _notificationRepoMock
            .Setup(n => n.GetLogsByEntityAsync(companyId, "DisciplinaryCase", item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NotificationLog>());

        var result = await _useCase.ExecuteAsync(companyId);

        result.Success.Should().BeTrue();
        result.Data!.EscalationsExecuted.Should().Be(1);
        _notificationRepoMock.Verify(n => n.AddAsync(It.Is<Notification>(notif => notif.UserId == openerUserId), It.IsAny<CancellationToken>()), Times.Once);
    }
}
