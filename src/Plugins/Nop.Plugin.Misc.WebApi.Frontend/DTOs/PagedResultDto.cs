namespace Nop.Plugin.Misc.WebApi.Frontend.DTOs;

public class PagedResultDto<T> : ApiResponse<T>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPrevious => PageIndex > 0;
    public bool HasNext => PageIndex + 1 < TotalPages;
}

public record CountryListItemDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string TwoLetterIsoCode { get; init; } = string.Empty;
    public string ThreeLetterIsoCode { get; init; } = string.Empty;
    public bool AllowsBilling { get; init; }
    public bool AllowsShipping { get; init; }
    public bool SubjectToVat { get; init; }
}

public record CityListItemDto
{
    public int Id { get; init; }
    public int CountryId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Abbreviation { get; init; } = string.Empty;
    public bool Published { get; init; }
    public int DisplayOrder { get; init; }
}
