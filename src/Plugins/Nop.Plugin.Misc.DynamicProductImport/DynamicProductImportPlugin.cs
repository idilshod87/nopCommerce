using Nop.Plugin.Misc.DynamicProductImport.Components;
using Nop.Services.Common;
using Nop.Services.Cms;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Web.Framework.Infrastructure;

namespace Nop.Plugin.Misc.DynamicProductImport;

/// <summary>
/// Represents Dynamic Product Import plugin
/// </summary>
public class DynamicProductImportPlugin : BasePlugin, IMiscPlugin, IWidgetPlugin
{
    #region Fields

    private readonly ILocalizationService _localizationService;

    #endregion

    #region Ctor

    public DynamicProductImportPlugin(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Install plugin
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public override async Task InstallAsync()
    {
        await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
        {
            ["Admin.Catalog.Products.DynamicImport"] = "Dynamic Import",
            ["Admin.Catalog.Products.DynamicImport.RequiredFieldMissing"] = "Required field mapping is missing",
            ["Admin.Catalog.Products.DynamicImport.DuplicateColumn"] = "Duplicate column mapping detected",
            ["Admin.Catalog.Products.DynamicImport.FileNotFound"] = "Uploaded file not found",
            ["Admin.Catalog.Products.DynamicImport.VendorNotAllowed"] = "Vendor selection is required",
            ["Admin.Common.ImportFromExcel.DynamicMappingTip"] = "Map Excel columns to product fields"
        });

        await base.InstallAsync();
    }

    /// <summary>
    /// Uninstall plugin
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public override async Task UninstallAsync()
    {
        await _localizationService.DeleteLocaleResourcesAsync("Admin.Catalog.Products.DynamicImport");
        await _localizationService.DeleteLocaleResourcesAsync("Admin.Common.ImportFromExcel.DynamicMappingTip");

        await base.UninstallAsync();
    }

    /// <summary>
    /// Gets widget zones where this widget should be rendered
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the widget zones
    /// </returns>
    public Task<IList<string>> GetWidgetZonesAsync()
    {
        return Task.FromResult<IList<string>>(new List<string>
        {
            AdminWidgetZones.ProductListButtons
        });
    }

    /// <summary>
    /// Gets a type of a view component for displaying widget
    /// </summary>
    /// <param name="widgetZone">Name of the widget zone</param>
    /// <returns>View component type</returns>
    public Type GetWidgetViewComponent(string widgetZone)
    {
        if (widgetZone.Equals(AdminWidgetZones.ProductListButtons))
            return typeof(DynamicImportButtonViewComponent);

        return null;
    }

    /// <summary>
    /// Gets a value indicating whether to hide this plugin on the widget list page in the admin area
    /// </summary>
    public bool HideInWidgetList => true;

    #endregion
}

