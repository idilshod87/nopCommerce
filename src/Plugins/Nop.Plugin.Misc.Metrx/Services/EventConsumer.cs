using Microsoft.AspNetCore.Http;
using Nop.Core.Domain.Vendors;
using Nop.Core.Events;
using Nop.Core.Http.Extensions;
using Nop.Services.Events;
using Nop.Web.Areas.Admin.Models.Vendors;
using Nop.Web.Framework.Events;
using Nop.Web.Framework.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.Metrx.Services;

/// <summary>
/// Handles nopCommerce events for the Metrx plugin
/// </summary>
public class EventConsumer :
    IConsumer<ModelReceivedEvent<BaseNopModel>>,
    IConsumer<EntityInsertedEvent<Vendor>>,
    IConsumer<EntityUpdatedEvent<Vendor>>
{
    private const string PendingExistingAssignmentsKey = "Nop.Plugin.Misc.Metrx.PendingVendorDeliveryDates";
    private const string PendingNewVendorAssignmentKey = "Nop.Plugin.Misc.Metrx.PendingNewVendorDeliveryDate";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IVendorDeliveryDateService _vendorDeliveryDateService;

    public EventConsumer(IHttpContextAccessor httpContextAccessor, IVendorDeliveryDateService vendorDeliveryDateService)
    {
        _httpContextAccessor = httpContextAccessor;
        _vendorDeliveryDateService = vendorDeliveryDateService;
    }

    public async Task HandleEventAsync(ModelReceivedEvent<BaseNopModel> eventMessage)
    {
        if (eventMessage.Model is not VendorModel vendorModel)
            return;

        var request = _httpContextAccessor.HttpContext?.Request;
        if (request == null || !request.IsPostRequest())
            return;

        var (exists, rawValue) = await request.TryGetFormValueAsync(MetrxDefaults.VendorDeliveryDateFieldName);
        if (!exists)
            return;

        int? deliveryDateId = null;
        if (int.TryParse(rawValue, out var parsed) && parsed > 0)
            deliveryDateId = parsed;

        var items = _httpContextAccessor.HttpContext.Items;
        if (vendorModel.Id > 0)
        {
            var assignments = GetOrCreateAssignments(items);
            assignments[vendorModel.Id] = deliveryDateId;
        }
        else
        {
            items[PendingNewVendorAssignmentKey] = deliveryDateId;
        }
    }

    public async Task HandleEventAsync(EntityInsertedEvent<Vendor> eventMessage)
    {
        if (eventMessage?.Entity == null)
            return;

        if (!TryGetPendingNewAssignment(out var deliveryDateId))
            return;

        await _vendorDeliveryDateService.SaveDeliveryDateIdAsync(eventMessage.Entity, deliveryDateId);
    }

    public async Task HandleEventAsync(EntityUpdatedEvent<Vendor> eventMessage)
    {
        if (eventMessage?.Entity == null)
            return;

        if (!TryGetAssignment(eventMessage.Entity.Id, out var deliveryDateId))
            return;

        await _vendorDeliveryDateService.SaveDeliveryDateIdAsync(eventMessage.Entity, deliveryDateId);
    }

    private IDictionary<int, int?> GetOrCreateAssignments(IDictionary<object, object> items)
    {
        if (!items.TryGetValue(PendingExistingAssignmentsKey, out var data) || data is not IDictionary<int, int?> assignments)
        {
            assignments = new Dictionary<int, int?>();
            items[PendingExistingAssignmentsKey] = assignments;
        }

        return assignments;
    }

    private bool TryGetAssignment(int vendorId, out int? deliveryDateId)
    {
        deliveryDateId = null;

        var items = _httpContextAccessor.HttpContext?.Items;
        if (items == null)
            return false;

        if (!items.TryGetValue(PendingExistingAssignmentsKey, out var data) || data is not IDictionary<int, int?> assignments)
            return false;

        if (!assignments.TryGetValue(vendorId, out deliveryDateId))
            return false;

        assignments.Remove(vendorId);
        return true;
    }

    private bool TryGetPendingNewAssignment(out int? deliveryDateId)
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
}
