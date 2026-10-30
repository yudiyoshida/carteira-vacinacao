using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VaccinationCard.Api.Modules.People.Infra.Driven.Persistence.EfCore;

// The foreign key to vaccines is declared in AppDbContext, because People does not reference the records of Vaccines
// (TechSpec §4.3).
public sealed class VaccinationRecordConfiguration : IEntityTypeConfiguration<VaccinationRecord>
{
    // The Repository turns a violation of this index into the error of RN11.
    public const string DoseIndex = "ux_vaccinations_person_vaccine_dose";

    // The Repository turns a violation of this key into the error of RN16: the person was removed after the read.
    public const string PersonForeignKey = "fk_vaccinations_people_person_id";

    public void Configure(EntityTypeBuilder<VaccinationRecord> builder)
    {
        builder.ToTable("vaccinations");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.DoseType).HasConversion<string>().HasMaxLength(10);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        builder.HasIndex(x => new { x.PersonId, x.VaccineId, x.DoseType, x.DoseNumber }).IsUnique().HasDatabaseName(DoseIndex);

        // Removing a person removes the vaccinations in the database (RN04, ADR-0009).
        builder.HasOne<PersonRecord>()
            .WithMany(x => x.Vaccinations)
            .HasForeignKey(x => x.PersonId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName(PersonForeignKey);
    }
}
