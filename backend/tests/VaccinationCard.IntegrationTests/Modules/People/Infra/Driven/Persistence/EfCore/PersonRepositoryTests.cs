using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VaccinationCard.Api.Infra.Database;
using VaccinationCard.Api.Modules.People.Application.UseCases.CorrectVaccinationDate;
using VaccinationCard.Api.Modules.People.Application.UseCases.CreatePerson;
using VaccinationCard.Api.Modules.People.Application.UseCases.DeletePerson;
using VaccinationCard.Api.Modules.People.Application.UseCases.DeleteVaccination;
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
        record.Version.ShouldBe(0);
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

    [Fact]
    public async Task Should_Update_Only_The_Corrected_Vaccination_When_Editing()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var created = await SendAsync(new CreatePersonCommand("Maria Silva", "12345678909"), cancellationToken);
        var vaccine = await SendAsync(new CreateVaccineCommand("Hepatite B", 3, 0), cancellationToken);
        var first = await SendAsync(new RegisterVaccinationCommand(created.Id, vaccine.Id, new DoseDto(DoseType.Primary, 1), new DateOnly(2020, 1, 15)), cancellationToken);
        var second = await SendAsync(new RegisterVaccinationCommand(created.Id, vaccine.Id, new DoseDto(DoseType.Primary, 2), new DateOnly(2020, 2, 15)), cancellationToken);
        var input = (await _sut.FindByIdAsync(created.Id, cancellationToken)).ShouldNotBeNull();
        input.CorrectVaccinationDate(second.Id, new DateOnly(2020, 2, 20), ApiFactory.Today);

        // Act
        await _sut.EditAsync(input, cancellationToken);

        // Assert
        var dates = await ExecuteDbContextAsync(db => db.Set<VaccinationRecord>()
            .Where(x => x.PersonId == created.Id)
            .ToDictionaryAsync(x => x.Id, x => x.AppliedOn, cancellationToken));
        dates.Count.ShouldBe(2);
        dates[first.Id].ShouldBe(new DateOnly(2020, 1, 15));
        dates[second.Id].ShouldBe(new DateOnly(2020, 2, 20));
    }

    [Fact]
    public async Task Should_Delete_Only_The_Removed_Vaccination_When_Editing()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var created = await SendAsync(new CreatePersonCommand("Maria Silva", "12345678909"), cancellationToken);
        var vaccine = await SendAsync(new CreateVaccineCommand("Hepatite B", 3, 0), cancellationToken);
        var first = await SendAsync(new RegisterVaccinationCommand(created.Id, vaccine.Id, new DoseDto(DoseType.Primary, 1), new DateOnly(2020, 1, 15)), cancellationToken);
        var second = await SendAsync(new RegisterVaccinationCommand(created.Id, vaccine.Id, new DoseDto(DoseType.Primary, 2), new DateOnly(2020, 2, 15)), cancellationToken);
        var input = (await _sut.FindByIdAsync(created.Id, cancellationToken)).ShouldNotBeNull();
        input.RemoveVaccination(second.Id);

        // Act
        await _sut.EditAsync(input, cancellationToken);

        // Assert
        var ids = await ExecuteDbContextAsync(db => db.Set<VaccinationRecord>()
            .Where(x => x.PersonId == created.Id)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken));
        ids.ShouldHaveSingleItem().ShouldBe(first.Id);
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

    // Operations in sequence read the current version, so none of them conflicts.
    [Fact]
    [Trait("RN", "RN16")]
    public async Task Should_Increment_Version_When_Each_Operation_Changes_The_Card()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var created = await SendAsync(new CreatePersonCommand("Maria Silva", "12345678909"), cancellationToken);
        var vaccine = await SendAsync(new CreateVaccineCommand("Hepatite B", 3, 0), cancellationToken);

        // Act
        var registered = await SendAsync(new RegisterVaccinationCommand(created.Id, vaccine.Id, new DoseDto(DoseType.Primary, 1), new DateOnly(2020, 1, 15)), cancellationToken);
        var afterRegistration = await ReadVersionAsync(created.Id, cancellationToken);
        await SendAsync(new CorrectVaccinationDateCommand(created.Id, registered.Id, new DateOnly(2020, 1, 20)), cancellationToken);
        var afterCorrection = await ReadVersionAsync(created.Id, cancellationToken);
        await SendAsync(new DeleteVaccinationCommand(created.Id, registered.Id), cancellationToken);
        var afterDeletion = await ReadVersionAsync(created.Id, cancellationToken);

        // Assert
        afterRegistration.ShouldBe(1);
        afterCorrection.ShouldBe(2);
        afterDeletion.ShouldBe(3);
    }

    // The next three tests read the same version of the card in two scopes. Each one sends a different command next to
    // the version check: an insert, an update and a delete.
    [Fact]
    [Trait("RN", "RN16")]
    public async Task Should_Throw_Conflict_And_Keep_Only_The_First_Edit_When_Both_Register_On_The_Same_Version()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var created = await SendAsync(new CreatePersonCommand("Maria Silva", "12345678909"), cancellationToken);
        var vaccine = await SendAsync(new CreateVaccineCommand("Hepatite B", 3, 0), cancellationToken);
        var otherVaccine = await SendAsync(new CreateVaccineCommand("Tríplice viral", 3, 0), cancellationToken);
        await using var otherScope = Fixture.Factory.Services.CreateAsyncScope();
        var otherRepository = new PersonRepository(otherScope.ServiceProvider.GetRequiredService<AppDbContext>());
        var input = (await _sut.FindByIdAsync(created.Id, cancellationToken)).ShouldNotBeNull();
        var other = (await otherRepository.FindByIdAsync(created.Id, cancellationToken)).ShouldNotBeNull();
        input.RegisterVaccination(vaccine.Id, ThreeDoses, FirstDose, new DateOnly(2020, 1, 15), ApiFactory.Today);
        var first = other.RegisterVaccination(otherVaccine.Id, ThreeDoses, FirstDose, new DateOnly(2020, 1, 16), ApiFactory.Today);
        await otherRepository.EditAsync(other, cancellationToken);

        // Act
        var act = () => _sut.EditAsync(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<ConflictException>(act);
        exception.Code.ShouldBe("vaccination_card.concurrent_update");
        var ids = await ExecuteDbContextAsync(db => db.Set<VaccinationRecord>()
            .Where(x => x.PersonId == created.Id)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken));
        ids.ShouldHaveSingleItem().ShouldBe(first.Id);
    }

    [Fact]
    [Trait("RN", "RN16")]
    public async Task Should_Throw_Conflict_And_Keep_The_First_Date_When_Both_Correct_On_The_Same_Version()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var created = await SendAsync(new CreatePersonCommand("Maria Silva", "12345678909"), cancellationToken);
        var vaccine = await SendAsync(new CreateVaccineCommand("Hepatite B", 3, 0), cancellationToken);
        var registered = await SendAsync(new RegisterVaccinationCommand(created.Id, vaccine.Id, new DoseDto(DoseType.Primary, 1), new DateOnly(2020, 1, 15)), cancellationToken);
        await using var otherScope = Fixture.Factory.Services.CreateAsyncScope();
        var otherRepository = new PersonRepository(otherScope.ServiceProvider.GetRequiredService<AppDbContext>());
        var input = (await _sut.FindByIdAsync(created.Id, cancellationToken)).ShouldNotBeNull();
        var other = (await otherRepository.FindByIdAsync(created.Id, cancellationToken)).ShouldNotBeNull();
        input.CorrectVaccinationDate(registered.Id, new DateOnly(2020, 1, 20), ApiFactory.Today);
        other.CorrectVaccinationDate(registered.Id, new DateOnly(2020, 1, 25), ApiFactory.Today);
        await otherRepository.EditAsync(other, cancellationToken);

        // Act
        var act = () => _sut.EditAsync(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<ConflictException>(act);
        exception.Code.ShouldBe("vaccination_card.concurrent_update");
        var record = await ExecuteDbContextAsync(db => db.Set<VaccinationRecord>().SingleAsync(x => x.Id == registered.Id, cancellationToken));
        record.AppliedOn.ShouldBe(new DateOnly(2020, 1, 25));
    }

    [Fact]
    [Trait("RN", "RN16")]
    public async Task Should_Throw_Conflict_And_Keep_The_Dose_When_A_Correction_Comes_Before_The_Removal()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var created = await SendAsync(new CreatePersonCommand("Maria Silva", "12345678909"), cancellationToken);
        var vaccine = await SendAsync(new CreateVaccineCommand("Hepatite B", 3, 0), cancellationToken);
        var registered = await SendAsync(new RegisterVaccinationCommand(created.Id, vaccine.Id, new DoseDto(DoseType.Primary, 1), new DateOnly(2020, 1, 15)), cancellationToken);
        await using var otherScope = Fixture.Factory.Services.CreateAsyncScope();
        var otherRepository = new PersonRepository(otherScope.ServiceProvider.GetRequiredService<AppDbContext>());
        var input = (await _sut.FindByIdAsync(created.Id, cancellationToken)).ShouldNotBeNull();
        var other = (await otherRepository.FindByIdAsync(created.Id, cancellationToken)).ShouldNotBeNull();
        input.RemoveVaccination(registered.Id);
        other.CorrectVaccinationDate(registered.Id, new DateOnly(2020, 1, 25), ApiFactory.Today);
        await otherRepository.EditAsync(other, cancellationToken);

        // Act
        var act = () => _sut.EditAsync(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<ConflictException>(act);
        exception.Code.ShouldBe("vaccination_card.concurrent_update");
        var record = await ExecuteDbContextAsync(db => db.Set<VaccinationRecord>().SingleAsync(x => x.Id == registered.Id, cancellationToken));
        record.AppliedOn.ShouldBe(new DateOnly(2020, 1, 25));
    }

    // The insert fails on the foreign key, not on the version check (TechSpec §6.3).
    [Fact]
    [Trait("RN", "RN16")]
    public async Task Should_Throw_Conflict_When_Person_Is_Removed_Before_Registering()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var created = await SendAsync(new CreatePersonCommand("Maria Silva", "12345678909"), cancellationToken);
        var vaccine = await SendAsync(new CreateVaccineCommand("Hepatite B", 3, 0), cancellationToken);
        var input = (await _sut.FindByIdAsync(created.Id, cancellationToken)).ShouldNotBeNull();
        input.RegisterVaccination(vaccine.Id, ThreeDoses, FirstDose, new DateOnly(2020, 1, 15), ApiFactory.Today);
        await SendAsync(new DeletePersonCommand(created.Id), cancellationToken);

        // Act
        var act = () => _sut.EditAsync(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<ConflictException>(act);
        exception.Code.ShouldBe("vaccination_card.concurrent_update");
    }

    [Fact]
    [Trait("RN", "RN16")]
    public async Task Should_Throw_Conflict_When_Person_Is_Removed_Before_Correcting()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var created = await SendAsync(new CreatePersonCommand("Maria Silva", "12345678909"), cancellationToken);
        var vaccine = await SendAsync(new CreateVaccineCommand("Hepatite B", 3, 0), cancellationToken);
        var registered = await SendAsync(new RegisterVaccinationCommand(created.Id, vaccine.Id, new DoseDto(DoseType.Primary, 1), new DateOnly(2020, 1, 15)), cancellationToken);
        var input = (await _sut.FindByIdAsync(created.Id, cancellationToken)).ShouldNotBeNull();
        input.CorrectVaccinationDate(registered.Id, new DateOnly(2020, 1, 20), ApiFactory.Today);
        await SendAsync(new DeletePersonCommand(created.Id), cancellationToken);

        // Act
        var act = () => _sut.EditAsync(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<ConflictException>(act);
        exception.Code.ShouldBe("vaccination_card.concurrent_update");
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

    private Task<int> ReadVersionAsync(Guid id, CancellationToken cancellationToken) =>
        ExecuteDbContextAsync(db => db.Set<PersonRecord>().Where(x => x.Id == id).Select(x => x.Version).SingleAsync(cancellationToken));
}
