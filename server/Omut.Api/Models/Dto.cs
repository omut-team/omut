namespace Omut.Api.Models;

// То, что приходит в запросах и уходит в ответах (DTO). Сущности базы наружу не отдаём.
public record AuthRequest(string Name, string Password);

public record AuthResponse(string Token, string Name);

public record CatchRequest(string FishId, string FishName, double WeightKg, bool Night, string Zone, int GameDay, int Price);

public record CatchResponse(int Id, string Player, string FishId, string FishName, double WeightKg, bool Night, string Zone, int GameDay, int Price, DateTime CaughtAt);

public record PlayerScore(string Player, int Catches, double TotalKg, double BiggestKg, int TotalPrice);

public record FishRecord(string FishId, string FishName, string Player, double WeightKg, DateTime CaughtAt);

public record StatsResponse(int Players, int Catches, double TotalKg);

public record ErrorResponse(string Error);
