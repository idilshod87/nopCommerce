using Nop.Core.Domain.Cms;
using Nop.Plugin.Misc.DynamicProductImport.Components;
using Nop.Services.Cms;
using Nop.Services.Common;
using Nop.Services.Configuration;
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
    private readonly ISettingService _settingService;
    private readonly WidgetSettings _widgetSettings;

    #endregion

    #region Ctor

    public DynamicProductImportPlugin(
        ILocalizationService localizationService,
        ISettingService settingService,
        WidgetSettings widgetSettings)
    {
        _localizationService = localizationService;
        _settingService = settingService;
        _widgetSettings = widgetSettings;
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
            ["Admin.Catalog.Products.DynamicImport.NoMoreFields"] = "No more fields available to add",
            ["Admin.Catalog.Products.DynamicImport.Template.NameRequired"] = "Template name is required",
            ["Admin.Catalog.Products.DynamicImport.Template.MappingsRequired"] = "At least one field mapping is required",
            ["Admin.Catalog.Products.DynamicImport.Template.Saved"] = "Template saved successfully",
            ["Admin.Catalog.Products.DynamicImport.Template.NotFound"] = "Template not found",
            ["Admin.Catalog.Products.DynamicImport.Template.Deleted"] = "Template deleted successfully",
            ["Admin.Catalog.Products.DynamicImport.Template.Load"] = "Load template",
            ["Admin.Catalog.Products.DynamicImport.Template.Save"] = "Save as template",
            ["Admin.Catalog.Products.DynamicImport.Template.Name"] = "Template name",
            ["Admin.Catalog.Products.DynamicImport.Template.SystemTemplate"] = "System template (visible to all vendors)",
            ["Admin.Catalog.Products.DynamicImport.Template.Select"] = "Select a template",
            ["Admin.Common.ImportFromExcel.DynamicMappingTip"] = "Map Excel columns to product fields",
            ["Admin.Common.Field"] = "Field",
            ["Admin.Common.ExcelColumn"] = "Excel Column",
            ["Admin.Common.AddField"] = "Add field",
            ["Admin.Common.Next"] = "Next"
        });

        //activate widget by default
        if (!_widgetSettings.ActiveWidgetSystemNames.Contains("Misc.DynamicProductImport"))
        {
            _widgetSettings.ActiveWidgetSystemNames.Add("Misc.DynamicProductImport");
            await _settingService.SaveSettingAsync(_widgetSettings);
        }

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

        //deactivate widget
        if (_widgetSettings.ActiveWidgetSystemNames.Contains("Misc.DynamicProductImport"))
        {
            _widgetSettings.ActiveWidgetSystemNames.Remove("Misc.DynamicProductImport");
            await _settingService.SaveSettingAsync(_widgetSettings);
        }

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

