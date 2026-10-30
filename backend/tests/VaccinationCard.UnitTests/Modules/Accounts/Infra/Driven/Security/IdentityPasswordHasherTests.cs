using VaccinationCard.Api.Modules.Accounts.Infra.Driven.Security;

namespace VaccinationCard.UnitTests.Modules.Accounts.Infra.Driven.Security;

public sealed class IdentityPasswordHasherTests
{
    private const string Password = "Demo@2026!";

    private readonly IdentityPasswordHasher _sut = new();

    [Fact]
    public void Should_Accept_Password_When_It_Matches_The_Hash()
    {
        // Arrange
        var hash = _sut.Hash(Password);

        // Act
        var verified = _sut.Verify(hash, Password);

        // Assert
        verified.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Demo@2026")]
    [InlineData(" Demo@2026! ")]
    [InlineData("demo@2026!")]
    public void Should_Reject_Password_When_It_Does_Not_Match_The_Hash(string password)
    {
        // Arrange
        var hash = _sut.Hash(Password);

        // Act
        var verified = _sut.Verify(hash, password);

        // Assert
        verified.ShouldBeFalse();
    }

    [Fact]
    public void Should_Produce_A_Salted_Hash_When_Hashing_A_Password()
    {
        // Act
        var first = _sut.Hash(Password);
        var second = _sut.Hash(Password);

        // Assert
        first.ShouldNotContain(Password);
        first.ShouldNotBe(second);
    }

    [Fact]
    [Trait("RN", "RN22")]
    public void Should_Reject_Password_When_There_Is_No_Hash()
    {
        // Act
        var verified = _sut.Verify(null, Password);

        // Assert
        verified.ShouldBeFalse();
    }
}
