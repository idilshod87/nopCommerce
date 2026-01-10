using System.Collections.Generic;
using Nop.Core;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Web.Framework.Events;
using Nop.Web.Framework.Menu;

namespace Nop.Plugin.Misc.Metrx.Services;

/// <summary>
/// Adds the vendor warehouse navigation node for vendor managers.
/// </summary>
public class VendorWarehouseMenuConsumer : IConsumer<AdminMenuCreatedEvent>
{
    private readonly ILocalizationService _localizationService;
    private readonly IWorkContext _workContext;

    public VendorWarehouseMenuConsumer(ILocalizationService localizationService, IWorkContext workContext)
    {
        _localizationService = localizationService;
        _workContext = workContext;
    }

    public async Task HandleEventAsync(AdminMenuCreatedEvent eventMessage)
    {
        var vendor = await _workContext.GetCurrentVendorAsync();
        if (vendor == null)
            return;

        var menuItem = new AdminMenuItem
        {
            SystemName = MetrxDefaults.VendorWarehousesMenuSystemName,
            Title = await _localizationService.GetResourceAsync("Plugins.Misc.Metrx.Warehouses.Menu"),
            IconClass = "fas fa-warehouse",
            Url = MetrxDefaults.VendorWarehousesListRoute,
            PermissionNames = new List<string> { StandardPermission.Security.ACCESS_ADMIN_PANEL }
        };

        if (!eventMessage.RootMenuItem.InsertAfter("Catalog", menuItem))
            eventMessage.RootMenuItem.ChildNodes.Add(menuItem);
    }
}
