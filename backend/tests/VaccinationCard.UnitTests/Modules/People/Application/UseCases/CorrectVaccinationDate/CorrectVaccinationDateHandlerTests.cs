using VaccinationCard.Api.Core.Messages;
using VaccinationCard.Api.Core.Time;
using VaccinationCard.Api.Modules.People.Application.Persistence.Repository;
using VaccinationCard.Api.Modules.People.Application.UseCases.CorrectVaccinationDate;
using VaccinationCard.Api.Modules.People.Domain.Entities;
using VaccinationCard.Api.Modules.People.Domain.Factories;
using VaccinationCard.Api.Modules.Vaccines.Domain.Enums;
using VaccinationCard.Api.Modules.Vaccines.Domain.ValueObjects;
using VaccinationCard.Api.Shared.Errors;
using VaccinationCard.Api.Shared.ValueObjects;

namespace VaccinationCard.UnitTests.Modules.People.Application.UseCases.CorrectVaccinationDate;

public sealed class CorrectVaccinationDateHandlerTests
{
    // A past date, so a handler that read the real clock would accept the day after it.
    private static readonly DateOnly Today = new(2025, 6, 15);
    private static readonly Guid VaccineId = Guid.CreateVersion7();

    private readonly IPersonRepository _repository = Substitute.For<IPersonRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly Vaccination _first = PersonFactory.LoadVaccination(Guid.CreateVersion7(), VaccineId, new Dose(DoseType.Primary, 1), new DateOnly(2020, 1, 15));
    private readonly Vaccination _second = PersonFactory.LoadVaccination(Guid.CreateVersion7(), VaccineId, new Dose(DoseType.Primary, 2), new DateOnly(2020, 2, 15));
    private readonly Person _person;
    private readonly CorrectVaccinationDateHandler _sut;

    public CorrectVaccinationDateHandlerTests()
    {
        _person = PersonFactory.Load(Guid.CreateVersion7(), "Maria Silva", Cpf.Create("12345678909"), [_first, _second]);
        _repository.FindByIdAsync(_person.Id, Arg.Any<CancellationToken>()).Returns(_person);
        _clock.Today.Returns(Today);
        _sut = new CorrectVaccinationDateHandler(_repository, _clock);
    }

    [Fact]
    [Trait("RN", "RN14")]
    public async Task Should_Correct_Date_And_Edit_Person_When_New_Date_Is_Valid()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeCorrectVaccinationDateInput();

        // Act
        var output = await _sut.Handle(input, cancellationToken);

        // Assert
        output.ShouldBe(new SuccessMessage("Data de aplicação corrigida com sucesso."));
        _person.Vaccinations.Single(vaccination => vaccination.Id == _second.Id).AppliedOn.ShouldBe(input.AppliedOn);
        await _repository.Received(1).EditAsync(_person, cancellationToken);
    }

    [Fact]
    public async Task Should_Throw_Not_Found_When_Person_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeCorrectVaccinationDateInput() with { PersonId = Guid.CreateVersion7() };
        _repository.FindByIdAsync(input.PersonId, Arg.Any<CancellationToken>()).Returns((Person?)null);

        // Act
        var act = () => _sut.Handle(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<NotFoundException>(act);
        exception.Code.ShouldBe("person.not_found");
        await _repository.DidNotReceive().EditAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Not_Edit_Person_When_Vaccination_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeCorrectVaccinationDateInput() with { VaccinationId = Guid.CreateVersion7() };

        // Act
        var act = () => _sut.Handle(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<NotFoundException>(act);
        exception.Code.ShouldBe("vaccination.not_found");
        await _repository.DidNotReceive().EditAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("RN", "RN12")]
    public async Task Should_Check_The_Date_Against_The_Clock_When_Correcting()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeCorrectVaccinationDateInput() with { AppliedOn = Today.AddDays(1) };

        // Act
        var act = () => _sut.Handle(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<BusinessRuleException>(act);
        exception.Code.ShouldBe("vaccination.applied_on_in_future");
        await _repository.DidNotReceive().EditAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("RN", "RN14")]
    public async Task Should_Not_Edit_Person_When_New_Date_Is_Out_Of_Order()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeCorrectVaccinationDateInput() with { AppliedOn = _first.AppliedOn };

        // Act
        var act = () => _sut.Handle(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<BusinessRuleException>(act);
        exception.Code.ShouldBe("vaccination.applied_on_out_of_order");
        await _repository.DidNotReceive().EditAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>());
    }

    // The second dose is the last one, and the new date comes after the first.
    private CorrectVaccinationDateCommand MakeCorrectVaccinationDateInput() =>
        new(_person.Id, _second.Id, new DateOnly(2020, 3, 15));
}
