using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.Metrx.Models.Admin;

/// <summary>
/// View model used to render the vendor delivery date card in the admin area
/// </summary>
public partial record VendorDeliveryDateModel : BaseNopModel
{
    public VendorDeliveryDateModel()
    {
        AvailableDeliveryDates = new List<SelectListItem>();
    }

    public int VendorId { get; set; }

    public int? SelectedDeliveryDateId { get; set; }

    public IList<SelectListItem> AvailableDeliveryDates { get; set; }

    public bool AllowEditing { get; set; }

    public bool IsCollapsed { get; set; }
}
