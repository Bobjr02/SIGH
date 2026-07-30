using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SIGH.Application.Notifications.DTOs;
using SIGH.Application.Options;
using SIGH.Infrastructure.Services;
using Xunit;

namespace SIGH.UnitTests.Notifications;

public class NotificationServiceTests
{
    private readonly Mock<ILogger<NotificationService>> _loggerMock;
    private readonly IOptions<NotificationOptions> _options;
    private readonly NotificationService _service;

    public NotificationServiceTests()
    {
        _loggerMock = new Mock<ILogger<NotificationService>>();
        _options = Options.Create(new NotificationOptions
        {
            EnableEmailNotifications = true,
            EnableInAppNotifications = true,
            DefaultSenderName = "SIGH Testes"
        });
        _service = new NotificationService(_loggerMock.Object, _options);
    }

    [Fact]
    public async Task SendNotificationAsync_WithValidData_ShouldReturnSuccessResult()
    {
        var request = new SendNotificationRequestDto
        {
            RecipientUserId = Guid.NewGuid(),
            RecipientEmail = "usuario@teste.com",
            Title = "Notificação de Teste",
            Message = "Mensagem de teste de notificação.",
            Type = "Info",
            Channel = "InApp"
        };

        var result = await _service.SendNotificationAsync(request);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Title.Should().Be("Notificação de Teste");
        result.Data.RecipientEmail.Should().Be("usuario@teste.com");
    }

    [Fact]
    public async Task SendNotificationAsync_WithMissingTitleOrMessage_ShouldReturnFailure()
    {
        var request = new SendNotificationRequestDto
        {
            RecipientEmail = "usuario@teste.com",
            Title = "",
            Message = ""
        };

        var result = await _service.SendNotificationAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("obrigatori");
    }

    [Fact]
    public async Task SendBatchNotificationsAsync_ShouldSendAllValidRequests()
    {
        var requests = new[]
        {
            new SendNotificationRequestDto { Title = "T1", Message = "M1", RecipientEmail = "a@a.com" },
            new SendNotificationRequestDto { Title = "T2", Message = "M2", RecipientEmail = "b@b.com" }
        };

        var result = await _service.SendBatchNotificationsAsync(requests);

        result.Success.Should().BeTrue();
        result.Data.Should().Be(2);
    }
}
