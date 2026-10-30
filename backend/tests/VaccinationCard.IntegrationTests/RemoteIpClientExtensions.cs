using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace VaccinationCard.IntegrationTests;

public static class RemoteIpClientExtensions
{
    // TestServer reports no remote IP, and the forwarded headers middleware accepts the headers of a client without one.
    public static HttpClient CreateClientFromIp(this WebApplicationFactory<Program> factory, IPAddress remoteIp)
    {
        var handler = factory.Server.CreateHandler(context => context.Connection.RemoteIpAddress = remoteIp);
        return new HttpClient(handler) { BaseAddress = factory.Server.BaseAddress };
    }
}
