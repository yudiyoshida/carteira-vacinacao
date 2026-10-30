using FluentValidation;

namespace VaccinationCard.Api.Modules.Authentication.Application.UseCases.SignIn;

public sealed class SignInValidator : AbstractValidator<SignInCommand>
{
    // The size of accounts.email (TechSpec §6.1).
    private const int MaxEmailLength = 254;

    public SignInValidator()
    {
        // Only the form of an e-mail: whether the account exists is the answer that RN22 keeps generic.
        RuleFor(x => x.Email)
            .Must(email => email?.Trim().Length is > 0 and <= MaxEmailLength && email.Contains('@'))
            .WithMessage("O e-mail informado é inválido.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("A senha é obrigatória.");
    }
}
