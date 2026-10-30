using VaccinationCard.Api.Modules.Authentication.Infra.Driven.Security;

namespace VaccinationCard.UnitTests.Modules.Authentication.Infra.Driven.Security;

public sealed class JwtOptionsTests
{
    private static readonly string Secret32Bytes = new('a', 32);
    private static readonly string OtherSecret32Bytes = new('b', 32);

    private readonly JwtOptions _sut = new()
    {
        Issuer = "vaccination-card",
        Audience = "vaccination-card",
        AccessTokenSecret = Secret32Bytes,
        RefreshTokenSecret = OtherSecret32Bytes,
        AccessTokenMinutes = 15,
        RefreshTokenDays = 7,
    };

    public static TheoryData<string, string> InvalidSecrets => new()
    {
        { new string('a', 31), OtherSecret32Bytes },
        { Secret32Bytes, new string('b', 31) },
        { "", OtherSecret32Bytes },
        { Secret32Bytes, "" },
        { Secret32Bytes, Secret32Bytes },
    };

    [Fact]
    public void Should_Accept_Options_When_Every_Value_Is_Valid()
    {
        // Act
        var valid = _sut.IsValid();

        // Assert
        valid.ShouldBeTrue();
    }

    // The size counts bytes: 16 two-byte characters are 32 bytes.
    [Fact]
    public void Should_Accept_Secret_When_Its_Bytes_Reach_The_Minimum()
    {
        // Arrange
        _sut.AccessTokenSecret = new string('é', 16);

        // Act
        var valid = _sut.IsValid();

        // Assert
        valid.ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(InvalidSecrets))]
    public void Should_Reject_Options_When_Secrets_Are_Short_Or_Equal(string accessTokenSecret, string refreshTokenSecret)
    {
        // Arrange
        _sut.AccessTokenSecret = accessTokenSecret;
        _sut.RefreshTokenSecret = refreshTokenSecret;

        // Act
        var valid = _sut.IsValid();

        // Assert
        valid.ShouldBeFalse();
    }

    [Theory]
    [InlineData("", "vaccination-card", 15, 7)]
    [InlineData("vaccination-card", "", 15, 7)]
    [InlineData("vaccination-card", "vaccination-card", 0, 7)]
    [InlineData("vaccination-card", "vaccination-card", 15, 0)]
    public void Should_Reject_Options_When_A_Value_Is_Missing(string issuer, string audience, int accessTokenMinutes, int refreshTokenDays)
    {
        // Arrange
        _sut.Issuer = issuer;
        _sut.Audience = audience;
        _sut.AccessTokenMinutes = accessTokenMinutes;
        _sut.RefreshTokenDays = refreshTokenDays;

        // Act
        var valid = _sut.IsValid();

        // Assert
        valid.ShouldBeFalse();
    }
}
