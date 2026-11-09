# Сервер

ASP.NET Core Web API (.NET 10). Сейчас это заглушка: `/api/health` и `/api/leaderboard`.
Настоящие аккаунты, трофеи и база появятся в патче 0.3.

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

Планируемые запросы: [docs/CONCEPT.md](../docs/CONCEPT.md#9-онлайн-часть).
