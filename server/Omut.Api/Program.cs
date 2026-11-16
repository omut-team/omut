// Сервер «Омута»: аккаунты (JWT), трофеи из игры, рекорды для сайта.
// Запуск: docker compose up --build (из корня репозитория), документация запросов: server/README.md.
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Omut.Api;
using Omut.Api.Models;
using Omut.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// База: PostgreSQL по строке подключения, в автотестах (Database:Provider = InMemory) база в памяти.
string? connection = builder.Configuration.GetConnectionString("Default");
bool inMemory = builder.Configuration["Database:Provider"] == "InMemory" || string.IsNullOrEmpty(connection);
builder.Services.AddDbContext<OmutDb>(options =>
{
    if (inMemory)
        options.UseInMemoryDatabase(builder.Configuration["Database:Name"] ?? "omut");
    else
        options.UseNpgsql(connection);
});

builder.Services.AddSingleton<TokenService>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = TokenService.Issuer,
            ValidateAudience = true,
            ValidAudience = TokenService.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = TokenService.Key(builder.Configuration),
            ValidateLifetime = true
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Создать таблицы при первом запуске. База ещё может подниматься, поэтому пробуем несколько раз.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OmutDb>();
    for (int attempt = 1; ; attempt++)
    {
        try
        {
            db.Database.EnsureCreated();
            break;
        }
        catch (Exception e) when (attempt < 10)
        {
            app.Logger.LogWarning("База не готова ({Attempt}/10): {Message}", attempt, e.Message);
            Thread.Sleep(2000);
        }
    }
}

// ---------- проверка ----------

app.MapGet("/api/health", () => Results.Ok(new { status = "ok", game = "Омут" }));

// ---------- аккаунты ----------

app.MapPost("/api/auth/register", async (AuthRequest request, OmutDb db, TokenService tokens) =>
{
    string name = (request.Name ?? "").Trim();
    string? problem = Validation.CheckName(name) ?? Validation.CheckPassword(request.Password);
    if (problem != null)
        return Results.BadRequest(new ErrorResponse(problem));

    if (await db.Players.AnyAsync(p => p.Name.ToLower() == name.ToLower()))
        return Results.Conflict(new ErrorResponse("Такое имя уже занято"));

    var player = new Player { Name = name, PasswordHash = PasswordHasher.Hash(request.Password) };
    db.Players.Add(player);
    await db.SaveChangesAsync();
    return Results.Ok(new AuthResponse(tokens.Create(player), player.Name));
});

app.MapPost("/api/auth/login", async (AuthRequest request, OmutDb db, TokenService tokens) =>
{
    string name = (request.Name ?? "").Trim();
    Player? player = await db.Players.FirstOrDefaultAsync(p => p.Name.ToLower() == name.ToLower());
    if (player == null || !PasswordHasher.Verify(request.Password ?? "", player.PasswordHash))
        return Results.Json(new ErrorResponse("Неверное имя или пароль"), statusCode: StatusCodes.Status401Unauthorized);

    return Results.Ok(new AuthResponse(tokens.Create(player), player.Name));
});

// ---------- трофеи ----------

app.MapPost("/api/catches", async (CatchRequest request, ClaimsPrincipal user, OmutDb db) =>
{
    string? problem = Validation.CheckCatch(request);
    if (problem != null)
        return Results.BadRequest(new ErrorResponse(problem));

    int playerId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    if (!await db.Players.AnyAsync(p => p.Id == playerId))
        return Results.Unauthorized();

    var item = new Catch
    {
        PlayerId = playerId,
        FishId = request.FishId.Trim(),
        FishName = request.FishName.Trim(),
        WeightKg = Math.Round(request.WeightKg, 2),
        Night = request.Night,
        Zone = request.Zone ?? "",
        GameDay = request.GameDay,
        Price = request.Price
    };
    db.Catches.Add(item);
    await db.SaveChangesAsync();
    return Results.Created($"/api/catches/{item.Id}", ToResponse(item, user.Identity!.Name!));
}).RequireAuthorization();

app.MapGet("/api/catches/me", async (ClaimsPrincipal user, OmutDb db) =>
{
    int playerId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    List<Catch> list = await db.Catches.Where(c => c.PlayerId == playerId)
        .OrderByDescending(c => c.CaughtAt).Take(200).ToListAsync();
    return Results.Ok(list.Select(c => ToResponse(c, user.Identity!.Name!)));
}).RequireAuthorization();

app.MapGet("/api/catches/recent", async (OmutDb db, int? top) =>
{
    List<Catch> list = await db.Catches.Include(c => c.Player)
        .OrderByDescending(c => c.CaughtAt).Take(Math.Clamp(top ?? 20, 1, 100)).ToListAsync();
    return Results.Ok(list.Select(c => ToResponse(c, c.Player!.Name)));
});

// ---------- рекорды ----------

// Самые тяжёлые трофеи (можно по одной рыбе: ?fish=pike).
app.MapGet("/api/leaderboard", async (OmutDb db, string? fish, int? top) =>
{
    IQueryable<Catch> query = db.Catches.Include(c => c.Player);
    if (!string.IsNullOrWhiteSpace(fish))
        query = query.Where(c => c.FishId == fish);

    List<Catch> list = await query.OrderByDescending(c => c.WeightKg).Take(Math.Clamp(top ?? 10, 1, 100)).ToListAsync();
    return Results.Ok(list.Select(c => ToResponse(c, c.Player!.Name)));
});

// Лучшие рыбаки: сколько поймали и сколько килограммов.
app.MapGet("/api/leaderboard/players", async (OmutDb db, int? top) =>
{
    var rows = await db.Catches
        .GroupBy(c => c.Player!.Name)
        .Select(g => new PlayerScore(g.Key, g.Count(), g.Sum(c => c.WeightKg), g.Max(c => c.WeightKg), g.Sum(c => c.Price)))
        .ToListAsync();

    return Results.Ok(rows.OrderByDescending(r => r.TotalKg).Take(Math.Clamp(top ?? 10, 1, 100))
        .Select(r => r with { TotalKg = Math.Round(r.TotalKg, 2) }));
});

// Рекорд по каждому виду рыбы.
app.MapGet("/api/leaderboard/fish", async (OmutDb db) =>
{
    List<Catch> heavy = await db.Catches.Include(c => c.Player).OrderByDescending(c => c.WeightKg).Take(5000).ToListAsync();
    var records = heavy.GroupBy(c => c.FishId)
        .Select(g => g.First())
        .OrderBy(c => c.FishName)
        .Select(c => new FishRecord(c.FishId, c.FishName, c.Player!.Name, c.WeightKg, c.CaughtAt));
    return Results.Ok(records);
});

app.MapGet("/api/stats", async (OmutDb db) =>
{
    int players = await db.Players.CountAsync();
    int catches = await db.Catches.CountAsync();
    double kg = catches == 0 ? 0 : await db.Catches.SumAsync(c => c.WeightKg);
    return Results.Ok(new StatsResponse(players, catches, Math.Round(kg, 2)));
});

app.Run();

static CatchResponse ToResponse(Catch c, string player) =>
    new(c.Id, player, c.FishId, c.FishName, c.WeightKg, c.Night, c.Zone, c.GameDay, c.Price, c.CaughtAt);

// Нужен автотестам (WebApplicationFactory<Program>).
public partial class Program { }
