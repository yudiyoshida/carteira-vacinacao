using MediatR;
using Microsoft.EntityFrameworkCore;
using VaccinationCard.Api.Modules.People.Application.UseCases.CreatePerson;
using VaccinationCard.Api.Modules.People.Application.UseCases.RegisterVaccination;
using VaccinationCard.Api.Modules.People.Infra.Driven.Persistence.EfCore;
using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;
using VaccinationCard.Api.Modules.Vaccines.Application.UseCases.CreateVaccine;
using VaccinationCard.Api.Modules.Vaccines.Domain.Enums;
using VaccinationCard.Api.Modules.Vaccines.Infra.Driven.Persistence.EfCore;

namespace VaccinationCard.Api.Infra.Database.Seeds;

public sealed partial class PersonSeed(AppDbContext db, ISender sender, ILogger<PersonSeed> logger)
{
    private const string Name = "Maria Demonstração";
    private const string Cpf = "123.456.789-09";

    // A partial card: a complete vaccine, a primary series in progress and a booster still missing. The dates are fixed
    // and in the past, so the card is the same on every run (TechSpec §6.4).
    private static readonly (CreateVaccineCommand Vaccine, DoseDto Dose, DateOnly AppliedOn)[] Doses =
    [
        (VaccineSeed.Bcg, new DoseDto(DoseType.Primary, 1), new DateOnly(2023, 3, 10)),
        (VaccineSeed.HepatitisB, new DoseDto(DoseType.Primary, 1), new DateOnly(2023, 3, 10)),
        (VaccineSeed.HepatitisB, new DoseDto(DoseType.Primary, 2), new DateOnly(2023, 5, 10)),
        (VaccineSeed.Dtp, new DoseDto(DoseType.Primary, 1), new DateOnly(2023, 5, 10)),
        (VaccineSeed.Dtp, new DoseDto(DoseType.Primary, 2), new DateOnly(2023, 7, 10)),
        (VaccineSeed.Dtp, new DoseDto(DoseType.Primary, 3), new DateOnly(2023, 9, 11)),
        (VaccineSeed.Dtp, new DoseDto(DoseType.Booster, 1), new DateOnly(2024, 6, 10)),
    ];

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        if (await db.Set<PersonRecord>().AnyAsync(cancellationToken))
        {
            return;
        }

        // The seed can be switched on over vaccines registered by hand. The key compares the name and the scheme, and a
        // different vaccine leaves the person out instead of stopping the API (TechSpec §6.4).
        var vaccineIds = await db.Set<VaccineRecord>()
            .AsNoTracking()
            .ToDictionaryAsync(x => new CreateVaccineCommand(x.Name, x.PrimaryDoses, x.BoosterDoses), x => x.Id, cancellationToken);

        if (!Doses.All(dose => vaccineIds.ContainsKey(dose.Vaccine)))
        {
            LogDemoVaccinesMissing(logger);
            return;
        }

        // The use cases take the doses through the aggregate, so the data follows the rules.
        var person = await sender.Send(new CreatePersonCommand(Name, Cpf), cancellationToken);
        foreach (var (vaccine, dose, appliedOn) in Doses)
        {
            await sender.Send(new RegisterVaccinationCommand(person.Id, vaccineIds[vaccine], dose, appliedOn), cancellationToken);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Pessoa de demonstração não criada: falta uma vacina de demonstração com o nome e o esquema do seed")]
    private static partial void LogDemoVaccinesMissing(ILogger logger);
}
