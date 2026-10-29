using FluentValidation;
using VaccinationCard.Api.Core.Behaviors;

namespace VaccinationCard.Api.Core;

public static class CoreExtensions
{
    // The API is a single assembly, so one scan registers the handlers and validators of every module.
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        var assembly = typeof(CoreExtensions).Assembly;

        services.AddMediatR(options =>
        {
            options.RegisterServicesFromAssembly(assembly);
            options.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
