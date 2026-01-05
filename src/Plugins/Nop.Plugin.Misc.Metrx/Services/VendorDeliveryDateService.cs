using Nop.Core.Domain.Vendors;
using Nop.Services.Common;
using Nop.Services.Vendors;

namespace Nop.Plugin.Misc.Metrx.Services;

/// <summary>
/// Default implementation for vendor delivery date helpers
/// </summary>
public class VendorDeliveryDateService : IVendorDeliveryDateService
{
    private readonly IGenericAttributeService _genericAttributeService;
    private readonly IVendorService _vendorService;

    public VendorDeliveryDateService(IGenericAttributeService genericAttributeService, IVendorService vendorService)
    {
        _genericAttributeService = genericAttributeService;
        _vendorService = vendorService;
    }

    public async Task<int?> GetDeliveryDateIdAsync(int vendorId)
    {
        var vendor = await _vendorService.GetVendorByIdAsync(vendorId);
        if (vendor == null)
            return null;

        return await GetDeliveryDateIdAsync(vendor);
    }

    public Task<int?> GetDeliveryDateIdAsync(Vendor vendor)
    {
        ArgumentNullException.ThrowIfNull(vendor);
        return _genericAttributeService.GetAttributeAsync<int?>(vendor, MetrxDefaults.VendorDeliveryDateAttribute);
    }

    public Task SaveDeliveryDateIdAsync(Vendor vendor, int? deliveryDateId)
    {
        ArgumentNullException.ThrowIfNull(vendor);
        return _genericAttributeService.SaveAttributeAsync(vendor, MetrxDefaults.VendorDeliveryDateAttribute, deliveryDateId);
    }
}
