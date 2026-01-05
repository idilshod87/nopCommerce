using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Plugin.Misc.Metrx.Services;

namespace Nop.Plugin.Misc.Metrx.Infrastructure;

/// <summary>
/// Registers Metrx plugin services with the nopCommerce dependency injection container
/// </summary>
public class PluginNopStartup : INopStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IVendorDeliveryDateService, VendorDeliveryDateService>();
        services.Configure<RazorViewEngineOptions>(options => options.ViewLocationExpanders.Add(new MetrxViewLocationExpander()));
    }

    public void Configure(IApplicationBuilder application)
    {
    }

    public int Order => 3000;
}
