using System;
using System.Buffers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Nop.Core.Infrastructure;

namespace Nop.Plugin.ExternalAuth.Telegram.Infrastructure;

/// <summary>
/// Custom JSON output formatter that applies camelCase only for /api/telegram-auth routes
/// </summary>
public class TelegramApiCamelCaseJsonFormatter : NewtonsoftJsonOutputFormatter
{
    public TelegramApiCamelCaseJsonFormatter(JsonSerializerSettings serializerSettings, ArrayPool<char> charPool, MvcOptions mvcOptions)
        : base(serializerSettings, charPool, mvcOptions)
    {
    }

    public override bool CanWriteResult(OutputFormatterCanWriteContext context)
    {
        var path = context.HttpContext.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        return path.StartsWith("/api/telegram-auth");
    }
}

public class TelegramGatewayStartup : INopStartup
{
    public int Order => 1;

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Add custom camelCase formatter for /api/telegram-auth routes only
        // Use PostConfigure to add formatter after all other MVC configurations
        services.PostConfigure<MvcOptions>(options =>
        {
            // Insert our custom formatter at the beginning
            // It will only apply to /api/telegram-auth routes based on CanWriteResult check
            var camelCaseSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
                DateFormatHandling = DateFormatHandling.IsoDateFormat
            };
            
            options.OutputFormatters.Insert(0, new TelegramApiCamelCaseJsonFormatter(
                camelCaseSettings,
                ArrayPool<char>.Shared,
                options));
        });
    }

    public void Configure(IApplicationBuilder app)
    {
    }
}
