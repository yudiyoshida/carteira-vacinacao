using FluentValidation;

namespace VaccinationCard.Api.Core.Validation;

// The parameters every list accepts (TechSpec §7.1). A missing value passes, and each use case picks its default.
public static class ListQueryRules
{
    public const int MaxSize = 100;
    public const int MaxSearchLength = 100;

    public static IRuleBuilderOptions<T, int?> ValidPage<T>(this IRuleBuilder<T, int?> rule) =>
        rule.GreaterThanOrEqualTo(1)
            .WithMessage("A página deve ser maior ou igual a 1.");

    public static IRuleBuilderOptions<T, int?> ValidSize<T>(this IRuleBuilder<T, int?> rule) =>
        rule.InclusiveBetween(1, MaxSize)
            .WithMessage("O tamanho da página deve ser de 1 a 100.");

    public static IRuleBuilderOptions<T, string?> ValidSearch<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(search => search is null || search.Trim().Length <= MaxSearchLength)
            .WithMessage("A busca deve ter no máximo 100 caracteres.");
}
