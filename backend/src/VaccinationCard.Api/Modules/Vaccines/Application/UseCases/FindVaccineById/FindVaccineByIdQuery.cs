using MediatR;
using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;

namespace VaccinationCard.Api.Modules.Vaccines.Application.UseCases.FindVaccineById;

// Also sent by People, to read the scheme of a vaccine (TechSpec §4.3).
public sealed record FindVaccineByIdQuery(Guid Id) : IRequest<VaccineDto>;
