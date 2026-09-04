using System.Net;
using System.Net.Http.Json;
using ECommerce.Application.Auth.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ECommerce.IntegrationTests;

public class AuthTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthTests()
    {
        _factory = TestDatabase.CreateFactory("Auth");
    }

    public async Task InitializeAsync() => await TestDatabase.SeedAsync(_factory);
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Register_Returns_Token_And_User()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("Jane", "Doe", "jane@test.com", "Password123!"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.NotEmpty(auth.Token);
        Assert.Equal("jane@test.com", auth.Email);
        Assert.Equal("Customer", auth.Role);
        Assert.NotEqual(Guid.Empty, auth.UserId);
    }

    [Fact]
    public async Task Register_Duplicate_Email_Returns_Conflict()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("Jane", "Doe", "dup@test.com", "Password123!"));

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("Jane", "Doe", "dup@test.com", "Password123!"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Login_With_Valid_Credentials_Returns_Token()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("Jane", "Doe", "login@test.com", "Password123!"));

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("login@test.com", "Password123!"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.NotEmpty(auth.Token);
        Assert.Equal("login@test.com", auth.Email);
    }

    [Fact]
    public async Task Login_With_Invalid_Password_Returns_Unauthorized()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("Jane", "Doe", "badpass@test.com", "Password123!"));

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("badpass@test.com", "WrongPassword!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_With_Unknown_Email_Returns_Unauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("nobody@test.com", "Password123!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_Cart_Request_Returns_Unauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/cart");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_Token_Returns_Unauthorized()
    {
        var client = AuthHelper.CreateClientWithToken(_factory, "not-a-valid-token");

        var response = await client.GetAsync("/api/cart");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}