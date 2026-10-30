using Microsoft.AspNetCore.Identity;
using VaccinationCard.Api.Modules.Accounts.Application.Security;

namespace VaccinationCard.Api.Modules.Accounts.Infra.Driven.Security;

public sealed class IdentityPasswordHasher : IPasswordHasher
{
    // PasswordHasher<T> does not read the user: the parameter serves the implementations that derive the salt from it.
    private static readonly object NoUser = new();

    private static readonly PasswordHasher<object> Hasher = new();

    // Matches no password that anyone knows. It exists so a check without an account costs a real verification (RN22).
    private static readonly string UnknownAccountHash = Hasher.HashPassword(NoUser, Guid.NewGuid().ToString());

    public string Hash(string password)
    {
        return Hasher.HashPassword(NoUser, password);
    }

    public bool Verify(string? passwordHash, string password)
    {
        var result = Hasher.VerifyHashedPassword(NoUser, passwordHash ?? UnknownAccountHash, password);
        return passwordHash is not null && result != PasswordVerificationResult.Failed;
    }
}
