using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;

namespace Nop.Plugin.Sms.TelegramGateway.Infrastructure;

public class TelegramGatewayStartup : INopStartup
{
    public int Order => 1;

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void Configure(IApplicationBuilder app)
    {
    }
}
