#nullable enable
using Nop.Web.Models.Checkout;
using Nop.Web.Models.Common;

namespace Nop.Plugin.Misc.WebApi.Frontend.DTOs;

/// <summary>
/// DTO for checkout step response with NextStep indicator
/// </summary>
public class CheckoutStepResponseDto
{
    public int NextStep { get; set; }
    public CheckoutBillingAddressModel? BillingAddressModel { get; set; }
    public CheckoutShippingAddressModel? ShippingAddressModel { get; set; }
    public CheckoutShippingMethodModel? ShippingMethodModel { get; set; }
    public CheckoutPaymentMethodModel? PaymentMethodModel { get; set; }
    public CheckoutPaymentInfoModel? PaymentInfoModel { get; set; }
    public CheckoutConfirmModel? ConfirmOrderModel { get; set; }
}

/// <summary>
/// DTO for billing address request
/// </summary>
public class SaveBillingRequest
{
    public int? BillingAddressId { get; set; }
    public AddressModel? BillingNewAddress { get; set; }
    public bool ShipToSameAddress { get; set; }
    public string? VatNumber { get; set; }
}

/// <summary>
/// DTO for shipping address request
/// </summary>
public class SaveShippingRequest
{
    public int? ShippingAddressId { get; set; }
    public AddressModel? ShippingNewAddress { get; set; }
    public bool PickupInStore { get; set; }
    public string? PickupPointsId { get; set; }
}

/// <summary>
/// DTO for shipping method request
/// </summary>
public class SaveShippingMethodRequest
{
    public string? ShippingOption { get; set; }
    public bool PickupInStore { get; set; }
    public string? PickupPointsId { get; set; }
}

/// <summary>
/// DTO for payment method request
/// </summary
public class SavePaymentMethodRequest
{
    public string? PaymentMethod { get; set; }
    public bool UseRewardPoints { get; set; }
    public List<VendorPaymentSelectionDto> VendorPayments { get; set; } = new();
}

/// <summary>
/// DTO describing selected payment method for a specific vendor
/// </summary>
public class VendorPaymentSelectionDto
{
    public int VendorId { get; set; }
    public string? PaymentMethod { get; set; }
}

/// <summary>
/// DTO for confirming order with selected cart items
/// </summary>
public class ConfirmSelectedOrderRequest
{
    public List<int> ItemIds { get; set; } = new();
}

