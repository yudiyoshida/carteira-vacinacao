# Esqueleto de módulo

Crie só as pastas que o módulo usa. Entidade, factory e Repository existem só em módulo com regra de negócio.

```
Modules/<Módulo>/
├── <Módulo>Module.cs
├── Domain/
│   ├── Entities/
│   ├── Factories/
│   ├── ValueObjects/
│   ├── Enums/
│   └── Errors/<Módulo>Errors.cs
├── Application/
│   ├── Dtos/
│   ├── Persistence/
│   │   ├── Dao/I<Entidade>Dao.cs
│   │   └── Repository/I<Entidade>Repository.cs
│   ├── Security/
│   ├── Services/
│   └── UseCases/<Verbo><Entidade>/
└── Infra/
    ├── Drivers/Http/
    │   └── Requests/
    └── Driven/
        ├── Persistence/EfCore/
        └── Security/
```

`<Módulo>Module.cs` registra só os ports e adapters do módulo. O MediatR e o FluentValidation varrem o assembly uma vez, no `Program.cs`.

```csharp
namespace VaccinationCard.Api.Modules.Vaccines;

public static class VaccinesModule
{
    public static IServiceCollection AddVaccinesModule(this IServiceCollection services)
    {
        services.AddScoped<IVaccineDao, VaccineDao>();
        return services;
    }
}
```
