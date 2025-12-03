#nullable enable
using System.Collections.Generic;

namespace Nop.Plugin.Misc.WebApi.Frontend.DTOs;

public class AddToCartResultDto
{
    public bool Success { get; set; }
    public IList<string> Warnings { get; set; } = new List<string>();
}

public class ProductAttributeChangeResultDto
{
    public int ProductId { get; set; }
    public string StockAvailability { get; set; } = string.Empty;
    public IList<string> Errors { get; set; } = new List<string>();
}


