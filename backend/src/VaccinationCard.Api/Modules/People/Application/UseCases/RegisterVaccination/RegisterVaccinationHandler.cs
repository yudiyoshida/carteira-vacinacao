using MediatR;
using VaccinationCard.Api.Core.Time;
using VaccinationCard.Api.Modules.People.Application.Persistence.Repository;
using VaccinationCard.Api.Modules.People.Domain.Errors;
using VaccinationCard.Api.Modules.Vaccines.Application.UseCases.FindVaccineById;
using VaccinationCard.Api.Modules.Vaccines.Domain.ValueObjects;

namespace VaccinationCard.Api.Modules.People.Application.UseCases.RegisterVaccination;

public sealed class RegisterVaccinationHandler(IPersonRepository repository, ISender sender, IClock clock)
    : IRequestHandler<RegisterVaccinationCommand, RegisterVaccinationOutput>
{
    // The scheme comes from Vaccines by query (TechSpec §4.3), and the aggregate checks the rules against it.
    public async Task<RegisterVaccinationOutput> Handle(RegisterVaccinationCommand request, CancellationToken cancellationToken)
    {
        var person = await repository.FindByIdAsync(request.PersonId, cancellationToken) ?? throw PeopleErrors.PersonNotFound();
        var vaccine = await sender.Send(new FindVaccineByIdQuery(request.VaccineId), cancellationToken);

        var vaccination = person.RegisterVaccination(
            vaccine.Id,
            new DoseScheme(vaccine.PrimaryDoses, vaccine.BoosterDoses),
            new Dose(request.Dose.Type, request.Dose.Number),
            request.AppliedOn,
            clock.Today
        );

        await repository.EditAsync(person, cancellationToken);
        return new RegisterVaccinationOutput(vaccination.Id);
    }
}
