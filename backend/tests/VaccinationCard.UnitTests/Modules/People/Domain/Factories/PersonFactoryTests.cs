using VaccinationCard.Api.Modules.People.Domain.Factories;
using VaccinationCard.Api.Shared.ValueObjects;

namespace VaccinationCard.UnitTests.Modules.People.Domain.Factories;

public sealed class PersonFactoryTests
{
    private static readonly Cpf ValidCpf = Cpf.Create("12345678909");

    [Fact]
    public void Should_Create_Person_With_A_New_Id_When_Name_And_Cpf_Are_Valid()
    {
        // Arrange
        const string name = "Maria Silva";

        // Act
        var person = PersonFactory.Create(name, ValidCpf);

        // Assert
        person.Id.Version.ShouldBe(7);
        person.Name.ShouldBe(name);
        person.Cpf.ShouldBe(ValidCpf);
        person.Vaccinations.ShouldBeEmpty();
    }

    [Theory]
    [Trait("RN", "RN01")]
    [InlineData(3)]
    [InlineData(150)]
    public void Should_Create_Person_When_Name_Length_Is_At_The_Limits(int length)
    {
        // Arrange
        var name = new string('A', length);

        // Act
        var person = PersonFactory.Create(name, ValidCpf);

        // Assert
        person.Name.ShouldBe(name);
    }

    [Theory]
    [Trait("RN", "RN01")]
    [InlineData(2)]
    [InlineData(151)]
    public void Should_Reject_Name_When_Length_Is_Out_Of_Range(int length)
    {
        // Arrange
        var name = new string('A', length);

        // Act
        var act = () => PersonFactory.Create(name, ValidCpf);

        // Assert
        var exception = Should.Throw<ArgumentOutOfRangeException>(act);
        exception.ParamName.ShouldBe(nameof(name));
    }

    [Theory]
    [Trait("RN", "RN01")]
    [InlineData(0)]
    [InlineData(3)]
    public void Should_Reject_Name_When_It_Is_Blank(int length)
    {
        // Arrange
        var name = new string(' ', length);

        // Act
        var act = () => PersonFactory.Create(name, ValidCpf);

        // Assert
        var exception = Should.Throw<ArgumentException>(act);
        exception.ParamName.ShouldBe(nameof(name));
    }

    [Fact]
    public void Should_Keep_The_Given_Id_When_Loading_A_Person()
    {
        // Arrange
        var id = Guid.CreateVersion7();
        const string name = "Maria Silva";

        // Act
        var person = PersonFactory.Load(id, name, ValidCpf);

        // Assert
        person.Id.ShouldBe(id);
        person.Name.ShouldBe(name);
        person.Cpf.ShouldBe(ValidCpf);
    }
}
