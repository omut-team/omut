# Установка (Windows)

Делается один раз. Займёт около часа, большая часть времени уходит на скачивание Unity.

## 1. GitHub

1. Зарегистрируйся на https://github.com (если ещё нет аккаунта).
2. Скинь свой ник в общий чат, тебя добавят в репозиторий.
3. На почту придёт приглашение, нажми **Accept invitation**.

## 2. Git и Git LFS

1. Скачай Git: https://git-scm.com/download/win и установи, везде нажимая «Next».
2. Git LFS уже входит в установщик Git. Проверь в терминале (Win + R, `cmd`):

```bash
git --version
git lfs install
```

3. Представься git'у (подставь своё имя и почту от GitHub):

```bash
git config --global user.name "Имя Фамилия"
git config --global user.email "почта@от.github"
```

**Дизайнерам:** вместо терминала удобнее **GitHub Desktop**: https://desktop.github.com. Войди в аккаунт GitHub, дальше всё кнопками.

## 3. Скачать проект

Через терминал:

```bash
cd %USERPROFILE%\Desktop
git clone https://github.com/Yarik002/omut.git
cd omut
git lfs pull
git checkout dev
```

Через GitHub Desktop: **File → Clone repository → Yarik002/omut**, потом сверху выбери ветку **dev**.

## 4. Unity

1. Скачай Unity Hub: https://unity.com/download
2. Войди в Unity-аккаунт (бесплатная лицензия Personal).
3. Во вкладке **Installs** поставь **точно ту же версию**, что указана в файле `game/ProjectSettings/ProjectVersion.txt` (файл появится в патче 0.1). Другая версия = сломанный проект у всех.
4. Модули при установке: достаточно **Windows Build Support (Mono)**. Visual Studio Community ставь, если будешь писать код.
5. В Unity Hub: **Projects → Add → Add project from disk** → выбери папку `omut\game`.

## 5. Docker (только для сервера)

Нужен Ярославу и тем, кто хочет запускать сервер рекордов у себя. Остальным можно пропустить.

1. Скачай Docker Desktop: https://www.docker.com/products/docker-desktop
2. Запусти его и дождись зелёного статуса.
3. В папке проекта:

```bash
docker compose up
```

## Если что-то не работает

Напиши в общий чат: что делал, какая ошибка (скриншот). Не удаляй папку проекта и не делай `git reset`, сначала спроси.
