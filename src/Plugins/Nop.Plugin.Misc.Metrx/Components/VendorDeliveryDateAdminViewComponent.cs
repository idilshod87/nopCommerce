using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Plugin.Misc.Metrx.Models.Admin;
using Nop.Plugin.Misc.Metrx.Services;
using Nop.Services.Common;
using Nop.Services.Shipping.Date;
using Nop.Web.Areas.Admin.Models.Vendors;
using Nop.Web.Framework.Components;
using System.Linq;

namespace Nop.Plugin.Misc.Metrx.Components;

/// <summary>
/// Renders the vendor delivery date card inside the admin vendor details page
/// </summary>
public class VendorDeliveryDateAdminViewComponent : NopViewComponent
{
    private readonly IDateRangeService _dateRangeService;
    private readonly IVendorDeliveryDateService _vendorDeliveryDateService;
    private readonly IGenericAttributeService _genericAttributeService;
    private readonly IWorkContext _workContext;

    public VendorDeliveryDateAdminViewComponent(IDateRangeService dateRangeService,
        IVendorDeliveryDateService vendorDeliveryDateService,
        IGenericAttributeService genericAttributeService,
        IWorkContext workContext)
    {
        _dateRangeService = dateRangeService;
        _vendorDeliveryDateService = vendorDeliveryDateService;
        _genericAttributeService = genericAttributeService;
        _workContext = workContext;
    }

    public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
    {
        if (additionalData is not VendorModel vendorModel)
            return Content(string.Empty);

        var model = new VendorDeliveryDateModel
        {
            VendorId = vendorModel.Id,
            AllowEditing = vendorModel.Id > 0
        };

        if (model.AllowEditing)
            model.SelectedDeliveryDateId = await _vendorDeliveryDateService.GetDeliveryDateIdAsync(vendorModel.Id);

        var deliveryDates = await _dateRangeService.GetAllDeliveryDatesAsync();

        foreach (var deliveryDate in deliveryDates.OrderBy(dd => dd.DisplayOrder).ThenBy(dd => dd.Id))
        {
            model.AvailableDeliveryDates.Add(new SelectListItem
            {
                Value = deliveryDate.Id.ToString(),
                Text = deliveryDate.Name,
                Selected = model.SelectedDeliveryDateId.HasValue && model.SelectedDeliveryDateId.Value == deliveryDate.Id
            });
        }

        var currentCustomer = await _workContext.GetCurrentCustomerAsync();
        model.IsCollapsed = await _genericAttributeService.GetAttributeAsync<bool>(currentCustomer,
            "VendorPage.HideMetrxDeliveryBlock", defaultValue: true);

        return View(MetrxDefaults.VendorDeliveryDateAdminViewPath, model);
    }
}
