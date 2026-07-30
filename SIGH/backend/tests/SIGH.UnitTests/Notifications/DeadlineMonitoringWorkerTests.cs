namespace SIGH.UnitTests.Notifications;

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SIGH.Application.Common.Models;
using SIGH.Application.Notifications.DTOs;
using SIGH.Application.Notifications.UseCases;
using SIGH.Application.Options;
using SIGH.Infrastructure.BackgroundWorkers;
using Xunit;

public class DeadlineMonitoringWorkerTests
{
    private readonly Mock<ILogger<DisciplinaryDeadlineBackgroundWorker>> _loggerMock;
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<IServiceScope> _scopeMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IProcessDeadlineMonitoringUseCase> _monitoringUseCaseMock;

    public DeadlineMonitoringWorkerTests()
    {
        _loggerMock = new Mock<ILogger<DisciplinaryDeadlineBackgroundWorker>>();
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _scopeMock = new Mock<IServiceScope>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _monitoringUseCaseMock = new Mock<IProcessDeadlineMonitoringUseCase>();

        _scopeFactoryMock.Setup(s => s.CreateScope()).Returns(_scopeMock.Object);
        _scopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IProcessDeadlineMonitoringUseCase)))
            .Returns(_monitoringUseCaseMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEnabled_ShouldInvokeMonitoringUseCase()
    {
        var options = Options.Create(new BackgroundWorkerOptions
        {
            Enabled = true,
            CheckIntervalInSeconds = 1
        });

        _monitoringUseCaseMock
            .Setup(m => m.ExecuteAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DeadlineProcessingResultDto>.Ok(new DeadlineProcessingResultDto
            {
                TotalItemsProcessed = 2,
                RemindersGenerated = 1,
                EscalationsExecuted = 1
            }));

        var worker = new DisciplinaryDeadlineBackgroundWorker(_loggerMock.Object, _scopeFactoryMock.Object, options);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(300); // Cancela após 300ms

        var task = worker.StartAsync(cts.Token);
        await task;

        _monitoringUseCaseMock.Verify(m => m.ExecuteAsync(null, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDisabled_ShouldNotInvokeMonitoringUseCase()
    {
        var options = Options.Create(new BackgroundWorkerOptions
        {
            Enabled = false,
            CheckIntervalInSeconds = 1
        });

        var worker = new DisciplinaryDeadlineBackgroundWorker(_loggerMock.Object, _scopeFactoryMock.Object, options);
        using var cts = new CancellationTokenSource();

        await worker.StartAsync(cts.Token);

        _monitoringUseCaseMock.Verify(m => m.ExecuteAsync(null, It.IsAny<CancellationToken>()), Times.Never);
    }
}
