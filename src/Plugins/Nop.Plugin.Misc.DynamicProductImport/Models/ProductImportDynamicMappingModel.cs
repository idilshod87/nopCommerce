using System.ComponentModel.DataAnnotations;

namespace Nop.Plugin.Misc.DynamicProductImport.Models;

public class ProductImportDynamicMappingModel
{
    [Required]
    public string Property { get; set; }

    public int ColumnIndex { get; set; }
}

public class ProductImportDynamicRequestModel
{
    [Required]
    public string FileId { get; set; }

    public int? VendorId { get; set; }

    public IList<ProductImportDynamicMappingModel> Mappings { get; set; } = new List<ProductImportDynamicMappingModel>();

    public bool SaveTemplate { get; set; }

    public string TemplateName { get; set; }
}

