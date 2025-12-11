using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Plugin.Misc.DynamicProductImport.Services;

namespace Nop.Plugin.Misc.DynamicProductImport.Infrastructure;

public class DynamicImportDependencyRegistrar : INopStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IDynamicImportManager, DynamicImportManager>();
    }

    public void Configure(IApplicationBuilder application)
    {
    }

    public int Order => 10;
}

