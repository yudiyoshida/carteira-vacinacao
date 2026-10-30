using Microsoft.EntityFrameworkCore;
using VaccinationCard.Api.Modules.People.Infra.Driven.Persistence.EfCore;
using VaccinationCard.Api.Modules.Vaccines.Infra.Driven.Persistence.EfCore;

namespace VaccinationCard.Api.Infra.Database;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // The name searches ignore accents with unaccent (RN19, RN20).
        modelBuilder.HasPostgresExtension("unaccent");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // The key between two modules lives here, the only place that sees the records of both: the database keeps the
        // integrity, and People never reads the vaccines table (TechSpec §4.3).
        modelBuilder.Entity<VaccinationRecord>()
            .HasOne<VaccineRecord>()
            .WithMany()
            .HasForeignKey(x => x.VaccineId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
