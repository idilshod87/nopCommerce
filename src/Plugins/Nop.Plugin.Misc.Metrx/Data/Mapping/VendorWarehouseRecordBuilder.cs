using FluentMigrator.Builders.Create.Table;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Vendors;
using Nop.Data.Extensions;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Misc.Metrx.Domain;

namespace Nop.Plugin.Misc.Metrx.Data.Mapping;

/// <summary>
/// Configures the vendor-warehouse mapping entity.
/// </summary>
public class VendorWarehouseRecordBuilder : NopEntityBuilder<VendorWarehouseRecord>
{
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table.WithColumn(nameof(VendorWarehouseRecord.VendorId))
            .AsInt32().NotNullable().ForeignKey<Vendor>();

        table.WithColumn(nameof(VendorWarehouseRecord.WarehouseId))
            .AsInt32().NotNullable().ForeignKey<Warehouse>();
    }
}
