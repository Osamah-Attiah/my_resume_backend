using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Resume.Tests;

public sealed class AuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;
    public AuthorizationTests(WebApplicationFactory<Program> factory) => client = factory.CreateClient();

    [Theory]
    [InlineData("/api/v1/admin/person")]
    [InlineData("/api/v1/admin/projects")]
    [InlineData("/api/v1/admin/sites")]
    [InlineData("/api/v1/admin/experiences")]
    [InlineData("/api/v1/admin/data-export")]
    [InlineData("/api/v1/admin/media")]
    [InlineData("/api/v1/admin/publications/00000000-0000-0000-0000-000000000000/artifacts/00000000-0000-0000-0000-000000000000/download")]
    [InlineData("/api/v1/internal/publications/00000000-0000-0000-0000-000000000000/snapshot")]
    public async Task Anonymous_admin_requests_are_rejected(string path) => Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(path, TestContext.Current.CancellationToken)).StatusCode);

    [Fact]
    public async Task Live_health_does_not_require_the_database() => Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live", TestContext.Current.CancellationToken)).StatusCode);
}
