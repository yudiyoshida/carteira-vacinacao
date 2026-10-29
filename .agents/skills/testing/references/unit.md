# Teste unitário

Modelo. Quando o repositório já tiver um exemplo real, siga o exemplo real.

## Domínio

```csharp
namespace VaccinationCard.UnitTests.Modules.People.Domain.Entities;

public sealed class PersonTests
{
    private static readonly DateOnly Today = new(2026, 10, 28);
    private static readonly Guid VaccineId = Guid.CreateVersion7();
    private static readonly DoseScheme ThreeDoses = new(3, 0);

    private readonly Person _sut = PersonFactory.Create("Maria Silva", Cpf.Create("12345678909"));

    [Fact]
    [Trait("RN", "RN10")]
    public void Should_Reject_Dose_When_Previous_Dose_Is_Missing()
    {
        // Arrange
        var secondDose = new Dose(DoseType.Primary, 2);

        // Act
        var act = () => _sut.RegisterVaccination(VaccineId, ThreeDoses, secondDose, Today, Today);

        // Assert
        var exception = Should.Throw<BusinessRuleException>(act);
        exception.Code.ShouldBe("vaccination.dose_out_of_sequence");
    }
}
```

## Handler com ports mockados

```csharp
public sealed class CreateVaccineHandlerTests
{
    private readonly IVaccineDao _dao = Substitute.For<IVaccineDao>();
    private readonly CreateVaccineHandler _sut;

    public CreateVaccineHandlerTests() => _sut = new CreateVaccineHandler(_dao);

    [Fact]
    [Trait("RN", "RN05")]
    public async Task Should_Reject_Vaccine_When_Name_Is_Already_Registered()
    {
        // Arrange
        var input = MakeCreateVaccineInput();
        _dao.ExistsByNameAsync(input.Name, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var act = () => _sut.Handle(input, TestContext.Current.CancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<ConflictException>(act);
        exception.Code.ShouldBe("vaccine.name_already_registered");
        await _dao.DidNotReceive().SaveAsync(Arg.Any<VaccineDto>(), Arg.Any<CancellationToken>());
    }

    private static CreateVaccineCommand MakeCreateVaccineInput() => new("Hepatite B", 3, 0);
}
```

## Log

Exemplo real: `backend/tests/VaccinationCard.UnitTests/Infra/Http/ProblemDetailsExceptionHandlerTests.cs`. O `FakeLogger<T>` entra no construtor do objeto testado, e o teste confere o nível e os campos estruturados, não o texto:

```csharp
private readonly FakeLogger<ProblemDetailsExceptionHandler> _logger = new();

// Assert
var record = _logger.LatestRecord;
record.Level.ShouldBe(LogLevel.Error);
record.GetStructuredStateValue("Code").ShouldBe("internal.unexpected");
```

## Validator

```csharp
[Theory]
[Trait("RN", "RN06")]
[InlineData(0)]
[InlineData(11)]
public void Should_Reject_Input_When_Primary_Doses_Are_Out_Of_Range(int primaryDoses)
{
    // Arrange
    var input = MakeCreateVaccineInput() with { PrimaryDoses = primaryDoses };

    // Act
    var result = _sut.Validate(input);

    // Assert
    result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateVaccineCommand.PrimaryDoses));
}
```
