using MediatR;

namespace VaccinationCard.Api.Modules.People.Application.UseCases.CreatePerson;

public sealed record CreatePersonCommand(string Name, string Cpf) : IRequest<CreatePersonOutput>;
