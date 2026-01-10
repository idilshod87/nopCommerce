using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Shipping;
using Nop.Services.Common;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Shipping;
using Nop.Services.Shipping.Date;
using Nop.Services.Shipping.Pickup;
using Nop.Services.Vendors;
using Nop.Plugin.Misc.Metrx.Areas.Admin.Models;
using Nop.Plugin.Misc.Metrx.Services;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Areas.Admin.Models.Shipping;
using Nop.Web.Framework.Factories;
using Nop.Web.Framework.Models.Extensions;

namespace Nop.Plugin.Misc.Metrx.Factories;

/// <summary>
/// Overrides the default shipping model factory to inject vendor-aware warehouse behavior.
/// </summary>
public class MetrxShippingModelFactory : ShippingModelFactory
{
    private readonly IVendorService _vendorService;
    private readonly IWorkContext _workContext;
    private readonly IVendorWarehouseService _vendorWarehouseService;

    public MetrxShippingModelFactory(IAddressModelFactory addressModelFactory,
        IAddressService addressService,
        ICountryService countryService,
        IDateRangeService dateRangeService,
        ILocalizationService localizationService,
        ILocalizedModelFactory localizedModelFactory,
        IPickupPluginManager pickupPluginManager,
        IShippingPluginManager shippingPluginManager,
        IShippingMethodsService shippingMethodsService,
        IStateProvinceService stateProvinceService,
        IWarehouseService warehouseService,
        IVendorService vendorService,
        IWorkContext workContext,
        IVendorWarehouseService vendorWarehouseService)
        : base(addressModelFactory, addressService, countryService, dateRangeService, localizationService,
            localizedModelFactory, pickupPluginManager, shippingPluginManager, shippingMethodsService,
            stateProvinceService, warehouseService)
    {
        _vendorService = vendorService;
        _workContext = workContext;
        _vendorWarehouseService = vendorWarehouseService;
    }

    public override async Task<WarehouseSearchModel> PrepareWarehouseSearchModelAsync(WarehouseSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);
        var model = EnsureVendorWarehouseSearchModel(searchModel);

        await base.PrepareWarehouseSearchModelAsync(model);

        var currentVendor = await _workContext.GetCurrentVendorAsync();
        model.AllowVendorFilter = currentVendor == null;

        model.AvailableVendors ??= new List<SelectListItem>();
        model.AvailableVendors.Clear();

        if (model.AllowVendorFilter)
        {
            model.AvailableVendors.Add(new SelectListItem
            {
                Value = "0",
                Text = await _localizationService.GetResourceAsync("Admin.Common.All")
            });

            var vendors = await _vendorService.GetAllVendorsAsync(showHidden: true);
            foreach (var vendor in vendors)
            {
                model.AvailableVendors.Add(new SelectListItem
                {
                    Value = vendor.Id.ToString(),
                    Text = vendor.Name
                });
            }
        }

        if (currentVendor != null)
            model.SearchVendorId = currentVendor.Id;

        return model;
    }

    public override async Task<WarehouseListModel> PrepareWarehouseListModelAsync(WarehouseSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);
        var vendorSearchModel = EnsureVendorWarehouseSearchModel(searchModel);
        var currentVendor = await _workContext.GetCurrentVendorAsync();
        var requestedVendorId = NormalizeVendorId(currentVendor?.Id ?? 0, vendorSearchModel?.SearchVendorId);

        var warehouses = (await _warehouseService.GetAllWarehousesAsync(name: vendorSearchModel?.SearchName)).ToList();
        var vendorMap = await _vendorWarehouseService.GetVendorsForWarehousesAsync(warehouses.Select(w => w.Id));

        if (requestedVendorId.HasValue)
        {
            warehouses = warehouses
                .Where(w => vendorMap.TryGetValue(w.Id, out var vendorId) && vendorId == requestedVendorId)
                .ToList();
        }

        var pagedWarehouses = warehouses.ToPagedList(vendorSearchModel);
        var vendorNames = await LoadVendorNamesAsync(vendorMap.Values);

        var model = new WarehouseListModel().PrepareToGrid(vendorSearchModel, pagedWarehouses, () =>
        {
            return pagedWarehouses.Select(warehouse =>
            {
                var baseModel = warehouse.ToModel<WarehouseModel>();
                var warehouseModel = EnsureVendorWarehouseModel(baseModel);

                if (vendorMap.TryGetValue(warehouse.Id, out var vendorId) && vendorId.HasValue)
                {
                    warehouseModel.VendorId = vendorId;
                    warehouseModel.VendorName = vendorNames.TryGetValue(vendorId.Value, out var name)
                        ? name
                        : null;
                }

                warehouseModel.CanModifyVendor = currentVendor == null;
                return (WarehouseModel)warehouseModel;
            });
        });

        return model;
    }

    public override async Task<WarehouseModel> PrepareWarehouseModelAsync(WarehouseModel model, Warehouse warehouse, bool excludeProperties = false)
    {
        var baseInputModel = warehouse == null ? model : null;
        var baseModel = await base.PrepareWarehouseModelAsync(baseInputModel, warehouse, excludeProperties) ?? model;
        var vendorModel = EnsureVendorWarehouseModel(baseModel);

        var currentVendor = await _workContext.GetCurrentVendorAsync();

        if (!excludeProperties && warehouse != null)
            vendorModel.VendorId ??= await _vendorWarehouseService.GetVendorIdAsync(warehouse.Id);

        if (currentVendor != null)
        {
            vendorModel.CanModifyVendor = false;
            vendorModel.VendorId = currentVendor.Id;
            vendorModel.VendorName = currentVendor.Name;
        }
        else
        {
            await PopulateVendorDropdownAsync(vendorModel);
        }

        if (!string.IsNullOrEmpty(vendorModel.VendorName) || !vendorModel.VendorId.HasValue)
            return vendorModel;

        if (vendorModel.VendorId.HasValue)
        {
            var vendor = await _vendorService.GetVendorByIdAsync(vendorModel.VendorId.Value);
            vendorModel.VendorName = vendor?.Name;
        }

        return vendorModel;
    }

    private static int? NormalizeVendorId(int currentVendorId, int? requestedVendorId)
    {
        if (currentVendorId > 0)
            return currentVendorId;

        if (!requestedVendorId.HasValue || requestedVendorId.Value <= 0)
            return null;

        return requestedVendorId;
    }

    private static VendorWarehouseSearchModel EnsureVendorWarehouseSearchModel(WarehouseSearchModel searchModel)
    {
        if (searchModel is VendorWarehouseSearchModel vendorModel)
            return vendorModel;

        var result = new VendorWarehouseSearchModel
        {
            SearchName = searchModel.SearchName,
            AvailablePageSizes = searchModel.AvailablePageSizes,
            Draw = searchModel.Draw,
            Start = searchModel.Start,
            Length = searchModel.Length
        };

        CopyCustomProperties(searchModel.CustomProperties, result.CustomProperties);

        return result;
    }

    private static VendorWarehouseModel EnsureVendorWarehouseModel(WarehouseModel model)
    {
        if (model is VendorWarehouseModel vendorModel)
            return vendorModel;

        var result = new VendorWarehouseModel();

        if (model == null)
            return result;

        result.Id = model.Id;
        result.Name = model.Name;
        result.AdminComment = model.AdminComment;
        result.Address = model.Address ?? result.Address;
        CopyCustomProperties(model.CustomProperties, result.CustomProperties);

        return result;
    }

    private static void CopyCustomProperties(IDictionary<string, string> source, IDictionary<string, string> destination)
    {
        if (source == null || destination == null)
            return;

        foreach (var (key, value) in source)
        {
            destination[key] = value;
        }
    }

    private async Task PopulateVendorDropdownAsync(VendorWarehouseModel model)
    {
        model.CanModifyVendor = true;
        model.AvailableVendors = new List<SelectListItem>
        {
            new()
            {
                Value = "0",
                Text = await _localizationService.GetResourceAsync("Plugins.Misc.Metrx.Warehouses.Fields.Vendor.None")
            }
        };

        var vendors = await _vendorService.GetAllVendorsAsync(showHidden: true);
        foreach (var vendor in vendors)
        {
            model.AvailableVendors.Add(new SelectListItem
            {
                Value = vendor.Id.ToString(),
                Text = vendor.Name
            });
        }
    }

    private async Task<Dictionary<int, string>> LoadVendorNamesAsync(IEnumerable<int?> vendorIds)
    {
        var result = new Dictionary<int, string>();
        var ids = vendorIds.Where(id => id.HasValue).Select(id => id.Value).Distinct().ToArray();
        if (!ids.Any())
            return result;

        foreach (var id in ids)
        {
            var vendor = await _vendorService.GetVendorByIdAsync(id);
            if (vendor != null)
                result[id] = vendor.Name;
        }

        return result;
    }
}
