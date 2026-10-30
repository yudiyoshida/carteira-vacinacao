using FluentValidation;
using VaccinationCard.Api.Core.Behaviors;
using VaccinationCard.Api.Core.Time;

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

        // "Today" comes from the time zone of App:TimeZone (RN12). The tests replace the TimeProvider with a fake one.
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IClock>(provider =>
        {
            var timeZone = provider.GetRequiredService<IConfiguration>()["App:TimeZone"] ?? throw new InvalidOperationException("A chave 'App:TimeZone' não está configurada.");
            return new Clock(provider.GetRequiredService<TimeProvider>(), TimeZoneInfo.FindSystemTimeZoneById(timeZone));
        });

        return services;
    }
}
