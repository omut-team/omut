# Сервер

ASP.NET Core Web API (.NET 10). Сейчас это заглушка: `/api/health` и `/api/leaderboard`.
Настоящие аккаунты, трофеи и база появятся в патче 0.3 (этапы 15 и 16 в [docs/PLAN.md](../docs/PLAN.md)).

Отвечает: Ярослав.

## Запуск

Всё вместе (сервер, база, сайт), из корня репозитория:

```bash
docker compose up --build
```

- сайт: http://localhost:8080
- сервер: http://localhost:5000/api/health

Только сервер, без Docker:

```bash
cd server/Omut.Api
dotnet run
```

Планируемые запросы (этап 15):

| Запрос | Что делает |
|---|---|
| `POST /api/auth/register`, `POST /api/auth/login` | регистрация и вход (JWT) |
| `POST /api/catches` | отправить трофей |
| `GET /api/leaderboard` | рекорды |

Аналитика (`POST /api/events`) в «если успеем».

Нужно: Docker Desktop или .NET SDK 10 (Windows 10/11 и macOS), см. [docs/SETUP.md](../docs/SETUP.md#часть-6-для-ярослава-код-сервер-сайт).
