using Microsoft.Extensions.Time.Testing;
using VaccinationCard.Api.Core.Time;

namespace VaccinationCard.UnitTests.Core.Time;

// Brasília is three hours behind UTC, so its date changes at 03:00 UTC.
public sealed class ClockTests
{
    private readonly FakeTimeProvider _timeProvider = new();
    private readonly Clock _sut;

    public ClockTests() => _sut = new Clock(_timeProvider, TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"));

    [Fact]
    [Trait("RN", "RN12")]
    public void Should_Keep_The_Brasilia_Date_When_Utc_Is_Already_On_The_Next_Day()
    {
        // Arrange
        // 23:30 in Brasília.
        _timeProvider.SetUtcNow(new DateTimeOffset(2026, 10, 31, 2, 30, 0, TimeSpan.Zero));

        // Act
        var today = _sut.Today;

        // Assert
        today.ShouldBe(new DateOnly(2026, 10, 30));
    }

    [Fact]
    [Trait("RN", "RN12")]
    public void Should_Change_The_Date_When_It_Is_Midnight_In_Brasilia()
    {
        // Arrange
        _timeProvider.SetUtcNow(new DateTimeOffset(2026, 10, 31, 3, 0, 0, TimeSpan.Zero));

        // Act
        var today = _sut.Today;

        // Assert
        today.ShouldBe(new DateOnly(2026, 10, 31));
    }
}
