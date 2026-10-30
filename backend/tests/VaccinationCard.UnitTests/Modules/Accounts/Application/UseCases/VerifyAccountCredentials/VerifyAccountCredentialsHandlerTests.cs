using VaccinationCard.Api.Modules.Accounts.Application.Dtos;
using VaccinationCard.Api.Modules.Accounts.Application.Persistence.Dao;
using VaccinationCard.Api.Modules.Accounts.Application.Security;
using VaccinationCard.Api.Modules.Accounts.Application.UseCases.VerifyAccountCredentials;

namespace VaccinationCard.UnitTests.Modules.Accounts.Application.UseCases.VerifyAccountCredentials;

public sealed class VerifyAccountCredentialsHandlerTests
{
    private readonly IAccountDao _dao = Substitute.For<IAccountDao>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly VerifyAccountCredentialsHandler _sut;

    public VerifyAccountCredentialsHandlerTests() => _sut = new VerifyAccountCredentialsHandler(_dao, _passwordHasher);

    [Fact]
    [Trait("RN", "RN22")]
    public async Task Should_Return_Account_When_Password_Is_Right()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeVerifyAccountCredentialsInput();
        var credentials = new AccountCredentialsDto(Guid.CreateVersion7(), input.Email, "hash");
        _dao.FindCredentialsByEmailAsync(input.Email, Arg.Any<CancellationToken>()).Returns(credentials);
        _passwordHasher.Verify("hash", input.Password).Returns(true);

        // Act
        var account = await _sut.Handle(input, cancellationToken);

        // Assert
        account.ShouldBe(new AccountDto(credentials.Id, credentials.Email));
    }

    [Fact]
    [Trait("RN", "RN22")]
    public async Task Should_Return_Null_When_Password_Is_Wrong()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeVerifyAccountCredentialsInput();
        _dao.FindCredentialsByEmailAsync(input.Email, Arg.Any<CancellationToken>()).Returns(new AccountCredentialsDto(Guid.CreateVersion7(), input.Email, "hash"));
        _passwordHasher.Verify("hash", input.Password).Returns(false);

        // Act
        var account = await _sut.Handle(input, cancellationToken);

        // Assert
        account.ShouldBeNull();
    }

    // Verifying without a hash costs the same as a wrong password: the response time does not reveal the e-mail.
    [Fact]
    [Trait("RN", "RN22")]
    public async Task Should_Still_Verify_The_Password_When_Email_Is_Not_Registered()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = MakeVerifyAccountCredentialsInput();

        // Act
        var account = await _sut.Handle(input, cancellationToken);

        // Assert
        account.ShouldBeNull();
        _passwordHasher.Received(1).Verify(null, input.Password);
    }

    private static VerifyAccountCredentialsQuery MakeVerifyAccountCredentialsInput() => new("operador@demo.local", "Demo@2026!");
}
