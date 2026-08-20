using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SIGH.Application;
using SIGH.Application.Interfaces;
using SIGH.Application.Options;
using SIGH.Infrastructure;
using SIGH.Infrastructure.BackgroundWorkers;
using Xunit;

namespace SIGH.UnitTests.Notifications;

public class BackgroundWorkerAndDITests
{
    [Fact]
    public void DependencyInjection_ShouldRegisterNotificationAndWorkerServices()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"NotificationOptions:EnableEmailNotifications", "true"},
            {"BackgroundWorkerOptions:CheckIntervalInSeconds", "30"}
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication(configuration);
        services.AddInfrastructure(configuration);

        var serviceProvider = services.BuildServiceProvider();

        var notificationService = serviceProvider.GetService<INotificationService>();
        notificationService.Should().NotBeNull();

        var deadlineCalculator = serviceProvider.GetService<IDeadlineCalculator>();
        deadlineCalculator.Should().NotBeNull();

        var backgroundJobScheduler = serviceProvider.GetService<IBackgroundJobScheduler>();
        backgroundJobScheduler.Should().NotBeNull();

        var notificationOptions = serviceProvider.GetService<IOptions<NotificationOptions>>();
        notificationOptions.Should().NotBeNull();
        notificationOptions!.Value.EnableEmailNotifications.Should().BeTrue();

        var backgroundWorkerOptions = serviceProvider.GetService<IOptions<BackgroundWorkerOptions>>();
        backgroundWorkerOptions.Should().NotBeNull();
        backgroundWorkerOptions!.Value.CheckIntervalInSeconds.Should().Be(30);

        var hostedServices = serviceProvider.GetServices<IHostedService>();
        hostedServices.Should().ContainSingle(s => s is DisciplinaryDeadlineBackgroundWorker);
    }
}
