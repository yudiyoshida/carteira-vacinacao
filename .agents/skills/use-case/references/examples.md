# Exemplos e modelos de caso de uso

Uma seção cujo código já existe aponta o arquivo real e lista o que observar nele. Uma seção cujo código ainda não existe traz um modelo; quando a task cria o código, a seção passa a apontar o arquivo real. Os modelos omitem os `using` e juntam num bloco os tipos de um caso de uso, que no código ficam um por arquivo.

## Command de módulo CRUD

Exemplo real: `backend/src/VaccinationCard.Api/Modules/Vaccines/Application/UseCases/CreateVaccine/`.

- Um arquivo por tipo: `CreateVaccineCommand`, `CreateVaccineOutput`, `CreateVaccineValidator` e `CreateVaccineHandler`.
- O validator mede o nome já aparado, sem quebrar com `null`: `.Must(name => name?.Trim().Length is >= 2 and <= 100)`. Os limites das doses vêm das constantes do `DoseScheme`.
- O handler apara o nome, confere o nome repetido pelo `dao.ExistsByNameAsync` e lança `VaccinesErrors.NameAlreadyRegistered()`.
- O handler monta o `DoseScheme` só para garantir as invariantes (RN06). Sem entidade no módulo, o handler grava pelo `dao.SaveAsync` um `VaccineDto` com id de `Guid.CreateVersion7()`.
- A saída é o `CreateVaccineOutput` com o id, que a rota devolve como `201 { id }`.

## Command que cria o agregado

Exemplo real: `backend/src/VaccinationCard.Api/Modules/People/Application/UseCases/CreatePerson/`.

- O validator mede o nome aparado com os limites de `Person` (`MinNameLength` e `MaxNameLength`) e confere o CPF com `Cpf.IsValid`.
- O handler apara o nome, monta o `Cpf` (só os dígitos), confere o CPF repetido pelo `dao.ExistsByCpfAsync` e lança `PeopleErrors.CpfAlreadyRegistered()`.
- A entidade nasce pelo `PersonFactory.Create` e é gravada pelo `repository.SaveAsync`. A leitura usa o DAO, e a escrita, o Repository (ADR-0003).

## Query com a forma do CPF

Exemplo real: `backend/src/VaccinationCard.Api/Modules/People/Application/UseCases/FindPersonById/`.

- O DAO devolve o CPF só com os dígitos, e o handler escolhe a forma da saída: `person with { Cpf = Cpf.Create(person.Cpf).Formatted }` (RN26).
- A query não tem validator, e o handler lança `PeopleErrors.PersonNotFound()` quando o DAO devolve `null`.

## Command sobre o agregado

Modelo para a T14.

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

Exemplos reais: `backend/src/VaccinationCard.Api/Modules/Vaccines/Application/UseCases/FindAllVaccines/` e `backend/src/VaccinationCard.Api/Modules/People/Application/UseCases/FindAllPeople/`.

- A query recebe `Page`, `Size` e `Search` anuláveis e devolve o envelope do DTO do módulo: `Paginated<VaccineDto>` ou `Paginated<PersonSummaryDto>`.
- O validator usa `ValidPage`, `ValidSize` e `ValidSearch`, de `Core/Validation/ListQueryRules.cs`. Um valor ausente passa, e o handler aplica o padrão.
- O handler repassa ao DAO a busca aparada e a página padrão: `dao.FindAllAsync(request.Search?.Trim(), request.Page ?? 1, request.Size, cancellationToken)`. Sem `size`, uma página só traz todas as vacinas (RN20).
- Na lista de pessoas, o tamanho padrão fica numa constante do handler: `request.Size ?? DefaultSize`, com `DefaultSize` igual a 20 (RN19). O handler mascara o CPF de cada item e devolve o mesmo envelope: `page with { Data = [.. page.Data.Select(person => person with { Cpf = Cpf.Create(person.Cpf).Masked })] }` (RN26).

## Query usada por outro módulo

Exemplo real: `backend/src/VaccinationCard.Api/Modules/Vaccines/Application/UseCases/FindVaccineById/`.

- A query não tem validator: o id já chega como `Guid`.
- O handler lê pelo DAO e lança `VaccinesErrors.VaccineNotFound()` quando o DAO devolve `null`.
- A mesma query atende a rota `GET /api/v1/vaccines/{id}` e o registro de vacinação, que a envia por `ISender.Send` (TechSpec §4.3). O comentário da query registra esse segundo uso.

## Command com SuccessMessage

Exemplo real: `backend/src/VaccinationCard.Api/Modules/People/Application/UseCases/DeletePerson/`.

- O `SuccessMessage` (`Core/Messages`, TechSpec §4.1) é um `record` com `Message`, que a rota devolve como `200 { message }`.
- O command não tem validator: o id chega como `Guid`.
- O handler remove pelo `repository.DeleteAsync`, que devolve se a pessoa existia, sem ler a pessoa antes. Quando ela não existia, lança `PeopleErrors.PersonNotFound()`.
- A mensagem é a da TechSpec §7.1: `new SuccessMessage("Pessoa removida com sucesso.")`.
