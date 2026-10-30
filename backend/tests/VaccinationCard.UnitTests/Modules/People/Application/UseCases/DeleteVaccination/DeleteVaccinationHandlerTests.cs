using VaccinationCard.Api.Core.Messages;
using VaccinationCard.Api.Modules.People.Application.Persistence.Repository;
using VaccinationCard.Api.Modules.People.Application.UseCases.DeleteVaccination;
using VaccinationCard.Api.Modules.People.Domain.Entities;
using VaccinationCard.Api.Modules.People.Domain.Factories;
using VaccinationCard.Api.Modules.Vaccines.Domain.Enums;
using VaccinationCard.Api.Modules.Vaccines.Domain.ValueObjects;
using VaccinationCard.Api.Shared.Errors;
using VaccinationCard.Api.Shared.ValueObjects;

namespace VaccinationCard.UnitTests.Modules.People.Application.UseCases.DeleteVaccination;

public sealed class DeleteVaccinationHandlerTests
{
    private static readonly Guid VaccineId = Guid.CreateVersion7();

    private readonly IPersonRepository _repository = Substitute.For<IPersonRepository>();
    private readonly Vaccination _first = PersonFactory.LoadVaccination(Guid.CreateVersion7(), VaccineId, new Dose(DoseType.Primary, 1), new DateOnly(2020, 1, 15));
    private readonly Vaccination _second = PersonFactory.LoadVaccination(Guid.CreateVersion7(), VaccineId, new Dose(DoseType.Primary, 2), new DateOnly(2020, 2, 15));
    private readonly Person _person;
    private readonly DeleteVaccinationHandler _sut;

    public DeleteVaccinationHandlerTests()
    {
        _person = PersonFactory.Load(Guid.CreateVersion7(), "Maria Silva", Cpf.Create("12345678909"), [_first, _second]);
        _repository.FindByIdAsync(_person.Id, Arg.Any<CancellationToken>()).Returns(_person);
        _sut = new DeleteVaccinationHandler(_repository);
    }

    [Fact]
    [Trait("RN", "RN15")]
    public async Task Should_Remove_Vaccination_And_Edit_Person_When_It_Is_The_Last_Dose()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeDeleteVaccinationInput();

        // Act
        var output = await _sut.Handle(input, cancellationToken);

        // Assert
        output.ShouldBe(new SuccessMessage("Vacinação excluída com sucesso."));
        _person.Vaccinations.ShouldHaveSingleItem().Id.ShouldBe(_first.Id);
        await _repository.Received(1).EditAsync(_person, cancellationToken);
    }

    [Fact]
    public async Task Should_Throw_Not_Found_When_Person_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeDeleteVaccinationInput() with { PersonId = Guid.CreateVersion7() };
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
        var input = MakeDeleteVaccinationInput() with { VaccinationId = Guid.CreateVersion7() };

        // Act
        var act = () => _sut.Handle(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<NotFoundException>(act);
        exception.Code.ShouldBe("vaccination.not_found");
        await _repository.DidNotReceive().EditAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("RN", "RN15")]
    public async Task Should_Not_Edit_Person_When_Vaccination_Is_Not_The_Last_Dose()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeDeleteVaccinationInput() with { VaccinationId = _first.Id };

        // Act
        var act = () => _sut.Handle(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<BusinessRuleException>(act);
        exception.Code.ShouldBe("vaccination.not_last_dose");
        await _repository.DidNotReceive().EditAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>());
    }

    // The second dose is the last one.
    private DeleteVaccinationCommand MakeDeleteVaccinationInput() => new(_person.Id, _second.Id);
}
