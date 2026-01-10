using Nop.Core;

namespace Nop.Plugin.Misc.Metrx.Domain;

/// <summary>
/// Represents a vendor-to-warehouse ownership mapping persisted by the Metrx plugin.
/// </summary>
public class VendorWarehouseRecord : BaseEntity
{
    /// <summary>
    /// Gets or sets the vendor identifier.
    /// </summary>
    public int VendorId { get; set; }

    /// <summary>
    /// Gets or sets the warehouse identifier.
    /// </summary>
    public int WarehouseId { get; set; }
}
