#nullable enable
using System.Collections.Generic;

namespace Nop.Plugin.Misc.WebApi.Frontend.DTOs;

public class ProductSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SeName { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? Sku { get; set; }
}

public class HomeCategoryWithProductsDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SeName { get; set; } = string.Empty;
    public IList<ProductSummaryDto> Products { get; set; } = new List<ProductSummaryDto>();
}

public class ManufacturerSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SeName { get; set; } = string.Empty;
}

public class CategoryTreeNodeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SeName { get; set; } = string.Empty;
    public IList<CategoryTreeNodeDto> SubCategories { get; set; } = new List<CategoryTreeNodeDto>();
}


