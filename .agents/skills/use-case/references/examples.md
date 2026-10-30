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

Exemplo real: `backend/src/VaccinationCard.Api/Modules/People/Application/UseCases/RegisterVaccination/`.

- O handler recebe o Repository, o `ISender` e o `IClock`. Ele carrega a pessoa pelo `repository.FindByIdAsync` e lança `PeopleErrors.PersonNotFound()` quando ela não existe.
- A vacina vem de Vaccines pela `FindVaccineByIdQuery`; uma vacina inexistente sai como o `vaccine.not_found` do outro módulo, sem tradução.
- O handler monta o `DoseScheme` e a `Dose`, chama `person.RegisterVaccination(..., clock.Today)` e grava pelo `repository.EditAsync`. A saída é o id da vacinação nova.
- `Dose` é um `DoseDto` de Vaccines. O validator confere só a forma: vacina, dose e data presentes, tipo definido e número a partir de 1. O esquema, a sequência e as datas são regras do agregado.

Command sobre um filho do agregado: `backend/src/VaccinationCard.Api/Modules/People/Application/UseCases/CorrectVaccinationDate/`.

- O handler recebe o Repository e o `IClock`, sem query de outro módulo, porque a correção não depende do esquema da vacina. Ele chama `person.CorrectVaccinationDate(..., clock.Today)` e grava pelo `repository.EditAsync`.
- A vacinação inexistente, ou de outra pessoa, sai do agregado como `vaccination.not_found`, sem consulta antes.
- O validator confere só a presença da data, e a saída é o `SuccessMessage` com a mensagem da TechSpec §7.1.

Sem data no comando: `backend/src/VaccinationCard.Api/Modules/People/Application/UseCases/DeleteVaccination/`.

- O handler recebe só o Repository. Ele chama `person.RemoveVaccination(request.VaccinationId)` e grava pelo `repository.EditAsync`.
- O command não tem validator: os dois ids chegam da rota como `Guid`.

## Query de um filho do agregado

Exemplo real: `backend/src/VaccinationCard.Api/Modules/People/Application/UseCases/FindVaccinationById/`.

- O handler lê a vacinação pelo `dao.FindVaccinationAsync(personId, vaccinationId)`, que filtra pela pessoa e pelo id.
- Quando o DAO devolve `null`, o handler confere a pessoa pelo `dao.FindByIdAsync`: uma pessoa inexistente responde `PeopleErrors.PersonNotFound()`, como nas rotas que carregam o agregado; senão, `PeopleErrors.VaccinationNotFound()` (TechSpec §5.2). A consulta extra só roda no caminho de erro.

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

Query só para outro módulo, sem rota: `backend/src/VaccinationCard.Api/Modules/Vaccines/Application/UseCases/FindVaccinesByIds/`.

- A query recebe `IReadOnlyCollection<Guid>` e devolve `IReadOnlyList<VaccineDto>`, na ordem por nome que o DAO aplica (TechSpec §6.2).
- Um id sem vacina fica fora da lista, sem `NotFound`: os ids vêm das vacinações, e a chave estrangeira delas mantém as vacinas. O comentário do handler registra o motivo.

## Query que junta dois módulos

Exemplo real: `backend/src/VaccinationCard.Api/Modules/People/Application/UseCases/FindVaccinationCard/`.

- O handler lê os dados do módulo pelo DAO (`dao.FindCardDataAsync`) e lança `PeopleErrors.PersonNotFound()` quando ele devolve `null`, antes de consultar o outro módulo.
- O outro módulo entra por uma query só, com os ids distintos: `sender.Send(new FindVaccinesByIdsQuery(vaccineIds), cancellationToken)` (ADR-0011).
- A ordem das vacinas vem da query; o handler junta as vacinações de cada vacina por `ToLookup` e mantém essa ordem (RN17).
- A montagem fica num helper privado do handler (`ToEntry`), porque A7 não aceita na pasta do caso de uso um tipo sem os sufixos dela.
- As regras de dose vêm do domínio de Vaccines (exceção de A4): as doses se ordenam pela `Dose`, e a próxima dose esperada sai de `DoseScheme.NextExpectedDose`, o mesmo método do agregado.
- O CPF sai formatado (RN26), e a saída é o DTO do contrato da TechSpec §7.3 (`VaccinationCardDto`).

## Command com SuccessMessage

Exemplo real: `backend/src/VaccinationCard.Api/Modules/People/Application/UseCases/DeletePerson/`.

- O `SuccessMessage` (`Core/Messages`, TechSpec §4.1) é um `record` com `Message`, que a rota devolve como `200 { message }`.
- O command não tem validator: o id chega como `Guid`.
- O handler remove pelo `repository.DeleteAsync`, que devolve se a pessoa existia, sem ler a pessoa antes. Quando ela não existia, lança `PeopleErrors.PersonNotFound()`.
- A mensagem é a da TechSpec §7.1: `new SuccessMessage("Pessoa removida com sucesso.")`.
