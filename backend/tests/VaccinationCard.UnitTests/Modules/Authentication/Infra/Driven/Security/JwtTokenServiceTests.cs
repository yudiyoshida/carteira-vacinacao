using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using VaccinationCard.Api.Modules.Authentication.Infra.Driven.Security;

namespace VaccinationCard.UnitTests.Modules.Authentication.Infra.Driven.Security;

public sealed class JwtTokenServiceTests
{
    private static readonly DateTimeOffset Now = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _timeProvider = new(Now);
    private readonly JwtTokenService _sut;

    public JwtTokenServiceTests() => _sut = MakeTokenService(MakeJwtOptions());

    [Fact]
    [Trait("RN", "RN23")]
    public void Should_Issue_Access_Token_Valid_For_15_Minutes_When_Issuing_An_Access_Token()
    {
        // Arrange
        var accountId = Guid.CreateVersion7();

        // Act
        var token = new JsonWebToken(_sut.IssueAccessToken(accountId));

        // Assert
        token.Alg.ShouldBe(SecurityAlgorithms.HmacSha256);
        token.Subject.ShouldBe(accountId.ToString());
        token.Issuer.ShouldBe("vaccination-card-issuer");
        token.Audiences.ShouldBe(["vaccination-card-audience"]);
        token.IssuedAt.ShouldBe(Now.UtcDateTime);
        token.ValidFrom.ShouldBe(Now.UtcDateTime);
        token.ValidTo.ShouldBe(Now.AddMinutes(15).UtcDateTime);
    }

    [Fact]
    [Trait("RN", "RN23")]
    public void Should_Issue_Refresh_Token_Valid_For_7_Days_When_Issuing_A_Refresh_Token()
    {
        // Arrange
        var accountId = Guid.CreateVersion7();

        // Act
        var refreshToken = _sut.IssueRefreshToken(accountId);

        // Assert
        var token = new JsonWebToken(refreshToken.Token);
        refreshToken.ExpiresAt.ShouldBe(Now.AddDays(7));
        token.Alg.ShouldBe(SecurityAlgorithms.HmacSha256);
        token.Subject.ShouldBe(accountId.ToString());
        token.Id.ShouldNotBeNullOrEmpty();
        token.ValidTo.ShouldBe(Now.AddDays(7).UtcDateTime);
    }

    // The clock does not move, so only the jti tells the two tokens apart: a rotation must always change the hash.
    [Fact]
    [Trait("RN", "RN23")]
    public void Should_Issue_Different_Refresh_Tokens_When_Issuing_Two_At_The_Same_Instant()
    {
        // Arrange
        var accountId = Guid.CreateVersion7();

        // Act
        var first = _sut.IssueRefreshToken(accountId);
        var second = _sut.IssueRefreshToken(accountId);

        // Assert
        first.Token.ShouldNotBe(second.Token);
    }

    // The date of the fake clock is in the past: a validation that read the real clock would refuse this token.
    [Fact]
    [Trait("RN", "RN23")]
    public async Task Should_Return_The_Account_When_Refresh_Token_Is_Valid()
    {
        // Arrange
        var accountId = Guid.CreateVersion7();
        var refreshToken = _sut.IssueRefreshToken(accountId);

        // Act
        var validated = await _sut.ValidateRefreshTokenAsync(refreshToken.Token);

        // Assert
        validated.ShouldBe(accountId);
    }

    // The clock skew of TechSpec §9: 30 seconds past the expiration is still valid, and 31 are not.
    [Theory]
    [Trait("RN", "RN23")]
    [InlineData(30, true)]
    [InlineData(31, false)]
    public async Task Should_Apply_The_Clock_Skew_When_Refresh_Token_Is_Past_Its_Expiration(int secondsPastExpiration, bool valid)
    {
        // Arrange
        var accountId = Guid.CreateVersion7();
        var refreshToken = _sut.IssueRefreshToken(accountId);
        _timeProvider.Advance(TimeSpan.FromDays(7) + TimeSpan.FromSeconds(secondsPastExpiration));

        // Act
        var validated = await _sut.ValidateRefreshTokenAsync(refreshToken.Token);

        // Assert
        validated.ShouldBe(valid ? accountId : null);
    }

    // Each kind of token has its own secret (ADR-0007): one never passes for the other.
    [Fact]
    public async Task Should_Reject_Refresh_Token_When_It_Is_An_Access_Token()
    {
        // Arrange
        var accessToken = _sut.IssueAccessToken(Guid.CreateVersion7());

        // Act
        var validated = await _sut.ValidateRefreshTokenAsync(accessToken);

        // Assert
        validated.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-token")]
    [InlineData("a.b.c")]
    public async Task Should_Reject_Refresh_Token_When_It_Is_Malformed(string refreshToken)
    {
        // Act
        var validated = await _sut.ValidateRefreshTokenAsync(refreshToken);

        // Assert
        validated.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Reject_Refresh_Token_When_It_Was_Tampered_With()
    {
        // Arrange
        var refreshToken = _sut.IssueRefreshToken(Guid.CreateVersion7());
        var header = refreshToken.Token[..refreshToken.Token.IndexOf('.')];
        var signature = refreshToken.Token[refreshToken.Token.LastIndexOf('.')..];
        var otherPayload = new JsonWebToken(_sut.IssueRefreshToken(Guid.CreateVersion7()).Token).EncodedPayload;

        // Act
        var validated = await _sut.ValidateRefreshTokenAsync($"{header}.{otherPayload}{signature}");

        // Assert
        validated.ShouldBeNull();
    }

    [Theory]
    [InlineData("other-issuer", "vaccination-card-audience", "refresh-token-secret-with-at-least-32-bytes")]
    [InlineData("vaccination-card-issuer", "other-audience", "refresh-token-secret-with-at-least-32-bytes")]
    [InlineData("vaccination-card-issuer", "vaccination-card-audience", "another-refresh-token-secret-with-32-bytes")]
    public async Task Should_Reject_Refresh_Token_When_Issued_With_Other_Settings(string issuer, string audience, string refreshTokenSecret)
    {
        // Arrange
        var options = MakeJwtOptions();
        options.Issuer = issuer;
        options.Audience = audience;
        options.RefreshTokenSecret = refreshTokenSecret;
        var refreshToken = MakeTokenService(options).IssueRefreshToken(Guid.CreateVersion7());

        // Act
        var validated = await _sut.ValidateRefreshTokenAsync(refreshToken.Token);

        // Assert
        validated.ShouldBeNull();
    }

    private JwtTokenService MakeTokenService(JwtOptions jwtOptions) => new(Options.Create(jwtOptions), _timeProvider);

    private static JwtOptions MakeJwtOptions() => new()
    {
        Issuer = "vaccination-card-issuer",
        Audience = "vaccination-card-audience",
        AccessTokenSecret = "access-token-secret-with-at-least-32-bytes",
        RefreshTokenSecret = "refresh-token-secret-with-at-least-32-bytes",
        AccessTokenMinutes = 15,
        RefreshTokenDays = 7,
    };
}
