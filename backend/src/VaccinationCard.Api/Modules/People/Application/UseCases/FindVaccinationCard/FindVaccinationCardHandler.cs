using MediatR;
using VaccinationCard.Api.Modules.People.Application.Dtos;
using VaccinationCard.Api.Modules.People.Application.Persistence.Dao;
using VaccinationCard.Api.Modules.People.Domain.Errors;
using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;
using VaccinationCard.Api.Modules.Vaccines.Application.UseCases.FindVaccinesByIds;
using VaccinationCard.Api.Modules.Vaccines.Domain.Enums;
using VaccinationCard.Api.Modules.Vaccines.Domain.ValueObjects;
using VaccinationCard.Api.Shared.ValueObjects;

namespace VaccinationCard.Api.Modules.People.Application.UseCases.FindVaccinationCard;

public sealed class FindVaccinationCardHandler(IPersonDao dao, ISender sender) : IRequestHandler<FindVaccinationCardQuery, VaccinationCardDto>
{
    // The names and schemes come from Vaccines by query (TechSpec §4.3), already in name order, which the card keeps
    // (RN17). The card shows the whole CPF, formatted (RN26).
    public async Task<VaccinationCardDto> Handle(FindVaccinationCardQuery request, CancellationToken cancellationToken)
    {
        var data = await dao.FindCardDataAsync(request.PersonId, cancellationToken) ?? throw PeopleErrors.PersonNotFound();
        var vaccineIds = data.Vaccinations.Select(vaccination => vaccination.VaccineId).Distinct().ToList();
        var vaccines = await sender.Send(new FindVaccinesByIdsQuery(vaccineIds), cancellationToken);
        var vaccinationsByVaccine = data.Vaccinations.ToLookup(vaccination => vaccination.VaccineId);

        var person = data.Person with { Cpf = Cpf.Create(data.Person.Cpf).Formatted };
        return new VaccinationCardDto(person, [.. vaccines.Select(vaccine => ToEntry(vaccine, vaccinationsByVaccine[vaccine.Id]))]);
    }

    // The doses follow the sequence order (RN17). The progress and the next expected dose follow the scheme, with the
    // same rule as the aggregate (TechSpec §5.2).
    private static VaccinationCardEntryDto ToEntry(VaccineDto vaccine, IEnumerable<VaccinationDto> vaccinations)
    {
        var scheme = new DoseScheme(vaccine.PrimaryDoses, vaccine.BoosterDoses);
        var doses = vaccinations
            .OrderBy(vaccination => new Dose(vaccination.Dose.Type, vaccination.Dose.Number))
            .Select(vaccination => new VaccinationCardDoseDto(vaccination.Id, vaccination.Dose, vaccination.AppliedOn))
            .ToList();
        var primaryApplied = doses.Count(dose => dose.Dose.Type == DoseType.Primary);
        var boostersApplied = doses.Count - primaryApplied;
        var nextExpectedDose = scheme.NextExpectedDose(doses.Count);

        return new VaccinationCardEntryDto(
            vaccine.Id,
            vaccine.Name,
            new DoseSchemeDto(scheme.PrimaryDoses, scheme.BoosterDoses),
            doses,
            new VaccinationCardProgressDto(primaryApplied, scheme.PrimaryDoses, primaryApplied == scheme.PrimaryDoses, boostersApplied, scheme.BoosterDoses),
            nextExpectedDose is null ? null : new DoseDto(nextExpectedDose.Type, nextExpectedDose.Number)
        );
    }
}
