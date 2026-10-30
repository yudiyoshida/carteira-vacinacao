using VaccinationCard.Api.Modules.People.Domain.Entities;
using VaccinationCard.Api.Shared.ValueObjects;

namespace VaccinationCard.Api.Modules.People.Domain.Factories;

public static class PersonFactory
{
    // The handler trims the name, and the validator rejects a name out of RN01 first; the guards catch a programming
    // error.
    public static Person Create(string name, Cpf cpf)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(name.Length, Person.MinNameLength, nameof(name));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(name.Length, Person.MaxNameLength, nameof(name));

        return new Person(Guid.CreateVersion7(), name, cpf);
    }

    // Rebuilds a person read from the database, without checking the rules again. Only the Repository calls it.
    public static Person Load(Guid id, string name, Cpf cpf)
    {
        return new Person(id, name, cpf);
    }
}
