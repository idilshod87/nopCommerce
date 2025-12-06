#nullable enable
using Nop.Web.Framework.UI.Paging;

namespace Nop.Plugin.Misc.WebApi.Frontend.DTOs;

/// <summary>
/// DTO for search term autocomplete response
/// </summary>
public class SearchTermAutoCompleteDto
{
    public string Label { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string? ProductPictureUrl { get; set; }
    public bool ShowLinkToResultSearch { get; set; }
}

/// <summary>
/// DTO for paginated vendors response
/// </summary>
public record PaginatedVendorsDto : BasePageableModel
{
    public IList<Nop.Web.Models.Catalog.VendorModel> Vendors { get; set; } = new List<Nop.Web.Models.Catalog.VendorModel>();
}

