# DAO

Modelo. Quando o repositório já tiver um exemplo real, siga o exemplo real.

```csharp
namespace VaccinationCard.Api.Modules.Vaccines.Infra.Driven.Persistence.EfCore;

public sealed class VaccineDao(AppDbContext db) : IVaccineDao
{
    public Task<VaccineDto?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Set<VaccineRecord>()
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new VaccineDto(x.Id, x.Name, x.PrimaryDoses, x.BoosterDoses))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<Paginated<VaccineDto>> FindAllAsync(string? search, int page, int? size, CancellationToken cancellationToken)
    {
        var query = db.Set<VaccineRecord>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = LikePattern.Contains(search);
            query = query.Where(x => EF.Functions.ILike(
                EF.Functions.Unaccent(x.Name), EF.Functions.Unaccent(pattern), LikePattern.EscapeCharacter));
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var itemsPerPage = size ?? Math.Max(totalItems, 1);
        var data = await query
            .OrderBy(x => x.Name)
            .Skip((page - 1) * itemsPerPage)
            .Take(itemsPerPage)
            .Select(x => new VaccineDto(x.Id, x.Name, x.PrimaryDoses, x.BoosterDoses))
            .ToListAsync(cancellationToken);

        return Paginated<VaccineDto>.Create(data, totalItems, page, itemsPerPage);
    }

    public async Task SaveAsync(VaccineDto vaccine, CancellationToken cancellationToken)
    {
        db.Add(new VaccineRecord
        {
            Id = vaccine.Id,
            Name = vaccine.Name,
            NormalizedName = vaccine.Name.ToLowerInvariant(),
            PrimaryDoses = (short)vaccine.PrimaryDoses,
            BoosterDoses = (short)vaccine.BoosterDoses,
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation("ux_vaccines_normalized_name"))
        {
            throw VaccinesErrors.NameAlreadyRegistered();
        }
    }
}
```

## Busca por nome

- `LikePattern.Contains(termo)` escapa `\`, `%` e `_` e devolve `%termo%`; `LikePattern.EscapeCharacter` é `\`. Os dois ficam em `Infra/Database`.
- `unaccent` nos dois lados faz "Joao" encontrar "João"; o `ILIKE` ignora maiúsculas (RN19 e RN20).

## Tradução do 23505

`IsUniqueViolation(<índice>)`, em `Infra/Database`, confere se a `DbUpdateException` vem de uma `PostgresException` com `SqlState` `23505` naquele índice. O `catch` com `when` converte só a violação esperada; o resto segue como erro inesperado.
