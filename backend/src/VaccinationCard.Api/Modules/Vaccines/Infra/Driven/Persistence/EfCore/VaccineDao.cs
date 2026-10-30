using Microsoft.EntityFrameworkCore;
using VaccinationCard.Api.Infra.Database;
using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;
using VaccinationCard.Api.Modules.Vaccines.Application.Persistence.Dao;
using VaccinationCard.Api.Modules.Vaccines.Domain.Errors;
using VaccinationCard.Api.Shared.Pagination;

namespace VaccinationCard.Api.Modules.Vaccines.Infra.Driven.Persistence.EfCore;

public sealed class VaccineDao(AppDbContext db) : IVaccineDao
{
    public Task<Paginated<VaccineDto>> FindAllAsync(string? search, int page, int? size, CancellationToken cancellationToken)
    {
        var query = db.Set<VaccineRecord>().AsNoTracking();

        // unaccent on both sides ignores the accents, and ILIKE ignores the case (RN20).
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = LikePattern.Contains(search);
            query = query.Where(x => EF.Functions.ILike(EF.Functions.Unaccent(x.Name), EF.Functions.Unaccent(pattern), LikePattern.EscapeCharacter));
        }

        return query
            .OrderBy(x => x.Name)
            .Select(x => new VaccineDto(x.Id, x.Name, x.PrimaryDoses, x.BoosterDoses))
            .ToPaginatedAsync(page, size, cancellationToken);
    }

    public Task<VaccineDto?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return db.Set<VaccineRecord>()
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new VaccineDto(x.Id, x.Name, x.PrimaryDoses, x.BoosterDoses))
            .SingleOrDefaultAsync(cancellationToken);
    }

    // An id that matches no vaccine is left out. The list keeps the name order of the database, as FindAllAsync does
    // (TechSpec §6.2).
    public async Task<IReadOnlyList<VaccineDto>> FindByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        return await db.Set<VaccineRecord>()
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .OrderBy(x => x.Name)
            .Select(x => new VaccineDto(x.Id, x.Name, x.PrimaryDoses, x.BoosterDoses))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken)
    {
        var normalizedName = Normalize(name);
        return db.Set<VaccineRecord>().AnyAsync(x => x.NormalizedName == normalizedName, cancellationToken);
    }

    public async Task SaveAsync(VaccineDto vaccine, CancellationToken cancellationToken)
    {
        db.Add(new VaccineRecord
        {
            Id = vaccine.Id,
            Name = vaccine.Name,
            NormalizedName = Normalize(vaccine.Name),
            PrimaryDoses = (short)vaccine.PrimaryDoses,
            BoosterDoses = (short)vaccine.BoosterDoses,
        });

        // The handler checks the name first; the unique index settles two registrations that pass that check together.
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation(VaccineRecordConfiguration.NormalizedNameIndex))
        {
            throw VaccinesErrors.NameAlreadyRegistered();
        }
    }

    // The name is unique without case and without the spaces at the ends (RN05).
    private static string Normalize(string name) => name.Trim().ToLowerInvariant();
}
