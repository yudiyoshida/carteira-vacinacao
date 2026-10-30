using MediatR;
using VaccinationCard.Api.Modules.People.Application.Dtos;
using VaccinationCard.Api.Modules.People.Application.Persistence.Dao;
using VaccinationCard.Api.Modules.People.Domain.Errors;
using VaccinationCard.Api.Shared.ValueObjects;

namespace VaccinationCard.Api.Modules.People.Application.UseCases.FindPersonById;

public sealed class FindPersonByIdHandler(IPersonDao dao) : IRequestHandler<FindPersonByIdQuery, PersonDto>
{
    // The lookup of a person shows the whole CPF, formatted (RN26).
    public async Task<PersonDto> Handle(FindPersonByIdQuery request, CancellationToken cancellationToken)
    {
        var person = await dao.FindByIdAsync(request.Id, cancellationToken) ?? throw PeopleErrors.PersonNotFound();
        return person with { Cpf = Cpf.Create(person.Cpf).Formatted };
    }
}
