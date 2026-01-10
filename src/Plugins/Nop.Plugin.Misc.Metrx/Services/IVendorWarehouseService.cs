using Nop.Core.Domain.Shipping;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.Metrx.Services;

/// <summary>
/// Provides operations for managing vendor-to-warehouse assignments.
/// </summary>
public interface IVendorWarehouseService
{
    Task AssignVendorAsync(int warehouseId, int? vendorId);

    Task<int?> GetVendorIdAsync(int warehouseId);

    Task<IDictionary<int, int?>> GetVendorsForWarehousesAsync(IEnumerable<int> warehouseIds);

    Task<IList<int>> GetWarehouseIdsForVendorAsync(int vendorId);

    Task RemoveAssignmentsAsync(IEnumerable<int> warehouseIds);

    Task<bool> VendorOwnsWarehouseAsync(int vendorId, int warehouseId);
}
