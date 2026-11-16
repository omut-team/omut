using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Omut.Api.Models;

namespace Omut.Api.Services;

// Выдаёт JWT-токен после входа. Игра шлёт его в заголовке Authorization: Bearer <токен>.
public class TokenService(IConfiguration config)
{
    public const string Issuer = "omut-api";
    public const string Audience = "omut-game";

    // Ключ подписи. В Docker задаётся переменной JWT__KEY (см. .env.example).
    public static SymmetricSecurityKey Key(IConfiguration config)
    {
        string key = config["Jwt:Key"] ?? "";
        if (key.Length < 32)
            key = "omut-dev-key-only-for-local-play-change-me!!";
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    }

    public string Create(Player player)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, player.Id.ToString()),
            new Claim(ClaimTypes.Name, player.Name)
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(30),
            signingCredentials: new SigningCredentials(Key(config), SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
