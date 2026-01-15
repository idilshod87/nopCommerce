using Nop.Web.Models.ShoppingCart;

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

    /// <summary>
    /// Customer-entered price (optional, required only for products that demand it)
    /// </summary>
    public decimal? CustomerEnteredPrice { get; set; }

    /// <summary>
    /// Product attributes selection (optional, required for products with required attributes).
    /// Use ProductAttributeMappingId as Id and selected value(s).
    /// Example: [{ "id": 15, "value": 40 }] for dropdown/radio selection
    /// </summary>
    public List<AddToCartAttributeDto>? ProductAttributes { get; set; }
}

/// <summary>
/// Product attribute selection for add to cart request
/// </summary>
public class AddToCartAttributeDto
{
    /// <summary>
    /// Product attribute mapping ID (from product details response)
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Single value ID (for dropdown, radio, color/image squares)
    /// </summary>
    public int? Value { get; set; }

    /// <summary>
    /// Multiple value IDs (for checkboxes)
    /// </summary>
    public List<int>? Values { get; set; }

    /// <summary>
    /// Text value (for textbox, multiline textbox)
    /// </summary>
    public string? Text { get; set; }
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

/// <summary>
/// Extended cart response containing shopping cart data and vendor payment selections.
/// </summary>
public class ShoppingCartResponseDto
{
    public ShoppingCartModel Cart { get; set; } = new();
    public IList<VendorPaymentInfoDto> Vendors { get; set; } = new List<VendorPaymentInfoDto>();
}

/// <summary>
/// Vendor payment selection details currently stored for the cart.
/// </summary>
public class VendorPaymentInfoDto
{
    public int VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string PaymentMethodSystemName { get; set; } = string.Empty;
    public string PaymentMethodName { get; set; } = string.Empty;
    public IList<VendorPaymentMethodDto> AvailablePaymentMethods { get; set; } = new List<VendorPaymentMethodDto>();
}

/// <summary>
/// Available payment method option for a vendor.
/// </summary>
public class VendorPaymentMethodDto
{
    public string SystemName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Selected { get; set; }
}

/// <summary>
/// Simplified request DTO for removing items from cart
/// </summary>
public class RemoveFromCartRequestDto
{
    /// <summary>
    /// Shopping cart item IDs to remove (required)
    /// </summary>
    public IList<int> ItemIds { get; set; } = new List<int>();
}

/// <summary>
/// Simplified response DTO for removing items from cart
/// </summary>
public class RemoveFromCartResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public CartSummaryDto? CartSummary { get; set; }
}

/// <summary>
/// Simplified request DTO for updating cart item quantity
/// </summary>
public class UpdateCartItemQuantityRequestDto
{
    /// <summary>
    /// Shopping cart item ID (required)
    /// </summary>
    public int ItemId { get; set; }

    /// <summary>
    /// New quantity (required, must be greater than 0)
    /// </summary>
    public int Quantity { get; set; }
}

/// <summary>
/// Simplified response DTO for updating cart item quantity
/// </summary>
public class UpdateCartItemQuantityResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public CartSummaryDto? CartSummary { get; set; }
    public IList<string> Warnings { get; set; } = new List<string>();
}


