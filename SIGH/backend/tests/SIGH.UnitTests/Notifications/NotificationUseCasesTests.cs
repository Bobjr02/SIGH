using FluentAssertions;
using FluentValidation;
using Moq;
using SIGH.Application.Common.Models;
using SIGH.Application.Interfaces;
using SIGH.Application.Notifications.DTOs;
using SIGH.Application.Notifications.Queries;
using SIGH.Application.Notifications.UseCases;
using Xunit;

namespace SIGH.UnitTests.Notifications;

public class NotificationUseCasesTests
{
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly GetUserNotificationsUseCase _getUserNotificationsUseCase;
    private readonly GetUnreadNotificationsUseCase _getUnreadNotificationsUseCase;
    private readonly GetUnreadCountUseCase _getUnreadCountUseCase;
    private readonly MarkNotificationAsReadUseCase _markNotificationAsReadUseCase;
    private readonly MarkAllNotificationsAsReadUseCase _markAllNotificationsAsReadUseCase;

    public NotificationUseCasesTests()
    {
        _notificationServiceMock = new Mock<INotificationService>();

        var getNotificationsValidator = new GetNotificationsQueryValidator();
        _getUserNotificationsUseCase = new GetUserNotificationsUseCase(_notificationServiceMock.Object, getNotificationsValidator);

        _getUnreadNotificationsUseCase = new GetUnreadNotificationsUseCase(_notificationServiceMock.Object);
        _getUnreadCountUseCase = new GetUnreadCountUseCase(_notificationServiceMock.Object);

        var markValidator = new MarkNotificationAsReadCommandValidator();
        _markNotificationAsReadUseCase = new MarkNotificationAsReadUseCase(_notificationServiceMock.Object, markValidator);

        var markAllValidator = new MarkAllNotificationsAsReadCommandValidator();
        _markAllNotificationsAsReadUseCase = new MarkAllNotificationsAsReadUseCase(_notificationServiceMock.Object, markAllValidator);
    }

    [Fact]
    public async Task GetUserNotifications_WithValidQuery_ShouldReturnSuccess()
    {
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var query = new GetNotificationsQuery
        {
            CompanyId = companyId,
            UserId = userId,
            PageNumber = 1,
            PageSize = 10
        };

        var expectedResult = PagedResult<NotificationDto>.Create(new List<NotificationDto>(), 0, 1, 10);
        _notificationServiceMock
            .Setup(s => s.GetUserNotificationsAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PagedResult<NotificationDto>>.Ok(expectedResult));

        var result = await _getUserNotificationsUseCase.ExecuteAsync(query);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        _notificationServiceMock.Verify(s => s.GetUserNotificationsAsync(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUserNotifications_WithInvalidPageNumber_ShouldReturnValidationError()
    {
        var query = new GetNotificationsQuery
        {
            CompanyId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            PageNumber = 0,
            PageSize = 10
        };

        var result = await _getUserNotificationsUseCase.ExecuteAsync(query);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("VALIDATION_ERROR");
        result.ValidationErrors.Should().ContainKey("PageNumber");
    }

    [Fact]
    public async Task GetUnreadNotifications_WithValidQuery_ShouldReturnUnreadList()
    {
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var query = new GetUnreadNotificationsQuery
        {
            CompanyId = companyId,
            UserId = userId
        };

        var notifications = new List<NotificationDto>
        {
            new NotificationDto { Id = Guid.NewGuid(), Title = "Alerta de Prazo", IsRead = false }
        };

        _notificationServiceMock
            .Setup(s => s.GetUnreadNotificationsAsync(companyId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<NotificationDto>>.Ok(notifications));

        var result = await _getUnreadNotificationsUseCase.ExecuteAsync(query);

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetUnreadCount_WithValidQuery_ShouldReturnCount()
    {
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var query = new GetUnreadCountQuery
        {
            CompanyId = companyId,
            UserId = userId
        };

        _notificationServiceMock
            .Setup(s => s.GetUnreadCountAsync(companyId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Ok(5));

        var result = await _getUnreadCountUseCase.ExecuteAsync(query);

        result.Success.Should().BeTrue();
        result.Data.Should().Be(5);
    }

    [Fact]
    public async Task MarkNotificationAsRead_WithValidCommand_ShouldReturnUpdatedNotification()
    {
        var notificationId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var command = new MarkNotificationAsReadCommand
        {
            NotificationId = notificationId,
            CompanyId = companyId,
            UserId = userId
        };

        var updatedDto = new NotificationDto
        {
            Id = notificationId,
            CompanyId = companyId,
            RecipientUserId = userId,
            IsRead = true,
            ReadAt = DateTimeOffset.UtcNow
        };

        _notificationServiceMock
            .Setup(s => s.MarkAsReadAsync(notificationId, companyId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<NotificationDto>.Ok(updatedDto));

        var result = await _markNotificationAsReadUseCase.ExecuteAsync(command);

        result.Success.Should().BeTrue();
        result.Data!.IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task MarkAllNotificationsAsRead_WithValidCommand_ShouldReturnCount()
    {
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var command = new MarkAllNotificationsAsReadCommand
        {
            CompanyId = companyId,
            UserId = userId
        };

        _notificationServiceMock
            .Setup(s => s.MarkAllAsReadAsync(companyId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Ok(3));

        var result = await _markAllNotificationsAsReadUseCase.ExecuteAsync(command);

        result.Success.Should().BeTrue();
        result.Data.Should().Be(3);
    }
}
