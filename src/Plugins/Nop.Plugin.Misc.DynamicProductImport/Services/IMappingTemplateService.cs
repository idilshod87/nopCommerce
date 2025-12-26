using Nop.Plugin.Misc.DynamicProductImport.Domain;

namespace Nop.Plugin.Misc.DynamicProductImport.Services;

/// <summary>
/// Mapping template service interface
/// </summary>
public interface IMappingTemplateService
{
    /// <summary>
    /// Gets a mapping template by identifier
    /// </summary>
    /// <param name="templateId">Template identifier</param>
    /// <returns>Mapping template</returns>
    Task<MappingTemplate> GetTemplateByIdAsync(int templateId);

    /// <summary>
    /// Gets all mapping templates for a vendor
    /// </summary>
    /// <param name="vendorId">Vendor identifier (0 for system templates)</param>
    /// <param name="includeSystemTemplates">Whether to include system templates</param>
    /// <returns>List of mapping templates</returns>
    Task<IList<MappingTemplate>> GetTemplatesAsync(int vendorId = 0, bool includeSystemTemplates = true);

    /// <summary>
    /// Inserts a mapping template
    /// </summary>
    /// <param name="template">Mapping template</param>
    Task InsertTemplateAsync(MappingTemplate template);

    /// <summary>
    /// Updates a mapping template
    /// </summary>
    /// <param name="template">Mapping template</param>
    Task UpdateTemplateAsync(MappingTemplate template);

    /// <summary>
    /// Deletes a mapping template
    /// </summary>
    /// <param name="template">Mapping template</param>
    Task DeleteTemplateAsync(MappingTemplate template);
}
