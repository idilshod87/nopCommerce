#nullable enable
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

