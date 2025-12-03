using System;
using System.Text.Json.Serialization;

namespace Nop.Plugin.ExternalAuth.Telegram.Models;

/// <summary>
/// Response contract for starting Telegram authentication session.
/// </summary>
public class StartTelegramSessionResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("sessionToken")]
    public Guid SessionToken { get; init; }

    [JsonPropertyName("botUrl")]
    public string BotUrl { get; init; } = string.Empty;

    [JsonPropertyName("codeLength")]
    public int CodeLength { get; init; }

    [JsonPropertyName("codeTtlMinutes")]
    public int CodeTtlMinutes { get; init; }
}

/// <summary>
/// Response contract for fetching Telegram authentication session state.
/// </summary>
public class GetTelegramSessionResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("phoneNumber")]
    public string? PhoneNumber { get; init; }

    [JsonPropertyName("expiresAtUtc")]
    public DateTime? ExpiresAtUtc { get; init; }

    [JsonPropertyName("verifiedAtUtc")]
    public DateTime? VerifiedAtUtc { get; init; }
}

/// <summary>
/// JWT payload returned after successful Telegram session verification.
/// Mirrors common JWT result used across the system.
/// </summary>
public class TelegramJwtTokenDto
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; init; } = string.Empty;

    [JsonPropertyName("tokenType")]
    public string TokenType { get; init; } = "Bearer";

    [JsonPropertyName("createdAtUtc")]
    public DateTime CreatedAtUtc { get; init; }

    [JsonPropertyName("expiresAtUtc")]
    public DateTime ExpiresAtUtc { get; init; }

    [JsonPropertyName("username")]
    public string? Username { get; init; }

    [JsonPropertyName("customerId")]
    public int CustomerId { get; init; }

    [JsonPropertyName("customerGuid")]
    public Guid CustomerGuid { get; init; }
}

/// <summary>
/// Response contract for verifying Telegram authentication session.
/// </summary>
public class VerifyTelegramSessionResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("token")]
    public TelegramJwtTokenDto Token { get; init; } = null!;

    [JsonPropertyName("isNewCustomer")]
    public bool IsNewCustomer { get; init; }
}

/// <summary>
/// Generic error contract for Telegram auth API endpoints.
/// </summary>
public class TelegramErrorResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("error")]
    public string Error { get; init; } = string.Empty;

    public TelegramErrorResponse(bool success, string error)
    {
        Success = success;
        Error = error;
    }
}


