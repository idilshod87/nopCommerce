using Nop.Core;
using Nop.Services.Localization;
using Nop.Services.Plugins;

namespace Nop.Plugin.Misc.TelegramNotifications;

/// <summary>
/// Telegram Notifications plugin
/// </summary>
public class TelegramNotificationsPlugin : BasePlugin
{
    private readonly ILocalizationService _localizationService;
    private readonly IWebHelper _webHelper;

    public TelegramNotificationsPlugin(
        ILocalizationService localizationService,
        IWebHelper webHelper)
    {
        _localizationService = localizationService;
        _webHelper = webHelper;
    }

    /// <summary>
    /// Gets a configuration page URL
    /// </summary>
    public override string GetConfigurationPageUrl()
    {
        return $"{_webHelper.GetStoreLocation()}Admin/TelegramNotifications/Configure";
    }

    /// <summary>
    /// Install plugin
    /// </summary>
    public override async Task InstallAsync()
    {
        // Install localization resources
        await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
        {
            ["Plugins.Misc.TelegramNotifications.Fields.BotToken"] = "Bot Token",
            ["Plugins.Misc.TelegramNotifications.Fields.BotToken.Hint"] = "Enter your Telegram bot token (get it from @BotFather)",
            ["Plugins.Misc.TelegramNotifications.Fields.ChatId"] = "Chat ID",
            ["Plugins.Misc.TelegramNotifications.Fields.ChatId.Hint"] = "Enter the chat ID where notifications will be sent",
            ["Plugins.Misc.TelegramNotifications.Fields.Enabled"] = "Enabled",
            ["Plugins.Misc.TelegramNotifications.Fields.NotifyOnOrderPlaced"] = "Notify on order placed",
            ["Plugins.Misc.TelegramNotifications.Fields.NotifyOnOrderStatusChanged"] = "Notify on order status changed",
            ["Plugins.Misc.TelegramNotifications.Fields.NotifyOnOrderPaid"] = "Notify on order paid",
            ["Plugins.Misc.TelegramNotifications.Fields.NotifyOnOrderCancelled"] = "Notify on order cancelled",
            ["Plugins.Misc.TelegramNotifications.Fields.NotifyOnOrderCompleted"] = "Notify on order completed",
            ["Plugins.Misc.TelegramNotifications.NotificationSettings"] = "Notification Settings"
        });

        await base.InstallAsync();
    }

    /// <summary>
    /// Uninstall plugin
    /// </summary>
    public override async Task UninstallAsync()
    {
        // Delete localization resources
        await _localizationService.DeleteLocaleResourcesAsync("Plugins.Misc.TelegramNotifications");

        await base.UninstallAsync();
    }
}
