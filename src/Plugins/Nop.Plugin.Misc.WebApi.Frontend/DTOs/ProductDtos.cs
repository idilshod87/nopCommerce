namespace Nop.Plugin.Misc.WebApi.Frontend.DTOs;

public class EstimateShippingRequest
{
    public int ProductId { get; set; }
    public int? CountryId { get; set; }
    public int? StateProvinceId { get; set; }
    public string? ZipPostalCode { get; set; }
    public string? City { get; set; }
    public int Quantity { get; set; } = 1;
}

public class AddProductReviewRequest
{
    public string? Title { get; set; }
    public string? ReviewText { get; set; }
    public int Rating { get; set; }
}
