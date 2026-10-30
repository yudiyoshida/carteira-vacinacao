using VaccinationCard.Api.Modules.People.Domain.Errors;
using VaccinationCard.Api.Modules.Vaccines.Domain.ValueObjects;
using VaccinationCard.Api.Shared.ValueObjects;

namespace VaccinationCard.Api.Modules.People.Domain.Entities;

public sealed class Person
{
    // The name has 3 to 150 characters, without the spaces at the ends (RN01).
    public const int MinNameLength = 3;
    public const int MaxNameLength = 150;

    private readonly List<Vaccination> _vaccinations = [];

    internal Person(Guid id, string name, Cpf cpf)
    {
        Id = id;
        Name = name;
        Cpf = cpf;
    }

    public Guid Id { get; }

    public string Name { get; }

    public Cpf Cpf { get; }

    public IReadOnlyList<Vaccination> Vaccinations => _vaccinations;

    // The checks follow the order of the PRD (RN09, RN11, RN10, RN12, RN13), which sets the error reported when the
    // registration breaks more than one rule.
    public Vaccination RegisterVaccination(Guid vaccineId, DoseScheme scheme, Dose dose, DateOnly appliedOn, DateOnly today)
    {
        if (!scheme.Contains(dose))
        {
            throw PeopleErrors.DoseOutsideScheme();
        }

        if (_vaccinations.Any(vaccination => vaccination.VaccineId == vaccineId && vaccination.Dose == dose))
        {
            throw PeopleErrors.DoseAlreadyRegistered();
        }

        if (dose != NextExpectedDose(vaccineId, scheme))
        {
            throw PeopleErrors.DoseOutOfSequence();
        }

        CheckAppliedOn(appliedOn, today);

        // The dose is the next expected one, so the previous dose is the last one registered for the vaccine (RN13).
        var previous = LastVaccination(vaccineId);
        if (previous is not null && appliedOn <= previous.AppliedOn)
        {
            throw PeopleErrors.AppliedOnNotAfterPrevious();
        }

        var vaccination = new Vaccination(Guid.CreateVersion7(), vaccineId, dose, appliedOn);
        _vaccinations.Add(vaccination);
        return vaccination;
    }

    // The registered doses of a vaccine are always a prefix of its sequence (RN10), so the next one sits at the index
    // given by their count; a complete scheme has none (TechSpec §5.2).
    public Dose? NextExpectedDose(Guid vaccineId, DoseScheme scheme)
    {
        var doses = scheme.OrderedDoses();
        var registered = _vaccinations.Count(vaccination => vaccination.VaccineId == vaccineId);
        return registered < doses.Count ? doses[registered] : null;
    }

    // The applied-on date is neither in the future nor before the minimum date (RN12).
    private static void CheckAppliedOn(DateOnly appliedOn, DateOnly today)
    {
        if (appliedOn > today)
        {
            throw PeopleErrors.AppliedOnInFuture();
        }

        if (appliedOn < Vaccination.MinAppliedOn)
        {
            throw PeopleErrors.AppliedOnBeforeMinimum();
        }
    }

    // The last dose is the most advanced in the sequence among the registered ones, whatever the order of the list.
    private Vaccination? LastVaccination(Guid vaccineId) =>
        _vaccinations.Where(vaccination => vaccination.VaccineId == vaccineId).MaxBy(vaccination => vaccination.Dose);
}
