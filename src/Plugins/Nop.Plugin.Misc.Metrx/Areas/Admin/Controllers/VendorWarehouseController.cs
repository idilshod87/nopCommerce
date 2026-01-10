using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Vendors;
using Nop.Plugin.Misc.Metrx.Areas.Admin.Models;
using Nop.Plugin.Misc.Metrx.Services;
using Nop.Services.Common;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Shipping;
using Nop.Web.Areas.Admin.Controllers;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Misc.Metrx.Areas.Admin.Controllers;

/// <summary>
/// Provides a vendor-friendly UI for managing warehouse records.
/// </summary>
public class VendorWarehouseController : BaseAdminController
{
    private readonly IAddressService _addressService;
    private readonly ILocalizationService _localizationService;
    private readonly INotificationService _notificationService;
    private readonly IShippingModelFactory _shippingModelFactory;
    private readonly IWarehouseService _warehouseService;
    private readonly IVendorWarehouseService _vendorWarehouseService;
    private readonly IWorkContext _workContext;

    public VendorWarehouseController(IAddressService addressService,
        ILocalizationService localizationService,
        INotificationService notificationService,
        IShippingModelFactory shippingModelFactory,
        IWarehouseService warehouseService,
        IVendorWarehouseService vendorWarehouseService,
        IWorkContext workContext)
    {
        _addressService = addressService;
        _localizationService = localizationService;
        _notificationService = notificationService;
        _shippingModelFactory = shippingModelFactory;
        _warehouseService = warehouseService;
        _vendorWarehouseService = vendorWarehouseService;
        _workContext = workContext;
    }

    public async Task<IActionResult> List()
    {
        var (vendor, errorResult) = await EnsureVendorAsync();
        if (errorResult != null)
            return errorResult;

        var preparedModel = await _shippingModelFactory.PrepareWarehouseSearchModelAsync(new VendorWarehouseSearchModel
        {
            SearchVendorId = vendor.Id,
            AllowVendorFilter = false
        });

        var model = preparedModel as VendorWarehouseSearchModel ?? new VendorWarehouseSearchModel
        {
            SearchVendorId = vendor.Id,
            AllowVendorFilter = false
        };

        model.AllowVendorFilter = false;

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> List(VendorWarehouseSearchModel searchModel)
    {
        var (vendor, errorResult) = await EnsureVendorAsync();
        if (errorResult != null)
            return errorResult;

        searchModel.SearchVendorId = vendor.Id;
        var model = await _shippingModelFactory.PrepareWarehouseListModelAsync(searchModel);
        return Json(model);
    }

    public async Task<IActionResult> Create()
    {
        var (vendor, errorResult) = await EnsureVendorAsync();
        if (errorResult != null)
            return errorResult;

        var preparedModel = await _shippingModelFactory.PrepareWarehouseModelAsync(new VendorWarehouseModel(), null);
        var model = preparedModel as VendorWarehouseModel ?? new VendorWarehouseModel();
        return View(model);
    }

    [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
    public async Task<IActionResult> Create(VendorWarehouseModel model, bool continueEditing)
    {
        var (vendor, errorResult) = await EnsureVendorAsync();
        if (errorResult != null)
            return errorResult;

        if (ModelState.IsValid)
        {
            var address = model.Address.ToEntity<Address>();
            address.CreatedOnUtc = DateTime.UtcNow;
            await _addressService.InsertAddressAsync(address);

            var warehouse = model.ToEntity<Warehouse>();
            warehouse.AddressId = address.Id;

            await _warehouseService.InsertWarehouseAsync(warehouse);
            await _vendorWarehouseService.AssignVendorAsync(warehouse.Id, vendor.Id);

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Misc.Metrx.Warehouses.Notifications.Created"));

            return continueEditing ? RedirectToAction(nameof(Edit), new { id = warehouse.Id }) : RedirectToAction(nameof(List));
        }

        var preparedModel = await _shippingModelFactory.PrepareWarehouseModelAsync(model, null, true);
        model = preparedModel as VendorWarehouseModel ?? model;
        return View(model);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var (vendor, errorResult) = await EnsureVendorAsync();
        if (errorResult != null)
            return errorResult;

        var warehouse = await _warehouseService.GetWarehouseByIdAsync(id);
        if (warehouse == null || !await _vendorWarehouseService.VendorOwnsWarehouseAsync(vendor.Id, id))
            return RedirectToAction(nameof(List));

        var preparedModel = await _shippingModelFactory.PrepareWarehouseModelAsync(new VendorWarehouseModel(), warehouse);
        var model = preparedModel as VendorWarehouseModel ?? new VendorWarehouseModel();
        return View(model);
    }

    [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
    public async Task<IActionResult> Edit(VendorWarehouseModel model, bool continueEditing)
    {
        var (vendor, errorResult) = await EnsureVendorAsync();
        if (errorResult != null)
            return errorResult;

        var warehouse = await _warehouseService.GetWarehouseByIdAsync(model.Id);
        if (warehouse == null || !await _vendorWarehouseService.VendorOwnsWarehouseAsync(vendor.Id, warehouse.Id))
            return RedirectToAction(nameof(List));

        if (ModelState.IsValid)
        {
            var address = await _addressService.GetAddressByIdAsync(warehouse.AddressId) ?? new Address { CreatedOnUtc = DateTime.UtcNow };
            address = model.Address.ToEntity(address);
            if (address.Id > 0)
                await _addressService.UpdateAddressAsync(address);
            else
                await _addressService.InsertAddressAsync(address);

            warehouse = model.ToEntity(warehouse);
            warehouse.AddressId = address.Id;

            await _warehouseService.UpdateWarehouseAsync(warehouse);
            await _vendorWarehouseService.AssignVendorAsync(warehouse.Id, vendor.Id);

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Misc.Metrx.Warehouses.Notifications.Updated"));

            return continueEditing ? RedirectToAction(nameof(Edit), new { id = warehouse.Id }) : RedirectToAction(nameof(List));
        }

        var preparedModel = await _shippingModelFactory.PrepareWarehouseModelAsync(model, warehouse, true);
        model = preparedModel as VendorWarehouseModel ?? model;
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var (vendor, errorResult) = await EnsureVendorAsync();
        if (errorResult != null)
            return errorResult;

        var warehouse = await _warehouseService.GetWarehouseByIdAsync(id);
        if (warehouse == null || !await _vendorWarehouseService.VendorOwnsWarehouseAsync(vendor.Id, id))
            return RedirectToAction(nameof(List));

        await _warehouseService.DeleteWarehouseAsync(warehouse);
        await _vendorWarehouseService.RemoveAssignmentsAsync(new[] { warehouse.Id });

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Misc.Metrx.Warehouses.Notifications.Deleted"));

        return RedirectToAction(nameof(List));
    }

    private async Task<(Vendor vendor, IActionResult errorResult)> EnsureVendorAsync()
    {
        var vendor = await _workContext.GetCurrentVendorAsync();
        if (vendor == null)
            return (null, AccessDeniedView());

        return (vendor, null);
    }
}
