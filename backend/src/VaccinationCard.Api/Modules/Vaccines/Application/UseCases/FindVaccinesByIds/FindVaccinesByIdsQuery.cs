using MediatR;
using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;

namespace VaccinationCard.Api.Modules.Vaccines.Application.UseCases.FindVaccinesByIds;

// Sent by People, to read the names and schemes of the vaccines of a vaccination card (TechSpec §4.3).
public sealed record FindVaccinesByIdsQuery(IReadOnlyCollection<Guid> Ids) : IRequest<IReadOnlyList<VaccineDto>>;
