# Nop.Plugin.Misc.WebApi.Frontend - Refresh Token Implementation

## Overview

This plugin now includes a complete refresh token implementation for JWT-based authentication, allowing secure token renewal without requiring users to re-enter credentials.

## Components

### Domain Models

#### RefreshToken
Located in `Domain/RefreshToken.cs`

Represents a refresh token entity stored in the database:
- `Token` - Unique cryptographically secure token string
- `CustomerId` - Associated customer ID
- `CreatedAtUtc` - Token creation timestamp
- `ExpiresAtUtc` - Token expiration timestamp (7 days default)
- `IsRevoked` - Whether token has been revoked
- `RevokedAtUtc` - When token was revoked
- `CreatedByIp` - IP address that requested the token
- `IsExpired` - Computed property checking if token is expired
- `IsActive` - Computed property checking if token is valid (not revoked and not expired)

### Data Layer

#### RefreshTokenSchemaMigration
Located in `Data/RefreshTokenSchemaMigration.cs`

FluentMigrator migration that creates:
- `RefreshToken` table with all necessary columns
- Unique index on `Token` column
- Index on `CustomerId` column
- Index on `ExpiresAtUtc` column for cleanup operations

### Models

#### TokenRequest
Located in `Models/Authentication/TokenRequest.cs`

Request model for initial authentication:
```json
{
  "username": "user@example.com",
  "password": "password",
  "remember_me": false,
  "guest": false
}
```

#### TokenResponse
Located in `Models/Authentication/TokenResponse.cs`

Response model containing both access and refresh tokens:
```json
{
  "access_token": "eyJ...",
  "token_type": "Bearer",
  "refresh_token": "Base64String...",
  "expires_in": 86400,
  "created_at_utc": "2026-01-27T12:00:00Z",
  "expires_at_utc": "2026-01-28T12:00:00Z",
  "username": "user@example.com",
  "customer_id": 1,
  "customer_guid": "guid-here"
}
```

#### RefreshTokenRequest
Located in `Models/Authentication/RefreshTokenRequest.cs`

Request model for token refresh:
```json
{
  "refresh_token": "Base64EncodedTokenString..."
}
```

### Services

#### IRefreshTokenService
Located in `Services/IRefreshTokenService.cs`

Interface defining refresh token operations:
- `GenerateRefreshTokenAsync` - Creates new refresh token
- `ValidateRefreshTokenAsync` - Validates token and returns customer
- `RevokeRefreshTokenAsync` - Revokes specific token
- `RevokeAllCustomerTokensAsync` - Revokes all tokens for a customer
- `RemoveExpiredTokensAsync` - Cleanup expired tokens
- `GetRefreshTokenAsync` - Retrieves token by string

#### RefreshTokenService
Located in `Services/RefreshTokenService.cs`

Default implementation with:
- Cryptographically secure token generation (64 bytes random)
- 7-day expiration period (configurable via constant)
- Automatic token validation
- IP address tracking
- Batch operations for cleanup

### Controllers

#### AuthController
Located in `Controllers/AuthController.cs`

Endpoints:
- `POST /public-api/auth/token` - Initial login/guest authentication
- `GET /public-api/auth/token/check` - Validate current access token
- `POST /public-api/auth/token/refresh` - Refresh access token

## Registration

Services are registered in `Infrastructure/FrontendApiStartup.cs`:

```csharp
services.AddScoped<Services.IRefreshTokenService, Services.RefreshTokenService>();
```

## Security Features

1. **Cryptographically Secure Tokens**: Uses `RandomNumberGenerator` for token generation
2. **Token Rotation**: Old refresh token is revoked when used
3. **IP Tracking**: Records IP address for audit purposes
4. **Expiration Management**: Automatic expiration after 7 days
5. **Revocation Support**: Individual or bulk token revocation
6. **Single Use**: Refresh tokens can only be used once

## Usage Flow

1. **Initial Login**
   - Client sends credentials to `/public-api/auth/token`
   - Server returns access token + refresh token
   - Client stores both tokens securely

2. **API Requests**
   - Client uses access token in Authorization header
   - `Authorization: Bearer {access_token}`

3. **Token Refresh**
   - When access token expires, client sends refresh token to `/public-api/auth/token/refresh`
   - Server validates refresh token, revokes it, and issues new pair
   - Client updates stored tokens

4. **Token Revocation**
   - Refresh tokens are revoked when used
   - Can be revoked manually if needed
   - Expired tokens can be cleaned up periodically

## Best Practices

1. **Storage**
   - Store access tokens in memory (short-lived)
   - Store refresh tokens in secure storage (HttpOnly cookies recommended)
   - Never expose refresh tokens in URLs or logs

2. **Transport**
   - Always use HTTPS in production
   - Implement rate limiting on auth endpoints
   - Monitor for suspicious token usage patterns

3. **Maintenance**
   - Periodically call `RemoveExpiredTokensAsync` to clean up database
   - Consider implementing scheduled task for cleanup
   - Monitor token usage for security auditing

## Configuration

Default refresh token expiration is 7 days. To change:

Edit `RefreshTokenService.cs`:
```csharp
private const int REFRESH_TOKEN_EXPIRY_DAYS = 7; // Change this value
```

## Database Schema

Table: `RefreshToken`
- `Id` (int, PK)
- `Token` (nvarchar, unique index)
- `CustomerId` (int, index, FK to Customer)
- `CreatedAtUtc` (datetime)
- `ExpiresAtUtc` (datetime, index)
- `IsRevoked` (bit)
- `RevokedAtUtc` (datetime, nullable)
- `CreatedByIp` (nvarchar, nullable)

## Migration Notes

This implementation is self-contained within the Nop.Plugin.Misc.WebApi.Frontend plugin and does not depend on Nop.Plugin.Api for refresh token functionality.

All models, services, and data migrations are local to this plugin, making it easier to:
- Deploy independently
- Customize behavior
- Maintain separate versioning
- Avoid cross-plugin dependencies

## See Also

- [AuthController Documentation](AuthController.md)
- [API Authentication Guide](../Docs/AuthController.md)
