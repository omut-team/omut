using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Omut.Api.Models;
using Omut.Api.Services;
using Xunit;

namespace Omut.Api.Tests;

// Сервер поднимается в памяти с базой в памяти (каждый тест со своей базой).
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string dbName = "test_" + Guid.NewGuid().ToString("N");

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseSetting("Database:Provider", "InMemory");
        builder.UseSetting("Database:Name", dbName);
        builder.UseSetting("Jwt:Key", "test-key-test-key-test-key-test-key-1234");
    }
}

public class ApiTests : IDisposable
{
    private readonly ApiFactory factory = new();
    private readonly HttpClient client;

    public ApiTests()
    {
        client = factory.CreateClient();
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
    }

    private async Task<string> Register(string name, string password = "secret123")
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/register", new AuthRequest(name, password));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!.Token;
    }

    private async Task SendCatch(string token, string fishId, string fishName, double kg)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/catches")
        {
            Content = JsonContent.Create(new CatchRequest(fishId, fishName, kg, false, "Pool", 1, 100))
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        HttpResponseMessage response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Health_IsOk()
    {
        HttpResponseMessage response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_ThenLogin_GivesToken()
    {
        await Register("Symon");

        HttpResponseMessage login = await client.PostAsJsonAsync("/api/auth/login", new AuthRequest("symon", "secret123"));

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        AuthResponse? body = await login.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.False(string.IsNullOrEmpty(body!.Token));
        Assert.Equal("Symon", body.Name);
    }

    [Fact]
    public async Task Register_SameName_Conflict()
    {
        await Register("Taras");

        HttpResponseMessage again = await client.PostAsJsonAsync("/api/auth/register", new AuthRequest("TARAS", "another1"));

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Theory]
    [InlineData("ab", "secret123")]
    [InlineData("good_name", "123")]
    [InlineData("плохое имя", "secret123")]
    public async Task Register_BadInput_400(string name, string password)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/register", new AuthRequest(name, password));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_401()
    {
        await Register("Mikhalych");

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/login", new AuthRequest("Mikhalych", "wrongpass"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Catch_WithoutToken_401()
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/catches", new CatchRequest("pike", "Щука", 5, false, "Pool", 1, 100));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Catch_BadWeight_400()
    {
        string token = await Register("Heavy");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/catches")
        {
            Content = JsonContent.Create(new CatchRequest("pike", "Щука", 500, false, "Pool", 1, 100))
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Leaderboard_SortedByWeight()
    {
        string a = await Register("Alpha");
        string b = await Register("Bravo");
        await SendCatch(a, "pike", "Щука", 4.5);
        await SendCatch(b, "pike", "Щука", 7.25);
        await SendCatch(a, "crucian", "Карась", 1.1);

        List<CatchResponse>? top = await client.GetFromJsonAsync<List<CatchResponse>>("/api/leaderboard?top=2");

        Assert.Equal(2, top!.Count);
        Assert.Equal("Bravo", top[0].Player);
        Assert.Equal(7.25, top[0].WeightKg);
    }

    [Fact]
    public async Task Leaderboard_FilterByFish()
    {
        string a = await Register("Gamma");
        await SendCatch(a, "pike", "Щука", 4.5);
        await SendCatch(a, "crucian", "Карась", 1.1);

        List<CatchResponse>? list = await client.GetFromJsonAsync<List<CatchResponse>>("/api/leaderboard?fish=crucian");

        Assert.Single(list!);
        Assert.Equal("crucian", list![0].FishId);
    }

    [Fact]
    public async Task Players_AndFishRecords_AndStats()
    {
        string a = await Register("Delta");
        string b = await Register("Echo");
        await SendCatch(a, "pike", "Щука", 4);
        await SendCatch(a, "pike", "Щука", 6);
        await SendCatch(b, "carp", "Карп", 3);

        List<PlayerScore>? players = await client.GetFromJsonAsync<List<PlayerScore>>("/api/leaderboard/players");
        List<FishRecord>? records = await client.GetFromJsonAsync<List<FishRecord>>("/api/leaderboard/fish");
        StatsResponse? stats = await client.GetFromJsonAsync<StatsResponse>("/api/stats");

        Assert.Equal("Delta", players![0].Player);
        Assert.Equal(2, players[0].Catches);
        Assert.Equal(10, players[0].TotalKg);
        Assert.Equal(6, records!.Single(r => r.FishId == "pike").WeightKg);
        Assert.Equal(2, stats!.Players);
        Assert.Equal(3, stats.Catches);
    }

    [Fact]
    public async Task MyCatches_OnlyMine()
    {
        string a = await Register("Foxtrot");
        string b = await Register("Golf");
        await SendCatch(a, "pike", "Щука", 4);
        await SendCatch(b, "carp", "Карп", 3);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/catches/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", a);
        HttpResponseMessage response = await client.SendAsync(request);
        List<CatchResponse>? mine = await response.Content.ReadFromJsonAsync<List<CatchResponse>>();

        Assert.Single(mine!);
        Assert.Equal("pike", mine![0].FishId);
    }
}

public class PasswordHasherTests
{
    [Fact]
    public void Hash_ThenVerify()
    {
        string hash = PasswordHasher.Hash("secret123");

        Assert.True(PasswordHasher.Verify("secret123", hash));
        Assert.False(PasswordHasher.Verify("Secret123", hash));
    }

    [Fact]
    public void SamePassword_DifferentHashes()
    {
        Assert.NotEqual(PasswordHasher.Hash("secret123"), PasswordHasher.Hash("secret123"));
    }

    [Fact]
    public void BrokenHash_IsFalse()
    {
        Assert.False(PasswordHasher.Verify("x", "not-a-hash"));
    }
}
