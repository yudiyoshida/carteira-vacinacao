using MediatR;
using VaccinationCard.Api.Modules.People.Application.Persistence.Dao;
using VaccinationCard.Api.Modules.People.Application.Persistence.Repository;
using VaccinationCard.Api.Modules.People.Domain.Errors;
using VaccinationCard.Api.Modules.People.Domain.Factories;
using VaccinationCard.Api.Shared.ValueObjects;

namespace VaccinationCard.Api.Modules.People.Application.UseCases.CreatePerson;

public sealed class CreatePersonHandler(IPersonDao dao, IPersonRepository repository) : IRequestHandler<CreatePersonCommand, CreatePersonOutput>
{
    public async Task<CreatePersonOutput> Handle(CreatePersonCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var cpf = Cpf.Create(request.Cpf);
        if (await dao.ExistsByCpfAsync(cpf, cancellationToken))
        {
            throw PeopleErrors.CpfAlreadyRegistered();
        }

        var person = PersonFactory.Create(name, cpf);
        await repository.SaveAsync(person, cancellationToken);

        return new CreatePersonOutput(person.Id);
    }
}
