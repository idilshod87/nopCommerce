using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Misc.WebApi.Frontend.Configuration;
using Nop.Plugin.Misc.WebApi.Frontend.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Misc.WebApi.Frontend.Controllers;

[AutoValidateAntiforgeryToken]
[AuthorizeAdmin]
[Area(AreaNames.ADMIN)]
public class WebApiFrontendController : BasePluginController
{
    #region Fields

    protected readonly IPermissionService _permissionService;
    protected readonly ISettingService _settingService;
    protected readonly ILocalizationService _localizationService;
    protected readonly INotificationService _notificationService;

    #endregion

    #region Ctor 

    public WebApiFrontendController(
        IPermissionService permissionService,
        ISettingService settingService,
        ILocalizationService localizationService,
        INotificationService notificationService)
    {
        _permissionService = permissionService;
        _settingService = settingService;
        _localizationService = localizationService;
        _notificationService = notificationService;
    }

    #endregion

    #region Methods

    [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
    public virtual async Task<IActionResult> Configure()
    {
        var settings = await _settingService.LoadSettingAsync<MobileAppSettings>();
        
        var model = new MobileAppSettingsModel
        {
            ShowFeaturedProducts = settings.ShowFeaturedProducts,
            ShowBestsellersOnHomepage = settings.ShowBestsellersOnHomepage,
            ShowHomepageCategoryProducts = settings.ShowHomepageCategoryProducts,
            ShowManufacturers = settings.ShowManufacturers
        };

        return View("~/Plugins/Misc.WebApi.Frontend/Views/Configure.cshtml", model);
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
    public virtual async Task<IActionResult> Configure(MobileAppSettingsModel model)
    {
        if (!ModelState.IsValid)
            return await Configure();

        var settings = await _settingService.LoadSettingAsync<MobileAppSettings>();
        
        settings.ShowFeaturedProducts = model.ShowFeaturedProducts;
        settings.ShowBestsellersOnHomepage = model.ShowBestsellersOnHomepage;
        settings.ShowHomepageCategoryProducts = model.ShowHomepageCategoryProducts;
        settings.ShowManufacturers = model.ShowManufacturers;

        await _settingService.SaveSettingAsync(settings);

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

        return await Configure();
    }

    #endregion
}