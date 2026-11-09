// Минимальный сервер «Омута». Пока заглушки, настоящая логика будет в патче 0.3.
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Проверка, что сервер жив
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", game = "Омут" }));

// Заглушка доски трофеев
app.MapGet("/api/leaderboard", () => Results.Ok(new[]
{
    new { player = "Сымон", fish = "Щука", weightKg = 4.2 },
    new { player = "Тарас", fish = "Сом", weightKg = 11.7 }
}));

app.Run();
