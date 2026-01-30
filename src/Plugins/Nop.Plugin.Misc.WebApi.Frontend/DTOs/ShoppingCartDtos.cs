#nullable enable
using Nop.Web.Models.Catalog;
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
    public ProductPriceModel? ProductPrice { get; set; }
    public string? SubTotal { get; set; }
    public decimal SubTotalValue { get; set; }
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
    public ShoppingCartDto Cart { get; set; } = new();
    public IList<VendorPaymentInfoDto> Vendors { get; set; } = new List<VendorPaymentInfoDto>();
}

/// <summary>
/// Extended shopping cart model with structured items
/// </summary>
public class ShoppingCartDto
{
    public bool OnePageCheckoutEnabled { get; set; }
    public bool ShowSku { get; set; }
    public bool ShowProductImages { get; set; }
    public bool IsEditable { get; set; }
    public bool IsReadyToCheckout { get; set; }
    public IList<ShoppingCartItemDto> Items { get; set; } = new List<ShoppingCartItemDto>();
    public IList<ShoppingCartModel.CheckoutAttributeModel> CheckoutAttributes { get; set; } = new List<ShoppingCartModel.CheckoutAttributeModel>();
    public ShoppingCartModel.OrderReviewDataModel OrderReviewData { get; set; } = new();
    public ShoppingCartModel.DiscountBoxModel DiscountBox { get; set; } = new();
    public ShoppingCartModel.GiftCardBoxModel GiftCardBox { get; set; } = new();
    public OrderTotalsModel OrderTotals { get; set; } = new();
    public Dictionary<string, string> CustomProperties { get; set; } = new();
}

/// <summary>
/// Extended shopping cart item with structured attributes
/// </summary>
public class ShoppingCartItemDto
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public int VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public Nop.Web.Models.Media.PictureModel Picture { get; set; } = new();
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSeName { get; set; } = string.Empty;
    public string UnitPrice { get; set; } = string.Empty;
    public decimal UnitPriceValue { get; set; }
    public string SubTotal { get; set; } = string.Empty;
    public decimal SubTotalValue { get; set; }
    public decimal DiscountValue { get; set; }
    public int Quantity { get; set; }
    public string AllowedQuantities { get; set; } = string.Empty;
    public string AttributeInfo { get; set; } = string.Empty;
    public bool AllowItemEditing { get; set; }
    public bool DisableRemoval { get; set; }
    public IList<string> Warnings { get; set; } = new List<string>();
    
    /// <summary>
    /// Structured product attributes with selected values
    /// </summary>
    public IList<CartItemAttributeDto> Attributes { get; set; } = new List<CartItemAttributeDto>();
    
    public Dictionary<string, string> CustomProperties { get; set; } = new();
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

/// <summary>
/// Structured attribute information for cart items
/// </summary>
public class CartItemAttributeDto
{
    /// <summary>
    /// Product attribute mapping ID
    /// </summary>
    public int AttributeId { get; set; }
    
    /// <summary>
    /// Attribute name (e.g., "Размер", "Цвет")
    /// </summary>
    public string AttributeName { get; set; } = string.Empty;
    
    /// <summary>
    /// Selected value IDs (for dropdown, radio, checkboxes, color/image squares)
    /// </summary>
    public List<int> SelectedValueIds { get; set; } = new();
    
    /// <summary>
    /// Selected value names (e.g., "11", "Red")
    /// </summary>
    public List<string> SelectedValues { get; set; } = new();
    
    /// <summary>
    /// Text input (for textbox, multiline textbox)
    /// </summary>
    public string? TextValue { get; set; }
    
    /// <summary>
    /// Attribute control type (DropdownList, RadioList, Checkboxes, TextBox, etc.)
    /// </summary>
    public string ControlType { get; set; } = string.Empty;
    
    /// <summary>
    /// Price adjustment for this attribute (formatted)
    /// </summary>
    public string PriceAdjustment { get; set; } = string.Empty;
    
    /// <summary>
    /// Price adjustment value
    /// </summary>
    public decimal PriceAdjustmentValue { get; set; }
}


