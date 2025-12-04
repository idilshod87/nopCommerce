using Nop.Core;
using Nop.Services.Common;
using Nop.Services.Localization;
using Nop.Services.Plugins;

namespace Nop.Plugin.Misc.WebApi.Frontend;

/// <summary>
/// Represents the Web API frontend plugin
/// </summary>
public class WebApiFrontendPlugin : BasePlugin, IMiscPlugin
{
    #region Fields

    protected readonly IWebHelper _webHelper;
    protected readonly ILocalizationService _localizationService;

    #endregion

    #region Ctor

    public WebApiFrontendPlugin(IWebHelper webHelper, ILocalizationService localizationService)
    {
        _webHelper = webHelper;
        _localizationService = localizationService;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets a configuration page URL
    /// </summary>
    public override string GetConfigurationPageUrl()
    {
        return $"{_webHelper.GetStoreLocation()}Admin/WebApiFrontend/Configure";
    }

    /// <summary>
    /// Install the plugin
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public override async Task InstallAsync()
    {
        // Add localization resources
        await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
        {
            ["Plugins.Misc.WebApi.Frontend.Configuration"] = "Web API Frontend Configuration",
            ["Plugins.Misc.WebApi.Frontend.MobileApp.Settings"] = "Mobile App Homepage Settings",
            ["Plugins.Misc.WebApi.Frontend.MobileApp.Settings.Description"] = "Configure which sections to display on the mobile app homepage. These settings are separate from the website homepage settings.",
            ["Plugins.Misc.WebApi.Frontend.MobileApp.ShowFeaturedProducts"] = "Show Featured Products",
            ["Plugins.Misc.WebApi.Frontend.MobileApp.ShowBestsellersOnHomepage"] = "Show Bestsellers",
            ["Plugins.Misc.WebApi.Frontend.MobileApp.ShowHomepageCategoryProducts"] = "Show Homepage Category Products",
            ["Plugins.Misc.WebApi.Frontend.MobileApp.ShowManufacturers"] = "Show Manufacturers"
        });

        await base.InstallAsync();
    }
    
    /// <summary>
    /// Uninstall the plugin
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public override async Task UninstallAsync()
    {
        // Delete localization resources
        await _localizationService.DeleteLocaleResourceAsync("Plugins.Misc.WebApi.Frontend.Configuration");
        await _localizationService.DeleteLocaleResourceAsync("Plugins.Misc.WebApi.Frontend.MobileApp.Settings");
        await _localizationService.DeleteLocaleResourceAsync("Plugins.Misc.WebApi.Frontend.MobileApp.Settings.Description");
        await _localizationService.DeleteLocaleResourceAsync("Plugins.Misc.WebApi.Frontend.MobileApp.ShowFeaturedProducts");
        await _localizationService.DeleteLocaleResourceAsync("Plugins.Misc.WebApi.Frontend.MobileApp.ShowBestsellersOnHomepage");
        await _localizationService.DeleteLocaleResourceAsync("Plugins.Misc.WebApi.Frontend.MobileApp.ShowHomepageCategoryProducts");
        await _localizationService.DeleteLocaleResourceAsync("Plugins.Misc.WebApi.Frontend.MobileApp.ShowManufacturers");

        await base.UninstallAsync();
    }

    #endregion
}