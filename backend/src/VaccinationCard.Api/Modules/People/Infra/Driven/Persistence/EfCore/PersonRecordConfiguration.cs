using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VaccinationCard.Api.Modules.People.Infra.Driven.Persistence.EfCore;

public sealed class PersonRecordConfiguration : IEntityTypeConfiguration<PersonRecord>
{
    // The Repository turns a violation of this index into the error of RN03.
    public const string CpfIndex = "ux_people_cpf";

    public void Configure(EntityTypeBuilder<PersonRecord> builder)
    {
        builder.ToTable("people");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).HasMaxLength(150);
        builder.Property(x => x.Cpf).HasMaxLength(11).IsFixedLength();
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        builder.HasIndex(x => x.Cpf).IsUnique().HasDatabaseName(CpfIndex);
    }
}
