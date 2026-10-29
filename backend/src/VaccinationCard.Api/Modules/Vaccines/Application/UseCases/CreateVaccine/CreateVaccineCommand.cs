using MediatR;

namespace VaccinationCard.Api.Modules.Vaccines.Application.UseCases.CreateVaccine;

public sealed record CreateVaccineCommand(string Name, int PrimaryDoses, int BoosterDoses) : IRequest<CreateVaccineOutput>;
