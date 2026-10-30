using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using VaccinationCard.Api.Modules.Authentication.Application.Security;

namespace VaccinationCard.IntegrationTests;

public static class AuthenticatedClientExtensions
{
    // The token comes from the ITokenService, without the login route: the limit of login attempts does not interfere.
    // The fallback policy reads only the token, so its account does not need to exist.
    public static HttpClient CreateAuthenticatedClient(this WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        var accessToken = factory.Services.GetRequiredService<ITokenService>().IssueAccessToken(Guid.CreateVersion7());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }
}
