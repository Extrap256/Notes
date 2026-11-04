# Документация API

Это правила общения клиента (Android) с сервером (ASP.NET Core)

## Порядок чтения

| # | Файл | О чём |
|---|------|-------|
| 1 | [01-architecture.md](01-architecture.md) | Как устроен проект в целом |
| 2 | [02-authentication.md](02-authentication.md) | Регистрация, логин, JWT-токены |
| 3 | [03-api-reference.md](03-api-reference.md) | Все HTTP-эндпоинты с примерами |
| 4 | [04-data-models.md](04-data-models.md) | Структуры данных |
| 5 | [05-error-handling.md](05-error-handling.md) | Формат ошибок, коды, как обрабатывать |
| 6 | [06-sync-strategy.md](06-sync-strategy.md) | Синхронизация, офлайн, конфликты |
| 7 | [07-conventions.md](07-conventions.md) | Формат дат, пагинация, именование |

## Быстрый старт для клиента

1. Зарегистрировать пользователя
2. Получить токены через `api/auth/login`
3. Каждый последующий запрос отправлять с заголовком `Authorization: Bearer <accessToken>`
4. При ответе `401` - обновить токен через `/api/auth/refresh` и повторить запрос.

## Базовый URL

| Среда | URL |
|-------|-----|
| Dev (локально) | `http://localhost:5000/api` ||