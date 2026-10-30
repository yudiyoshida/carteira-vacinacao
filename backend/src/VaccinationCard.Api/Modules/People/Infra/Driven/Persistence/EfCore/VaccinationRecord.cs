using VaccinationCard.Api.Modules.Vaccines.Domain.Enums;

namespace VaccinationCard.Api.Modules.People.Infra.Driven.Persistence.EfCore;

public sealed class VaccinationRecord
{
    public required Guid Id { get; set; }

    public required Guid PersonId { get; set; }

    public required Guid VaccineId { get; set; }

    public required DoseType DoseType { get; set; }

    public required short DoseNumber { get; set; }

    public required DateOnly AppliedOn { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
