using VaccinationCard.Api.Modules.Accounts.Application.Dtos;
using VaccinationCard.Api.Modules.Accounts.Application.UseCases.FindAccountById;

namespace VaccinationCard.IntegrationTests.Modules.Accounts.Application.UseCases.FindAccountById;

public sealed class FindAccountByIdHandlerTests(IntegrationFixture fixture) : IntegrationTest(fixture)
{
    [Fact]
    public async Task Should_Return_Account_When_It_Exists()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var accountId = await CreateAccountAsync("operador@demo.local", "Demo@2026!", cancellationToken);

        // Act
        var account = await SendAsync(new FindAccountByIdQuery(accountId), cancellationToken);

        // Assert
        account.ShouldBe(new AccountDto(accountId, "operador@demo.local"));
    }

    [Fact]
    public async Task Should_Return_Null_When_Account_Does_Not_Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var account = await SendAsync(new FindAccountByIdQuery(Guid.CreateVersion7()), cancellationToken);

        // Assert
        account.ShouldBeNull();
    }
}
