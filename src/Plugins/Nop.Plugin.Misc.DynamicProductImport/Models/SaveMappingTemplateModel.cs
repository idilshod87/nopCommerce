namespace Nop.Plugin.Misc.DynamicProductImport.Models;

/// <summary>
/// Represents a model for saving mapping template
/// </summary>
public class SaveMappingTemplateModel
{
    /// <summary>
    /// Gets or sets the template name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is a system template
    /// </summary>
    public bool IsSystemTemplate { get; set; }

    /// <summary>
    /// Gets or sets the vendor identifier
    /// </summary>
    public int VendorId { get; set; }

    /// <summary>
    /// Gets or sets the list of field mappings
    /// </summary>
    public List<ImportProductMapping> Mappings { get; set; }
}
