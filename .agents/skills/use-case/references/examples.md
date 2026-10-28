# Modelos de caso de uso

Modelo. Quando o repositório já tiver um exemplo real, siga o exemplo real. Um tipo por arquivo; cada bloco abaixo junta os arquivos de um caso de uso.

## Command de módulo CRUD

```csharp
namespace VaccinationCard.Api.Modules.Vaccines.Application.UseCases.CreateVaccine;

public sealed record CreateVaccineCommand(string Name, int PrimaryDoses, int BoosterDoses)
    : IRequest<CreateVaccineOutput>;

public sealed record CreateVaccineOutput(Guid Id);

public sealed class CreateVaccineValidator : AbstractValidator<CreateVaccineCommand>
{
    public CreateVaccineValidator()
    {
        RuleFor(x => x.Name)
            .Must(name => name?.Trim().Length is >= 2 and <= 100)
            .WithMessage("O nome deve ter entre 2 e 100 caracteres.");

        RuleFor(x => x.PrimaryDoses)
            .InclusiveBetween(1, DoseScheme.MaxPrimaryDoses)
            .WithMessage("A vacina deve ter de 1 a 10 doses primárias.");

        RuleFor(x => x.BoosterDoses)
            .InclusiveBetween(0, DoseScheme.MaxBoosterDoses)
            .WithMessage("A vacina deve ter de 0 a 5 doses de reforço.");
    }
}

public sealed class CreateVaccineHandler(IVaccineDao dao) : IRequestHandler<CreateVaccineCommand, CreateVaccineOutput>
{
    public async Task<CreateVaccineOutput> Handle(CreateVaccineCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (await dao.ExistsByNameAsync(name, cancellationToken))
        {
            throw VaccinesErrors.NameAlreadyRegistered();
        }

        var scheme = new DoseScheme(request.PrimaryDoses, request.BoosterDoses);
        var vaccine = new VaccineDto(Guid.CreateVersion7(), name, scheme.PrimaryDoses, scheme.BoosterDoses);
        await dao.SaveAsync(vaccine, cancellationToken);

        return new CreateVaccineOutput(vaccine.Id);
    }
}
```

## Command sobre o agregado

```csharp
public sealed class RegisterVaccinationHandler(IPersonRepository repository, ISender sender, IClock clock)
    : IRequestHandler<RegisterVaccinationCommand, RegisterVaccinationOutput>
{
    public async Task<RegisterVaccinationOutput> Handle(RegisterVaccinationCommand request, CancellationToken cancellationToken)
    {
        var person = await repository.FindByIdAsync(request.PersonId, cancellationToken)
            ?? throw PeopleErrors.PersonNotFound();
        var vaccine = await sender.Send(new FindVaccineByIdQuery(request.VaccineId), cancellationToken);

        var vaccination = person.RegisterVaccination(
            vaccine.Id,
            new DoseScheme(vaccine.PrimaryDoses, vaccine.BoosterDoses),
            new Dose(request.Dose.Type, request.Dose.Number),
            request.AppliedOn,
            clock.Today);

        await repository.EditAsync(person, cancellationToken);
        return new RegisterVaccinationOutput(vaccination.Id);
    }
}
```

`request.Dose` é um `DoseDto` de Vaccines: a dose chega como DTO, o validator confere o tipo e o número, e o handler monta o value object.

## Query de lista

```csharp
public sealed record FindAllVaccinesQuery(int? Page, int? Size, string? Search) : IRequest<Paginated<VaccineDto>>;

public sealed class FindAllVaccinesHandler(IVaccineDao dao) : IRequestHandler<FindAllVaccinesQuery, Paginated<VaccineDto>>
{
    public Task<Paginated<VaccineDto>> Handle(FindAllVaccinesQuery request, CancellationToken cancellationToken) =>
        dao.FindAllAsync(request.Search?.Trim(), request.Page ?? 1, request.Size, cancellationToken);
}
```

Na lista de pessoas, o tamanho padrão é 20, e o handler mascara o CPF de cada item (RN26).

## Query usada por outro módulo

```csharp
public sealed record FindVaccineByIdQuery(Guid Id) : IRequest<VaccineDto>;

public sealed class FindVaccineByIdHandler(IVaccineDao dao) : IRequestHandler<FindVaccineByIdQuery, VaccineDto>
{
    public async Task<VaccineDto> Handle(FindVaccineByIdQuery request, CancellationToken cancellationToken) =>
        await dao.FindByIdAsync(request.Id, cancellationToken) ?? throw VaccinesErrors.VaccineNotFound();
}
```

A mesma query atende a rota `GET /api/v1/vaccines/{id}` e o registro de vacinação.

## Command com SuccessMessage

```csharp
public sealed class DeletePersonHandler(IPersonRepository repository) : IRequestHandler<DeletePersonCommand, SuccessMessage>
{
    public async Task<SuccessMessage> Handle(DeletePersonCommand request, CancellationToken cancellationToken)
    {
        if (!await repository.DeleteAsync(request.Id, cancellationToken))
        {
            throw PeopleErrors.PersonNotFound();
        }

        return new SuccessMessage("Pessoa removida com sucesso.");
    }
}
```
