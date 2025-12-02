using Microsoft.IdentityModel.Tokens;

using Nop.Core.Configuration;
using Nop.Core.Domain.Customers;
using Nop.Core.Infrastructure;
using Nop.Plugin.Api.Configuration;
using Nop.Plugin.Api.Domain;
using Nop.Plugin.Api.Infrastructure;
using Nop.Plugin.Api.Models.Authentication;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Nop.Plugin.Api.Services;

public record JwtTokenService(CustomerSettings customerSettings, ApiSettings apiSettings) : IJwtTokenService
{
    public TokenResponse GenerateToken(Customer customer)
    {
        var currentTime = DateTimeOffset.Now;
        var expirationTime = currentTime.AddDays(GetTokenExpiryInDays());

        var claims = new List<Claim>
             {
               new(JwtRegisteredClaimNames.Nbf, currentTime.ToUnixTimeSeconds().ToString()),
               new(JwtRegisteredClaimNames.Exp, expirationTime.ToUnixTimeSeconds().ToString()),
               new("CustomerId", customer.Id.ToString()),
               new(ClaimTypes.NameIdentifier, customer.CustomerGuid.ToString()),
             };

        if (!string.IsNullOrEmpty(customer.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, customer.Email));
        }

        if (customerSettings.UsernamesEnabled)
        {
            if (!string.IsNullOrEmpty(customer.Username))
            {
                claims.Add(new Claim(ClaimTypes.Name, customer.Username));
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(customer.Email))
            {
                claims.Add(new Claim(ClaimTypes.Name, customer.Email));
            }
        }
        var apiConfiguration = Singleton<AppSettings>.Instance.Get<ApiConfiguration>();
        var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(apiConfiguration.SecurityKey)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(new JwtHeader(signingCredentials), new JwtPayload(claims));
        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new TokenResponse(accessToken, currentTime.UtcDateTime, expirationTime.UtcDateTime)
        {
            CustomerId = customer.Id,
            CustomerGuid = customer.CustomerGuid,
            Username = customerSettings.UsernamesEnabled ? customer.Username : customer.Email,
            TokenType = "Bearer",
        };
    }

    private int GetTokenExpiryInDays()
    {
        return apiSettings.TokenExpiryInDays <= 0
               ? Constants.Configurations.DefaultAccessTokenExpirationInDays
               : apiSettings.TokenExpiryInDays;
    }
}
