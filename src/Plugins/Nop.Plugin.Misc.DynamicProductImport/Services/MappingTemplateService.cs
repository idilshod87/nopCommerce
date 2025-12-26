using Nop.Data;
using Nop.Plugin.Misc.DynamicProductImport.Domain;

namespace Nop.Plugin.Misc.DynamicProductImport.Services;

/// <summary>
/// Mapping template service
/// </summary>
public class MappingTemplateService : IMappingTemplateService
{
    #region Fields

    private readonly IRepository<MappingTemplate> _templateRepository;

    #endregion

    #region Ctor

    public MappingTemplateService(IRepository<MappingTemplate> templateRepository)
    {
        _templateRepository = templateRepository;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets a mapping template by identifier
    /// </summary>
    /// <param name="templateId">Template identifier</param>
    /// <returns>Mapping template</returns>
    public virtual async Task<MappingTemplate> GetTemplateByIdAsync(int templateId)
    {
        return await _templateRepository.GetByIdAsync(templateId);
    }

    /// <summary>
    /// Gets all mapping templates for a vendor
    /// </summary>
    /// <param name="vendorId">Vendor identifier (0 for system templates)</param>
    /// <param name="includeSystemTemplates">Whether to include system templates</param>
    /// <returns>List of mapping templates</returns>
    public virtual async Task<IList<MappingTemplate>> GetTemplatesAsync(int vendorId = 0, bool includeSystemTemplates = true)
    {
        var query = _templateRepository.Table;

        if (vendorId == 0)
        {
            // Если поставщик не выбран, возвращаем только системные шаблоны
            query = query.Where(t => t.IsSystemTemplate);
        }
        else
        {
            // Если поставщик выбран, возвращаем его шаблоны + системные (если includeSystemTemplates = true)
            if (includeSystemTemplates)
            {
                query = query.Where(t => t.IsSystemTemplate || t.VendorId == vendorId);
            }
            else
            {
                query = query.Where(t => t.VendorId == vendorId && !t.IsSystemTemplate);
            }
        }

        return await query
            .OrderByDescending(t => t.IsSystemTemplate)
            .ThenBy(t => t.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Inserts a mapping template
    /// </summary>
    /// <param name="template">Mapping template</param>
    public virtual async Task InsertTemplateAsync(MappingTemplate template)
    {
        await _templateRepository.InsertAsync(template);
    }

    /// <summary>
    /// Updates a mapping template
    /// </summary>
    /// <param name="template">Mapping template</param>
    public virtual async Task UpdateTemplateAsync(MappingTemplate template)
    {
        await _templateRepository.UpdateAsync(template);
    }

    /// <summary>
    /// Deletes a mapping template
    /// </summary>
    /// <param name="template">Mapping template</param>
    public virtual async Task DeleteTemplateAsync(MappingTemplate template)
    {
        await _templateRepository.DeleteAsync(template);
    }

    #endregion
}
