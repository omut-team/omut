# Старт для каждого: установка и как работать

Здесь всё по шагам: какой нужен компьютер, что поставить, как скачать проект, куда класть свою работу и как её сдавать.
Подходит для **Windows 10, Windows 11 и macOS**. Где шаги разные, они расписаны отдельно.
Читай сверху вниз. Если застрял, пиши в общий чат со скриншотом.

План игры: [PLAN.md](PLAN.md). Что делаешь ты и к какому сроку: [TEAM_PLAN.md](TEAM_PLAN.md).

## Часть 0. Какой нужен компьютер

### Unity (Илья, Ярослав, Лиза; Ульяне по желанию)
Версия Unity у нас **6000.3.28f1** (Unity 6.3 LTS). Требования к редактору по [документации Unity](https://docs.unity.com/en-us/engine/6000.3/manual/get-started/install-and-upgrade/getting-started-installing-unity/system-requirements):

| | Windows | macOS |
|---|---|---|
| Система | Windows 10 версии 21H1 (сборка 19043) и новее, 64-бит; Windows 11 | macOS 13 Ventura и новее |
| Процессор | x64 с SSE2 (обычный Intel или AMD). Arm64 только на Windows 11 | Intel x64 или Apple M1 и новее (на Apple нужен Rosetta 2) |
| Видеокарта | с поддержкой DirectX 11/12 или Vulkan, свежие драйверы | с поддержкой Metal |
| Память | от 8 ГБ, лучше 16 ГБ | от 8 ГБ, лучше 16 ГБ |
| Диск | примерно 20 ГБ свободно (Unity + проект) | примерно 20 ГБ свободно |

Как узнать версию системы:
- **Windows:** Win + R → `winver` → Enter. Там будет «Версия 22H2 (сборка ОС 19045...)».
- **Mac:**  → «Об этом Mac». Там будет «macOS Ventura 13» или новее.

На Mac с процессором Apple (M1, M2…) один раз поставь Rosetta 2: открой «Терминал» и выполни `softwareupdate --install-rosetta`.

### Docker (Ярославу обязательно, остальным по желанию)
Нужен, чтобы запускать сервер рекордов и сайт. Требования по [документации Docker](https://docs.docker.com/desktop/setup/install/windows-install/):

| | Windows | macOS |
|---|---|---|
| Система | Windows 10 22H2 (сборка 19045) и новее или Windows 11 23H2 (сборка 22631) и новее, 64-бит. Home тоже подходит | одна из трёх последних версий macOS |
| Что ещё | WSL 2 и включённая виртуализация в BIOS/UEFI | на Apple M1 и новее желательно Rosetta 2 |
| Память | от 8 ГБ | от 4 ГБ |

### Готовая игра (.exe)
Игру собираем под **Windows 10 (21H1 и новее) и Windows 11, 64-бит**, видеокарта с DirectX 11. Это требования плеера Unity 6.3.
На Mac файл `.exe` не запустится. Если тестировать надо на Mac, смотри [вопрос про Mac](TEAM_PLAN.md#вопросы-к-команде).

### Остальное
GitHub Desktop, Git, программы для рисования (Aseprite, LibreSprite, Pixelorama, Krita) и draw.io работают и на Windows, и на Mac. Для них хватает любого обычного ноутбука.

## Часть 1. Кто что ставит

| Программа | Илья | Ярослав | Ульяна | Лиза | Сарина |
|---|:-:|:-:|:-:|:-:|:-:|
| Аккаунт GitHub | ✅ | ✅ | ✅ | ✅ | ✅ |
| GitHub Desktop | ✅ | по желанию | ✅ | ✅ | ✅ |
| Git (+ Git LFS) | ✅ | ✅ | ✅ | ✅ | ✅ |
| Unity Hub + Unity 6000.3.28f1 | ✅ | ✅ | по желанию | ✅ | не нужно |
| VS Code или Visual Studio / Rider | по желанию | ✅ | | | |
| Docker Desktop | по желанию | ✅ | | | по желанию |
| Программа для рисования | | | ✅ | ✅ | |
| draw.io (диаграммы) | | | | | ✅ |

- **Илья** открывает Unity, чтобы собирать сцену `Main`, менять числа баланса и делать сборки. Код он не пишет.
- **Ульяна** может работать без Unity: готовые картинки она кладёт в `design/export/`, а в игру их подключает Ярослав (часть 7).
- **Сарина** проверяет готовые сборки игры из раздела **Releases** на GitHub, поэтому Unity ей не нужна.

## Часть 2. GitHub (все, 5 минут)

1. Зарегистрируйся на https://github.com, если ещё нет аккаунта.
2. Скинь ник в общий чат, тебя добавят в организацию.
3. **Сначала войди в свой GitHub** (github.com → Sign in). Без входа приглашение не видно и будет 404.
4. Прими приглашение одним из способов:
   - открой https://github.com/omut-team/omut, сверху жёлтая плашка **You've been invited** → **View invitation → Accept**;
   - или письмо от GitHub на почте → **View invitation**;
   - или колокольчик справа вверху: https://github.com/notifications
5. Если пришло приглашение в организацию **omut-team**, прими и его: https://github.com/orgs/omut-team/invitation
6. Проверка: открывается https://github.com/omut-team/omut и доска https://github.com/orgs/omut-team/projects/1.

## Часть 3. Git и GitHub Desktop (все, 10 минут)

**Windows 10/11:**
1. Скачай и поставь **Git**: https://git-scm.com/download/win. Везде жми «Next», ничего не меняй. Git LFS ставится вместе с ним.
2. Скачай и поставь **GitHub Desktop**: https://desktop.github.com
3. Открой GitHub Desktop → **Sign in to GitHub.com** → войди в свой аккаунт.
4. **File → Options → Git**: проверь, что имя и почта как в GitHub.

**macOS:**
1. Скачай **GitHub Desktop**: https://desktop.github.com, перетащи его в «Программы». Git и Git LFS у него встроены, больше ничего не нужно.
2. Открой → **Sign in to GitHub.com** → войди.
3. **GitHub Desktop → Settings → Git**: проверь имя и почту.

**Кто любит терминал.** Windows: Win + R → `cmd`. Mac: «Терминал». На Mac сначала поставь Git и LFS:

```bash
xcode-select --install
```

```bash
brew install git-lfs
```

Дальше одинаково на обеих системах:

```bash
git lfs install
git config --global user.name "Имя Фамилия"
git config --global user.email "почта@от.github"
```

`brew` это Homebrew (https://brew.sh). Если его нет, проще пользоваться GitHub Desktop.

## Часть 4. Скачать проект (все, 5 минут)

**Через GitHub Desktop:**
1. **File → Clone repository → вкладка GitHub.com → omut-team/omut**.
2. Local path: на Windows `C:\Users\<ты>\Desktop\omut`, на Mac `/Users/<ты>/Desktop/omut` → **Clone**.
3. Сверху **Current branch** → выбери **dev**.

**Через терминал (Windows):**

```bash
cd %USERPROFILE%\Desktop
git clone https://github.com/omut-team/omut.git
cd omut
git checkout dev
```

**Через терминал (Mac):**

```bash
cd ~/Desktop
git clone https://github.com/omut-team/omut.git
cd omut
git checkout dev
```

Проверка: в папке `omut` есть `README.md`, `docs`, `design`, `game`, `server`, `web`.

## Часть 5. Unity (Илья, Ярослав, Лиза; Ульяне по желанию)

**Важно:** у всех должна быть **одна и та же версия Unity: 6000.3.28f1**. Иначе проект ломается у всех.

1. Скачай **Unity Hub**: https://unity.com/download и установи (есть для Windows и Mac).
2. Открой Hub → войди в Unity-аккаунт (или зарегистрируйся) → лицензия **Personal** (бесплатно).
3. Нужная версия записана в `game/ProjectSettings/ProjectVersion.txt`: **6000.3.28f1**.
4. Hub → **Installs → Install Editor** → найди **6000.3.28f1**. Если её нет в списке, открой https://unity.com/releases/editor/archive, найди 6000.3.28f1 и нажми **Install** (откроется Hub).
   Модули:
   - **Windows Build Support (Mono)**: Илье и Ярославу, чтобы собирать `.exe`. На Mac этот модуль тоже ставится.
   - **Mac Build Support (Mono)**: только если решим делать сборку для Mac.
   - **Visual Studio** (Windows): Ярославу, если нет другого редактора кода.
5. Hub → **Projects → Add → Add project from disk** → выбери папку `omut/game`.
6. Первое открытие идёт долго (5–15 минут), так и должно быть.

Если Unity при открытии пишет про другую версию, **не соглашайся на обновление**, закрой и поставь 6000.3.28f1.

## Часть 6. Для Ярослава (код, сервер, сайт)

1. Редактор кода: **VS Code** (https://code.visualstudio.com, Windows и Mac), **Visual Studio Community** (только Windows) или **Rider**.
2. В VS Code открой папку `omut`. Справа внизу появится «Установить рекомендуемые расширения», жми **Install**.
3. В Unity: **Edit → Preferences** (на Mac **Unity → Settings**) **→ External Tools → External Script Editor** → выбери свой редактор.
4. Для сервера без Docker нужен **.NET SDK 10**: https://dotnet.microsoft.com/download

**Сервер и сайт (Docker):**
1. Поставь **Docker Desktop**: https://www.docker.com/products/docker-desktop (на Windows он сам предложит включить WSL 2). Запусти и дождись зелёного статуса.
2. В папке `omut`:
   ```bash
   docker compose up --build
   ```
3. Открой http://localhost:8080. Должна быть страница «Омут» и надпись «Сервер работает ✅».
4. Остановить: `Ctrl + C`, потом `docker compose down`.

## Часть 7. Кто куда кладёт свою работу

Главное правило: **каждый коммитит только в свои папки.** Тогда никто ничего не ломает, а Ярослав всегда знает, откуда забрать арт и тексты.

| Кто | Что | Куда коммитит | Кто забирает в игру |
|---|---|---|---|
| **Ульяна** | готовые PNG: лодки, герой, NPC, рыбы, предметы, интерфейс | `design/export/boats/`, `characters/`, `fish/`, `items/`, `ui/` | Ярослав переносит в `game/Assets/Art/...` |
| Ульяна | исходники (.aseprite, .psd, .kra, .pxo) | `design/src/ulyana/` | никто, это для правок |
| **Лиза** | тайлы и окружение (импортирует сама в Unity) | `game/Assets/Art/Tiles/`, `game/Assets/Art/Environment/` | уже в игре |
| Лиза | зоны на Tilemap | `game/Assets/Prefabs/Zones/`, своя сцена `game/Assets/Scenes/Sandbox/Sandbox_Liza.unity` | Илья ставит зоны в `Main` |
| Лиза | музыка и звуки | `game/Assets/Audio/Music/`, `game/Assets/Audio/SFX/` | Ярослав подключает в `AudioManager` |
| Лиза | исходники, логотип, обложка | `design/src/liza/`, `design/export/brand/` | Ярослав (логотип в меню) |
| **Сарина** | тексты: диалоги, квесты, дневник, описания рыб, тайна | `docs/texts/` | Ярослав переносит в данные игры |
| Сарина | ТЗ, диаграммы, тест-кейсы, руководство, презентация | `docs/TZ.md`, `docs/diagrams/`, `docs/tests/`, `docs/USER_GUIDE.md`, `docs/presentation/` | |
| Сарина | баги | Issues → **Баг** | Ярослав исправляет |
| **Илья** | числа баланса | ассеты в `game/Assets/Data/` и таблица `docs/BALANCE.md` | уже в игре |
| Илья | общая сцена | `game/Assets/Scenes/Main.unity` | уже в игре |
| **Ярослав** | код, тесты, префабы, данные | `game/Assets/Scripts/`, `Tests/`, `Prefabs/`, `Data/`, `Art/` (подключённый арт) | |
| Ярослав | сервер, сайт, Docker | `server/`, `web/`, `docker-compose.yml` | |

Если у Ульяны есть Unity и она хочет сама подключать арт, договоритесь с Ярославом. Тогда она импортирует PNG по правилам из [ART.md](ART.md#как-подключать-арт-в-unity) и коммитит вместе с `.meta`.

Подробнее про арт (размеры, имена, настройки): [ART.md](ART.md). Про тексты: [texts/README.md](texts/README.md).

## Часть 8. Как сдавать работу каждый день

Главное правило: **каждая задача делается в своей ветке, а в общую ветку `dev` попадает только через Pull Request.**
В `main` никто не пушит напрямую: туда тимлид сливает готовый патч.

### Через GitHub Desktop (проще)

**1. Взять задачу.**
Открой [доску задач](https://github.com/orgs/omut-team/projects/1) → своя задача → справа **Assignees → assign yourself** → перетащи карточку в **In Progress**. После слияния PR задача сама закроется, перетащи её в **Done**.

**2. Обновиться.**
GitHub Desktop → **Current branch: dev** → кнопка **Fetch origin / Pull origin**.

**3. Сделать свою ветку.**
**Current branch → New branch** → имя по задаче:
- `feature/boat-movement` (код, Ярослав)
- `art/fish-day` (арт, Ульяна и Лиза)
- `sound/fishing-sfx` (звук, Лиза)
- `level/bay-zone` (зоны и сцена, Лиза и Илья)
- `balance/fish-0-2` (числа, Илья)
- `docs/use-case` (документы и тексты, Сарина)
- `fix/inventory-drag` (исправление)

Based on: **dev** → **Create branch**.

**4. Работать.**
Рисуешь, пишешь, кладёшь файлы в **свои** папки из части 7. GitHub Desktop сам покажет слева, что изменилось.

**5. Закоммитить.**
Слева внизу пишешь короткое описание, например `art: спрайты карася и окуня` → **Commit to ...**.
Коммить небольшими кусками: сделал кусок, коммит.

**6. Отправить.**
Кнопка **Push origin** (или **Publish branch**, если первый раз).

**7. Открыть Pull Request.**
Кнопка **Create Pull Request** → откроется сайт.
- Сверху проверь: **base: dev ← compare: твоя ветка**.
- В описании напиши `Closes #12` (номер своей задачи).
- **Create pull request**.

**8. Проверка.**
Код, арт, баланс и документы проверяет Илья. Арт ещё смотрит Ярослав: встаёт ли он в игру. После **Approve** PR сливается.
Попросили исправить? Исправляешь в той же ветке, коммит, Push, PR обновится сам.

**9. После слияния.**
Переключись на **dev** → **Pull origin**. Для следующей задачи снова с шага 3.

### То же самое через терминал

```bash
git checkout dev
git pull
git checkout -b art/fish-day
```

Работаешь, потом:

```bash
git add .
git commit -m "art: спрайты карася и окуня"
git push -u origin art/fish-day
```

Дальше на сайте появится жёлтая плашка **Compare & pull request**: жми и оформляй PR в `dev`.

### Как писать коммиты

| Начало | Когда | Пример |
|---|---|---|
| `feat:` | новая возможность | `feat: заброс удочки по Space` |
| `fix:` | исправление | `fix: леска не рвалась` |
| `art:` | графика | `art: тайлы воды и берега` |
| `sound:` | звук | `sound: колокол маяка` |
| `level:` | зоны и сцена | `level: заросший залив` |
| `balance:` | числа в данных | `balance: цены дневной рыбы` |
| `docs:` | документы и тексты | `docs: диаграмма состояний рыбы` |
| `test:` | тесты | `test: тест-кейсы на трюм` |

## Часть 9. Правила, чтобы ничего не сломать

1. **Перед работой всегда Pull.** Перед открытием Unity тоже.
2. **Коммить только в свои папки** (часть 7). Чужие файлы не удаляй и не переименовывай без вопроса.
3. **Общую сцену `Main` трогает только Илья.** Остальные работают в своей сцене `Sandbox_<Имя>`.
4. Объекты делаем **префабами**. Один префаб правит один человек за раз.
5. Файлы `.meta` всегда коммитятся **вместе** со своим файлом.
6. Не коммить папки `Library`, `Temp`, `Logs`, `UserSettings` (они и так скрыты).
7. Никаких паролей и ключей в репозитории.
8. Хочешь что-то поменять в самой игре (рыбу, механику, сюжет)? Не меняй молча, смотри [как менять план](PLAN.md#как-менять-план).

### Если что-то пошло не так

| Проблема | Что делать |
|---|---|
| GitHub Desktop пишет про **conflict** | Ничего не жми, напиши в чат, разберём вместе |
| Unity ругается на версию | Значит стоит не та версия, см. часть 5 |
| Закоммитил не в ту ветку | Не пушь, напиши в чат |
| Картинки в проекте розовые или пустые | В терминале в папке проекта: `git lfs pull` |
| Не можешь сделать Push | Проверь, что принял приглашение (часть 2) |
| Mac пишет «приложение от неизвестного разработчика» | Правый клик по приложению → **Открыть** → **Открыть** |

**Никогда** не делай `git reset --hard`, `git push --force` и не удаляй папку проекта, не спросив.

## Часть 10. С чего начать

1. Части 0–4 этой инструкции (задача #1, скинуть ник).
2. Открыть [TEAM_PLAN.md](TEAM_PLAN.md) и найти свой раздел: там всё по патчам и срокам.
3. Взять свою первую задачу на [доске](https://github.com/orgs/omut-team/projects/1).

Все задачи: https://github.com/omut-team/omut/issues
