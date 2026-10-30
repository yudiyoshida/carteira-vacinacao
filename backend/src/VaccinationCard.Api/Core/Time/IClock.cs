namespace VaccinationCard.Api.Core.Time;

// "Today" is the current civil date in the time zone of App:TimeZone, Brasília (RN12). The handlers read it here and
// pass it to the domain, which never reads the clock.
public interface IClock
{
    DateOnly Today { get; }
}
