using Nop.Data;
using Nop.Plugin.Misc.Metrx.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.Metrx.Services;

/// <summary>
/// Default implementation for vendor warehouse ownership operations.
/// </summary>
public class VendorWarehouseService : IVendorWarehouseService
{
    private readonly IRepository<VendorWarehouseRecord> _mappingRepository;

    public VendorWarehouseService(IRepository<VendorWarehouseRecord> mappingRepository)
    {
        _mappingRepository = mappingRepository;
    }

    public async Task AssignVendorAsync(int warehouseId, int? vendorId)
    {
        var mapping = (await _mappingRepository.GetAllAsync(query => query.Where(record => record.WarehouseId == warehouseId)))
            .FirstOrDefault();

        if (!vendorId.HasValue)
        {
            if (mapping != null)
                await _mappingRepository.DeleteAsync(mapping);

            return;
        }

        if (mapping == null)
        {
            mapping = new VendorWarehouseRecord
            {
                WarehouseId = warehouseId,
                VendorId = vendorId.Value
            };

            await _mappingRepository.InsertAsync(mapping, false);
        }
        else
        {
            mapping.VendorId = vendorId.Value;
            await _mappingRepository.UpdateAsync(mapping, false);
        }
    }

    public async Task<int?> GetVendorIdAsync(int warehouseId)
    {
        var mapping = (await _mappingRepository.GetAllAsync(query => query.Where(record => record.WarehouseId == warehouseId)))
            .FirstOrDefault();

        return mapping?.VendorId;
    }

    public async Task<IDictionary<int, int?>> GetVendorsForWarehousesAsync(IEnumerable<int> warehouseIds)
    {
        ArgumentNullException.ThrowIfNull(warehouseIds);

        var ids = warehouseIds.Distinct().ToArray();
        if (!ids.Any())
            return new Dictionary<int, int?>();

        var mappings = await _mappingRepository.GetAllAsync(query => query.Where(record => ids.Contains(record.WarehouseId)));

        return mappings.ToDictionary(record => record.WarehouseId, record => (int?)record.VendorId);
    }

    public async Task<IList<int>> GetWarehouseIdsForVendorAsync(int vendorId)
    {
        var mappings = await _mappingRepository.GetAllAsync(query => query.Where(record => record.VendorId == vendorId));
        return mappings.Select(record => record.WarehouseId).ToList();
    }

    public async Task RemoveAssignmentsAsync(IEnumerable<int> warehouseIds)
    {
        ArgumentNullException.ThrowIfNull(warehouseIds);

        var ids = warehouseIds.Distinct().ToArray();
        if (!ids.Any())
            return;

        var mappings = await _mappingRepository.GetAllAsync(query => query.Where(record => ids.Contains(record.WarehouseId)));

        if (mappings.Any())
            await _mappingRepository.DeleteAsync(mappings, false);
    }

    public async Task<bool> VendorOwnsWarehouseAsync(int vendorId, int warehouseId)
    {
        var mappings = await _mappingRepository.GetAllAsync(query =>
            query.Where(record => record.VendorId == vendorId && record.WarehouseId == warehouseId));

        return mappings.Any();
    }
}
