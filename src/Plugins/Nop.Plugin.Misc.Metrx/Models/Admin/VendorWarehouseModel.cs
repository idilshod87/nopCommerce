using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Areas.Admin.Models.Shipping;
using Nop.Web.Framework.Mvc.ModelBinding;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.Metrx.Areas.Admin.Models;

/// <summary>
/// Warehouse model augmented with vendor-specific information for the Metrx plugin.
/// </summary>
public record VendorWarehouseModel : WarehouseModel
{
    [NopResourceDisplayName("Plugins.Misc.Metrx.Warehouses.Fields.Vendor")]
    public int? VendorId { get; set; }

    public string VendorName { get; set; }

    public bool CanModifyVendor { get; set; } = true;

    public IList<SelectListItem> AvailableVendors { get; set; } = new List<SelectListItem>();
}
