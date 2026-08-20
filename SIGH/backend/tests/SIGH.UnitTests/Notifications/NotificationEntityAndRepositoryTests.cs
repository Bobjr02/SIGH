using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SIGH.Domain.Notifications.Entities;
using SIGH.Persistence.Context;
using SIGH.Persistence.Repositories;
using Xunit;

namespace SIGH.UnitTests.Notifications;

public class NotificationEntityAndRepositoryTests
{
    [Fact]
    public void NotificationEntity_Creation_ShouldInitializeCorrectly()
    {
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var notification = Notification.Create(
            companyId,
            userId,
            "Notificação de Teste",
            "Mensagem de teste de notificação",
            "Warning",
            "High");

        notification.CompanyId.Should().Be(companyId);
        notification.UserId.Should().Be(userId);
        notification.Title.Should().Be("Notificação de Teste");
        notification.Message.Should().Be("Mensagem de teste de notificação");
        notification.Type.Should().Be("Warning");
        notification.Priority.Should().Be("High");
        notification.IsRead.Should().BeFalse();
        notification.ReadAt.Should().BeNull();
        notification.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void NotificationEntity_MarkAsRead_ShouldUpdateStatusAndReadAt()
    {
        var notification = Notification.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Título",
            "Mensagem");

        var readAt = DateTimeOffset.UtcNow;
        notification.MarkAsRead(readAt);

        notification.IsRead.Should().BeTrue();
        notification.ReadAt.Should().Be(readAt);
    }

    [Fact]
    public async Task NotificationRepository_CrudAndFilter_ShouldPersistAndRetrieve()
    {
        var options = new DbContextOptionsBuilder<SighDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new SighDbContext(options);
        var repository = new NotificationRepository(context);

        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var notification1 = Notification.Create(companyId, userId, "Title 1", "Msg 1");
        var notification2 = Notification.Create(companyId, userId, "Title 2", "Msg 2");
        var notificationOtherUser = Notification.Create(companyId, Guid.NewGuid(), "Title Other", "Msg Other");

        await repository.AddAsync(notification1);
        await repository.AddAsync(notification2);
        await repository.AddAsync(notificationOtherUser);
        await context.SaveChangesAsync();

        // Retrieve unread for specific user
        var unread = await repository.GetUnreadUserNotificationsAsync(companyId, userId);
        unread.Should().HaveCount(2);

        // Mark notification 1 as read
        var entity = await repository.GetByIdForUserAsync(notification1.Id, companyId, userId);
        entity.Should().NotBeNull();
        entity!.MarkAsRead(DateTimeOffset.UtcNow);
        await repository.UpdateAsync(entity);
        await context.SaveChangesAsync();

        // Retrieve unread again
        var unreadAfterMark = await repository.GetUnreadUserNotificationsAsync(companyId, userId);
        unreadAfterMark.Should().HaveCount(1);

        // Get paged user notifications
        var paged = await repository.GetPagedUserNotificationsAsync(companyId, userId, 1, 10);
        paged.TotalCount.Should().Be(2);
        paged.Items.Should().HaveCount(2);
    }
}
