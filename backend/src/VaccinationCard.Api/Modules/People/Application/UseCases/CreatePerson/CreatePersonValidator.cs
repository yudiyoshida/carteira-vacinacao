using FluentValidation;
using VaccinationCard.Api.Modules.People.Domain.Entities;
using VaccinationCard.Api.Shared.ValueObjects;

namespace VaccinationCard.Api.Modules.People.Application.UseCases.CreatePerson;

public sealed class CreatePersonValidator : AbstractValidator<CreatePersonCommand>
{
    public CreatePersonValidator()
    {
        RuleFor(x => x.Name)
            .Must(name => name?.Trim().Length is >= Person.MinNameLength and <= Person.MaxNameLength)
            .WithMessage("O nome deve ter entre 3 e 150 caracteres.");

        RuleFor(x => x.Cpf)
            .Must(Cpf.IsValid)
            .WithMessage("O CPF informado é inválido.");
    }
}
