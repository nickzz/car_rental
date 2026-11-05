using System.Net;
using System.Text;
using Xunit;

namespace CarRentalAPI.Tests.Integration;

[Collection(PostgresTestDatabaseCollection.Name)]
public sealed class UsersControllerIntegrationTests(PostgresTestDatabase database)
{
    [Fact]
    public async Task MalformedJson_ReturnsApiResponse400()
    {
        using var factory = new CustomWebApplicationFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        using var content = new StringContent("{", Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/v1/Users/register", content);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("\"success\":false", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"errors\"", body, StringComparison.OrdinalIgnoreCase);
    }
}
