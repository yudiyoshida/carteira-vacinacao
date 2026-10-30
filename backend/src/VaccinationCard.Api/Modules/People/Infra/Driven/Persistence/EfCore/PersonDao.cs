using Microsoft.EntityFrameworkCore;
using VaccinationCard.Api.Infra.Database;
using VaccinationCard.Api.Modules.People.Application.Dtos;
using VaccinationCard.Api.Modules.People.Application.Persistence.Dao;
using VaccinationCard.Api.Shared.ValueObjects;

namespace VaccinationCard.Api.Modules.People.Infra.Driven.Persistence.EfCore;

public sealed class PersonDao(AppDbContext db) : IPersonDao
{
    public Task<PersonDto?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return db.Set<PersonRecord>()
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new PersonDto(x.Id, x.Name, x.Cpf))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> ExistsByCpfAsync(Cpf cpf, CancellationToken cancellationToken)
    {
        return db.Set<PersonRecord>().AnyAsync(x => x.Cpf == cpf.Value, cancellationToken);
    }
}
