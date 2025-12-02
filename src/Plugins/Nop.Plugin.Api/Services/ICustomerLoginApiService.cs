using Nop.Plugin.Api.Domain;

namespace Nop.Plugin.Api.Services;

public interface ICustomerLoginApiService
{
    Task<CustomerLogin> GetCustomerLoginAsync(string loginProvider, string providerKey);

    Task InsertCustomerLoginAsync(CustomerLogin login);
}
