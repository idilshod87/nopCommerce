using Nop.Core;
using Nop.Services.Plugins;

namespace Nop.Plugin.ExternalAuth.Telegram;

public class TelegramGatewayPlugin : BasePlugin
{
    private readonly IWebHelper _webHelper;

    public TelegramGatewayPlugin(IWebHelper webHelper)
    {
        _webHelper = webHelper;
    }

    /// <summary>
    /// Gets a configuration page URL
    /// </summary>
    public override string GetConfigurationPageUrl()
    {
        return $"{_webHelper.GetStoreLocation()}Admin/TelegramGateway/Configure";
    }

    public override async Task InstallAsync()
    {
        await base.InstallAsync();
    }

    public override async Task UninstallAsync()
    {
        await base.UninstallAsync();
    }
}
