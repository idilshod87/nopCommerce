using FluentMigrator;
using Nop.Data;
using Nop.Data.Extensions;
using Nop.Data.Mapping;
using Nop.Data.Migrations;
using Nop.Plugin.Misc.Metrx.Domain;

namespace Nop.Plugin.Misc.Metrx.Data.Migrations;

/// <summary>
/// Creates the vendor-warehouse mapping schema required by the Metrx plugin.
/// </summary>
[NopMigration("2026-01-10 09:00:00", "Misc.Metrx vendor warehouse schema", MigrationProcessType.Installation)]
public class VendorWarehouseSchemaMigration : AutoReversingMigration
{
    public override void Up()
    {
        var tableName = NameCompatibilityManager.GetTableName(typeof(VendorWarehouseRecord));

        if (Schema.Table(tableName).Exists())
            return;

        Create.TableFor<VendorWarehouseRecord>();

        Create.Index("IX_Metrx_VendorWarehouse_WarehouseId")
            .OnTable(tableName)
            .OnColumn(nameof(VendorWarehouseRecord.WarehouseId))
            .Unique();

        Create.Index("IX_Metrx_VendorWarehouse_VendorId")
            .OnTable(tableName)
            .OnColumn(nameof(VendorWarehouseRecord.VendorId));
    }
}
