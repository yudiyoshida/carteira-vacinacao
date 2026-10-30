using VaccinationCard.Api.Modules.People.Application.Dtos;
using VaccinationCard.Api.Shared.ValueObjects;

namespace VaccinationCard.Api.Modules.People.Application.Persistence.Dao;

// Only reads: the aggregate is written by IPersonRepository (ADR-0003). The CPF comes back as its 11 digits, and each
// use case picks the form of its output (RN26).
public interface IPersonDao
{
    Task<PersonDto?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByCpfAsync(Cpf cpf, CancellationToken cancellationToken);
}
