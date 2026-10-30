using VaccinationCard.Api.Shared.ValueObjects;

namespace VaccinationCard.Api.Modules.People.Domain.Entities;

public sealed class Person
{
    // The name has 3 to 150 characters, without the spaces at the ends (RN01).
    public const int MinNameLength = 3;
    public const int MaxNameLength = 150;

    internal Person(Guid id, string name, Cpf cpf)
    {
        Id = id;
        Name = name;
        Cpf = cpf;
    }

    public Guid Id { get; }

    public string Name { get; }

    public Cpf Cpf { get; }
}
