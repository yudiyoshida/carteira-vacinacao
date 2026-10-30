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

Exemplo real de módulo CRUD: `backend/src/VaccinationCard.Api/Modules/Vaccines/`, com `Domain/ValueObjects`, `Domain/Errors`, `Application/Dtos`, `Application/Persistence/Dao`, `Application/UseCases`, `Infra/Drivers/Http` e `Infra/Driven/Persistence/EfCore`.

## Registro do módulo

Exemplo real: `backend/src/VaccinationCard.Api/Modules/Vaccines/VaccinesModule.cs`.

- A classe estática tem a extensão `Add<Módulo>Module`, que registra só os ports e adapters do módulo, como `services.AddScoped<IVaccineDao, VaccineDao>();`, e devolve o `services`.
- O MediatR e o FluentValidation varrem o assembly uma vez, no `AddCore` (`Core/CoreExtensions.cs`). Handler e validator novos não entram no registro do módulo.
- O `Program.cs` chama o `Add<Módulo>Module` depois dos registros globais, como `AddCore` e `AddDatabase`.
