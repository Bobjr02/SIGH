using FluentAssertions;
using SIGH.Domain.Notifications.Entities;
using Xunit;

namespace SIGH.UnitTests.Notifications;

public class NotificationCaseLinkTests
{
    private static readonly Guid Case = Guid.Parse("3f1c2a7e-1111-4222-8333-944455556666");

    [Theory]
    [InlineData("SourceEntity=DisciplinaryCase;SourceId=3f1c2a7e-1111-4222-8333-944455556666", true)]
    [InlineData("SourceEntity=DisciplinaryCase;SourceId=3f1c2a7e-1111-4222-8333-944455556666;EscalationLevel=2", true)]
    [InlineData("DisciplinaryCaseId=3f1c2a7e-1111-4222-8333-944455556666", true)]
    [InlineData("SourceEntity=Employee;SourceId=3f1c2a7e-1111-4222-8333-944455556666", false)]
    [InlineData("Origem=Sistema", false)]
    [InlineData(null, false)]
    public void TheCaseIsTakenFromTheMetadata_WhenNotGivenExplicitly(string? metadata, bool linked)
    {
        var notification = Notification.Create(Guid.NewGuid(), Guid.NewGuid(), "Título", "Mensagem", metadata: metadata);
        notification.DisciplinaryCaseId.Should().Be(linked ? Case : null);
    }

    [Fact]
    public void AnExplicitCaseWins()
    {
        var other = Guid.NewGuid();
        Notification.Create(Guid.NewGuid(), Guid.NewGuid(), "T", "M", metadata: $"SourceEntity=DisciplinaryCase;SourceId={Case}", disciplinaryCaseId: other)
            .DisciplinaryCaseId.Should().Be(other);
    }
}
