using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Net.Http.Headers;

namespace VaccinationCard.IntegrationTests.Modules.Authentication.Infra.Drivers.Http;

public sealed class AuthEndpointTests(IntegrationFixture fixture) : HttpTest(fixture)
{
    private const string LoginUrl = "/api/v1/auth/login";
    private const string Email = "operador@demo.local";
    private const string Password = "Demo@2026!";

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

    // The clock of the API stops on ApiFactory.Today, at 12:00 UTC.
    [Fact]
    [Trait("RN", "RN23")]
    public async Task Should_Write_The_Refresh_Token_Cookie_When_Credentials_Are_Valid()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await CreateAccountAsync(Email, Password, cancellationToken);
        var expiresAt = new DateTimeOffset(ApiFactory.Today, new TimeOnly(12, 0), TimeSpan.Zero).AddDays(7);

        // Act
        var response = await AnonymousClient.PostAsJsonAsync(LoginUrl, MakeLoginBody(), cancellationToken);

        // Assert
        var cookie = ReadRefreshTokenCookie(response);
        cookie.Value.Value.ShouldNotBeNullOrEmpty();
        cookie.HttpOnly.ShouldBeTrue();
        cookie.Secure.ShouldBeTrue();
        cookie.SameSite.ShouldBe(SameSiteMode.Strict);
        cookie.Path.Value.ShouldBe("/api/v1/auth");
        cookie.Expires.ShouldBe(expiresAt);
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
        var refreshTokenHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
        var records = factory.Services.GetFakeLogCollector().GetSnapshot();
        records.ShouldContain(record => record.Category == DbLoggerCategory.Database.Command.Name);
        records.ShouldNotContain(record =>
            record.Message.Contains(Password) || record.Message.Contains(accessToken) || record.Message.Contains(refreshToken) || record.Message.Contains(refreshTokenHash));
    }

    private static SetCookieHeaderValue ReadRefreshTokenCookie(HttpResponseMessage response)
    {
        var cookie = SetCookieHeaderValue.Parse(response.Headers.GetValues(HeaderNames.SetCookie).Single());
        cookie.Name.Value.ShouldBe("refresh_token");
        return cookie;
    }

    private static Dictionary<string, string> WithoutTraceId(JsonElement problem) =>
        problem.EnumerateObject().Where(field => field.Name != "traceId").ToDictionary(field => field.Name, field => field.Value.GetRawText());

    private static object MakeLoginBody() => new { email = Email, password = Password };
}
