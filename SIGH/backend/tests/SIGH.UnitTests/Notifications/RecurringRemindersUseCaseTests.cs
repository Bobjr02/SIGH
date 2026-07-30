namespace SIGH.UnitTests.Notifications;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SIGH.Application.Interfaces;
using SIGH.Application.Interfaces.Repositories;
using SIGH.Application.Notifications.DTOs;
using SIGH.Application.Notifications.UseCases;
using SIGH.Application.Options;
using SIGH.Domain.Notifications.Entities;
using Xunit;

public class RecurringRemindersUseCaseTests
{
    private readonly Mock<IDeadlineMonitoringQueryRepository> _queryRepoMock;
    private readonly Mock<INotificationRepository> _notificationRepoMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly IOptions<NotificationReminderOptions> _options;
    private readonly Mock<ILogger<ProcessRecurringRemindersUseCase>> _loggerMock;
    private readonly ProcessRecurringRemindersUseCase _useCase;
    private readonly DateTimeOffset _now;

    public RecurringRemindersUseCaseTests()
    {
        _queryRepoMock = new Mock<IDeadlineMonitoringQueryRepository>();
        _notificationRepoMock = new Mock<INotificationRepository>();
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<ProcessRecurringRemindersUseCase>>();

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

        _useCase = new ProcessRecurringRemindersUseCase(
            _queryRepoMock.Object,
            _notificationRepoMock.Object,
            _dateTimeProviderMock.Object,
            _unitOfWorkMock.Object,
            _options,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_FirstReminder_ShouldCreateNotificationAndLog()
    {
        var companyId = Guid.NewGuid();
        var item = new DeadlineItemDto
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            SourceEntity = "DisciplinaryCase",
            ReferenceNumber = "PROC-2026-001",
            Title = "Caso de Teste",
            DueDate = _now.AddDays(1), // Dentro dos 2 dias de antecedência
            OpenedByUserId = Guid.NewGuid(),
            ResponsibleUserId = Guid.NewGuid()
        };

        _queryRepoMock
            .Setup(q => q.GetActiveItemsWithDeadlinesAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { item });

        _notificationRepoMock
            .Setup(n => n.GetLogAsync(companyId, "DisciplinaryCase", item.Id, item.ResponsibleUserId!.Value, "DeadlineReminder", It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationLog?)null);

        var result = await _useCase.ExecuteAsync(companyId);

        result.Success.Should().BeTrue();
        result.Data!.RemindersGenerated.Should().Be(1);
        _notificationRepoMock.Verify(n => n.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Once);
        _notificationRepoMock.Verify(n => n.AddLogAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_BeforeInterval_ShouldSkipDueToDuplicity()
    {
        var companyId = Guid.NewGuid();
        var recipientUserId = Guid.NewGuid();
        var item = new DeadlineItemDto
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            SourceEntity = "DisciplinaryCase",
            ReferenceNumber = "PROC-2026-002",
            Title = "Caso em Intervalo",
            DueDate = _now.AddDays(1),
            ResponsibleUserId = recipientUserId
        };

        var existingLog = NotificationLog.Create(
            companyId,
            "DisciplinaryCase",
            item.Id,
            "DeadlineReminder",
            recipientUserId,
            sentAt: _now.AddHours(-12), // Enviado há 12h (mínimo de 24h)
            remindersSentCount: 1);

        _queryRepoMock
            .Setup(q => q.GetActiveItemsWithDeadlinesAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { item });

        _notificationRepoMock
            .Setup(n => n.GetLogAsync(companyId, "DisciplinaryCase", item.Id, recipientUserId, "DeadlineReminder", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingLog);

        var result = await _useCase.ExecuteAsync(companyId);

        result.Success.Should().BeTrue();
        result.Data!.RemindersGenerated.Should().Be(0);
        result.Data!.ItemsSkippedDueToDuplicity.Should().Be(1);
        _notificationRepoMock.Verify(n => n.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_AfterInterval_ShouldSendRecurrentReminder()
    {
        var companyId = Guid.NewGuid();
        var recipientUserId = Guid.NewGuid();
        var item = new DeadlineItemDto
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            SourceEntity = "DisciplinaryCase",
            ReferenceNumber = "PROC-2026-003",
            Title = "Caso Elegível para Recorrência",
            DueDate = _now.AddDays(-1),
            ResponsibleUserId = recipientUserId
        };

        var existingLog = NotificationLog.Create(
            companyId,
            "DisciplinaryCase",
            item.Id,
            "DeadlineReminder",
            recipientUserId,
            sentAt: _now.AddHours(-25), // Enviado há 25h (passou de 24h)
            remindersSentCount: 1);

        _queryRepoMock
            .Setup(q => q.GetActiveItemsWithDeadlinesAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { item });

        _notificationRepoMock
            .Setup(n => n.GetLogAsync(companyId, "DisciplinaryCase", item.Id, recipientUserId, "DeadlineReminder", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingLog);

        var result = await _useCase.ExecuteAsync(companyId);

        result.Success.Should().BeTrue();
        result.Data!.RemindersGenerated.Should().Be(1);
        _notificationRepoMock.Verify(n => n.UpdateLogAsync(existingLog, It.IsAny<CancellationToken>()), Times.Once);
        existingLog.RemindersSentCount.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteAsync_MaxRemindersReached_ShouldSkip()
    {
        var companyId = Guid.NewGuid();
        var recipientUserId = Guid.NewGuid();
        var item = new DeadlineItemDto
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            SourceEntity = "DisciplinaryCase",
            ReferenceNumber = "PROC-2026-004",
            Title = "Caso no Limite",
            DueDate = _now.AddDays(-2),
            ResponsibleUserId = recipientUserId
        };

        var existingLog = NotificationLog.Create(
            companyId,
            "DisciplinaryCase",
            item.Id,
            "DeadlineReminder",
            recipientUserId,
            sentAt: _now.AddHours(-30),
            remindersSentCount: 3); // Atingiu o limite 3

        _queryRepoMock
            .Setup(q => q.GetActiveItemsWithDeadlinesAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { item });

        _notificationRepoMock
            .Setup(n => n.GetLogAsync(companyId, "DisciplinaryCase", item.Id, recipientUserId, "DeadlineReminder", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingLog);

        var result = await _useCase.ExecuteAsync(companyId);

        result.Success.Should().BeTrue();
        result.Data!.RemindersGenerated.Should().Be(0);
        result.Data!.ItemsSkippedDueToDuplicity.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_PartialBatchFailure_ShouldContinueProcessingOtherItems()
    {
        var companyId = Guid.NewGuid();
        var item1 = new DeadlineItemDto { Id = Guid.NewGuid(), CompanyId = companyId, ReferenceNumber = "P-1", DueDate = _now.AddDays(1), ResponsibleUserId = Guid.NewGuid() };
        var item2 = new DeadlineItemDto { Id = Guid.NewGuid(), CompanyId = companyId, ReferenceNumber = "P-2", DueDate = _now.AddDays(1), ResponsibleUserId = Guid.NewGuid() };

        _queryRepoMock
            .Setup(q => q.GetActiveItemsWithDeadlinesAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { item1, item2 });

        // Faz o item1 lançar exceção na busca do log
        _notificationRepoMock
            .Setup(n => n.GetLogAsync(companyId, "DisciplinaryCase", item1.Id, item1.ResponsibleUserId!.Value, "DeadlineReminder", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Simulated database failure for item 1"));

        _notificationRepoMock
            .Setup(n => n.GetLogAsync(companyId, "DisciplinaryCase", item2.Id, item2.ResponsibleUserId!.Value, "DeadlineReminder", It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationLog?)null);

        var result = await _useCase.ExecuteAsync(companyId);

        result.Success.Should().BeTrue();
        result.Data!.TotalItemsProcessed.Should().Be(2);
        result.Data!.FailedItemsCount.Should().Be(1);
        result.Data!.RemindersGenerated.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancellationRequested_ShouldThrowOperationCanceledException()
    {
        var companyId = Guid.NewGuid();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var action = () => _useCase.ExecuteAsync(companyId, cts.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
    }
}
