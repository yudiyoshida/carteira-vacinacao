using MediatR;
using VaccinationCard.Api.Core.Messages;
using VaccinationCard.Api.Modules.People.Application.Persistence.Repository;
using VaccinationCard.Api.Modules.People.Domain.Errors;

namespace VaccinationCard.Api.Modules.People.Application.UseCases.DeletePerson;

public sealed class DeletePersonHandler(IPersonRepository repository) : IRequestHandler<DeletePersonCommand, SuccessMessage>
{
    public async Task<SuccessMessage> Handle(DeletePersonCommand request, CancellationToken cancellationToken)
    {
        if (!await repository.DeleteAsync(request.Id, cancellationToken))
        {
            throw PeopleErrors.PersonNotFound();
        }

        return new SuccessMessage("Pessoa removida com sucesso.");
    }
}
