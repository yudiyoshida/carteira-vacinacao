using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VaccinationCard.Api.Modules.Authentication.Infra.Driven.Persistence.EfCore;

public sealed class SessionRecordConfiguration : IEntityTypeConfiguration<SessionRecord>
{
    public void Configure(EntityTypeBuilder<SessionRecord> builder)
    {
        builder.ToTable("sessions");

        // The account is the key: one session per account (RN23).
        builder.HasKey(x => x.AccountId);
        builder.Property(x => x.AccountId).ValueGeneratedNever();
        builder.Property(x => x.RefreshTokenHash).HasMaxLength(64).IsFixedLength();
        builder.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
    }
}
