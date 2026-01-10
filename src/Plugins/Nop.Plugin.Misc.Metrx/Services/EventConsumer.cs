using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Vendors;
using Nop.Core.Events;
using Nop.Core.Http.Extensions;
using Nop.Plugin.Misc.Metrx.Areas.Admin.Models;
using Nop.Services.Events;
using Nop.Web.Areas.Admin.Models.Catalog;
using Nop.Web.Areas.Admin.Models.Shipping;
using Nop.Web.Areas.Admin.Models.Vendors;
using Nop.Web.Framework.Events;
using Nop.Web.Framework.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.Metrx.Services;

/// <summary>
/// Handles nopCommerce events for the Metrx plugin
/// </summary>
public class EventConsumer :
    IConsumer<ModelReceivedEvent<BaseNopModel>>,
    IConsumer<ModelPreparedEvent<BaseNopModel>>,
    IConsumer<EntityInsertedEvent<Vendor>>,
    IConsumer<EntityUpdatedEvent<Vendor>>,
    IConsumer<EntityInsertedEvent<Warehouse>>,
    IConsumer<EntityUpdatedEvent<Warehouse>>,
    IConsumer<EntityDeletedEvent<Warehouse>>
{
    private const string PendingExistingVendorAssignmentsKey = "Nop.Plugin.Misc.Metrx.PendingVendorDeliveryDates";
    private const string PendingNewVendorAssignmentKey = "Nop.Plugin.Misc.Metrx.PendingNewVendorDeliveryDate";
    private const string PendingExistingWarehouseAssignmentsKey = "Nop.Plugin.Misc.Metrx.PendingWarehouseVendors";
    private const string PendingNewWarehouseAssignmentKey = "Nop.Plugin.Misc.Metrx.PendingNewWarehouseVendor";
    private const string WarehouseFilterPlaceholderValue = "MetrxVendorWarehousePlaceholder";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IVendorDeliveryDateService _vendorDeliveryDateService;
    private readonly IVendorWarehouseService _vendorWarehouseService;
    private readonly IWorkContext _workContext;

    public EventConsumer(IHttpContextAccessor httpContextAccessor,
        IVendorDeliveryDateService vendorDeliveryDateService,
        IVendorWarehouseService vendorWarehouseService,
        IWorkContext workContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _vendorDeliveryDateService = vendorDeliveryDateService;
        _vendorWarehouseService = vendorWarehouseService;
        _workContext = workContext;
    }

    public async Task HandleEventAsync(ModelReceivedEvent<BaseNopModel> eventMessage)
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        if (request == null || !request.IsPostRequest())
            return;

        switch (eventMessage.Model)
        {
            case VendorModel vendorModel:
                await HandleVendorModelAsync(vendorModel, request);
                break;
            case VendorWarehouseModel vendorWarehouseModel:
                await HandleWarehouseModelAsync(vendorWarehouseModel, request);
                break;
            case WarehouseModel warehouseModel:
                await HandleWarehouseModelAsync(warehouseModel, request);
                break;
        }
    }

    public async Task HandleEventAsync(ModelPreparedEvent<BaseNopModel> eventMessage)
    {
        if (eventMessage?.Model is not ProductSearchModel productSearchModel)
            return;

        await FilterWarehousesForVendorAsync(productSearchModel);
    }

    public async Task HandleEventAsync(EntityInsertedEvent<Vendor> eventMessage)
    {
        if (eventMessage?.Entity == null)
            return;

        if (!TryGetPendingNewVendorAssignment(out var deliveryDateId))
            return;

        await _vendorDeliveryDateService.SaveDeliveryDateIdAsync(eventMessage.Entity, deliveryDateId);
    }

    private async Task FilterWarehousesForVendorAsync(ProductSearchModel searchModel)
    {
        if (searchModel?.AvailableWarehouses == null || !searchModel.AvailableWarehouses.Any())
            return;

        var vendor = await _workContext.GetCurrentVendorAsync();
        if (vendor == null)
            return;

        var vendorWarehouseIds = await _vendorWarehouseService.GetWarehouseIdsForVendorAsync(vendor.Id) ?? new List<int>();

        if (!vendorWarehouseIds.Any())
        {
            TrimToDefaultWarehouseOption(searchModel);
            EnsureWarehouseFilterVisibility(searchModel);
            return;
        }

        var allowedValues = vendorWarehouseIds.Select(id => id.ToString()).ToHashSet();

        for (var i = searchModel.AvailableWarehouses.Count - 1; i >= 0; i--)
        {
            var value = searchModel.AvailableWarehouses[i]?.Value;
            if (string.IsNullOrEmpty(value) || value == "0")
                continue;

            if (!allowedValues.Contains(value))
                searchModel.AvailableWarehouses.RemoveAt(i);
        }

        EnsureWarehouseFilterVisibility(searchModel);
    }

    private static void TrimToDefaultWarehouseOption(ProductSearchModel searchModel)
    {
        for (var i = searchModel.AvailableWarehouses.Count - 1; i >= 0; i--)
        {
            var value = searchModel.AvailableWarehouses[i]?.Value;
            if (string.IsNullOrEmpty(value) || value == "0")
                continue;

            searchModel.AvailableWarehouses.RemoveAt(i);
        }
    }

    private static void EnsureWarehouseFilterVisibility(ProductSearchModel searchModel)
    {
        var selectableCount = searchModel.AvailableWarehouses.Count(item =>
            !string.IsNullOrEmpty(item?.Value) && !item.Value.Equals("0"));

        if (selectableCount >= 2 || searchModel.AvailableWarehouses.Any(item => item?.Value == WarehouseFilterPlaceholderValue))
            return;

        searchModel.AvailableWarehouses.Add(new SelectListItem
        {
            Value = WarehouseFilterPlaceholderValue,
            Text = "\u200B",
            Disabled = true
        });
    }

    public async Task HandleEventAsync(EntityUpdatedEvent<Vendor> eventMessage)
    {
        if (eventMessage?.Entity == null)
            return;

        if (!TryGetVendorAssignment(eventMessage.Entity.Id, out var deliveryDateId))
            return;

        await _vendorDeliveryDateService.SaveDeliveryDateIdAsync(eventMessage.Entity, deliveryDateId);
    }

    public async Task HandleEventAsync(EntityInsertedEvent<Warehouse> eventMessage)
    {
        if (eventMessage?.Entity == null)
            return;

        TryGetPendingNewWarehouseAssignment(out var vendorId);
        await _vendorWarehouseService.AssignVendorAsync(eventMessage.Entity.Id, vendorId);
    }

    public async Task HandleEventAsync(EntityUpdatedEvent<Warehouse> eventMessage)
    {
        if (eventMessage?.Entity == null)
            return;

        if (!TryGetWarehouseAssignment(eventMessage.Entity.Id, out var vendorId))
            return;

        await _vendorWarehouseService.AssignVendorAsync(eventMessage.Entity.Id, vendorId);
    }

    public async Task HandleEventAsync(EntityDeletedEvent<Warehouse> eventMessage)
    {
        if (eventMessage?.Entity == null)
            return;

        await _vendorWarehouseService.RemoveAssignmentsAsync(new[] { eventMessage.Entity.Id });
    }

    private async Task HandleVendorModelAsync(VendorModel vendorModel, HttpRequest request)
    {
        var (exists, rawValue) = await request.TryGetFormValueAsync(MetrxDefaults.VendorDeliveryDateFieldName);
        if (!exists)
            return;

        int? deliveryDateId = null;
        if (int.TryParse(rawValue, out var parsed) && parsed > 0)
            deliveryDateId = parsed;

        var items = _httpContextAccessor.HttpContext?.Items;
        if (items == null)
            return;

        if (vendorModel.Id > 0)
        {
            var assignments = GetOrCreateVendorAssignments(items);
            assignments[vendorModel.Id] = deliveryDateId;
        }
        else
        {
            items[PendingNewVendorAssignmentKey] = deliveryDateId;
        }
    }

    private async Task HandleWarehouseModelAsync(WarehouseModel warehouseModel, HttpRequest request)
    {
        if (warehouseModel == null)
            return;

        var items = _httpContextAccessor.HttpContext?.Items;
        if (items == null)
            return;

        int? vendorId = null;

        if (warehouseModel is VendorWarehouseModel vendorModel && vendorModel.VendorId.HasValue && vendorModel.VendorId.Value > 0)
        {
            vendorId = vendorModel.VendorId;
        }
        else if (request != null)
        {
            var (hasValue, rawValue) = await request.TryGetFormValueAsync(nameof(VendorWarehouseModel.VendorId));
            if (hasValue && int.TryParse(rawValue, out var parsedVendorId) && parsedVendorId > 0)
                vendorId = parsedVendorId;
        }

        if (warehouseModel.Id > 0)
        {
            var assignments = GetOrCreateWarehouseAssignments(items);
            assignments[warehouseModel.Id] = vendorId;
        }
        else
        {
            items[PendingNewWarehouseAssignmentKey] = vendorId;
        }
    }

    private IDictionary<int, int?> GetOrCreateVendorAssignments(IDictionary<object, object> items)
    {
        if (!items.TryGetValue(PendingExistingVendorAssignmentsKey, out var data) || data is not IDictionary<int, int?> assignments)
        {
            assignments = new Dictionary<int, int?>();
            items[PendingExistingVendorAssignmentsKey] = assignments;
        }

        return assignments;
    }

    private IDictionary<int, int?> GetOrCreateWarehouseAssignments(IDictionary<object, object> items)
    {
        if (!items.TryGetValue(PendingExistingWarehouseAssignmentsKey, out var data) || data is not IDictionary<int, int?> assignments)
        {
            assignments = new Dictionary<int, int?>();
            items[PendingExistingWarehouseAssignmentsKey] = assignments;
        }

        return assignments;
    }

    private bool TryGetVendorAssignment(int vendorId, out int? deliveryDateId)
    {
        deliveryDateId = null;

        var items = _httpContextAccessor.HttpContext?.Items;
        if (items == null)
            return false;

        if (!items.TryGetValue(PendingExistingVendorAssignmentsKey, out var data) || data is not IDictionary<int, int?> assignments)
            return false;

        if (!assignments.TryGetValue(vendorId, out deliveryDateId))
            return false;

        assignments.Remove(vendorId);
        return true;
    }

    private bool TryGetWarehouseAssignment(int warehouseId, out int? vendorId)
    {
        vendorId = null;

        var items = _httpContextAccessor.HttpContext?.Items;
        if (items == null)
            return false;

        if (!items.TryGetValue(PendingExistingWarehouseAssignmentsKey, out var data) || data is not IDictionary<int, int?> assignments)
            return false;

        if (!assignments.TryGetValue(warehouseId, out vendorId))
            return false;

        assignments.Remove(warehouseId);
        return true;
    }

    private bool TryGetPendingNewVendorAssignment(out int? deliveryDateId)
    {
        deliveryDateId = null;
        var items = _httpContextAccessor.HttpContext?.Items;
        if (items == null)
            return false;

        if (!items.TryGetValue(PendingNewVendorAssignmentKey, out var value))
            return false;

        deliveryDateId = value as int? ?? (value is int raw ? raw : null);
        items.Remove(PendingNewVendorAssignmentKey);
        return true;
    }

    private bool TryGetPendingNewWarehouseAssignment(out int? vendorId)
    {
        vendorId = null;
        var items = _httpContextAccessor.HttpContext?.Items;
        if (items == null)
            return false;

        if (!items.TryGetValue(PendingNewWarehouseAssignmentKey, out var value))
            return false;

        vendorId = value as int? ?? (value is int raw ? raw : null);
        items.Remove(PendingNewWarehouseAssignmentKey);
        return true;
    }
}
