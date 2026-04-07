using System.Net;
using System.Net.Http.Json;
using Budgeteria.Api.Dtos;
using FluentAssertions;

namespace Budgeteria.Api.Tests;

public class AuthTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public AuthTests()
    {
        _factory = new TestWebApplicationFactory();
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task GetProfile_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProfile_AutoCreatesUserFromAuth0Claims()
    {
        var sub = $"auth0|{Guid.NewGuid()}";
        var client = _factory.CreateAuthenticatedClient(sub, "alice@test.com", "Alice");

        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        user!.Id.Should().Be(sub);
        user.Name.Should().Be("Alice");
        user.Email.Should().Be("alice@test.com");
    }

    [Fact]
    public async Task GetProfile_ReturnsSameUserOnSecondCall()
    {
        var sub = $"auth0|{Guid.NewGuid()}";
        var client = _factory.CreateAuthenticatedClient(sub, "bob@test.com", "Bob");

        await client.GetAsync("/api/auth/me");
        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        user!.Id.Should().Be(sub);
    }

    [Fact]
    public async Task UpdateProfile_ChangesName()
    {
        var sub = $"auth0|{Guid.NewGuid()}";
        var client = _factory.CreateAuthenticatedClient(sub, "profile@test.com", "Old");

        // First call auto-creates
        await client.GetAsync("/api/auth/me");

        var response = await client.PutAsJsonAsync("/api/auth/me", new UpdateProfileRequest("New", null));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        user!.Name.Should().Be("New");
    }
}
