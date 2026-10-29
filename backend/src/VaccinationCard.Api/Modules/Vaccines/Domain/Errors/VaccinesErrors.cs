using VaccinationCard.Api.Shared.Errors;

namespace VaccinationCard.Api.Modules.Vaccines.Domain.Errors;

public static class VaccinesErrors
{
    public static NotFoundException VaccineNotFound()
    {
        return new("vaccine.not_found", "Vacina não encontrada.");
    }

    public static ConflictException NameAlreadyRegistered()
    {
        return new("vaccine.name_already_registered", "Já existe uma vacina cadastrada com este nome.");
    }
}
