# Проект: Система обработки заявок (Helpdesk API)

## О проекте
Данный проект представляет собой REST API для внутренней ИТ-системы обработки заявок (Helpdesk). 
Система позволяет сотрудникам создавать заявки (например, на ремонт техники или установку ПО), а ИТ-специалистам — назначать исполнителей, комментировать ход работ и отслеживать историю изменения статусов.

**Основные возможности:**
- Создание и фильтрация заявок по статусу и приоритету.
- Назначение исполнителей на заявки.
- Ведение истории изменений статусов (аудит).
- Добавление комментариев и загрузка файлов (вложений) к заявкам.

**Стек технологий:** 
- Язык: **C#** (ASP.NET Core Web API)
- База данных: **PostgreSQL** (ADO.NET через провайдер `Npgsql`)
- Документация: **Swagger UI** (OpenAPI)

---

## Как запустить проект
1. Убедитесь, что установлены **.NET SDK** и **PostgreSQL**.
2. Создайте базу данных `admin` в pgAdmin 4 и выполните SQL-скрипт для создания таблиц.
3. В файле `appsettings.json` укажите вашу строку подключения (пароль от PostgreSQL).
4. В терминале выполните:
   dotnet run

## 1, 2, 3. Структура базы данных, ER-диаграмма и описание полей

Вся структура проекта (таблицы, их поля, типы данных и связи) спроектирована в Figma и представлена на единой схеме ниже:

![ER-диаграмма](docs/er-diagram.png)

Ссылка на figma: https://www.figma.com/board/nmEpMTlCbkSpVpOpNOsAjV/projectrpo?t=dTTsm9YsdGbsj4ec-6

**Краткое описание связей:**
*   **Один-ко-многим (1:N):** 
    *   `Roles` → `Users` (одна роль у многих пользователей)
    *   `Users` → `Requests` (один автор создает много заявок)
    *   `Requests` → `Comments`, `Assignments`, `Status_history` (у заявки много комментариев, назначений и записей в истории)
*   **Многие-ко-многим (M:N):** 
    *   `Requests` ↔ `Resources` (реализована через таблицу `Requests_Resources`)

Все названия полей и типы данных (INT, NVARCHAR, TEXT, ENUM, DATE) указаны непосредственно на схеме.

---

## 4. CRUD для каждой сущности проекта

| Сущность | Create (Создание) | Read (Чтение) | Update (Обновление) | Delete (Удаление) |
| :--- | :--- | :--- | :--- | :--- |
| **Заявки (Requests)** | `POST /api/requests` | `GET /api/requests` | `PATCH /api/requests/{id}/status` | Удаление через БД |
| **Комментарии (Comments)** | `POST /api/requests/{id}/comments` | `GET /api/requests/{id}/comments` | `PUT /api/requests/{id}/comments/{cid}` | `DELETE /api/requests/{id}/comments/{cid}` |
| **Пользователи (Users)** | Добавление через БД/регистрацию | Получение профиля | Смена роли/данных | Удаление из БД |
| **Вложения (Attachments)** | `POST /api/requests/{id}/comments/attachments` | Получение метаданных | — *(файлы не редактируются)* | Удаление файла |

---

## 5. Список REST API Endpoints

| Метод | Endpoint | Описание |
| :--- | :--- | :--- |
| `GET` | `/api/requests` | Получить список заявок (фильтрация, сортировка, пагинация) |
| `PATCH` | `/api/requests/{id}/status` | Сменить статус заявки |
| `POST` | `/api/requests/{id}/assign` | Назначить исполнителя на заявку |
| `GET` | `/api/requests/{id}/history` | Получить историю изменений заявки |
| `GET` | `/api/requests/{requestId}/comments` | Получить комментарии заявки (JOIN с Users) |
| `POST` | `/api/requests/{requestId}/comments` | Добавить комментарий |
| `PUT` | `/api/requests/{requestId}/comments/{commentId}` | Изменить комментарий |
| `DELETE` | `/api/requests/{requestId}/comments/{commentId}` | Удалить комментарий |
| `POST` | `/api/requests/{requestId}/comments/attachments` | Загрузить файл (PDF, PNG, JPEG до 5 МБ) |

---

## 6. Язык клиентского приложения

Выбран **C#**. Клиентское приложение будет использовать `HttpClient` для отправки HTTP-запросов к нашему REST API и получения данных в формате JSON.

---  
