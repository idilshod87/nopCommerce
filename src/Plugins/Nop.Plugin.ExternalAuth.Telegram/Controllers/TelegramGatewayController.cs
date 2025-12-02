using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.ExternalAuth.Telegram.Configuration;
using Nop.Plugin.ExternalAuth.Telegram.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.ExternalAuth.Telegram.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
[AutoValidateAntiforgeryToken]
public class TelegramGatewayController : BasePluginController
{
    private readonly TelegramGatewayConfiguration _telegramGatewaySettings;
    private readonly ILocalizationService _localizationService;
    private readonly INotificationService _notificationService;
    private readonly ISettingService _settingService;

    public TelegramGatewayController(
        TelegramGatewayConfiguration telegramGatewaySettings,
        ILocalizationService localizationService,
        INotificationService notificationService,
        ISettingService settingService)
    {
        _telegramGatewaySettings = telegramGatewaySettings;
        _localizationService = localizationService;
        _notificationService = notificationService;
        _settingService = settingService;
    }

    [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
    public IActionResult Configure()
    {
        var model = new ConfigurationModel
        {
            BotToken = _telegramGatewaySettings.BotToken,
            BotUsername = _telegramGatewaySettings.BotUsername,
            WebhookSecretToken = _telegramGatewaySettings.WebhookSecretToken,
            SessionTtlMinutes = _telegramGatewaySettings.SessionTtlMinutes,
            VerificationCodeLength = _telegramGatewaySettings.VerificationCodeLength,
            VerificationCodeTtlMinutes = _telegramGatewaySettings.VerificationCodeTtlMinutes
        };

        return View("~/Plugins/ExternalAuth.Telegram/Views/Configure.cshtml", model);
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        _telegramGatewaySettings.BotToken = model.BotToken;
        _telegramGatewaySettings.BotUsername = model.BotUsername;
        _telegramGatewaySettings.WebhookSecretToken = model.WebhookSecretToken;
        _telegramGatewaySettings.SessionTtlMinutes = model.SessionTtlMinutes;
        _telegramGatewaySettings.VerificationCodeLength = model.VerificationCodeLength;
        _telegramGatewaySettings.VerificationCodeTtlMinutes = model.VerificationCodeTtlMinutes;

        await _settingService.SaveSettingAsync(_telegramGatewaySettings);

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

        return Configure();
    }
}

