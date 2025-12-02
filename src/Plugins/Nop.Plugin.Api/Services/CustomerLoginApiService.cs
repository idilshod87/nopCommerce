using Nop.Data;
using Nop.Plugin.Api.Domain;

namespace Nop.Plugin.Api.Services;

internal record CustomerLoginApiService(IRepository<CustomerLogin> repository) : ICustomerLoginApiService
{
    public async Task<CustomerLogin> GetCustomerLoginAsync(string loginProvider, string providerKey)
    {
        return await repository.Table
                .FirstOrDefaultAsync(x => x.LoginProvider == loginProvider && x.ProviderKey == providerKey);
    }

    public async Task InsertCustomerLoginAsync(CustomerLogin login)
    {
        await repository.InsertAsync(login);
    }
}
