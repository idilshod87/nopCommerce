# AuthController - Authentication API

## Overview

AuthController provides JWT-based authentication endpoints for the nopCommerce Frontend API. It handles user login, guest authentication, token validation, and token refresh functionality.

## Base Path

`/public-api/auth`

## Endpoints

### 1. Request Token (Login)

**Endpoint:** `POST /public-api/auth/token`

**Description:** Authenticates a user or creates a guest session, returning access and refresh tokens.

**Request Body:**
```json
{
  "username": "admin@example.com",
  "password": "yourpassword",
  "remember_me": false,
  "guest": false
}
```

**Request Parameters:**
- `username` (string, optional): Username or email (required if not guest)
- `password` (string, optional): User password (required if not guest)
- `remember_me` (boolean, optional): Whether to remember the user (default: false)
- `guest` (boolean, optional): Whether to create a guest session (default: false)

**Response (200 OK):**
```json
{
  "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "token_type": "Bearer",
  "refresh_token": "Base64EncodedRefreshTokenString...",
  "expires_in": 86400,
  "created_at_utc": "2026-01-27T12:00:00Z",
  "expires_at_utc": "2026-01-28T12:00:00Z",
  "username": "admin@example.com",
  "customer_id": 1,
  "customer_guid": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

**Error Responses:**
- `400 Bad Request`: Missing username or password
- `403 Forbidden`: Wrong username or password

### 2. Validate Token

**Endpoint:** `GET /public-api/auth/token/check`

**Description:** Validates the current JWT access token.

**Headers:**
```
Authorization: Bearer <access_token>
```

**Response (200 OK):**
```json
"OK"
```

**Error Responses:**
- `401 Unauthorized`: Invalid or expired token
- `404 Not Found`: Customer not found

### 3. Refresh Token

**Endpoint:** `POST /public-api/auth/token/refresh`

**Description:** Refreshes an expired access token using a valid refresh token.

**Request Body:**
```json
{
  "refresh_token": "Base64EncodedRefreshTokenString..."
}
```

**Response (200 OK):**
```json
{
  "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "token_type": "Bearer",
  "refresh_token": "NewBase64EncodedRefreshTokenString...",
  "expires_in": 86400,
  "created_at_utc": "2026-01-27T13:00:00Z",
  "expires_at_utc": "2026-01-28T13:00:00Z",
  "username": "admin@example.com",
  "customer_id": 1,
  "customer_guid": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

**Error Responses:**
- `400 Bad Request`: Refresh token is required
- `401 Unauthorized`: Invalid or expired refresh token

## Usage Examples

### Login Example

```bash
curl -X POST "https://yourstore.com/public-api/auth/token" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin@example.com",
    "password": "yourpassword",
    "remember_me": false
  }'
```

### Guest Login Example

```bash
curl -X POST "https://yourstore.com/public-api/auth/token" \
  -H "Content-Type: application/json" \
  -d '{
    "guest": true
  }'
```

### Validate Token Example

```bash
curl -X GET "https://yourstore.com/public-api/auth/token/check" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

### Refresh Token Example

```bash
curl -X POST "https://yourstore.com/public-api/auth/token/refresh" \
  -H "Content-Type: application/json" \
  -d '{
    "refresh_token": "Base64EncodedRefreshTokenString..."
  }'
```

## Features

- **JWT-based Authentication**: Secure token-based authentication
- **Refresh Token Support**: Long-lived refresh tokens (7 days) for seamless token renewal
- **Guest Sessions**: Support for anonymous shopping
- **Shopping Cart Migration**: Automatic cart migration when switching from guest to authenticated user
- **Activity Logging**: All authentication events are logged
- **IP Tracking**: Refresh tokens track IP addresses for security

## Token Lifecycle

1. **Access Token**: Short-lived (configurable, default varies)
2. **Refresh Token**: Long-lived (7 days)
3. **Token Rotation**: When refreshing, old refresh token is revoked and new one is issued

## Security Considerations

- Access tokens should be stored securely (e.g., in memory or secure storage)
- Refresh tokens should be stored even more securely (e.g., HttpOnly cookies or secure local storage)
- Always use HTTPS in production
- Refresh tokens are automatically revoked when used
- All refresh tokens can be revoked for a user if needed

## Dependencies

This controller requires the following services to be registered:
- `ICustomerService` - Customer management
- `ICustomerRegistrationService` - User authentication
- `ICustomerActivityService` - Activity logging
- `IShoppingCartService` - Shopping cart operations
- `IAuthenticationService` - Authentication services
- `IJwtTokenService` - JWT token generation
- `IRefreshTokenService` - Refresh token management
- `CustomerSettings` - Customer configuration

All dependencies are automatically registered via dependency injection.
