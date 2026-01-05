using Nop.Core.Domain.Vendors;

namespace Nop.Plugin.Misc.Metrx.Services;

/// <summary>
/// Provides helper methods for working with vendor delivery dates
/// </summary>
public interface IVendorDeliveryDateService
{
    Task<int?> GetDeliveryDateIdAsync(int vendorId);

    Task<int?> GetDeliveryDateIdAsync(Vendor vendor);

    Task SaveDeliveryDateIdAsync(Vendor vendor, int? deliveryDateId);
}
