using Nop.Core.Domain.Cms;
using Nop.Plugin.Misc.Metrx.Areas.Components;
using Nop.Services.Cms;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Web.Framework.Infrastructure;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.Metrx;

/// <summary>
/// Represents the core Metrx customization plugin
/// </summary>
public class MetrxPlugin : BasePlugin, IMiscPlugin, IWidgetPlugin
{
    private readonly ILocalizationService _localizationService;
    private readonly ISettingService _settingService;
    private readonly WidgetSettings _widgetSettings;

    public MetrxPlugin(ILocalizationService localizationService,
        ISettingService settingService,
        WidgetSettings widgetSettings)
    {
        _localizationService = localizationService;
        _settingService = settingService;
        _widgetSettings = widgetSettings;
    }

    public bool HideInWidgetList => true;

    public Type GetWidgetViewComponent(string widgetZone)
    {
        return widgetZone == AdminWidgetZones.VendorDetailsBlock
            ? typeof(VendorDeliveryDateAdminViewComponent)
            : null;
    }

    public Task<IList<string>> GetWidgetZonesAsync()
    {
        IList<string> zones = new List<string> { AdminWidgetZones.VendorDetailsBlock };
        return Task.FromResult(zones);
    }

    public override async Task InstallAsync()
    {
        await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
        {
            ["Plugins.Misc.Metrx.Vendors.DeliveryDateCard"] = "Vendor delivery date",
            ["Plugins.Misc.Metrx.Vendors.Fields.DeliveryDate"] = "Delivery date",
            ["Plugins.Misc.Metrx.Vendors.Fields.DeliveryDate.Hint"] = "Select the default delivery date that should be applied to products for this vendor.",
            ["Plugins.Misc.Metrx.Vendors.Fields.DeliveryDate.NoVendor"] = "Select a delivery date now or after saving; it will be stored once the vendor is created.",
            ["Plugins.Misc.Metrx.Vendors.Fields.DeliveryDate.NoneAvailable"] = "Create delivery dates in Catalog -> Attributes -> Delivery dates to enable this option."
        });

        if (!_widgetSettings.ActiveWidgetSystemNames.Contains(MetrxDefaults.SystemName))
        {
            _widgetSettings.ActiveWidgetSystemNames.Add(MetrxDefaults.SystemName);
            await _settingService.SaveSettingAsync(_widgetSettings);
        }

        await base.InstallAsync();
    }

    public override async Task UninstallAsync()
    {
        await _localizationService.DeleteLocaleResourcesAsync("Plugins.Misc.Metrx");

        if (_widgetSettings.ActiveWidgetSystemNames.Contains(MetrxDefaults.SystemName))
        {
            _widgetSettings.ActiveWidgetSystemNames.Remove(MetrxDefaults.SystemName);
            await _settingService.SaveSettingAsync(_widgetSettings);
        }

        await base.UninstallAsync();
    }
}
