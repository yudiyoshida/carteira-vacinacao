using VaccinationCard.Api.Modules.Vaccines.Application.Dtos;
using VaccinationCard.Api.Modules.Vaccines.Application.Persistence.Dao;
using VaccinationCard.Api.Modules.Vaccines.Application.UseCases.CreateVaccine;
using VaccinationCard.Api.Shared.Errors;

namespace VaccinationCard.UnitTests.Modules.Vaccines.Application.UseCases.CreateVaccine;

public sealed class CreateVaccineHandlerTests
{
    private readonly IVaccineDao _dao = Substitute.For<IVaccineDao>();
    private readonly CreateVaccineHandler _sut;

    public CreateVaccineHandlerTests() => _sut = new CreateVaccineHandler(_dao);

    [Fact]
    public async Task Should_Save_Vaccine_And_Return_Its_Id_When_Name_Is_New()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeCreateVaccineInput();

        // Act
        var output = await _sut.Handle(input, cancellationToken);

        // Assert
        output.Id.Version.ShouldBe(7);
        await _dao.Received(1).SaveAsync(new VaccineDto(output.Id, "Hepatite B", 3, 0), cancellationToken);
    }

    [Fact]
    [Trait("RN", "RN05")]
    public async Task Should_Trim_Name_When_Name_Has_Surrounding_Spaces()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeCreateVaccineInput() with { Name = "  Hepatite B  " };

        // Act
        await _sut.Handle(input, cancellationToken);

        // Assert
        await _dao.Received(1).ExistsByNameAsync("Hepatite B", cancellationToken);
        await _dao.Received(1).SaveAsync(Arg.Is<VaccineDto>(vaccine => vaccine.Name == "Hepatite B"), cancellationToken);
    }

    [Fact]
    [Trait("RN", "RN05")]
    public async Task Should_Reject_Vaccine_When_Name_Is_Already_Registered()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeCreateVaccineInput();
        _dao.ExistsByNameAsync(input.Name, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var act = () => _sut.Handle(input, cancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<ConflictException>(act);
        exception.Code.ShouldBe("vaccine.name_already_registered");
        await _dao.DidNotReceive().SaveAsync(Arg.Any<VaccineDto>(), Arg.Any<CancellationToken>());
    }

    private static CreateVaccineCommand MakeCreateVaccineInput() => new("Hepatite B", 3, 0);
}
