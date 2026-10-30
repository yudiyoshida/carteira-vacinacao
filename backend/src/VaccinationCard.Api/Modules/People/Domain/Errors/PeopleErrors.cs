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

    public static NotFoundException VaccinationNotFound()
    {
        return new("vaccination.not_found", "Vacinação não encontrada.");
    }

    public static BusinessRuleException DoseOutsideScheme()
    {
        return new("vaccination.dose_outside_scheme", "A dose informada não faz parte do esquema desta vacina.");
    }

    public static ConflictException DoseAlreadyRegistered()
    {
        return new("vaccination.dose_already_registered", "Esta dose já foi registrada para esta pessoa.");
    }

    public static BusinessRuleException DoseOutOfSequence()
    {
        return new("vaccination.dose_out_of_sequence", "A dose informada não é a próxima dose esperada desta vacina.");
    }

    public static BusinessRuleException AppliedOnInFuture()
    {
        return new("vaccination.applied_on_in_future", "A data de aplicação não pode ser futura.");
    }

    public static BusinessRuleException AppliedOnBeforeMinimum()
    {
        return new("vaccination.applied_on_before_minimum", "A data de aplicação não pode ser anterior a 01/01/1900.");
    }

    public static BusinessRuleException AppliedOnNotAfterPrevious()
    {
        return new("vaccination.applied_on_not_after_previous", "A data de aplicação deve ser posterior à da dose anterior.");
    }

    public static BusinessRuleException AppliedOnOutOfOrder()
    {
        return new("vaccination.applied_on_out_of_order", "A nova data deve ficar entre as datas da dose anterior e da dose seguinte.");
    }

    public static BusinessRuleException NotLastDose()
    {
        return new("vaccination.not_last_dose", "Só a última dose desta vacina pode ser excluída.");
    }
}
