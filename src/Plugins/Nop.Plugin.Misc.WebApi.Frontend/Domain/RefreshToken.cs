using Nop.Core;
using Nop.Core.Domain.Customers;

namespace Nop.Plugin.Misc.WebApi.Frontend.Domain;

/// <summary>
/// Represents a refresh token for JWT authentication
/// </summary>
public class RefreshToken : BaseEntity
{
    /// <summary>
    /// Gets or sets the refresh token value (unique identifier)
    /// </summary>
    public string Token { get; set; }

    /// <summary>
    /// Gets or sets the customer identifier
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// Gets or sets when the refresh token was created (UTC)
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets when the refresh token expires (UTC)
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>
    /// Gets or sets whether the refresh token has been revoked
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// Gets or sets when the refresh token was revoked (UTC)
    /// </summary>
    public DateTime? RevokedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the IP address that created this token
    /// </summary>
    public string CreatedByIp { get; set; }

    /// <summary>
    /// Gets or sets navigation property to customer
    /// </summary>
    public virtual Customer Customer { get; set; }

    /// <summary>
    /// Gets whether the refresh token is expired
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;

    /// <summary>
    /// Gets whether the refresh token is active (not revoked and not expired)
    /// </summary>
    public bool IsActive => !IsRevoked && !IsExpired;
}
