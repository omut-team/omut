# Сервер

ASP.NET Core Web API (.NET 10) + PostgreSQL (EF Core) + вход по JWT. Хранит аккаунты игроков и трофеи, отдаёт рекорды игре и сайту.

Отвечает: Ярослав.

## Запуск

Всё вместе (база, сервер, сайт), из корня репозитория, нужен запущенный Docker Desktop:

```bash
docker compose up --build
```

- сайт с рекордами: http://localhost:8080
- сервер: http://localhost:5000/api/health

Остановить: `Ctrl+C`, потом `docker compose down`. Стереть базу совсем: `docker compose down -v`.

Только сервер, без Docker и без PostgreSQL (база в памяти, после перезапуска пустая):

```bash
cd server/Omut.Api
dotnet run
```

Автотесты (xUnit, 16 штук):

```bash
cd server/Omut.Api.Tests
dotnet test
```

## Запросы

| Запрос | Что делает | Вход |
|---|---|---|
| `GET /api/health` | сервер жив | |
| `POST /api/auth/register` `{ "name", "password" }` | регистрация, ответ `{ token, name }` | |
| `POST /api/auth/login` `{ "name", "password" }` | вход, ответ `{ token, name }` | |
| `POST /api/catches` `{ fishId, fishName, weightKg, night, zone, gameDay, price }` | отправить трофей из игры | JWT |
| `GET /api/catches/me` | мои трофеи | JWT |
| `GET /api/catches/recent?top=20` | последние трофеи всех игроков | |
| `GET /api/leaderboard?fish=pike&top=10` | самые тяжёлые трофеи (можно по виду рыбы) | |
| `GET /api/leaderboard/players` | лучшие рыбаки: сколько поймали, сколько кг | |
| `GET /api/leaderboard/fish` | рекорд по каждому виду рыбы | |
| `GET /api/stats` | всего игроков, трофеев, кг | |

JWT передаётся в заголовке `Authorization: Bearer <токен>`. Токен живёт 30 дней.

## Как устроено

- `Program.cs`: настройка и все запросы (Minimal API).
- `OmutDb.cs`: база (таблицы `Players`, `Catches`). Таблицы создаются сами при первом запуске (`EnsureCreated`).
- `Services/PasswordHasher.cs`: пароль хранится только как хеш PBKDF2 с солью.
- `Services/TokenService.cs`: выдаёт JWT. Ключ из переменной `JWT__KEY` (см. `.env.example`).
- `Services/Validation.cs`: проверка имени, пароля и трофея.
- `Omut.Api.Tests`: тесты поднимают сервер в памяти (`WebApplicationFactory`) с базой в памяти.

Аналитика (`POST /api/events`) в «если успеем».
