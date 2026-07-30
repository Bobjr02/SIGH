using FluentAssertions;
using Moq;
using SIGH.Application.Interfaces;
using SIGH.Application.Notifications.DTOs;
using SIGH.Infrastructure.Services;
using Xunit;

namespace SIGH.UnitTests.Notifications;

public class DeadlineCalculatorTests
{
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly DeadlineCalculator _calculator;

    public DeadlineCalculatorTests()
    {
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _dateTimeProviderMock.Setup(d => d.UtcNow).Returns(new DateTime(2026, 7, 28, 12, 0, 0, DateTimeKind.Utc));
        _calculator = new DeadlineCalculator(_dateTimeProviderMock.Object);
    }

    [Fact]
    public void CalculateDueDate_ShouldSkipWeekends()
    {
        var startDate = new DateTime(2026, 7, 27);
        var dueDate = _calculator.CalculateDueDate(startDate, 5);

        dueDate.Should().Be(new DateTime(2026, 8, 3));
    }

    [Fact]
    public void CalculateDueDate_WithHolidays_ShouldSkipHolidaysAndWeekends()
    {
        var startDate = new DateTime(2026, 7, 27);
        var holidays = new[] { new DateTime(2026, 7, 28) };

        var dueDate = _calculator.CalculateDueDate(startDate, 5, holidays);

        dueDate.Should().Be(new DateTime(2026, 8, 4));
    }

    [Fact]
    public void IsDeadlineExpired_WhenPastDueDate_ShouldReturnTrue()
    {
        var dueDate = new DateTime(2026, 7, 20);
        var isExpired = _calculator.IsDeadlineExpired(dueDate);

        isExpired.Should().BeTrue();
    }

    [Fact]
    public void GetRemainingTime_WhenFutureDueDate_ShouldReturnPositiveTimeSpan()
    {
        var dueDate = new DateTime(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);
        var remaining = _calculator.GetRemainingTime(dueDate);

        remaining.Should().Be(TimeSpan.FromDays(2));
    }

    [Fact]
    public void GetRemainingDays_ShouldReturnDaysCeiling()
    {
        var dueDate = new DateTime(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);
        var days = _calculator.GetRemainingDays(dueDate);

        days.Should().Be(2);
    }

    [Fact]
    public void GetOverdueDays_WhenPastDueDate_ShouldReturnPositiveOverdueDays()
    {
        var dueDate = new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc);
        var overdue = _calculator.GetOverdueDays(dueDate);

        overdue.Should().Be(3);
    }

    [Fact]
    public void GetOverdueDays_WhenFutureDueDate_ShouldReturnZero()
    {
        var dueDate = new DateTime(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);
        var overdue = _calculator.GetOverdueDays(dueDate);

        overdue.Should().Be(0);
    }

    [Fact]
    public void CalculateDeadlineDetails_ShouldReturnPopulatedDto()
    {
        var request = new DeadlineCalculationRequestDto
        {
            StartDate = new DateTime(2026, 7, 27),
            BusinessDays = 3
        };

        var result = _calculator.CalculateDeadlineDetails(request);

        result.Should().NotBeNull();
        result.BusinessDaysCount.Should().Be(3);
        result.DueDate.Should().Be(new DateTime(2026, 7, 30));
    }
}
