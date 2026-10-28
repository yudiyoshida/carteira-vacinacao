# Registro e configuração

Modelo. Quando o repositório já tiver um exemplo real, siga o exemplo real. Um tipo por arquivo.

```csharp
namespace VaccinationCard.Api.Modules.Vaccines.Infra.Driven.Persistence.EfCore;

public sealed class VaccineRecord
{
    public required Guid Id { get; set; }

    public required string Name { get; set; }

    public required string NormalizedName { get; set; }

    public required short PrimaryDoses { get; set; }

    public required short BoosterDoses { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class VaccineRecordConfiguration : IEntityTypeConfiguration<VaccineRecord>
{
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
        builder.HasIndex(x => x.NormalizedName).IsUnique().HasDatabaseName("ux_vaccines_normalized_name");
    }
}
```

O `AppDbContext` aplica todas as configurações do assembly: um registro novo entra no modelo sem mudar o contexto.

## Filho do agregado

```csharp
public sealed class PersonRecord
{
    public required Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Cpf { get; set; }

    public List<VaccinationRecord> Vaccinations { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }
}
```

A configuração de `VaccinationRecord` declara:

- `person_id` com `OnDelete(DeleteBehavior.Cascade)` e `vaccine_id` com `OnDelete(DeleteBehavior.Restrict)`;
- `dose_type` como texto, com `HasConversion<string>()`;
- o índice único da dose, `ux_vaccinations_person_vaccine_dose`, sobre `(person_id, vaccine_id, dose_type, dose_number)`.
