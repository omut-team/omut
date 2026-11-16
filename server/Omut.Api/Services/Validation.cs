using Omut.Api.Models;

namespace Omut.Api.Services;

// Проверка входных данных. null = всё хорошо, иначе текст ошибки для игрока.
public static class Validation
{
    public static string? CheckName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length < 3 || name.Length > 20)
            return "Имя от 3 до 20 символов";

        foreach (char ch in name)
        {
            if (!char.IsLetterOrDigit(ch) && ch != '_' && ch != '-')
                return "В имени только буквы, цифры, _ и -";
        }

        return null;
    }

    public static string? CheckPassword(string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 6)
            return "Пароль не короче 6 символов";
        return password.Length > 100 ? "Слишком длинный пароль" : null;
    }

    public static string? CheckCatch(CatchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FishId) || request.FishId.Length > 40)
            return "Неверный вид рыбы";
        if (string.IsNullOrWhiteSpace(request.FishName) || request.FishName.Length > 60)
            return "Неверное название рыбы";
        if (request.WeightKg <= 0 || request.WeightKg > 100)
            return "Вес от 0 до 100 кг";
        if (request.Price < 0 || request.GameDay < 0)
            return "Неверная цена или день";
        return null;
    }
}
