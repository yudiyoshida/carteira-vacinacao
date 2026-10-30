using MediatR;
using VaccinationCard.Api.Core.Messages;
using VaccinationCard.Api.Modules.People.Application.Persistence.Repository;
using VaccinationCard.Api.Modules.People.Domain.Errors;

namespace VaccinationCard.Api.Modules.People.Application.UseCases.DeleteVaccination;

public sealed class DeleteVaccinationHandler(IPersonRepository repository)
    : IRequestHandler<DeleteVaccinationCommand, SuccessMessage>
{
    public async Task<SuccessMessage> Handle(DeleteVaccinationCommand request, CancellationToken cancellationToken)
    {
        var person = await repository.FindByIdAsync(request.PersonId, cancellationToken) ?? throw PeopleErrors.PersonNotFound();

        person.RemoveVaccination(request.VaccinationId);

        await repository.EditAsync(person, cancellationToken);
        return new SuccessMessage("Vacinação excluída com sucesso.");
    }
}
