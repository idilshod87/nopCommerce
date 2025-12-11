namespace Nop.Plugin.Misc.DynamicProductImport.Models;

/// <summary>
/// Represents a mapping between a product property and an Excel column.
/// </summary>
public class ImportProductMapping
{
    public string Property { get; set; }

    public int ColumnIndex { get; set; }
}
