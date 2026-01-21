using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Payments.CardTransfer.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Payments.CardTransfer.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
[AutoValidateAntiforgeryToken]
public class PaymentCardTransferController : BasePaymentController
{
    #region Fields

    protected readonly ILanguageService _languageService;
    protected readonly ILocalizationService _localizationService;
    protected readonly INotificationService _notificationService;
    protected readonly IPermissionService _permissionService;
    protected readonly ISettingService _settingService;
    protected readonly IStoreContext _storeContext;

    #endregion

    #region Ctor

    public PaymentCardTransferController(ILanguageService languageService,
        ILocalizationService localizationService,
        INotificationService notificationService,
        IPermissionService permissionService,
        ISettingService settingService,
        IStoreContext storeContext)
    {
        _languageService = languageService;
        _localizationService = localizationService;
        _notificationService = notificationService;
        _permissionService = permissionService;
        _settingService = settingService;
        _storeContext = storeContext;
    }

    #endregion

    #region Methods

    [CheckPermission(StandardPermission.Configuration.MANAGE_PAYMENT_METHODS)]
    public async Task<IActionResult> Configure()
    {
        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
        var cardTransferPaymentSettings = await _settingService.LoadSettingAsync<CardTransferPaymentSettings>(storeScope);

        var model = new ConfigurationModel
        {
            DescriptionText = cardTransferPaymentSettings.DescriptionText
        };

        await AddLocalesAsync(_languageService, model.Locales, async (locale, languageId) =>
        {
            locale.DescriptionText = await _localizationService
                .GetLocalizedSettingAsync(cardTransferPaymentSettings, x => x.DescriptionText, languageId, 0, false, false);
        });

        model.AdditionalFee = cardTransferPaymentSettings.AdditionalFee;
        model.AdditionalFeePercentage = cardTransferPaymentSettings.AdditionalFeePercentage;
        model.ShippableProductRequired = cardTransferPaymentSettings.ShippableProductRequired;

        model.ActiveStoreScopeConfiguration = storeScope;
        if (storeScope > 0)
        {
            model.DescriptionText_OverrideForStore = await _settingService.SettingExistsAsync(cardTransferPaymentSettings, x => x.DescriptionText, storeScope);
            model.AdditionalFee_OverrideForStore = await _settingService.SettingExistsAsync(cardTransferPaymentSettings, x => x.AdditionalFee, storeScope);
            model.AdditionalFeePercentage_OverrideForStore = await _settingService.SettingExistsAsync(cardTransferPaymentSettings, x => x.AdditionalFeePercentage, storeScope);
            model.ShippableProductRequired_OverrideForStore = await _settingService.SettingExistsAsync(cardTransferPaymentSettings, x => x.ShippableProductRequired, storeScope);
        }

        return View("~/Plugins/Payments.CardTransfer/Views/Configure.cshtml", model);
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Configuration.MANAGE_PAYMENT_METHODS)]
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        if (!ModelState.IsValid)
            return await Configure();

        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
        var cardTransferPaymentSettings = await _settingService.LoadSettingAsync<CardTransferPaymentSettings>(storeScope);

        cardTransferPaymentSettings.DescriptionText = model.DescriptionText;
        cardTransferPaymentSettings.AdditionalFee = model.AdditionalFee;
        cardTransferPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;
        cardTransferPaymentSettings.ShippableProductRequired = model.ShippableProductRequired;

        await _settingService.SaveSettingOverridablePerStoreAsync(cardTransferPaymentSettings, x => x.DescriptionText, model.DescriptionText_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(cardTransferPaymentSettings, x => x.AdditionalFee, model.AdditionalFee_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(cardTransferPaymentSettings, x => x.AdditionalFeePercentage, model.AdditionalFeePercentage_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(cardTransferPaymentSettings, x => x.ShippableProductRequired, model.ShippableProductRequired_OverrideForStore, storeScope, false);

        await _settingService.ClearCacheAsync();

        foreach (var localized in model.Locales)
        {
            await _localizationService.SaveLocalizedSettingAsync(cardTransferPaymentSettings,
                x => x.DescriptionText, localized.LanguageId, localized.DescriptionText);
        }

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

        return await Configure();
    }

    #endregion
}
