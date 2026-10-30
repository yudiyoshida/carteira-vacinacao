using MediatR;
using VaccinationCard.Api.Modules.Accounts.Application.Dtos;

namespace VaccinationCard.Api.Modules.Accounts.Application.UseCases.VerifyAccountCredentials;

// Sent by Authentication at login. Null does not say which credential failed (RN22).
public sealed record VerifyAccountCredentialsQuery(string Email, string Password) : IRequest<AccountDto?>;
