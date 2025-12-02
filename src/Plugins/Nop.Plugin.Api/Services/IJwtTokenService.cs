using Nop.Core.Domain.Customers;
using Nop.Plugin.Api.Models.Authentication;

namespace Nop.Plugin.Api.Services;

public interface IJwtTokenService
{
    TokenResponse GenerateToken(Customer customer);
}
