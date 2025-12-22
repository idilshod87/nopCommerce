# Frontend REST API Design Guide

Audience: developers and AI agents adding or modifying endpoints in this plugin. Goal: keep contracts consistent with the existing public API surface under `/public-api/*`.

## Scope and base conventions
- Base path: `/public-api` (Swagger UI at `/public-api/swagger`).
- JSON only, camelCase output (formatter registered per route).
- Versioning: current doc assumes `v1` routes; add `/v2` in the path if breaking changes are introduced.
- Authentication: JWT Bearer where required; anonymous allowed where noted.

## Response envelope
- Основной контракт плагина — обёртка `ApiResponse<T>` с единственным верхнеуровневым свойством `data`. Большинство эндпоинтов (например, `customer/*`) продолжают использовать её.
- Пагинированные справочники стран/городов используют `PagedResultDto<T> : ApiResponse<T>` и возвращаются напрямую (без дополнительной обёртки). Фактический JSON выглядит так: `pageIndex`, `pageSize`, `totalCount`, `totalPages`, `hasPrevious`, `hasNext`, `data: [ ...items... ]` на одном уровне. При добавлении новых коллекций придерживайтесь этого формата, если хотите оставаться консистентными с `/common/countries` и `/common/cities`.
- На успехе: `200/201/202/204`. Для непагинированных ответов предпочтительно `ApiResponse<T>` с `data`. Для пагинированных — `PagedResultDto<TItem>` c полями выше.
- Ошибки: правильный HTTP-код и структурированное тело:
  ```json
  {
    "error": {
      "code": "not_found",
      "message": "Product not found",
      "details": null
    }
  }
  ```
  Если по техническим причинам обёртка недоступна, минимум `{ "message": "..." }`.

## Resource naming and routing
- Nouns, plural for collections: `/products`, `/vendors`, `/orders/{id}`.
- Nested only for strong containment: `/vendors/{id}/products`.
- Query parameters for filtering/pagination/sorting, not verbs in the path.

## HTTP methods
- GET: read-only, cacheable; no side effects.
- POST: create or execute a command; may return 201 with `Location` or 200/202.
- PUT: full replace of a resource.
- PATCH: partial update.
- DELETE: idempotent removal; return 204 or 200 with envelope.

## Pagination, filters, sorting
- Текущий стандарт для `/common/countries` и `/common/cities` — офсетная пагинация: `?page=1&pageSize=20` (страницы с 1). Ответ содержит метаданные на верхнем уровне и массив элементов в `data`:
  ```json
  {
    "pageIndex": 0,
    "pageSize": 20,
    "totalCount": 187,
    "totalPages": 10,
    "hasPrevious": false,
    "hasNext": true,
    "data": [ /* items */ ]
  }
  ```
- Если добавляете новые коллекции и хотите совместимость с существующими справочниками — используйте такой же формат. Если потребуется курсорная пагинация для больших выборок, добавьте новые поля `cursor/nextCursor` и опишите их в Swagger, не ломая текущую схему.
- Поиск/фильтры: простые query-параметры (`search`, `countryId`, и т.п.).
- Сортировка пока отсутствует; если вводите, используйте `?sort=field,-field2`.

## Including related data
- Tight relations can be inlined (e.g., product paymentMethods).
- Broader relations can go to an `included` object to avoid duplication across items.
  ```json
  {
    "data": [
      {
        "id": "prd_101",
        "name": "Red Sneakers",
        "vendorId": "vnd_55",
        "paymentMethods": [
          { "id": "pm_card", "name": "Card", "type": "card" }
        ]
      }
    ],
    "included": {
      "vendors": [
        { "id": "vnd_55", "name": "Sneaker Corp", "country": "US" }
      ]
    },
    "pageIndex": 0,
    "pageSize": 20,
    "totalCount": 187,
    "totalPages": 10,
    "hasPrevious": false,
    "hasNext": true
  }
  ```

## Request/response standards
- Headers: `Content-Type: application/json; charset=utf-8`, `Accept: application/json`, `Authorization: Bearer <token>` when required.
- Dates: ISO 8601 UTC (`2025-12-22T10:15:30Z`).
- Numbers: use numeric JSON types, not strings.
- File upload: use `[FromForm]` + `multipart/form-data`; Swagger filter already maps `IFormFile` to binary.

## Swagger / OpenAPI
- Document new endpoints in the `frontend-v1` Swagger document.
- OperationId should be method name (configured by Startup); keep names descriptive.
- Ensure request/response schemas reflect camelCase properties.

## Error codes (recommended set)
- `validation_failed`, `not_found`, `conflict`, `unauthorized`, `forbidden`, `rate_limited`, `internal_error`.
- Attach per-field details for validation where possible:
  ```json
  {
    "error": {
      "code": "validation_failed",
      "message": "One or more validation errors occurred.",
      "details": [
        { "field": "email", "code": "invalid_format", "message": "Invalid email" }
      ]
    }
  }
  ```

## Observability
- Accept `Request-Id`/`Traceparent` and return the same as `traceId` in responses when available (even if inside `data` for now).
- Log method, path, status, duration, and customer/vendor identifiers when applicable.

## Security
- Enforce JWT Bearer on protected routes; anonymous GETs allowed only when business rules permit.
- Validate inputs, especially identifiers and file uploads; apply size limits where relevant.
- Do not place secrets or PII in URLs; prefer headers/body.

## Testing expectations
- Add integration tests for new endpoints ensuring:
  - Envelope shape (`data` present).
  - Status codes for success and typical failures.
  - Pagination cursors/limits behave as documented.
- Update or add example requests/responses to the Docs folder alongside this guide.
