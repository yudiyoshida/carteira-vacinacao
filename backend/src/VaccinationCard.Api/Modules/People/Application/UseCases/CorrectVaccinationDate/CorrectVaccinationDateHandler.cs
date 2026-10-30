using MediatR;
using VaccinationCard.Api.Core.Messages;
using VaccinationCard.Api.Core.Time;
using VaccinationCard.Api.Modules.People.Application.Persistence.Repository;
using VaccinationCard.Api.Modules.People.Domain.Errors;

namespace VaccinationCard.Api.Modules.People.Application.UseCases.CorrectVaccinationDate;

public sealed class CorrectVaccinationDateHandler(IPersonRepository repository, IClock clock)
    : IRequestHandler<CorrectVaccinationDateCommand, SuccessMessage>
{
    public async Task<SuccessMessage> Handle(CorrectVaccinationDateCommand request, CancellationToken cancellationToken)
    {
        var person = await repository.FindByIdAsync(request.PersonId, cancellationToken) ?? throw PeopleErrors.PersonNotFound();

        person.CorrectVaccinationDate(request.VaccinationId, request.AppliedOn, clock.Today);

        await repository.EditAsync(person, cancellationToken);
        return new SuccessMessage("Data de aplicação corrigida com sucesso.");
    }
}
