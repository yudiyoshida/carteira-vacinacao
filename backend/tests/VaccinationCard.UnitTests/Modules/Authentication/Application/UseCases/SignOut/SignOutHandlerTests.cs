using VaccinationCard.Api.Core.Messages;
using VaccinationCard.Api.Modules.Authentication.Application.Persistence.Dao;
using VaccinationCard.Api.Modules.Authentication.Application.Security;
using VaccinationCard.Api.Modules.Authentication.Application.Services;
using VaccinationCard.Api.Modules.Authentication.Application.UseCases.SignOut;

namespace VaccinationCard.UnitTests.Modules.Authentication.Application.UseCases.SignOut;

public sealed class SignOutHandlerTests
{
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly ISessionDao _sessionDao = Substitute.For<ISessionDao>();
    private readonly SignOutHandler _sut;

    public SignOutHandlerTests() => _sut = new SignOutHandler(new RefreshTokenSession(_tokenService, _sessionDao));

    [Fact]
    [Trait("RN", "RN23")]
    public async Task Should_Delete_The_Session_Of_The_Account_When_Signing_Out()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new SignOutCommand(Guid.CreateVersion7());

        // Act
        var output = await _sut.Handle(input, cancellationToken);

        // Assert
        output.ShouldBe(new SuccessMessage("Sessão encerrada com sucesso."));
        await _sessionDao.Received(1).DeleteAsync(input.AccountId, cancellationToken);
    }
}
