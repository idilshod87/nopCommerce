using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Misc.DynamicProductImport.Domain;

namespace Nop.Plugin.Misc.DynamicProductImport.Infrastructure;

/// <summary>
/// Mapping template entity builder
/// </summary>
public class MappingTemplateBuilder : NopEntityBuilder<MappingTemplate>
{
    #region Methods

    /// <summary>
    /// Apply entity configuration
    /// </summary>
    /// <param name="table">Create table expression builder</param>
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(MappingTemplate.Name))
                .AsString(400)
                .NotNullable()
            .WithColumn(nameof(MappingTemplate.VendorId))
                .AsInt32()
                .NotNullable()
            .WithColumn(nameof(MappingTemplate.IsSystemTemplate))
                .AsBoolean()
                .NotNullable()
            .WithColumn(nameof(MappingTemplate.MappingsJson))
                .AsString(int.MaxValue)
                .Nullable()
            .WithColumn(nameof(MappingTemplate.CreatedOnUtc))
                .AsDateTime2()
                .NotNullable();
    }

    #endregion
}
