namespace VaccinationCard.Api.Core.Time;

public sealed class Clock(TimeProvider timeProvider, TimeZoneInfo timeZone) : IClock
{
    // The date in the time zone, not in UTC: at 23:30 in Brasília, UTC is already on the next day.
    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone).DateTime);
}
