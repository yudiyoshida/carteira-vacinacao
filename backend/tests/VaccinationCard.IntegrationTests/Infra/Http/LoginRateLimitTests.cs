using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using VaccinationCard.Api.Modules.Authentication.Application.Security;

namespace VaccinationCard.IntegrationTests.Infra.Http;

// The limiter reads the real clock, so these tests stay inside the window and the end of the window has no test here.
public sealed class LoginRateLimitTests(IntegrationFixture fixture) : HttpTest(fixture)
{
    private const string LoginUrl = "/api/v1/auth/login";
    private const string RefreshUrl = "/api/v1/auth/refresh";
    private const string Email = "operador@demo.local";
    private const string Password = "Demo@2026!";
    private const string KnownProxy = "10.0.0.1";
    private const string ClientIp = "203.0.113.10";
    private const string OtherClientIp = "203.0.113.20";
    private const int AttemptsPerMinute = 5;

    // The right credentials do not get past the limit.
    [Fact]
    [Trait("RN", "RN24")]
    public async Task Should_Refuse_The_Sixth_Attempt_When_All_Come_From_The_Same_IP()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await CreateAccountAsync(Email, Password, cancellationToken);
        await UseUpTheAttemptsAsync(AnonymousClient, forwardedFor: null, cancellationToken);

        // Act
        var response = await AnonymousClient.PostAsJsonAsync(LoginUrl, new { email = Email, password = Password }, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        var problem = await ReadProblemAsync(response, cancellationToken);
        problem.GetProperty("code").GetString().ShouldBe("auth.too_many_attempts");
        problem.GetProperty("title").GetString().ShouldBe("Muitas requisições");
        problem.GetProperty("detail").GetString().ShouldBe("Muitas tentativas de login. Aguarde um minuto e tente de novo.");
        problem.GetProperty("instance").GetString().ShouldBe(LoginUrl);
        response.Headers.GetValues("X-Content-Type-Options").ShouldBe(["nosniff"]);
        response.Headers.Contains(HeaderNames.SetCookie).ShouldBeFalse();
    }

    [Fact]
    [Trait("RN", "RN24")]
    public async Task Should_Count_The_Attempt_When_The_Body_Is_Invalid()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        for (var attempt = 0; attempt < AttemptsPerMinute; attempt++)
        {
            var allowed = await AnonymousClient.PostAsJsonAsync(LoginUrl, new { email = "operador", password = "" }, cancellationToken);
            allowed.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        // Act
        var response = await AttemptLoginAsync(AnonymousClient, forwardedFor: null, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    [Trait("RN", "RN24")]
    public async Task Should_Accept_The_Attempt_When_Only_Another_IP_Reached_The_Limit()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = StartApi();
        using var client = factory.CreateClientFromIp(IPAddress.Parse(ClientIp));
        using var otherClient = factory.CreateClientFromIp(IPAddress.Parse(OtherClientIp));
        await UseUpTheAttemptsAsync(client, forwardedFor: null, cancellationToken);

        // Act
        var response = await AttemptLoginAsync(otherClient, forwardedFor: null, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // Kestrel reports a direct IPv4 client mapped to IPv6, and the proxy forwards the plain IPv4.
    [Fact]
    [Trait("RN", "RN24")]
    public async Task Should_Count_As_The_Same_IP_When_The_IPv4_Arrives_Mapped_To_IPv6()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = StartApi();
        var ip = IPAddress.Parse(ClientIp);
        using var client = factory.CreateClientFromIp(ip);
        using var mappedClient = factory.CreateClientFromIp(ip.MapToIPv6());
        await UseUpTheAttemptsAsync(client, forwardedFor: null, cancellationToken);

        // Act
        var response = await AttemptLoginAsync(mappedClient, forwardedFor: null, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    [Trait("RN", "RN24")]
    public async Task Should_Keep_The_Renewal_Open_When_The_Login_Limit_Is_Reached()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await UseUpTheAttemptsAsync(AnonymousClient, forwardedFor: null, cancellationToken);

        // Act
        var response = await AnonymousClient.PostAsync(RefreshUrl, content: null, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // Each attempt claims another IP. With an empty list, the middleware would accept the header from any client.
    [Theory]
    [Trait("RN", "RN24")]
    [InlineData(KnownProxy)]
    [InlineData("")]
    public async Task Should_Ignore_X_Forwarded_For_When_The_Request_Comes_From_Outside_The_Proxy_List(string knownProxies)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = StartApi(knownProxies);
        using var client = factory.CreateClientFromIp(IPAddress.Parse(ClientIp));
        for (var attempt = 1; attempt <= AttemptsPerMinute; attempt++)
        {
            var allowed = await AttemptLoginAsync(client, $"198.51.100.{attempt}", cancellationToken);
            allowed.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        // Act
        var response = await AttemptLoginAsync(client, "198.51.100.6", cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    // Kestrel listens on both IP versions in the container, so it sees the proxy as an IPv4 mapped to IPv6.
    [Theory]
    [Trait("RN", "RN24")]
    [InlineData(KnownProxy, KnownProxy)]
    [InlineData(KnownProxy, "::ffff:10.0.0.1")]
    [InlineData("10.0.0.1, 10.0.0.2", "10.0.0.2")]
    public async Task Should_Count_By_The_Forwarded_IP_When_The_Request_Comes_From_A_Known_Proxy(string knownProxies, string proxyIp)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = StartApi(knownProxies);
        using var client = factory.CreateClientFromIp(IPAddress.Parse(proxyIp));
        await UseUpTheAttemptsAsync(client, ClientIp, cancellationToken);

        // Act
        var otherClient = await AttemptLoginAsync(client, OtherClientIp, cancellationToken);
        var sameClient = await AttemptLoginAsync(client, ClientIp, cancellationToken);

        // Assert
        otherClient.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        sameClient.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    // The Location of a creation carries the scheme of the request.
    [Theory]
    [InlineData(KnownProxy, "https")]
    [InlineData(ClientIp, "http")]
    public async Task Should_Accept_X_Forwarded_Proto_Only_When_The_Request_Comes_From_A_Known_Proxy(string remoteIp, string scheme)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = StartApi(KnownProxy);
        using var client = factory.CreateClientFromIp(IPAddress.Parse(remoteIp));
        var accessToken = factory.Services.GetRequiredService<ITokenService>().IssueAccessToken(Guid.CreateVersion7());
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/vaccines")
        {
            Content = JsonContent.Create(new { name = "Hepatite B", primaryDoses = 3, boosterDoses = 0 }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("X-Forwarded-Proto", "https");

        // Act
        var response = await client.SendAsync(request, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull().Scheme.ShouldBe(scheme);
    }

    // A wrong value would make every client behind the proxy share one limit.
    [Fact]
    public async Task Should_Refuse_To_Start_When_A_Known_Proxy_Is_Not_An_IP()
    {
        // Arrange
        await using var factory = StartApi("web");

        // Act
        var act = () => factory.CreateClient();

        // Assert
        Should.Throw<FormatException>(act);
    }

    // A factory of its own has a new limiter, so an IP used by another test starts with no attempts counted.
    private WebApplicationFactory<Program> StartApi(string knownProxies = "") =>
        Fixture.Factory.WithWebHostBuilder(builder => builder.UseSetting("ForwardedHeaders:KnownProxies", knownProxies));

    private static async Task UseUpTheAttemptsAsync(HttpClient client, string? forwardedFor, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < AttemptsPerMinute; attempt++)
        {
            var response = await AttemptLoginAsync(client, forwardedFor, cancellationToken);
            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }
    }

    private static async Task<HttpResponseMessage> AttemptLoginAsync(HttpClient client, string? forwardedFor, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, LoginUrl)
        {
            Content = JsonContent.Create(new { email = "outro@demo.local", password = "Senha@Errada1" }),
        };
        if (forwardedFor is not null)
        {
            request.Headers.Add("X-Forwarded-For", forwardedFor);
        }

        return await client.SendAsync(request, cancellationToken);
    }
}
