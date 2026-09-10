using System.Net;
using Attendance.Api.Tests.Infrastructure;
using Xunit;

namespace Attendance.Api.Tests.Operations;

public sealed class SpaHostingTests
{
    [Fact]
    public async Task Client_route_serves_the_spa_index_document()
    {
        using var factory = new StaticSpaApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/login");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Attendance SPA test document", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Unknown_api_route_returns_not_found_instead_of_the_spa_document()
    {
        using var factory = new StaticSpaApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/not-a-real-endpoint");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Missing_static_asset_returns_not_found_instead_of_the_spa_document()
    {
        using var factory = new StaticSpaApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/assets/not-found.js");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_mutating_route_returns_not_found_instead_of_the_spa_document()
    {
        using var factory = new StaticSpaApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/not-a-real-endpoint", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
