using VaccinationCard.Api.Shared.Errors;

namespace VaccinationCard.Api.Modules.People.Domain.Errors;

public static class PeopleErrors
{
    public static NotFoundException PersonNotFound()
    {
        return new("person.not_found", "Pessoa não encontrada.");
    }

    public static ConflictException CpfAlreadyRegistered()
    {
        return new("person.cpf_already_registered", "Já existe uma pessoa cadastrada com este CPF.");
    }
}
