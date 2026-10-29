using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace VaccinationCard.Api.Infra.Database;

public static class DbUpdateExceptionExtensions
{
    // Matches one unique index, so each index maps to the error code of its rule (TechSpec §6.2).
    public static bool IsUniqueViolation(this DbUpdateException exception, string indexName) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres
        && postgres.ConstraintName == indexName;
}
