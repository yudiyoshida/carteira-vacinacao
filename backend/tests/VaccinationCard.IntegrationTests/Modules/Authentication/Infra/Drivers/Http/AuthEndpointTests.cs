using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Net.Http.Headers;
using VaccinationCard.Api.Modules.Authentication.Application.Dtos;
using VaccinationCard.Api.Modules.Authentication.Application.Persistence.Dao;
using VaccinationCard.Api.Modules.Authentication.Application.Security;
using VaccinationCard.Api.Modules.Authentication.Application.UseCases.RefreshSession;
using VaccinationCard.Api.Modules.Authentication.Application.UseCases.SignIn;

namespace VaccinationCard.IntegrationTests.Modules.Authentication.Infra.Drivers.Http;

public sealed class AuthEndpointTests(IntegrationFixture fixture) : HttpTest(fixture)
{
    private const string LoginUrl = "/api/v1/auth/login";
    private const string RefreshUrl = "/api/v1/auth/refresh";
    private const string LogoutUrl = "/api/v1/auth/logout";
    private const string Email = "operador@demo.local";
    private const string Password = "Demo@2026!";

    // The clock of the API stops on ApiFactory.Today, at 12:00 UTC.
    private static readonly DateTimeOffset Now = new(ApiFactory.Today, new TimeOnly(12, 0), TimeSpan.Zero);

    [Fact]
    [Trait("RN", "RN23")]
    public async Task Should_Return_Only_The_Access_Token_When_Credentials_Are_Valid()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await CreateAccountAsync(Email, Password, cancellationToken);

        // Act
        var response = await AnonymousClient.PostAsJsonAsync(LoginUrl, MakeLoginBody(), cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        body.EnumerateObject().Select(field => field.Name).ShouldBe(["accessToken"]);
        body.GetProperty("accessToken").GetString().ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("RN", "RN21")]
    public async Task Should_Open_The_Protected_Routes_When_Using_The_Token_Of_The_Login()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await CreateAccountAsync(Email, Password, cancellationToken);
        var login = await AnonymousClient.PostAsJsonAsync(LoginUrl, MakeLoginBody(), cancellationToken);
        var body = await login.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/vaccines");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());

        // Act
        var response = await AnonymousClient.SendAsync(request, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("RN", "RN23")]
    public async Task Should_Write_The_Refresh_Token_Cookie_When_Credentials_Are_Valid()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await CreateAccountAsync(Email, Password, cancellationToken);

        // Act
        var response = await AnonymousClient.PostAsJsonAsync(LoginUrl, MakeLoginBody(), cancellationToken);

        // Assert
        var cookie = ReadRefreshTokenCookie(response);
        cookie.Value.Value.ShouldNotBeNullOrEmpty();
        ShouldHaveTheCookieAttributes(cookie);
        cookie.Expires.ShouldBe(Now.AddDays(7));
    }

    [Fact]
    public async Task Should_Leave_Secure_Out_Of_The_Cookie_When_The_Configuration_Turns_It_Off()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await CreateAccountAsync(Email, Password, cancellationToken);
        await using var factory = Fixture.Factory.WithWebHostBuilder(builder => builder.UseSetting("RefreshCookie:Secure", "false"));
        using var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(LoginUrl, MakeLoginBody(), cancellationToken);

        // Assert
        ReadRefreshTokenCookie(response).Secure.ShouldBeFalse();
    }

    [Fact]
    [Trait("RN", "RN22")]
    public async Task Should_Return_Bad_Request_With_Field_Errors_When_Login_Is_Invalid()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var response = await AnonymousClient.PostAsJsonAsync(LoginUrl, new { email = "operador", password = "" }, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await ReadProblemAsync(response, cancellationToken);
        problem.GetProperty("code").GetString().ShouldBe("validation.failed");
        problem.GetProperty("errors").EnumerateObject().Select(field => field.Name).ShouldBe(["email", "password"], ignoreOrder: true);
    }

    // Everything but the traceId is equal, so the response does not reveal whether the e-mail exists.
    [Fact]
    [Trait("RN", "RN22")]
    public async Task Should_Answer_The_Same_Way_When_Email_Is_Unknown_Or_Password_Is_Wrong()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await CreateAccountAsync(Email, Password, cancellationToken);

        // Act
        var wrongPassword = await AnonymousClient.PostAsJsonAsync(LoginUrl, new { email = Email, password = "Senha@Errada1" }, cancellationToken);
        var unknownEmail = await AnonymousClient.PostAsJsonAsync(LoginUrl, new { email = "outro@demo.local", password = Password }, cancellationToken);

        // Assert
        wrongPassword.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        unknownEmail.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var wrongPasswordProblem = await ReadProblemAsync(wrongPassword, cancellationToken);
        var unknownEmailProblem = await ReadProblemAsync(unknownEmail, cancellationToken);
        wrongPasswordProblem.GetProperty("code").GetString().ShouldBe("auth.invalid_credentials");
        WithoutTraceId(unknownEmailProblem).ShouldBe(WithoutTraceId(wrongPasswordProblem));
        wrongPassword.Headers.Contains(HeaderNames.SetCookie).ShouldBeFalse();
        unknownEmail.Headers.Contains(HeaderNames.SetCookie).ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Log_Neither_Password_Nor_Tokens_When_Signing_In()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await CreateAccountAsync(Email, Password, cancellationToken);
        await using var factory = Fixture.Factory.WithWebHostBuilder(builder => builder.ConfigureLogging(logging => logging.AddFakeLogging()));
        using var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(LoginUrl, MakeLoginBody(), cancellationToken);

        // Assert
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var accessToken = body.GetProperty("accessToken").GetString()!;
        var refreshToken = ReadRefreshTokenCookie(response).Value.Value!;
        var records = factory.Services.GetFakeLogCollector().GetSnapshot();
        records.ShouldContain(record => record.Category == DbLoggerCategory.Database.Command.Name);
        records.ShouldNotContain(record =>
            record.Message.Contains(Password) || record.Message.Contains(accessToken) || record.Message.Contains(refreshToken) || record.Message.Contains(Sha256(refreshToken)));
    }

    // The access token has no jti, so the frozen clock would repeat it: the renewal comes one minute after the login.
    [Fact]
    [Trait("RN", "RN23")]
    public async Task Should_Return_A_New_Access_Token_That_Opens_The_Protected_Routes_When_Refreshing()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var signedIn = await SignInAsync(cancellationToken);
        var timeProvider = new FakeTimeProvider(Now);
        await using var factory = WithClock(timeProvider);
        using var client = factory.CreateClient();
        timeProvider.Advance(TimeSpan.FromMinutes(1));

        // Act
        var response = await RefreshAsync(client, signedIn.RefreshToken, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var accessToken = body.GetProperty("accessToken").GetString();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/vaccines");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var protectedResponse = await client.SendAsync(request, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.EnumerateObject().Select(field => field.Name).ShouldBe(["accessToken"]);
        accessToken.ShouldNotBe(signedIn.AccessToken);
        protectedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // The 7 days count from the last renewal (TechSpec §5.4).
    [Fact]
    [Trait("RN", "RN23")]
    public async Task Should_Rewrite_The_Cookie_With_Seven_Days_From_The_Renewal_When_Refreshing()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var signedIn = await SignInAsync(cancellationToken);
        var timeProvider = new FakeTimeProvider(Now);
        await using var factory = WithClock(timeProvider);
        using var client = factory.CreateClient();
        timeProvider.Advance(TimeSpan.FromDays(1));

        // Act
        var response = await RefreshAsync(client, signedIn.RefreshToken, cancellationToken);

        // Assert
        var cookie = ReadRefreshTokenCookie(response);
        cookie.Value.Value.ShouldNotBeNullOrEmpty();
        cookie.Value.Value.ShouldNotBe(signedIn.RefreshToken);
        ShouldHaveTheCookieAttributes(cookie);
        cookie.Expires.ShouldBe(Now.AddDays(8));
    }

    [Fact]
    [Trait("RN", "RN23")]
    public async Task Should_Refuse_The_Previous_Refresh_Token_And_Delete_The_Cookie_When_The_Session_Was_Rotated()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var signedIn = await SignInAsync(cancellationToken);
        await SendAsync(new RefreshSessionCommand(signedIn.RefreshToken), cancellationToken);

        // Act
        var response = await RefreshAsync(AnonymousClient, signedIn.RefreshToken, cancellationToken);

        // Assert
        await ShouldRefuseTheRenewalAsync(response, cancellationToken);
    }

    // 7 days of validity (RN23) and 30 seconds of clock skew (TechSpec §9).
    [Fact]
    [Trait("RN", "RN23")]
    public async Task Should_Refuse_The_Refresh_Token_And_Delete_The_Cookie_When_The_Token_Expired()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var signedIn = await SignInAsync(cancellationToken);
        var timeProvider = new FakeTimeProvider(Now);
        await using var factory = WithClock(timeProvider);
        using var client = factory.CreateClient();
        timeProvider.Advance(TimeSpan.FromDays(7) + TimeSpan.FromSeconds(31));

        // Act
        var response = await RefreshAsync(client, signedIn.RefreshToken, cancellationToken);

        // Assert
        await ShouldRefuseTheRenewalAsync(response, cancellationToken);
    }

    [Fact]
    [Trait("RN", "RN23")]
    public async Task Should_Refuse_The_Renewal_And_Delete_The_Cookie_When_There_Is_No_Cookie()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var response = await AnonymousClient.PostAsync(RefreshUrl, content: null, cancellationToken);

        // Assert
        await ShouldRefuseTheRenewalAsync(response, cancellationToken);
    }

    // The session may still be valid after an unexpected failure, so the browser keeps the cookie (TechSpec §9).
    [Fact]
    public async Task Should_Keep_The_Cookie_When_The_Renewal_Fails_Unexpectedly()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var refreshToken = Fixture.Factory.Services.GetRequiredService<ITokenService>().IssueRefreshToken(Guid.CreateVersion7());
        await using var factory = Fixture.Factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddScoped<ISessionDao, FailingSessionDao>()));
        using var client = factory.CreateClient();

        // Act
        var response = await RefreshAsync(client, refreshToken.Token, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        var problem = await ReadProblemAsync(response, cancellationToken);
        problem.GetProperty("code").GetString().ShouldBe("internal.unexpected");
        response.Headers.Contains(HeaderNames.SetCookie).ShouldBeFalse();
    }

    // A browser ignores a Secure cookie from http://localhost, so the deletion follows the same setting as the login.
    [Fact]
    public async Task Should_Leave_Secure_Out_Of_The_Deleted_Cookie_When_The_Configuration_Turns_It_Off()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = Fixture.Factory.WithWebHostBuilder(builder => builder.UseSetting("RefreshCookie:Secure", "false"));
        using var client = factory.CreateClient();

        // Act
        var response = await client.PostAsync(RefreshUrl, content: null, cancellationToken);

        // Assert
        var cookie = ReadRefreshTokenCookie(response);
        cookie.Expires.ShouldBe(DateTimeOffset.UnixEpoch);
        cookie.Secure.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Log_Neither_Tokens_Nor_Hashes_When_Refreshing()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var signedIn = await SignInAsync(cancellationToken);
        await using var factory = Fixture.Factory.WithWebHostBuilder(builder => builder.ConfigureLogging(logging => logging.AddFakeLogging()));
        using var client = factory.CreateClient();

        // Act
        var response = await RefreshAsync(client, signedIn.RefreshToken, cancellationToken);

        // Assert
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var refreshToken = ReadRefreshTokenCookie(response).Value.Value!;
        string[] secrets =
        [
            signedIn.RefreshToken,
            Sha256(signedIn.RefreshToken),
            body.GetProperty("accessToken").GetString()!,
            refreshToken,
            Sha256(refreshToken),
        ];
        var records = factory.Services.GetFakeLogCollector().GetSnapshot();
        records.ShouldContain(record => record.Category == DbLoggerCategory.Database.Command.Name);
        records.ShouldNotContain(record => secrets.Any(record.Message.Contains));
    }

    [Fact]
    [Trait("RN", "RN23")]
    public async Task Should_End_The_Session_And_Delete_The_Cookie_When_Signing_Out()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var signedIn = await SignInAsync(cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Post, LogoutUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", signedIn.AccessToken);

        // Act
        var response = await AnonymousClient.SendAsync(request, cancellationToken);
        var renewal = await RefreshAsync(AnonymousClient, signedIn.RefreshToken, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        body.EnumerateObject().Select(field => field.Name).ShouldBe(["message"]);
        body.GetProperty("message").GetString().ShouldBe("Sessão encerrada com sucesso.");
        ShouldDeleteTheCookie(response);
        await ShouldRefuseTheRenewalAsync(renewal, cancellationToken);
    }

    // Through the use case, not the login route: the limit of login attempts does not interfere.
    private async Task<SessionTokensDto> SignInAsync(CancellationToken cancellationToken)
    {
        await CreateAccountAsync(Email, Password, cancellationToken);
        return await SendAsync(new SignInCommand(Email, Password), cancellationToken);
    }

    // The client of the tests calls over http and does not send a Secure cookie by itself, so the cookie goes in the header.
    private static async Task<HttpResponseMessage> RefreshAsync(HttpClient client, string refreshToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, RefreshUrl);
        request.Headers.Add(HeaderNames.Cookie, $"refresh_token={refreshToken}");
        return await client.SendAsync(request, cancellationToken);
    }

    // The tokens that the shared factory issues are valid here: same secrets, and this clock starts at the same instant.
    private WebApplicationFactory<Program> WithClock(FakeTimeProvider timeProvider) =>
        Fixture.Factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(timeProvider)));

    private static async Task ShouldRefuseTheRenewalAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var problem = await ReadProblemAsync(response, cancellationToken);
        problem.GetProperty("code").GetString().ShouldBe("auth.invalid_refresh_token");
        ShouldDeleteTheCookie(response);
    }

    private static void ShouldDeleteTheCookie(HttpResponseMessage response)
    {
        var cookie = ReadRefreshTokenCookie(response);
        cookie.Value.Value.ShouldBeNullOrEmpty();
        cookie.Expires.ShouldBe(DateTimeOffset.UnixEpoch);
        ShouldHaveTheCookieAttributes(cookie);
    }

    private static void ShouldHaveTheCookieAttributes(SetCookieHeaderValue cookie)
    {
        cookie.HttpOnly.ShouldBeTrue();
        cookie.Secure.ShouldBeTrue();
        cookie.SameSite.ShouldBe(SameSiteMode.Strict);
        cookie.Path.Value.ShouldBe("/api/v1/auth");
    }

    private static SetCookieHeaderValue ReadRefreshTokenCookie(HttpResponseMessage response)
    {
        var cookie = SetCookieHeaderValue.Parse(response.Headers.GetValues(HeaderNames.SetCookie).Single());
        cookie.Name.Value.ShouldBe("refresh_token");
        return cookie;
    }

    private static string Sha256(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static Dictionary<string, string> WithoutTraceId(JsonElement problem) =>
        problem.EnumerateObject().Where(field => field.Name != "traceId").ToDictionary(field => field.Name, field => field.Value.GetRawText());

    private static object MakeLoginBody() => new { email = Email, password = Password };
}
