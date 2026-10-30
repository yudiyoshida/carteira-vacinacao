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

    public Task<Paginated<VaccineDto>> FindAllAsync(string? search, int page, int? size, CancellationToken cancellationToken)
    {
        var query = db.Set<VaccineRecord>().AsNoTracking();

        // unaccent on both sides ignores the accents, and ILIKE ignores the case (RN20).
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = LikePattern.Contains(search);
            query = query.Where(x => EF.Functions.ILike(EF.Functions.Unaccent(x.Name), EF.Functions.Unaccent(pattern), LikePattern.EscapeCharacter));
        }

        return query
            .OrderBy(x => x.Name)
            .Select(x => new VaccineDto(x.Id, x.Name, x.PrimaryDoses, x.BoosterDoses))
            .ToPaginatedAsync(page, size, cancellationToken);
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

## Paginação

- `ToPaginatedAsync(page, size)`, em `Infra/Database`, recebe a consulta já filtrada, ordenada e projetada. Ele conta os itens, lê só a página pedida e monta o `Paginated<T>`.
- Sem `size`, uma página só traz todos os itens. Uma página além da última volta vazia, sem ler os itens (TechSpec §7.1).
- O offset é calculado em `long`, então um `page` enorme não estoura o `int` nem vira `OFFSET` negativo.

## Tradução do 23505

`IsUniqueViolation(<índice>)`, em `Infra/Database`, confere se a `DbUpdateException` vem de uma `PostgresException` com `SqlState` `23505` naquele índice. O `catch` com `when` converte só a violação esperada; o resto segue como erro inesperado.
