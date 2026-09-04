using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace FyM.Users.IntegrationTests;

public sealed class AuthFlowTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string SuperAdminEmail = "admin@fymtechnology.com";
    private const string SuperAdminPassword = "Adm1n#Test2026*";

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.InitializeDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_WithSeededSuperAdmin_ReturnsAccessToken()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email = SuperAdminEmail, password = SuperAdminPassword });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("user").GetProperty("roles").EnumerateArray().Select(r => r.GetString()).Should().Contain("SuperAdmin");
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnprocessableEntity()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email = SuperAdminEmail, password = "wrong-password" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Login_WithInvalidPayload_ReturnsBadRequestWithFieldErrors()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email = "not-an-email", password = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errors").TryGetProperty("Email", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Users_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminCreatedBySuperAdmin_CannotDeactivateSuperAdmin()
    {
        var superAdminToken = await LoginAsync(SuperAdminEmail, SuperAdminPassword);
        var superAdminId = await GetOwnUserIdAsync(superAdminToken);

        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users");
        createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", superAdminToken);
        createRequest.Content = JsonContent.Create(new
        {
            userName = "jadmin",
            email = "jadmin@fym.com",
            password = "Adm1n#Test2026*",
            firstName = "Juan",
            lastName = "Admin",
            roleIds = new[] { 2 },
        });
        var createResponse = await _client.SendAsync(createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var adminToken = await LoginAsync("jadmin@fym.com", "Adm1n#Test2026*");

        using var patchRequest = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/users/{superAdminId}/status");
        patchRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        patchRequest.Content = JsonContent.Create(new { isActive = false });
        var patchResponse = await _client.SendAsync(patchRequest);

        patchResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("accessToken").GetString()!;
    }

    private async Task<string> GetOwnUserIdAsync(string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetString()!;
    }
}
