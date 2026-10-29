using MediatR;
using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;
using VaccinationCard.Api.Modules.Vaccines.Application.Persistence.Dao;
using VaccinationCard.Api.Modules.Vaccines.Domain.Errors;
using VaccinationCard.Api.Modules.Vaccines.Domain.ValueObjects;

namespace VaccinationCard.Api.Modules.Vaccines.Application.UseCases.CreateVaccine;

public sealed class CreateVaccineHandler(IVaccineDao dao) : IRequestHandler<CreateVaccineCommand, CreateVaccineOutput>
{
    public async Task<CreateVaccineOutput> Handle(CreateVaccineCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (await dao.ExistsByNameAsync(name, cancellationToken))
        {
            throw VaccinesErrors.NameAlreadyRegistered();
        }

        var scheme = new DoseScheme(request.PrimaryDoses, request.BoosterDoses);
        var vaccine = new VaccineDto(Guid.CreateVersion7(), name, scheme.PrimaryDoses, scheme.BoosterDoses);
        await dao.SaveAsync(vaccine, cancellationToken);

        return new CreateVaccineOutput(vaccine.Id);
    }
}
