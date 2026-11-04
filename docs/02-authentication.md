# Аутентификация

Для аутентификации пользователя используется JWT-токен.
Токен передается как Bearer в заголовке `Authorization` запроса

# Что такое JWT

JWT (JSON Web Token) — это строка, которую сервер выдаёт после
успешного логина. Клиент прикрепляет её к каждому запросу,
чтобы сервер понимал, кто обращается.

Токен выглядит так:
`eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0In0.abc123`

Тебе не нужно её парсить или менять. Просто хранишь и подставляешь.

# 2 токена

Используем 2 токена: accessToken и refreshToken

| Токен | Время жизни | Когда используется |
|-------|------------|-------------------|
| `accessToken` | 30 минут | Каждый запрос к API |
| `refreshToken` | 7 дней | Только для получения нового accessToken |

accessToken живет мало, поэтому используем именно его для аутентификации
refreshToken жиет долго, поэтому его используем для обновления access токена

Если refreshToken иссяк, то просто логинимся по новой


# Диаграмма использования

```markdown
sequenceDiagram
    participant C as Клиент (Android)
    participant S as Сервер

    C->>S: POST /auth/login {login, password}
    S-->>C: 200 {accessToken, refreshToken}

    C->>S: GET /notes + Authorization: Bearer <accessToken>
    S-->>C: 200 [список заметок]

    Note over C: accessToken протух

    C->>S: GET /notes + Authorization: Bearer <старый токен>
    S-->>C: 401 Unauthorized

    C->>S: POST /auth/refresh {refreshToken}
    S-->>C: 200 {новый accessToken, новый refreshToken}

    C->>S: GET /notes + Authorization: Bearer <новый токен>
    S-->>C: 200 [список заметок]
```

## Changelog

| Дата | Изменение |
|------|-----------|
| 2026-10-29 | Первая версия документации |