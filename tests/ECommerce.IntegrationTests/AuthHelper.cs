using System.Net.Http.Headers;
using System.Net.Http.Json;
using ECommerce.Application.Auth.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Domain.ValueObjects;
using ECommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.IntegrationTests;

internal static class AuthHelper
{
    public static HttpClient CreateClientWithToken(WebApplicationFactory<Program> factory, string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static async Task<(Guid UserId, string Token)> RegisterCustomerAsync(
        HttpClient client,
        string email)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("Test", "Customer", email, "Password123!"));

        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return (auth!.UserId, auth.Token);
    }

    private static int _emailCounter;

    public static async Task<(HttpClient Client, Guid UserId)> NewCustomerAsync(
        WebApplicationFactory<Program> factory)
    {
        var email = $"customer{Interlocked.Increment(ref _emailCounter)}@test.com";
        var (userId, token) = await RegisterCustomerAsync(factory.CreateClient(), email);
        return (CreateClientWithToken(factory, token), userId);
    }

    public static async Task<string> EnsureAdminTokenAsync(
        WebApplicationFactory<Program> factory,
        string email = "admin@test.com")
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();

        var existing = db.Users.FirstOrDefault(u => u.Email == new Email(email));
        if (existing is null)
        {
            var admin = User.CreateAdmin(
                "Admin",
                "Test",
                new Email(email),
                BCrypt.Net.BCrypt.HashPassword("AdminPass123!"));
            db.Users.Add(admin);
            db.SaveChanges();
        }

        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, "AdminPass123!"));

        login.EnsureSuccessStatusCode();

        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        return auth!.Token;
    }
}
