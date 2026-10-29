using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VaccinationCard.Api.Modules.Vaccines.Infra.Driven.Persistence.EfCore;

public sealed class VaccineRecordConfiguration : IEntityTypeConfiguration<VaccineRecord>
{
    // The DAO turns a violation of this index into the error of RN05.
    public const string NormalizedNameIndex = "ux_vaccines_normalized_name";

    public void Configure(EntityTypeBuilder<VaccineRecord> builder)
    {
        builder.ToTable("vaccines", table =>
        {
            table.HasCheckConstraint("ck_vaccines_primary_doses", "primary_doses BETWEEN 1 AND 10");
            table.HasCheckConstraint("ck_vaccines_booster_doses", "booster_doses BETWEEN 0 AND 5");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).HasMaxLength(100);
        builder.Property(x => x.NormalizedName).HasMaxLength(100);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        builder.HasIndex(x => x.NormalizedName).IsUnique().HasDatabaseName(NormalizedNameIndex);
    }
}
