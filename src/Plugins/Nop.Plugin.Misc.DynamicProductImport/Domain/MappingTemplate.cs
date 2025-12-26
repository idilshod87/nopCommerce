using Nop.Core;

namespace Nop.Plugin.Misc.DynamicProductImport.Domain;

/// <summary>
/// Represents a saved mapping template
/// </summary>
public class MappingTemplate : BaseEntity
{
    /// <summary>
    /// Gets or sets the template name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the vendor identifier (0 for system templates)
    /// </summary>
    public int VendorId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is a system template
    /// </summary>
    public bool IsSystemTemplate { get; set; }

    /// <summary>
    /// Gets or sets the template mappings as JSON
    /// </summary>
    public string MappingsJson { get; set; }

    /// <summary>
    /// Gets or sets the date and time of template creation
    /// </summary>
    public DateTime CreatedOnUtc { get; set; }
}
