using System;

namespace Nop.Plugin.ExternalAuth.Telegram.Models;

/// <summary>
/// Response contract for starting Telegram authentication session.
/// </summary>
public record StartTelegramSessionResponse(
    bool Success,
    Guid SessionToken,
    string BotUrl,
    int CodeLength,
    int CodeTtlMinutes);

/// <summary>
/// Response contract for fetching Telegram authentication session state.
/// </summary>
public record GetTelegramSessionResponse(
    bool Success,
    string Status,
    string? PhoneNumber,
    DateTime? ExpiresAtUtc,
    DateTime? VerifiedAtUtc);

/// <summary>
/// JWT payload returned after successful Telegram session verification.
/// Mirrors common JWT result used across the system.
/// </summary>
public record TelegramJwtTokenDto(
    string AccessToken,
    string TokenType,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc,
    string? Username,
    int CustomerId,
    Guid CustomerGuid);

/// <summary>
/// Response contract for verifying Telegram authentication session.
/// </summary>
public record VerifyTelegramSessionResponse(
    bool Success,
    TelegramJwtTokenDto Token,
    bool IsNewCustomer);

/// <summary>
/// Generic error contract for Telegram auth API endpoints.
/// </summary>
public record TelegramErrorResponse(
    bool Success,
    string Error);


