using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Data;
using Nop.Plugin.Misc.WebApi.Frontend.Domain;
using Nop.Services.Customers;
using System.Security.Cryptography;

namespace Nop.Plugin.Misc.WebApi.Frontend.Services;

/// <summary>
/// Default implementation of refresh token service
/// </summary>
public class RefreshTokenService : IRefreshTokenService
{
    #region Fields

    private readonly IRepository<RefreshToken> _refreshTokenRepository;
    private readonly ICustomerService _customerService;

    // Refresh token validity period in days (default: 7 days)
    private const int REFRESH_TOKEN_EXPIRY_DAYS = 7;

    #endregion

    #region Ctor

    public RefreshTokenService(
        IRepository<RefreshToken> refreshTokenRepository,
        ICustomerService customerService)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _customerService = customerService;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Generate a new refresh token for the specified customer
    /// </summary>
    public virtual async Task<RefreshToken> GenerateRefreshTokenAsync(Customer customer, string ipAddress)
    {
        // Generate a cryptographically secure random token
        var tokenBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(tokenBytes);
        var token = Convert.ToBase64String(tokenBytes);

        var refreshToken = new RefreshToken
        {
            Token = token,
            CustomerId = customer.Id,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(REFRESH_TOKEN_EXPIRY_DAYS),
            IsRevoked = false,
            CreatedByIp = ipAddress
        };

        await _refreshTokenRepository.InsertAsync(refreshToken);

        return refreshToken;
    }

    /// <summary>
    /// Validate a refresh token and return the associated customer
    /// </summary>
    public virtual async Task<Customer> ValidateRefreshTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var refreshToken = await GetRefreshTokenAsync(token);

        if (refreshToken == null || !refreshToken.IsActive)
            return null;

        var customer = await _customerService.GetCustomerByIdAsync(refreshToken.CustomerId);

        return customer;
    }

    /// <summary>
    /// Revoke a refresh token
    /// </summary>
    public virtual async Task<bool> RevokeRefreshTokenAsync(string token, string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var refreshToken = await GetRefreshTokenAsync(token);

        if (refreshToken == null || !refreshToken.IsActive)
            return false;

        refreshToken.IsRevoked = true;
        refreshToken.RevokedAtUtc = DateTime.UtcNow;

        await _refreshTokenRepository.UpdateAsync(refreshToken);

        return true;
    }

    /// <summary>
    /// Revoke all refresh tokens for a customer
    /// </summary>
    public virtual async Task<int> RevokeAllCustomerTokensAsync(int customerId, string ipAddress)
    {
        var tokens = _refreshTokenRepository.Table
            .Where(rt => rt.CustomerId == customerId && rt.IsActive)
            .ToList();

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAtUtc = DateTime.UtcNow;
        }

        await _refreshTokenRepository.UpdateAsync(tokens);

        return tokens.Count;
    }

    /// <summary>
    /// Remove expired refresh tokens
    /// </summary>
    public virtual async Task<int> RemoveExpiredTokensAsync()
    {
        var expiredTokens = _refreshTokenRepository.Table
            .Where(rt => rt.ExpiresAtUtc < DateTime.UtcNow)
            .ToList();

        await _refreshTokenRepository.DeleteAsync(expiredTokens);

        return expiredTokens.Count;
    }

    /// <summary>
    /// Get refresh token by token string
    /// </summary>
    public virtual async Task<RefreshToken> GetRefreshTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var refreshToken = await _refreshTokenRepository.Table
            .FirstOrDefaultAsync(rt => rt.Token == token);

        return refreshToken;
    }

    #endregion
}
