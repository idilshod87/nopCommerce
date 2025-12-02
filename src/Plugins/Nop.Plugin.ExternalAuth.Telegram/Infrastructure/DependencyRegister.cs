using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Plugin.ExternalAuth.Telegram.Services;

namespace Nop.Plugin.ExternalAuth.Telegram.Infrastructure;

public class DependencyRegister : INopStartup
{
    public int Order => 3000;

    public void Configure(IApplicationBuilder application)
    {
    }

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ITelegramGatewayApiService, TelegramGatewayApiService>();
        services.AddScoped<ITelegramAuthService, TelegramAuthService>();
        services.AddScoped<ITelegramBotMessenger, TelegramBotMessenger>();

        // register Telegram bot authenticator and typed http clients
        services.AddHttpClient("telegram_gateway_client", client =>
        {
            client.BaseAddress = new Uri("https://api.telegram.org/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddHttpClient("telegram_bot_api", client =>
        {
            client.BaseAddress = new Uri("https://api.telegram.org/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddScoped<ITelegramBotAuthenticator, TelegramBotAuthenticator>();
    }
}
