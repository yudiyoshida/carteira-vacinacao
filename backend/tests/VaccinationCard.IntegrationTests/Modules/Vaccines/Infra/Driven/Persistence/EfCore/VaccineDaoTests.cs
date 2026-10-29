using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using VaccinationCard.Api.Infra.Database;
using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;
using VaccinationCard.Api.Modules.Vaccines.Infra.Driven.Persistence.EfCore;
using VaccinationCard.Api.Shared.Errors;

namespace VaccinationCard.IntegrationTests.Modules.Vaccines.Infra.Driven.Persistence.EfCore;

public sealed class VaccineDaoTests : IntegrationTest
{
    private readonly AsyncServiceScope _scope;
    private readonly VaccineDao _sut;

    public VaccineDaoTests(IntegrationFixture fixture) : base(fixture)
    {
        _scope = fixture.Factory.Services.CreateAsyncScope();
        _sut = new VaccineDao(_scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    public override async ValueTask DisposeAsync()
    {
        await _scope.DisposeAsync();
        await base.DisposeAsync();
    }

    [Fact]
    [Trait("RN", "RN05")]
    public async Task Should_Store_Normalized_Name_When_Saving_A_Vaccine()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeSaveVaccineInput();

        // Act
        await _sut.SaveAsync(input, cancellationToken);

        // Assert
        var record = await ExecuteDbContextAsync(db => db.Set<VaccineRecord>().SingleAsync(x => x.Id == input.Id, cancellationToken));
        record.Name.ShouldBe("Hepatite B");
        record.NormalizedName.ShouldBe("hepatite b");
        record.PrimaryDoses.ShouldBe((short)3);
        record.BoosterDoses.ShouldBe((short)0);
        record.CreatedAt.ShouldNotBe(default);
    }

    [Fact]
    public async Task Should_Return_Vaccine_When_Id_Exists()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeSaveVaccineInput();
        await _sut.SaveAsync(input, cancellationToken);

        // Act
        var vaccine = await _sut.FindByIdAsync(input.Id, cancellationToken);

        // Assert
        vaccine.ShouldBe(input);
    }

    [Fact]
    public async Task Should_Return_Null_When_Id_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sut.SaveAsync(MakeSaveVaccineInput(), cancellationToken);

        // Act
        var vaccine = await _sut.FindByIdAsync(Guid.CreateVersion7(), cancellationToken);

        // Assert
        vaccine.ShouldBeNull();
    }

    [Theory]
    [Trait("RN", "RN05")]
    [InlineData("Hepatite B")]
    [InlineData("hepatite b")]
    [InlineData("HEPATITE B")]
    [InlineData("  Hepatite B  ")]
    public async Task Should_Find_Name_When_It_Differs_Only_By_Case_Or_Surrounding_Spaces(string name)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sut.SaveAsync(MakeSaveVaccineInput(), cancellationToken);

        // Act
        var exists = await _sut.ExistsByNameAsync(name, cancellationToken);

        // Assert
        exists.ShouldBeTrue();
    }

    [Fact]
    [Trait("RN", "RN05")]
    public async Task Should_Not_Find_Name_When_No_Vaccine_Has_It()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sut.SaveAsync(MakeSaveVaccineInput(), cancellationToken);

        // Act
        var exists = await _sut.ExistsByNameAsync("Hepatite A", cancellationToken);

        // Assert
        exists.ShouldBeFalse();
    }

    [Fact]
    [Trait("RN", "RN05")]
    public async Task Should_Throw_Conflict_When_Unique_Name_Index_Is_Violated()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sut.SaveAsync(MakeSaveVaccineInput(), cancellationToken);
        var input = MakeSaveVaccineInput() with { Name = "HEPATITE B" };

        // Act
        var act = () => _sut.SaveAsync(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<ConflictException>(act);
        exception.Code.ShouldBe("vaccine.name_already_registered");
    }

    [Theory]
    [Trait("RN", "RN06")]
    [InlineData(0, 0, "ck_vaccines_primary_doses")]
    [InlineData(11, 0, "ck_vaccines_primary_doses")]
    [InlineData(1, -1, "ck_vaccines_booster_doses")]
    [InlineData(1, 6, "ck_vaccines_booster_doses")]
    public async Task Should_Reject_Scheme_When_Doses_Break_A_Database_Check(int primaryDoses, int boosterDoses, string constraint)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeSaveVaccineInput() with { PrimaryDoses = primaryDoses, BoosterDoses = boosterDoses };

        // Act
        var act = () => _sut.SaveAsync(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<DbUpdateException>(act);
        var postgres = exception.InnerException.ShouldBeOfType<PostgresException>();
        postgres.SqlState.ShouldBe(PostgresErrorCodes.CheckViolation);
        postgres.ConstraintName.ShouldBe(constraint);
    }

    private static VaccineDto MakeSaveVaccineInput() => new(Guid.CreateVersion7(), "Hepatite B", 3, 0);
}
