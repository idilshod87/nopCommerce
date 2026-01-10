using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Areas.Admin.Models.Shipping;
using Nop.Web.Framework.Mvc.ModelBinding;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.Metrx.Areas.Admin.Models;

/// <summary>
/// Search model exposing vendor-specific filters for warehouses.
/// </summary>
public record VendorWarehouseSearchModel : WarehouseSearchModel
{
    public VendorWarehouseSearchModel()
    {
        AvailableVendors = new List<SelectListItem>();
    }

    [NopResourceDisplayName("Plugins.Misc.Metrx.Warehouses.Fields.Vendor")]
    public int? SearchVendorId { get; set; }

    public bool AllowVendorFilter { get; set; } = true;

    public IList<SelectListItem> AvailableVendors { get; set; }
}
