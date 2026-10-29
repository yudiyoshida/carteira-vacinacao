namespace VaccinationCard.Api.Modules.Vaccines.Infra.Driven.Persistence.EfCore;

public sealed class VaccineRecord
{
    public required Guid Id { get; set; }

    public required string Name { get; set; }

    public required string NormalizedName { get; set; }

    public required short PrimaryDoses { get; set; }

    public required short BoosterDoses { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
