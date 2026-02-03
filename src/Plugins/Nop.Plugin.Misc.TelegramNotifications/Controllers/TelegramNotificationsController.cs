using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Misc.TelegramNotifications.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Misc.TelegramNotifications.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
[AutoValidateAntiforgeryToken]
public class TelegramNotificationsController : BasePluginController
{
    private readonly ILocalizationService _localizationService;
    private readonly INotificationService _notificationService;
    private readonly IPermissionService _permissionService;
    private readonly ISettingService _settingService;
    private readonly IStoreContext _storeContext;

    public TelegramNotificationsController(
        ILocalizationService localizationService,
        INotificationService notificationService,
        IPermissionService permissionService,
        ISettingService settingService,
        IStoreContext storeContext)
    {
        _localizationService = localizationService;
        _notificationService = notificationService;
        _permissionService = permissionService;
        _settingService = settingService;
        _storeContext = storeContext;
    }

    public async Task<IActionResult> Configure()
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermission.Configuration.MANAGE_PLUGINS))
            return AccessDeniedView();

        var storeId = (await _storeContext.GetCurrentStoreAsync()).Id;
        var settings = await _settingService.LoadSettingAsync<TelegramNotificationsSettings>(storeId);

        var model = new ConfigurationModel
        {
            BotToken = settings.BotToken,
            Enabled = settings.Enabled,
            NotifyOnOrderPlaced = settings.NotifyOnOrderPlaced,
            NotifyOnOrderStatusChanged = settings.NotifyOnOrderStatusChanged,
            NotifyOnOrderPaid = settings.NotifyOnOrderPaid,
            NotifyOnOrderCancelled = settings.NotifyOnOrderCancelled,
            NotifyOnOrderCompleted = settings.NotifyOnOrderCompleted,
            NotifyOnShipmentSent = settings.NotifyOnShipmentSent,
            NotifyOnShipmentDelivered = settings.NotifyOnShipmentDelivered
        };

        return View("~/Plugins/Misc.TelegramNotifications/Views/Configure.cshtml", model);
    }

    [HttpPost]
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermission.Configuration.MANAGE_PLUGINS))
            return AccessDeniedView();

        if (!ModelState.IsValid)
            return await Configure();

        var storeId = (await _storeContext.GetCurrentStoreAsync()).Id;
        var settings = await _settingService.LoadSettingAsync<TelegramNotificationsSettings>(storeId);

        settings.BotToken = model.BotToken;
        settings.Enabled = model.Enabled;
        settings.NotifyOnOrderPlaced = model.NotifyOnOrderPlaced;
        settings.NotifyOnOrderStatusChanged = model.NotifyOnOrderStatusChanged;
        settings.NotifyOnOrderPaid = model.NotifyOnOrderPaid;
        settings.NotifyOnOrderCancelled = model.NotifyOnOrderCancelled;
        settings.NotifyOnOrderCompleted = model.NotifyOnOrderCompleted;
        settings.NotifyOnShipmentSent = model.NotifyOnShipmentSent;
        settings.NotifyOnShipmentDelivered = model.NotifyOnShipmentDelivered;

        await _settingService.SaveSettingAsync(settings, storeId);

        _notificationService.SuccessNotification(
            await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

        return await Configure();
    }
}
