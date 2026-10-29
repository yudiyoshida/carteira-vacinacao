using FluentValidation;
using VaccinationCard.Api.Core.Behaviors;

namespace VaccinationCard.UnitTests.Core.Behaviors;

public sealed class ValidationBehaviorTests
{
    private const string Handled = "handled";

    private readonly InlineValidator<SampleCommand> _nameValidator = new();
    private readonly InlineValidator<SampleCommand> _numberValidator = new();
    private readonly ValidationBehavior<SampleCommand, string> _sut;
    private bool _nextCalled;

    public ValidationBehaviorTests()
    {
        _nameValidator.RuleFor(command => command.Name).NotEmpty().WithMessage("O nome é obrigatório.");
        _numberValidator.RuleFor(command => command.Number).GreaterThan(0).WithMessage("O número deve ser maior que zero.");
        _sut = new ValidationBehavior<SampleCommand, string>([_nameValidator, _numberValidator]);
    }

    [Fact]
    public async Task Should_Call_Next_When_Input_Is_Valid()
    {
        // Arrange
        var input = MakeSampleCommandInput();

        // Act
        var response = await _sut.Handle(input, Next, TestContext.Current.CancellationToken);

        // Assert
        response.ShouldBe(Handled);
    }

    [Fact]
    public async Task Should_Throw_Failures_Of_Every_Validator_When_Input_Is_Invalid()
    {
        // Arrange
        var input = MakeSampleCommandInput() with { Name = "", Number = 0 };

        // Act
        var act = () => _sut.Handle(input, Next, TestContext.Current.CancellationToken);

        // Assert
        var exception = await Should.ThrowAsync<ValidationException>(act);
        exception.Errors.Select(error => error.PropertyName).ShouldBe([nameof(SampleCommand.Name), nameof(SampleCommand.Number)], ignoreOrder: true);
        _nextCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Call_Next_When_Input_Has_No_Validator()
    {
        // Arrange
        var sut = new ValidationBehavior<SampleCommand, string>([]);
        var input = MakeSampleCommandInput() with { Name = "" };

        // Act
        var response = await sut.Handle(input, Next, TestContext.Current.CancellationToken);

        // Assert
        response.ShouldBe(Handled);
    }

    private Task<string> Next(CancellationToken cancellationToken)
    {
        _nextCalled = true;
        return Task.FromResult(Handled);
    }

    private static SampleCommand MakeSampleCommandInput() => new("Hepatite B", 3);

    public sealed record SampleCommand(string Name, int Number);
}
