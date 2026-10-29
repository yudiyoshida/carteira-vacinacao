using System.Text.RegularExpressions;
using ArchUnitNET.Domain;
using ArchUnitNET.Loader;

namespace VaccinationCard.ArchitectureTests;

public static partial class ApiArchitecture
{
    // The rules only see types from loaded assemblies, so the API is loaded with its dependencies.
    public static Architecture Instance { get; } = new ArchLoader()
        .LoadAssembliesIncludingDependencies(typeof(Program).Assembly)
        .Build();

    public static IReadOnlyList<string> Modules { get; } =
    [
        .. Instance.Namespaces
            .Select(@namespace => ModuleNamespace().Match(@namespace.FullName))
            .Where(match => match.Success)
            .Select(match => match.Groups["module"].Value)
            .Distinct()
            .Order(),
    ];

    [GeneratedRegex(@"^VaccinationCard\.Api\.Modules\.(?<module>\w+)(\.|$)")]
    private static partial Regex ModuleNamespace();
}
