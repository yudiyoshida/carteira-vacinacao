namespace VaccinationCard.Api.Modules.Accounts.Application.Security;

public interface IPasswordHasher
{
    string Hash(string password);

    // Without a hash, the check still runs and fails: an unknown e-mail costs the same time as a wrong password (RN22).
    bool Verify(string? passwordHash, string password);
}
