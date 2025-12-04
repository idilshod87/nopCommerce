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

/// <summary>
/// Simplified request DTO for adding product to cart
/// </summary>
public class AddToCartRequestDto
{
    /// <summary>
    /// Product ID (required)
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Quantity (optional, defaults to 1)
    /// </summary>
    public int? Quantity { get; set; }
}

/// <summary>
/// Simplified response DTO for adding product to cart
/// </summary>
public class AddToCartResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public CartSummaryDto? CartSummary { get; set; }
    public IList<string> Warnings { get; set; } = new List<string>();
}

/// <summary>
/// Cart summary information
/// </summary>
public class CartSummaryDto
{
    public int ItemsCount { get; set; }
    public int TotalQuantity { get; set; }
    public string Subtotal { get; set; } = string.Empty;
    public decimal SubtotalValue { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
}


