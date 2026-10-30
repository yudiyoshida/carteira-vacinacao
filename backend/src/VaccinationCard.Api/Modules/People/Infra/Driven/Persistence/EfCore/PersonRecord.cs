namespace VaccinationCard.Api.Modules.People.Infra.Driven.Persistence.EfCore;

public sealed class PersonRecord
{
    public required Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Cpf { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public List<VaccinationRecord> Vaccinations { get; set; } = [];
}
