using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VaccinationCard.Api.Modules.Accounts.Infra.Driven.Persistence.EfCore;

public sealed class AccountRecordConfiguration : IEntityTypeConfiguration<AccountRecord>
{
    public const string EmailIndex = "ux_accounts_email";

    public void Configure(EntityTypeBuilder<AccountRecord> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Email).HasMaxLength(254);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        builder.HasIndex(x => x.Email).IsUnique().HasDatabaseName(EmailIndex);
    }
}
