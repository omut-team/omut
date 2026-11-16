namespace Omut.Api.Models;

// Игрок (аккаунт). Пароль хранится только в виде хеша.
public class Player
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Catch> Catches { get; set; } = new();
}

// Пойманный трофей, присланный из игры.
public class Catch
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public Player? Player { get; set; }

    public string FishId { get; set; } = "";
    public string FishName { get; set; } = "";
    public double WeightKg { get; set; }
    public bool Night { get; set; }
    public string Zone { get; set; } = "";
    public int GameDay { get; set; }
    public int Price { get; set; }
    public DateTime CaughtAt { get; set; } = DateTime.UtcNow;
}
