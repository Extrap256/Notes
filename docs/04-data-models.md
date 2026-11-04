# Структуры данных

dto (data transfer object) - это шаблон проектирования, который представляет собой простой объект-контейнер, используемый для переноса данных между различными слоями или компонентами приложения.

## NoteDTO

Поля заметки, которые приходят с сервера и отправляются на сервер.

| Поле | Тип | Обязательное | Ограничения | Описание |
|------|-----|-------------|-------------|----------|
| `id` | string (GUID) | — (генерирует сервер) | UUID v4 | Уникальный идентификатор |
| `title` | string | Да | 1–200 символов | Заголовок заметки |
| `content` | string | Нет | до 100_000 символов | Текст заметки |
| `isDeleted` | boolean | — (по умолчанию `false`) | | Пометка мягкого удаления |
| `createdAt` | string (ISO 8601) | — (генерирует сервер) | UTC | Дата создания |
| `updatedAt` | string (ISO 8601) | — (генерирует сервер) | UTC | Дата последнего изменения |
| `version` | integer | — (генерирует сервер) | >= 1| Версия для обнаружения конфликтов |

### Пример

```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "title": "Идеи для проекта",
  "content": "Сделать заметки с синхронизацией",
  "isDeleted": false,
  "createdAt": "2026-10-29T08:00:00Z",
  "updatedAt": "2026-10-29T10:15:00Z",
  "version": 2
}
```


# 04 — Модели данных (DTO)

Все структуры, которые передаются между клиентом и сервером.
Каждый DTO привязан к конкретному эндпоинту из
[03-api-reference.md](03-api-reference.md).

**Соглашения:**

- Все имена полей в JSON — `camelCase`
- Все даты — ISO 8601 UTC с суффиксом `Z`, пример: `2026-10-29T14:30:00Z`
- Все идентификаторы — строки в формате UUID v4 (GUID)
- `null` и отсутствующее поле — разные вещи. Если поле не указано
  в таблице как обязательное, его можно не отправлять или отправить `null`

---

## Содержание

- [Общие типы](#общие-типы)
- [Аутентификация](#аутентификация)
  - [RegisterRequest](#registerrequest)
  - [RegisterResponse](#registerresponse)
  - [LoginRequest](#loginrequest)
  - [AuthResponse](#authresponse)
  - [RefreshRequest](#refreshrequest)
- [Заметки](#заметки)
  - [NoteDto](#notedto)
  - [CreateNoteRequest](#createnoterequest)
  - [UpdateNoteRequest](#updatenoterequest)
  - [NotesListResponse](#noteslistresponse)
- [Ошибки](#ошибки)
  - [ErrorResponse](#errorresponse)
  - [ValidationErrorDetail](#validationerrordetail)

---

## Общие типы

| Тип | Формат | Пример | Где используется |
|-----|--------|--------|-----------------|
| UUID (GUID) | строка, формат `8-4-4-4-12` | `"a1b2c3d4-e5f6-7890-abcd-ef1234567890"` | Все поля `id` |
| DateTime | строка, ISO 8601 UTC | `"2026-10-29T14:30:00Z"` | `createdAt`, `updatedAt`, `since` |
| Version | целое число ≥ 1 | `3` | Поле `version` |

---

## Аутентификация

---

### RegisterRequest

Тело запроса для `POST /api/auth/register`.

| Поле | Тип | Обязательное | Ограничения | Описание |
|------|-----|-------------|-------------|----------|
| `login` | string | Да | Минимум 3 символа | Логин пользователя |
| `password` | string | Да | Минимум 8 символов | Пароль |

**Пример JSON:**

```json
{
  "login": "Login123",
  "password": "Str0ng!Pass1"
}
```

---

### RegisterResponse

Тело ответа `201 Created` для `POST /api/auth/register`.

| Поле | Тип | Описание |
|------|-----|----------|
| `id` | string (UUID) | Идентификатор созданного пользователя |
| `login` | string | Логин пользователя |

**Пример JSON:**

```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "login": "Login123"
}
```

---

### LoginRequest

Тело запроса для `POST /api/auth/login`.

| Поле | Тип | Обязательное | Ограничения | Описание |
|------|-----|-------------|-------------|----------|
| `login` | string | Да | непустой login | Логин пользователя |
| `password` | string | Да | Непустая строка | Пароль |

**Пример JSON:**

```json
{
  "login": "login123",
  "password": "Str0ng!Pass1"
}
```

---

### AuthResponse

Тело ответа `200 OK` для `POST /api/auth/login`
и `POST /api/auth/refresh`.
Используется в обоих случаях — структура одинаковая.

| Поле | Тип | Описание |
|------|-----|----------|
| `accessToken` | string | Токен доступа. Подставляется в заголовок `Authorization: Bearer <accessToken>` каждого запроса. Время жизни — `expiresIn` секунд |
| `refreshToken` | string | Токен обновления. Используется **только** для `POST /api/auth/refresh`. Хранить в защищённом хранилище |
| `expiresIn` | int | Время жизни `accessToken` в секундах |

**Пример JSON:**

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhMWIyYzNkNCIsImVtYWlsIjoidXNlckBleGFtcGxlLmNvbSIsImV4cCI6MTczNTUwMDAwMH0.abc123",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4",
  "expiresIn": 1800
}
```

> **Для клиента:** не парсить `accessToken`. Не менять его.
> Просто сохранить строку как есть и подставлять в заголовок.

---

### RefreshRequest

Тело запроса для `POST /api/auth/refresh`.

| Поле | Тип | Обязательное | Описание |
|------|-----|-------------|----------|
| `refreshToken` | string | Да | Токен обновления, полученный при логине или предыдущем refresh |

**Пример JSON:**

```json
{
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4"
}
```

**Ответ:** [AuthResponse](#authresponse)

---

## Заметки

---

### NoteDto

Полное представление заметки. Возвращается сервером в ответах:

- `GET /api/notes` — внутри массива `items`
- `GET /api/notes/{id}` — как тело ответа
- `POST /api/notes` — как тело ответа `201`
- `PUT /api/notes/{id}` — как тело ответа `200`
- `DELETE /api/notes/{id}` — как тело ответа `200`
- `GET /api/notes/deleted` — внутри массива `items`

| Поле | Тип | Описание |
|------|-----|----------|
| `id` | string (UUID) | Уникальный идентификатор заметки. Генерируется сервером |
| `title` | string | Заголовок заметки. 1–200 символов |
| `content` | string | Текст заметки. Может быть пустой строкой `""`. Максимум 100 000 символов |
| `isDeleted` | boolean | Пометка мягкого удаления. `false` — обычная заметка, `true` — удалена |
| `createdAt` | string (DateTime) | Дата и время создания. Генерируется сервером. Не изменяется |
| `updatedAt` | string (DateTime) | Дата и время последнего изменения. Обновляется сервером при каждом `PUT` и `DELETE` |
| `version` | int | Версия заметки. Увеличивается на 1 при каждом изменении. Используется для обнаружения конфликтов при синхронизации |

**Пример JSON — обычная заметка:**

```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "title": "Список покупок",
  "content": "Молоко, хлеб, яйца",
  "isDeleted": false,
  "createdAt": "2026-10-28T09:00:00Z",
  "updatedAt": "2026-10-28T14:30:00Z",
  "version": 3
}
```

**Пример JSON — удалённая заметка:**

```json
{
  "id": "d4e5f6a7-b8c9-0123-def0-234567890123",
  "title": "Старая заметка",
  "content": "Удалённый текст",
  "isDeleted": true,
  "createdAt": "2026-10-20T10:00:00Z",
  "updatedAt": "2026-10-28T11:30:00Z",
  "version": 4
}
```

> **Для клиента:** поля `id`, `createdAt`, `updatedAt`, `version`,
> `isDeleted` никогда не отправляются на сервер при создании.
> При обновлении отправляется только `version` (см. `UpdateNoteRequest`).

---

### CreateNoteRequest

Тело запроса для `POST /api/notes`.

| Поле | Тип | Обязательное | Ограничения | Описание |
|------|-----|-------------|-------------|----------|
| `title` | string | Да | 1–200 символов, не пустая строка и не только пробелы | Заголовок заметки |
| `content` | string | Нет | до 100 000 символов | Текст заметки. Если не передан или `null` — сервер сохранит как пустую строку `""` |

> Поля `id`, `isDeleted`, `createdAt`, `updatedAt`, `version`
> **не отправляются**. Сервер генерирует их сам.

**Пример JSON — с текстом:**

```json
{
  "title": "Новая заметка",
  "content": "Текст заметки"
}
```

**Пример JSON — без текста:**

```json
{
  "title": "Заметка без текста"
}
```

**Ответ:** [NoteDto](#notedto) с кодом `201 Created`

---

### UpdateNoteRequest

Тело запроса для `PUT /api/notes/{id}`.

| Поле | Тип | Обязательное | Ограничения | Описание |
|------|-----|-------------|-------------|----------|
| `title` | string | Да | 1–200 символов, не пустая строка и не только пробелы | Новый заголовок заметки |
| `content` | string | Нет | до 100 000 символов | Новый текст заметки. Если не передан или `null` — сервер сохранит как пустую строку `""` |
| `version` | int | Да | ≥ 1 | Текущая версия заметки, которую клиент получил при последнем чтении. Обязателен для обнаружения конфликтов |

> **Для клиента:** поле `version` — это не то, что ты хочешь поставить.
> Это то, что ты **получил** от сервера в последний раз.
> Если ты получил заметку с `version: 3`, то при обновлении
> отправляешь `version: 3`. Сервер проверит, совпадает ли она
> с текущей. Если совпадает — обновит и вернёт `version: 4`.
> Если не совпадает — вернёт `409 Conflict`.

**Пример JSON:**

```json
{
  "title": "Обновлённый заголовок",
  "content": "Обновлённый текст",
  "version": 3
}
```

**Ответ:** [NoteDto](#notedto) с кодом `200 OK` (с новой `version`)

**Возможная ошибка:** `409 Conflict` — если `version` не совпадает

---

### NotesListResponse

Тело ответа `200 OK` для `GET /api/notes` и `GET /api/notes/deleted`.

| Поле | Тип | Описание |
|------|-----|----------|
| `items` | array of [NoteDto](#notedto) | Массив заметок. Может быть пустым `[]` |
| `totalCount` | int | Общее количество заметок, подходящих под фильтр (без учёта пагинации) |

**Пример JSON:**

```json
{
  "items": [
    {
      "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "title": "Список покупок",
      "content": "Молоко, хлеб, яйца",
      "isDeleted": false,
      "createdAt": "2026-10-28T09:00:00Z",
      "updatedAt": "2026-10-28T14:30:00Z",
      "version": 3
    },
    {
      "id": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
      "title": "Идеи для проекта",
      "content": "",
      "isDeleted": false,
      "createdAt": "2026-10-29T08:00:00Z",
      "updatedAt": "2026-10-29T08:00:00Z",
      "version": 1
    }
  ],
  "totalCount": 42
}
```

**Пример JSON — пустой список:**

```json
{
  "items": [],
  "totalCount": 0
}
```

> **Для клиента:** если `items` пустой — это не ошибка.
> Просто нет заметок, подходящих под фильтр.

---

## Ошибки

---

### ErrorResponse

Единый формат тела ошибки. Возвращается при любом коде `4xx` или `5xx`.

| Поле | Тип | Описание |
|------|-----|----------|
| `statusCode` | int | HTTP-код ошибки (`400`, `401`, `404`, `409`, `500`) |
| `error` | string | Машинно-читаемый код ошибки. По нему клиент определяет логику обработки |
| `message` | string | Человекочитаемое сообщение. Можно показать пользователю в UI |
| `details` | array of [ValidationErrorDetail](#validationerrordetail) или `null` | Детали валидации. Заполнено только при `400 ValidationError`. В остальных случаях — `null` |

**Пример JSON — ошибка валидации:**

```json
{
  "statusCode": 400,
  "error": "ValidationError",
  "message": "Не удалось создать заметку",
  "details": [
    {
      "field": "title",
      "message": "Заголовок обязателен"
    },
    {
      "field": "title",
      "message": "Заголовок не должен превышать 200 символов"
    }
  ]
}
```

**Пример JSON — ошибка авторизации:**

```json
{
  "statusCode": 401,
  "error": "Unauthorized",
  "message": "Токен доступа истёк",
  "details": null
}
```

**Пример JSON — конфликт версий:**

```json
{
  "statusCode": 409,
  "error": "Conflict",
  "message": "Заметка была изменена на сервере. Получите актуальную версию",
  "details": null
}
```

**Пример JSON — не найдено:**

```json
{
  "statusCode": 404,
  "error": "NotFound",
  "message": "Заметка не найдена",
  "details": null
}
```

---

### ValidationErrorDetail

Элемент массива `details` в [ErrorResponse](#errorresponse).
Появляется только при `400 ValidationError`.

| Поле | Тип | Описание |
|------|-----|----------|
| `field` | string | Имя поля, в котором ошибка. Совпадает с именем поля в запросе |
| `message` | string | Человекочитаемое описание ошибки для этого поля |


**Пример:**

```json
{
  "field": "title",
  "message": "Заголовок обязателен"
}
```

---

## Сводная таблица: какой DTO где используется

| Эндпоинт | Тело запроса | Тело ответа |
|----------|-------------|-------------|
| `POST /api/auth/register` | [RegisterRequest](#registerrequest) | [RegisterResponse](#registerresponse) |
| `POST /api/auth/login` | [LoginRequest](#loginrequest) | [AuthResponse](#authresponse) |
| `POST /api/auth/refresh` | [RefreshRequest](#refreshrequest) | [AuthResponse](#authresponse) |
| `GET /api/notes` | — | [NotesListResponse](#noteslistresponse) |
| `GET /api/notes/{id}` | — | [NoteDto](#notedto) |
| `POST /api/notes` | [CreateNoteRequest](#createnoterequest) | [NoteDto](#notedto) |
| `PUT /api/notes/{id}` | [UpdateNoteRequest](#updatenoterequest) | [NoteDto](#notedto) |
| `DELETE /api/notes/{id}` | — | [NoteDto](#notedto) |
| `GET /api/notes/deleted` | — | [NotesListResponse](#noteslistresponse) |

---

## Changelog

| Дата | Изменение |
|------|-----------|
| 2026-10-29 | Первая версия документации |

