using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VaccinationCard.Api.Infra.Database;
using VaccinationCard.Api.Modules.People.Application.UseCases.CreatePerson;
using VaccinationCard.Api.Modules.People.Application.UseCases.RegisterVaccination;
using VaccinationCard.Api.Modules.People.Domain.Entities;
using VaccinationCard.Api.Modules.People.Domain.Factories;
using VaccinationCard.Api.Modules.People.Infra.Driven.Persistence.EfCore;
using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;
using VaccinationCard.Api.Modules.Vaccines.Application.UseCases.CreateVaccine;
using VaccinationCard.Api.Modules.Vaccines.Domain.Enums;
using VaccinationCard.Api.Modules.Vaccines.Domain.ValueObjects;
using VaccinationCard.Api.Shared.Errors;
using VaccinationCard.Api.Shared.ValueObjects;

namespace VaccinationCard.IntegrationTests.Modules.People.Infra.Driven.Persistence.EfCore;

public sealed class PersonRepositoryTests : IntegrationTest
{
    private static readonly DoseScheme ThreeDoses = new(3, 0);
    private static readonly Dose FirstDose = new(DoseType.Primary, 1);

    private readonly AsyncServiceScope _scope;
    private readonly PersonRepository _sut;

    public PersonRepositoryTests(IntegrationFixture fixture) : base(fixture)
    {
        _scope = fixture.Factory.Services.CreateAsyncScope();
        _sut = new PersonRepository(_scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    public override async ValueTask DisposeAsync()
    {
        await _scope.DisposeAsync();
        await base.DisposeAsync();
    }

    [Fact]
    public async Task Should_Store_Person_When_Saving()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeSavePersonInput();

        // Act
        await _sut.SaveAsync(input, cancellationToken);

        // Assert
        var record = await ExecuteDbContextAsync(db => db.Set<PersonRecord>().SingleAsync(x => x.Id == input.Id, cancellationToken));
        record.Name.ShouldBe("Maria Silva");
        record.Cpf.ShouldBe("12345678909");
        record.CreatedAt.ShouldNotBe(default);
    }

    [Fact]
    [Trait("RN", "RN03")]
    public async Task Should_Throw_Conflict_When_Unique_Cpf_Index_Is_Violated()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sut.SaveAsync(MakeSavePersonInput(), cancellationToken);
        var input = PersonFactory.Create("Maria Souza", Cpf.Create("123.456.789-09"));

        // Act
        var act = () => _sut.SaveAsync(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<ConflictException>(act);
        exception.Code.ShouldBe("person.cpf_already_registered");
    }

    // The person is registered in another scope, so the read goes to the database.
    [Fact]
    public async Task Should_Load_Person_When_Id_Exists()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var created = await SendAsync(new CreatePersonCommand("Maria Silva", "12345678909"), cancellationToken);

        // Act
        var person = await _sut.FindByIdAsync(created.Id, cancellationToken);

        // Assert
        person.ShouldNotBeNull();
        person.Id.ShouldBe(created.Id);
        person.Name.ShouldBe("Maria Silva");
        person.Cpf.ShouldBe(Cpf.Create("12345678909"));
    }

    [Fact]
    public async Task Should_Load_Person_With_Vaccinations_When_Id_Exists()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var created = await SendAsync(new CreatePersonCommand("Maria Silva", "12345678909"), cancellationToken);
        var vaccine = await SendAsync(new CreateVaccineCommand("Hepatite B", 3, 0), cancellationToken);
        var first = await SendAsync(new RegisterVaccinationCommand(created.Id, vaccine.Id, new DoseDto(DoseType.Primary, 1), new DateOnly(2020, 1, 15)), cancellationToken);
        var second = await SendAsync(new RegisterVaccinationCommand(created.Id, vaccine.Id, new DoseDto(DoseType.Primary, 2), new DateOnly(2020, 2, 15)), cancellationToken);

        // Act
        var person = await _sut.FindByIdAsync(created.Id, cancellationToken);

        // Assert
        person.ShouldNotBeNull().Vaccinations
            .Select(vaccination => (vaccination.Id, vaccination.VaccineId, vaccination.Dose, vaccination.AppliedOn))
            .ShouldBe(
                [
                    (first.Id, vaccine.Id, new Dose(DoseType.Primary, 1), new DateOnly(2020, 1, 15)),
                    (second.Id, vaccine.Id, new Dose(DoseType.Primary, 2), new DateOnly(2020, 2, 15)),
                ],
                ignoreOrder: true);
    }

    [Fact]
    public async Task Should_Return_Null_When_Id_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sut.SaveAsync(MakeSavePersonInput(), cancellationToken);

        // Act
        var person = await _sut.FindByIdAsync(Guid.CreateVersion7(), cancellationToken);

        // Assert
        person.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Insert_Registered_Vaccination_When_Editing()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var created = await SendAsync(new CreatePersonCommand("Maria Silva", "12345678909"), cancellationToken);
        var vaccine = await SendAsync(new CreateVaccineCommand("Hepatite B", 3, 0), cancellationToken);
        var input = (await _sut.FindByIdAsync(created.Id, cancellationToken)).ShouldNotBeNull();
        var vaccination = input.RegisterVaccination(vaccine.Id, ThreeDoses, FirstDose, new DateOnly(2020, 1, 15), ApiFactory.Today);

        // Act
        await _sut.EditAsync(input, cancellationToken);

        // Assert
        var record = await ExecuteDbContextAsync(db => db.Set<VaccinationRecord>().SingleAsync(x => x.Id == vaccination.Id, cancellationToken));
        record.PersonId.ShouldBe(created.Id);
        record.VaccineId.ShouldBe(vaccine.Id);
        record.DoseType.ShouldBe(DoseType.Primary);
        record.DoseNumber.ShouldBe((short)1);
        record.AppliedOn.ShouldBe(new DateOnly(2020, 1, 15));
        record.CreatedAt.ShouldNotBe(default);
    }

    // Two requests read the person before either one writes, so both pass the check of the aggregate; the unique index
    // stops the second.
    [Fact]
    [Trait("RN", "RN11")]
    public async Task Should_Throw_Conflict_When_Another_Edit_Registered_The_Same_Dose()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var created = await SendAsync(new CreatePersonCommand("Maria Silva", "12345678909"), cancellationToken);
        var vaccine = await SendAsync(new CreateVaccineCommand("Hepatite B", 3, 0), cancellationToken);
        await using var otherScope = Fixture.Factory.Services.CreateAsyncScope();
        var otherRepository = new PersonRepository(otherScope.ServiceProvider.GetRequiredService<AppDbContext>());
        var input = (await _sut.FindByIdAsync(created.Id, cancellationToken)).ShouldNotBeNull();
        var other = (await otherRepository.FindByIdAsync(created.Id, cancellationToken)).ShouldNotBeNull();
        input.RegisterVaccination(vaccine.Id, ThreeDoses, FirstDose, new DateOnly(2020, 1, 15), ApiFactory.Today);
        other.RegisterVaccination(vaccine.Id, ThreeDoses, FirstDose, new DateOnly(2020, 1, 16), ApiFactory.Today);
        await otherRepository.EditAsync(other, cancellationToken);

        // Act
        var act = () => _sut.EditAsync(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<ConflictException>(act);
        exception.Code.ShouldBe("vaccination.dose_already_registered");
    }

    [Fact]
    [Trait("RN", "RN04")]
    public async Task Should_Delete_Person_When_Id_Exists()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var created = await SendAsync(new CreatePersonCommand("Maria Silva", "12345678909"), cancellationToken);

        // Act
        var deleted = await _sut.DeleteAsync(created.Id, cancellationToken);

        // Assert
        deleted.ShouldBeTrue();
        var exists = await ExecuteDbContextAsync(db => db.Set<PersonRecord>().AnyAsync(x => x.Id == created.Id, cancellationToken));
        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Delete_Nothing_When_Id_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var created = await SendAsync(new CreatePersonCommand("Maria Silva", "12345678909"), cancellationToken);

        // Act
        var deleted = await _sut.DeleteAsync(Guid.CreateVersion7(), cancellationToken);

        // Assert
        deleted.ShouldBeFalse();
        var exists = await ExecuteDbContextAsync(db => db.Set<PersonRecord>().AnyAsync(x => x.Id == created.Id, cancellationToken));
        exists.ShouldBeTrue();
    }

    private static Person MakeSavePersonInput() => PersonFactory.Create("Maria Silva", Cpf.Create("12345678909"));
}
