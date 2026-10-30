using MediatR;
using VaccinationCard.Api.Modules.Accounts.Application.Dtos;

namespace VaccinationCard.Api.Modules.Accounts.Application.UseCases.FindAccountById;

// Sent by Authentication, to check the account when renewing a session (TechSpec §4.3).
public sealed record FindAccountByIdQuery(Guid Id) : IRequest<AccountDto?>;
