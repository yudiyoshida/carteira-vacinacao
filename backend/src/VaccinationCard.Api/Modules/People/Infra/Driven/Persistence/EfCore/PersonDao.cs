using Microsoft.EntityFrameworkCore;
using VaccinationCard.Api.Infra.Database;
using VaccinationCard.Api.Modules.People.Application.Dtos;
using VaccinationCard.Api.Modules.People.Application.Persistence.Dao;
using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;
using VaccinationCard.Api.Shared.Pagination;
using VaccinationCard.Api.Shared.ValueObjects;

namespace VaccinationCard.Api.Modules.People.Infra.Driven.Persistence.EfCore;

public sealed class PersonDao(AppDbContext db) : IPersonDao
{
    public Task<Paginated<PersonSummaryDto>> FindAllAsync(string? search, int page, int size, CancellationToken cancellationToken)
    {
        var query = db.Set<PersonRecord>().AsNoTracking();

        // unaccent on both sides ignores the accents, and ILIKE ignores the case (RN19).
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = LikePattern.Contains(search);
            query = query.Where(x => EF.Functions.ILike(EF.Functions.Unaccent(x.Name), EF.Functions.Unaccent(pattern), LikePattern.EscapeCharacter));
        }

        // Names repeat, so the id breaks the ties: without it, a person could show up on two pages, or on none
        // (TechSpec §6.2).
        return query
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Select(x => new PersonSummaryDto(x.Id, x.Name, x.Cpf))
            .ToPaginatedAsync(page, size, cancellationToken);
    }

    public Task<PersonDto?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return db.Set<PersonRecord>()
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new PersonDto(x.Id, x.Name, x.Cpf))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<VaccinationDto?> FindVaccinationAsync(Guid personId, Guid vaccinationId, CancellationToken cancellationToken)
    {
        return db.Set<VaccinationRecord>()
            .AsNoTracking()
            .Where(x => x.PersonId == personId && x.Id == vaccinationId)
            .Select(x => new VaccinationDto(x.Id, x.VaccineId, new DoseDto(x.DoseType, x.DoseNumber), x.AppliedOn))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> ExistsByCpfAsync(Cpf cpf, CancellationToken cancellationToken)
    {
        return db.Set<PersonRecord>().AnyAsync(x => x.Cpf == cpf.Value, cancellationToken);
    }
}
