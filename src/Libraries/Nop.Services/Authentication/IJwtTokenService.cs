using System;
using Nop.Core.Domain.Customers;

namespace Nop.Services.Authentication;

/// <summary>
/// Represents result of JWT access token generation.
/// </summary>
public class JwtTokenResult
{
    /// <summary>
    /// Gets or sets generated access token.
    /// </summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets token creation moment (UTC).
    /// </summary>
    public DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// Gets or sets token expiration moment (UTC).
    /// </summary>
    public DateTime ExpiresAtUtc { get; init; }

    /// <summary>
    /// Gets or sets associated username (or email, depending on settings).
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// Gets or sets customer identifier.
    /// </summary>
    public int CustomerId { get; init; }

    /// <summary>
    /// Gets or sets customer GUID.
    /// </summary>
    public Guid CustomerGuid { get; init; }
}

/// <summary>
/// Service that generates JWT access tokens for customers.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generate access token for the specified customer.
    /// </summary>
    /// <param name="customer">Customer instance.</param>
    /// <returns>Token generation result.</returns>
    JwtTokenResult GenerateToken(Customer customer);
}


