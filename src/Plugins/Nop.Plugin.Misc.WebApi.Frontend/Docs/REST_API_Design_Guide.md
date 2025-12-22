# Frontend REST API Design Guide

Audience: developers and AI agents adding or modifying endpoints in this plugin. Goal: keep contracts consistent with the existing public API surface under `/public-api/*`.

## Scope and base conventions
- Base path: `/public-api` (Swagger UI at `/public-api/swagger`).
- JSON only, camelCase output (formatter registered per route).
- Versioning: current doc assumes `v1` routes; add `/v2` in the path if breaking changes are introduced.
- Authentication: JWT Bearer where required; anonymous allowed where noted.

## Response envelope
- Use the existing `ApiResponse<T>` wrapper: top-level property `data` only.
- On success: `200/201/202/204` with `ApiResponse<T>` where `data` holds the payload (for 204 may return `data: null`).
- On errors: return proper HTTP status code with a consistent body. Prefer a structured error:
  ```json
  {
    "error": {
      "code": "not_found",
      "message": "Product not found",
      "details": null
    }
  }
  ```
  If you cannot use the envelope, at minimum return `{ "message": "..." }` as current controllers do.

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
- Prefer cursor pagination: `?limit=50&cursor=abc`. Response should include cursor metadata inside `data`:
  ```json
  {
    "data": {
      "items": [ /* ... */ ],
      "pagination": {
        "limit": 50,
        "cursor": "abc",
        "nextCursor": "def",
        "hasNext": true,
        "total": null
      }
    }
  }
  ```
- Offset pagination if required: `?limit=50&offset=0`; include `total` when feasible.
- Sorting: `?sort=name,-price`.
- Filters: `?filter[field]=value` or simple query params when narrow in scope.

## Including related data
- Tight relations can be inlined (e.g., product paymentMethods).
- Broader relations can go to an `included` object to avoid duplication across items.
  ```json
  {
    "data": {
      "items": [
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
      "pagination": { "limit": 50, "cursor": "abc", "nextCursor": "def", "hasNext": true }
    }
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
