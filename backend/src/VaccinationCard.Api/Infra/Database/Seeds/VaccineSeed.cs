using MediatR;
using Microsoft.EntityFrameworkCore;
using VaccinationCard.Api.Modules.Vaccines.Application.UseCases.CreateVaccine;
using VaccinationCard.Api.Modules.Vaccines.Infra.Driven.Persistence.EfCore;

namespace VaccinationCard.Api.Infra.Database.Seeds;

public sealed class VaccineSeed(AppDbContext db, ISender sender)
{
    public static readonly CreateVaccineCommand Bcg = new("BCG", 1, 0);
    public static readonly CreateVaccineCommand HepatitisB = new("Hepatite B", 3, 0);
    public static readonly CreateVaccineCommand Dtp = new("DTP", 3, 2);

    // The table of PRD §9.
    private static readonly CreateVaccineCommand[] Vaccines =
    [
        Bcg,
        HepatitisB,
        Dtp,
        new("Poliomielite inativada (VIP)", 3, 1),
        new("Pneumocócica 10-valente", 2, 1),
        new("Meningocócica C", 2, 1),
        new("Tríplice viral", 2, 0),
        new("Febre amarela", 1, 0),
    ];

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        if (await db.Set<VaccineRecord>().AnyAsync(cancellationToken))
        {
            return;
        }

        foreach (var vaccine in Vaccines)
        {
            await sender.Send(vaccine, cancellationToken);
        }
    }
}
