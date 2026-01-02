using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Payments.BankTransfer.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Payments.BankTransfer.Controllers;

[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
[AutoValidateAntiforgeryToken]
public class PaymentBankTransferController : BasePaymentController
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

    public PaymentBankTransferController(ILanguageService languageService,
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
        var bankTransferPaymentSettings = await _settingService.LoadSettingAsync<BankTransferPaymentSettings>(storeScope);

        var model = new ConfigurationModel
        {
            DescriptionText = bankTransferPaymentSettings.DescriptionText
        };

        await AddLocalesAsync(_languageService, model.Locales, async (locale, languageId) =>
        {
            locale.DescriptionText = await _localizationService
                .GetLocalizedSettingAsync(bankTransferPaymentSettings, x => x.DescriptionText, languageId, 0, false, false);
        });

        model.AdditionalFee = bankTransferPaymentSettings.AdditionalFee;
        model.AdditionalFeePercentage = bankTransferPaymentSettings.AdditionalFeePercentage;
        model.ShippableProductRequired = bankTransferPaymentSettings.ShippableProductRequired;

        model.ActiveStoreScopeConfiguration = storeScope;
        if (storeScope > 0)
        {
            model.DescriptionText_OverrideForStore = await _settingService.SettingExistsAsync(bankTransferPaymentSettings, x => x.DescriptionText, storeScope);
            model.AdditionalFee_OverrideForStore = await _settingService.SettingExistsAsync(bankTransferPaymentSettings, x => x.AdditionalFee, storeScope);
            model.AdditionalFeePercentage_OverrideForStore = await _settingService.SettingExistsAsync(bankTransferPaymentSettings, x => x.AdditionalFeePercentage, storeScope);
            model.ShippableProductRequired_OverrideForStore = await _settingService.SettingExistsAsync(bankTransferPaymentSettings, x => x.ShippableProductRequired, storeScope);
        }

        return View("~/Plugins/Payments.BankTransfer/Views/Configure.cshtml", model);
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Configuration.MANAGE_PAYMENT_METHODS)]
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        if (!ModelState.IsValid)
            return await Configure();

        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
        var bankTransferPaymentSettings = await _settingService.LoadSettingAsync<BankTransferPaymentSettings>(storeScope);

        bankTransferPaymentSettings.DescriptionText = model.DescriptionText;
        bankTransferPaymentSettings.AdditionalFee = model.AdditionalFee;
        bankTransferPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;
        bankTransferPaymentSettings.ShippableProductRequired = model.ShippableProductRequired;

        await _settingService.SaveSettingOverridablePerStoreAsync(bankTransferPaymentSettings, x => x.DescriptionText, model.DescriptionText_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(bankTransferPaymentSettings, x => x.AdditionalFee, model.AdditionalFee_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(bankTransferPaymentSettings, x => x.AdditionalFeePercentage, model.AdditionalFeePercentage_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(bankTransferPaymentSettings, x => x.ShippableProductRequired, model.ShippableProductRequired_OverrideForStore, storeScope, false);

        await _settingService.ClearCacheAsync();

        foreach (var localized in model.Locales)
        {
            await _localizationService.SaveLocalizedSettingAsync(bankTransferPaymentSettings,
                x => x.DescriptionText, localized.LanguageId, localized.DescriptionText);
        }

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

        return await Configure();
    }

    #endregion
}
