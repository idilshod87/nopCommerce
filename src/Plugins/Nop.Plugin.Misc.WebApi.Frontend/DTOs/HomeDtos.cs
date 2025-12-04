#nullable enable
using System.Collections.Generic;
using Nop.Web.Models.Catalog;

namespace Nop.Plugin.Misc.WebApi.Frontend.DTOs;

public class HomeCategoryWithProductsDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SeName { get; set; } = string.Empty;
    public IList<HomeCategoryWithProductsDto> SubCategories { get; set; } = new List<HomeCategoryWithProductsDto>();
    public IList<ProductOverviewModel> Products { get; set; } = new List<ProductOverviewModel>();
    public Dictionary<string, object> CustomProperties { get; set; } = new Dictionary<string, object>();
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