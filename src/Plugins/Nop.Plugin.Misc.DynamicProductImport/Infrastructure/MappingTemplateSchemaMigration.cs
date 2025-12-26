using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Migrations;
using Nop.Plugin.Misc.DynamicProductImport.Domain;

namespace Nop.Plugin.Misc.DynamicProductImport.Infrastructure;

[NopMigration("2025/12/26 12:00:00", "Misc.DynamicProductImport base schema", MigrationProcessType.Installation)]
public class MappingTemplateSchemaMigration : AutoReversingMigration
{
    public override void Up()
    {
        Create.TableFor<MappingTemplate>();
    }
}
