# DAO

Uma seção cujo código já existe aponta o arquivo real e lista o que observar nele. Uma seção cujo código ainda não existe traz um modelo; quando a task cria o código, a seção passa a apontar o arquivo real.

Exemplo real: o port `backend/src/VaccinationCard.Api/Modules/Vaccines/Application/Persistence/Dao/IVaccineDao.cs` e a implementação `backend/src/VaccinationCard.Api/Modules/Vaccines/Infra/Driven/Persistence/EfCore/VaccineDao.cs`.

## Port

- O port recebe e devolve DTOs, value objects e tipos primitivos: `Paginated<VaccineDto>`, `VaccineDto?` e `bool`. O `IPersonDao.ExistsByCpfAsync` recebe o `Cpf`, que garante a consulta pelos 11 dígitos.
- Num módulo sem agregado, o DAO também grava (`SaveAsync`), como registra o comentário do port (ADR-0003). Num módulo com agregado, o DAO só lê: `backend/src/VaccinationCard.Api/Modules/People/Infra/Driven/Persistence/EfCore/PersonDao.cs`.
- O `PersonDao` devolve o CPF só com os dígitos, e o handler escolhe a forma de cada saída (RN26).

## Consulta

- A consulta parte de `db.Set<VaccineRecord>()`, com `AsNoTracking()` e projeção direta no DTO, com `Select`. O `AppDbContext` não declara `DbSet`.
- Consulta por id: `Where`, `Select` e `SingleOrDefaultAsync`. Um id inexistente volta `null`, e o handler lança o `NotFound` do catálogo.
- Lista, nesta ordem: o filtro da busca, `OrderBy(x => x.Name)`, o `Select` no DTO e `ToPaginatedAsync(page, size, cancellationToken)`.

## Gravação

- O DAO calcula o valor que garante a unicidade. O `Normalize` apara o nome e põe em minúsculas, e a mesma função serve à gravação (`NormalizedName`) e à consulta (`ExistsByNameAsync`), porque o nome é único sem maiúsculas e sem os espaços das pontas (RN05).
- Uma coluna `smallint` é `short` no registro. O DAO converte o `int` do DTO com `(short)`, e a projeção volta para `int` sem conversão explícita.
- O `SaveChangesAsync` fica num `try`. O `catch (DbUpdateException exception) when (exception.IsUniqueViolation(VaccineRecordConfiguration.NormalizedNameIndex))` lança o mesmo erro do handler, `VaccinesErrors.NameAlreadyRegistered()`. O handler confere o nome antes, e o índice resolve duas gravações que passam juntas por essa checagem.

## Busca por nome

- `LikePattern.Contains(termo)` escapa `\`, `%` e `_` e devolve `%termo%`; `LikePattern.EscapeCharacter` é `\`. Os dois ficam em `Infra/Database`.
- `unaccent` nos dois lados faz "Joao" encontrar "João"; o `ILIKE` ignora maiúsculas (RN19 e RN20).
- O filtro só entra quando a busca tem texto: `EF.Functions.ILike(EF.Functions.Unaccent(x.Name), EF.Functions.Unaccent(pattern), LikePattern.EscapeCharacter)`.

## Paginação

- `ToPaginatedAsync(page, size)`, em `Infra/Database`, recebe a consulta já filtrada, ordenada e projetada. Ele conta os itens, lê só a página pedida e monta o `Paginated<T>`.
- Sem `size`, uma página só traz todos os itens. Uma página além da última volta vazia, sem ler os itens (TechSpec §7.1).
- O offset é calculado em `long`, então um `page` enorme não estoura o `int` nem vira `OFFSET` negativo.

## Tradução do 23505

`IsUniqueViolation(<índice>)`, em `Infra/Database`, confere se a `DbUpdateException` vem de uma `PostgresException` com `SqlState` `23505` naquele índice. O nome do índice vem da constante da configuração (`references/records.md`). O `catch` com `when` converte só a violação esperada; o resto segue como erro inesperado.
