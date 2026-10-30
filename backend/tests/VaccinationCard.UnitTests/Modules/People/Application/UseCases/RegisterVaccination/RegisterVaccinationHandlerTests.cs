using MediatR;
using NSubstitute.ExceptionExtensions;
using VaccinationCard.Api.Core.Time;
using VaccinationCard.Api.Modules.People.Application.Persistence.Repository;
using VaccinationCard.Api.Modules.People.Application.UseCases.RegisterVaccination;
using VaccinationCard.Api.Modules.People.Domain.Entities;
using VaccinationCard.Api.Modules.People.Domain.Factories;
using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;
using VaccinationCard.Api.Modules.Vaccines.Application.UseCases.FindVaccineById;
using VaccinationCard.Api.Modules.Vaccines.Domain.Enums;
using VaccinationCard.Api.Modules.Vaccines.Domain.Errors;
using VaccinationCard.Api.Modules.Vaccines.Domain.ValueObjects;
using VaccinationCard.Api.Shared.Errors;
using VaccinationCard.Api.Shared.ValueObjects;

namespace VaccinationCard.UnitTests.Modules.People.Application.UseCases.RegisterVaccination;

public sealed class RegisterVaccinationHandlerTests
{
    // A past date, so a handler that read the real clock would accept the day after it.
    private static readonly DateOnly Today = new(2025, 6, 15);
    private static readonly VaccineDto HepatitisB = new(Guid.CreateVersion7(), "Hepatite B", 3, 0);

    private readonly IPersonRepository _repository = Substitute.For<IPersonRepository>();
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly Person _person = PersonFactory.Create("Maria Silva", Cpf.Create("12345678909"));
    private readonly RegisterVaccinationHandler _sut;

    public RegisterVaccinationHandlerTests()
    {
        _repository.FindByIdAsync(_person.Id, Arg.Any<CancellationToken>()).Returns(_person);
        _sender.Send(new FindVaccineByIdQuery(HepatitisB.Id), Arg.Any<CancellationToken>()).Returns(HepatitisB);
        _clock.Today.Returns(Today);
        _sut = new RegisterVaccinationHandler(_repository, _sender, _clock);
    }

    [Fact]
    public async Task Should_Register_Vaccination_And_Edit_Person_When_Dose_Is_The_Next_Expected()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeRegisterVaccinationInput();

        // Act
        var output = await _sut.Handle(input, cancellationToken);

        // Assert
        var vaccination = _person.Vaccinations.ShouldHaveSingleItem();
        output.Id.ShouldBe(vaccination.Id);
        vaccination.VaccineId.ShouldBe(HepatitisB.Id);
        vaccination.Dose.ShouldBe(new Dose(DoseType.Primary, 1));
        vaccination.AppliedOn.ShouldBe(input.AppliedOn);
        await _repository.Received(1).EditAsync(_person, cancellationToken);
    }

    [Fact]
    public async Task Should_Throw_Not_Found_When_Person_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeRegisterVaccinationInput() with { PersonId = Guid.CreateVersion7() };
        _repository.FindByIdAsync(input.PersonId, Arg.Any<CancellationToken>()).Returns((Person?)null);

        // Act
        var act = () => _sut.Handle(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<NotFoundException>(act);
        exception.Code.ShouldBe("person.not_found");
        await _repository.DidNotReceive().EditAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Not_Edit_Person_When_Vaccine_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeRegisterVaccinationInput() with { VaccineId = Guid.CreateVersion7() };
        _sender.Send(new FindVaccineByIdQuery(input.VaccineId), Arg.Any<CancellationToken>()).ThrowsAsync(VaccinesErrors.VaccineNotFound());

        // Act
        var act = () => _sut.Handle(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<NotFoundException>(act);
        exception.Code.ShouldBe("vaccine.not_found");
        await _repository.DidNotReceive().EditAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>());
    }

    // The scheme comes from the vaccine: Hepatite B has three primary doses.
    [Fact]
    [Trait("RN", "RN09")]
    public async Task Should_Check_The_Dose_Against_The_Vaccine_Scheme_When_Registering()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeRegisterVaccinationInput() with { Dose = new DoseDto(DoseType.Primary, 4) };

        // Act
        var act = () => _sut.Handle(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<BusinessRuleException>(act);
        exception.Code.ShouldBe("vaccination.dose_outside_scheme");
        await _repository.DidNotReceive().EditAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("RN", "RN12")]
    public async Task Should_Check_The_Date_Against_The_Clock_When_Registering()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeRegisterVaccinationInput() with { AppliedOn = Today.AddDays(1) };

        // Act
        var act = () => _sut.Handle(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<BusinessRuleException>(act);
        exception.Code.ShouldBe("vaccination.applied_on_in_future");
        await _repository.DidNotReceive().EditAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>());
    }

    private RegisterVaccinationCommand MakeRegisterVaccinationInput() =>
        new(_person.Id, HepatitisB.Id, new DoseDto(DoseType.Primary, 1), new DateOnly(2020, 1, 15));
}
