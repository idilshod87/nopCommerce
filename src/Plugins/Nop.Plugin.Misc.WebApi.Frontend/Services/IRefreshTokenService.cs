using Nop.Core.Domain.Customers;
using Nop.Plugin.Misc.WebApi.Frontend.Domain;

namespace Nop.Plugin.Misc.WebApi.Frontend.Services;

/// <summary>
/// Service for managing refresh tokens
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>
    /// Generate a new refresh token for the specified customer
    /// </summary>
    /// <param name="customer">Customer entity</param>
    /// <param name="ipAddress">IP address that requested the token</param>
    /// <returns>Generated refresh token entity</returns>
    Task<RefreshToken> GenerateRefreshTokenAsync(Customer customer, string ipAddress);

    /// <summary>
    /// Validate a refresh token and return the associated customer
    /// </summary>
    /// <param name="token">Refresh token string</param>
    /// <returns>Customer entity if token is valid, null otherwise</returns>
    Task<Customer> ValidateRefreshTokenAsync(string token);

    /// <summary>
    /// Revoke a refresh token
    /// </summary>
    /// <param name="token">Refresh token string</param>
    /// <param name="ipAddress">IP address that revoked the token</param>
    /// <returns>True if revoked successfully, false otherwise</returns>
    Task<bool> RevokeRefreshTokenAsync(string token, string ipAddress);

    /// <summary>
    /// Revoke all refresh tokens for a customer
    /// </summary>
    /// <param name="customerId">Customer identifier</param>
    /// <param name="ipAddress">IP address that revoked the tokens</param>
    /// <returns>Number of tokens revoked</returns>
    Task<int> RevokeAllCustomerTokensAsync(int customerId, string ipAddress);

    /// <summary>
    /// Remove expired refresh tokens
    /// </summary>
    /// <returns>Number of tokens removed</returns>
    Task<int> RemoveExpiredTokensAsync();

    /// <summary>
    /// Get refresh token by token string
    /// </summary>
    /// <param name="token">Token string</param>
    /// <returns>RefreshToken entity or null</returns>
    Task<RefreshToken> GetRefreshTokenAsync(string token);
}
