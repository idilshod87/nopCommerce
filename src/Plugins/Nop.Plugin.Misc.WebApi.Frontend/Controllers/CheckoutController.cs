using System.Text.Json;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Tax;
using Nop.Services.Directory;
using Nop.Plugin.Misc.WebApi.Frontend.DTOs;
using Nop.Services.Attributes;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Catalog;
using Nop.Services.Shipping;
using Nop.Services.Tax;
using Nop.Web.Factories;
using Nop.Web.Models.Checkout;

namespace Nop.Plugin.Misc.WebApi.Frontend.Controllers;

/// <summary>
/// Public API for checkout process, aligned with NopStation Cart API routes.
/// </summary>
[ApiController]
[IgnoreAntiforgeryToken]
[Produces("application/json")]
[Route("public-api/checkout")]
public class CheckoutController : ControllerBase
{
    #region Fields

    private readonly ICheckoutModelFactory _checkoutModelFactory;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly IStoreContext _storeContext;
    private readonly IWorkContext _workContext;
    private readonly ICustomerService _customerService;
    private readonly IAddressService _addressService;
    private readonly IAddressModelFactory _addressModelFactory;
    private readonly IAttributeParser<AddressAttribute, AddressAttributeValue> _addressAttributeParser;
    private readonly IGenericAttributeService _genericAttributeService;
    private readonly IShippingService _shippingService;
    private readonly IPaymentPluginManager _paymentPluginManager;
    private readonly IOrderProcessingService _orderProcessingService;
    private readonly IPaymentService _paymentService;
    private readonly IOrderService _orderService;
    private readonly ILocalizationService _localizationService;
    private readonly ITaxService _taxService;
    private readonly IProductService _productService;
    private readonly ICountryService _countryService;
    private readonly AddressSettings _addressSettings;
    private readonly CustomerSettings _customerSettings;
    private readonly OrderSettings _orderSettings;
    private readonly ShippingSettings _shippingSettings;
    private readonly PaymentSettings _paymentSettings;
    private readonly TaxSettings _taxSettings;
    private readonly RewardPointsSettings _rewardPointsSettings;
    private static readonly string[] _separator = ["___"];

    #endregion

    #region Ctor

    public CheckoutController(
        ICheckoutModelFactory checkoutModelFactory,
        IShoppingCartService shoppingCartService,
        IStoreContext storeContext,
        IWorkContext workContext,
        ICustomerService customerService,
        IAddressService addressService,
        IAddressModelFactory addressModelFactory,
        IAttributeParser<AddressAttribute, AddressAttributeValue> addressAttributeParser,
        IGenericAttributeService genericAttributeService,
        IShippingService shippingService,
        IPaymentPluginManager paymentPluginManager,
        IOrderProcessingService orderProcessingService,
        IPaymentService paymentService,
        IOrderService orderService,
        ILocalizationService localizationService,
        ITaxService taxService,
        IProductService productService,
        ICountryService countryService,
        AddressSettings addressSettings,
        CustomerSettings customerSettings,
        OrderSettings orderSettings,
        ShippingSettings shippingSettings,
        PaymentSettings paymentSettings,
        TaxSettings taxSettings,
        RewardPointsSettings rewardPointsSettings)
    {
        _checkoutModelFactory = checkoutModelFactory;
        _shoppingCartService = shoppingCartService;
        _storeContext = storeContext;
        _workContext = workContext;
        _customerService = customerService;
        _addressService = addressService;
        _addressModelFactory = addressModelFactory;
        _addressAttributeParser = addressAttributeParser;
        _genericAttributeService = genericAttributeService;
        _shippingService = shippingService;
        _paymentPluginManager = paymentPluginManager;
        _orderProcessingService = orderProcessingService;
        _paymentService = paymentService;
        _orderService = orderService;
        _localizationService = localizationService;
        _taxService = taxService;
        _productService = productService;
        _countryService = countryService;
        _addressSettings = addressSettings;
        _customerSettings = customerSettings;
        _orderSettings = orderSettings;
        _shippingSettings = shippingSettings;
        _paymentSettings = paymentSettings;
        _taxSettings = taxSettings;
        _rewardPointsSettings = rewardPointsSettings;
    }

    #endregion

    /// <summary>
    /// GET /checkout/getbilling
    /// Get billing address information for checkout.
    /// </summary>
    [HttpGet("getbilling")]
    [ProducesResponseType(typeof(ApiResponse<OnePageCheckoutModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetBilling()
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

        if (!cart.Any())
            return BadRequest(new { Message = "Cart is empty" });

        var model = await _checkoutModelFactory.PrepareOnePageCheckoutModelAsync(cart);

        return Ok(new ApiResponse<OnePageCheckoutModel> { Data = model });
    }

    /// <summary>
    /// POST /checkout/savebilling
    /// Save billing address (existing or new).
    /// </summary>
    [HttpPost("savebilling")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<CheckoutStepResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SaveBilling([FromBody] SaveBillingRequest request)
    {
        if (_orderSettings.CheckoutDisabled)
            return BadRequest(new { Message = await _localizationService.GetResourceAsync("Checkout.Disabled") });

        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

        if (!cart.Any())
            return BadRequest(new { Message = "Cart is empty" });

        var response = new CheckoutStepResponseDto();

        if (request?.BillingAddressId > 0)
        {
            //existing address
            var address = await _customerService.GetCustomerAddressAsync(customer.Id, request.BillingAddressId.Value);
            if (address == null)
                return BadRequest(new { Message = await _localizationService.GetResourceAsync("Checkout.Address.NotFound") });

            customer.BillingAddressId = address.Id;
            await _customerService.UpdateCustomerAsync(customer);
        }
        else if (request?.BillingNewAddress != null)
        {
            //new address
            if (await _customerService.IsGuestAsync(customer) && _taxSettings.EuVatEnabled && _taxSettings.EuVatEnabledForGuests)
            {
                var warning = await SaveCustomerVatNumberAsync(request.VatNumber, customer);
                if (!string.IsNullOrEmpty(warning))
                    return BadRequest(new { Message = warning });
            }

            //custom address attributes would be parsed from form, but for API we'll skip them for now
            var newAddress = request.BillingNewAddress;
            var address = _addressService.FindAddress((await _customerService.GetAddressesByCustomerIdAsync(customer.Id)).ToList(),
                newAddress.FirstName, newAddress.LastName, newAddress.PhoneNumber,
                newAddress.Email, newAddress.FaxNumber, newAddress.Company,
                newAddress.Address1, newAddress.Address2, newAddress.City,
                newAddress.County, newAddress.StateProvinceId, newAddress.ZipPostalCode,
                newAddress.CountryId, string.Empty);

            if (address == null)
            {
                address = newAddress.ToEntity();
                address.CustomAttributes = string.Empty;
                address.CreatedOnUtc = DateTime.UtcNow;

                if (address.CountryId == 0)
                    address.CountryId = null;
                if (address.StateProvinceId == 0)
                    address.StateProvinceId = null;

                await _addressService.InsertAddressAsync(address);
                await _customerService.InsertCustomerAddressAsync(customer, address);
            }

            customer.BillingAddressId = address.Id;
            await _customerService.UpdateCustomerAsync(customer);
        }

        //ship to the same address?
        if (await _shoppingCartService.ShoppingCartRequiresShippingAsync(cart))
        {
            var address = await _customerService.GetCustomerBillingAddressAsync(customer);
            var shippingAllowed = !_addressSettings.CountryEnabled || ((await _countryService.GetCountryByAddressAsync(address))?.AllowsShipping ?? false);
            if (_shippingSettings.ShipToSameAddress && request?.ShipToSameAddress == true && shippingAllowed)
            {
                customer.ShippingAddressId = customer.BillingAddressId;
                await _customerService.UpdateCustomerAsync(customer);
                await _genericAttributeService.SaveAttributeAsync<ShippingOption>(customer, NopCustomerDefaults.SelectedShippingOptionAttribute, null, store.Id);
                await _genericAttributeService.SaveAttributeAsync<PickupPoint>(customer, NopCustomerDefaults.SelectedPickupPointAttribute, null, store.Id);

                //load shipping method
                var shippingMethodModel = await _checkoutModelFactory.PrepareShippingMethodModelAsync(cart, address);
                response.ShippingMethodModel = shippingMethodModel;
                response.NextStep = 3; // shipping method step
            }
            else
            {
                //load shipping address
                var shippingAddressModel = new CheckoutShippingAddressModel();
                await _checkoutModelFactory.PrepareShippingAddressModelAsync(shippingAddressModel, cart, prePopulateNewAddressWithCustomerFields: true);
                response.ShippingAddressModel = shippingAddressModel;
                response.NextStep = 2; // shipping address step
            }
        }
        else
        {
            //shipping is not required
            await _genericAttributeService.SaveAttributeAsync<ShippingOption>(customer, NopCustomerDefaults.SelectedShippingOptionAttribute, null, store.Id);
            response.NextStep = 4; // payment method step
        }

        return Ok(new ApiResponse<CheckoutStepResponseDto> { Data = response });
    }

    /// <summary>
    /// POST /checkout/saveshipping
    /// Save shipping address (existing, new, or pickup in store).
    /// </summary>
    [HttpPost("saveshipping")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<CheckoutStepResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SaveShipping([FromBody] SaveShippingRequest request)
    {
        if (_orderSettings.CheckoutDisabled)
            return BadRequest(new { Message = await _localizationService.GetResourceAsync("Checkout.Disabled") });

        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

        if (!cart.Any())
            return BadRequest(new { Message = "Cart is empty" });

        //if (!await _shoppingCartService.ShoppingCartRequiresShippingAsync(cart))
        //    return BadRequest(new { Message = "Shipping is not required" });

        var response = new CheckoutStepResponseDto();

        //pickup point
        if (_shippingSettings.AllowPickupInStore && request?.PickupInStore == true)
        {
            if (!string.IsNullOrEmpty(request.PickupPointsId))
            {
                var pickupPoint = request.PickupPointsId.Split(_separator, StringSplitOptions.None);
                if (pickupPoint.Length >= 2)
                {
                    var address = customer.BillingAddressId.HasValue
                        ? await _addressService.GetAddressByIdAsync(customer.BillingAddressId.Value)
                        : null;

                    var selectedPoint = (await _shippingService.GetPickupPointsAsync(cart, address, customer, pickupPoint[1], store.Id))
                        .PickupPoints.FirstOrDefault(x => x.Id.Equals(pickupPoint[0]));

                    if (selectedPoint != null)
                    {
                        await SavePickupOptionAsync(selectedPoint, customer, store.Id);
                        response.NextStep = 4; // payment method step
                        return Ok(new ApiResponse<CheckoutStepResponseDto> { Data = response });
                    }
                }
            }
            return BadRequest(new { Message = "Pickup point is not allowed" });
        }

        //set value indicating that "pick up in store" option has not been chosen
        await _genericAttributeService.SaveAttributeAsync<PickupPoint>(customer, NopCustomerDefaults.SelectedPickupPointAttribute, null, store.Id);

        if (request?.ShippingAddressId > 0)
        {
            //existing address
            var address = await _customerService.GetCustomerAddressAsync(customer.Id, request.ShippingAddressId.Value);
            if (address == null)
                return BadRequest(new { Message = await _localizationService.GetResourceAsync("Checkout.Address.NotFound") });

            customer.ShippingAddressId = address.Id;
            await _customerService.UpdateCustomerAsync(customer);
        }
        else if (request?.ShippingNewAddress != null)
        {
            //new address
            var newAddress = request.ShippingNewAddress;
            var address = _addressService.FindAddress((await _customerService.GetAddressesByCustomerIdAsync(customer.Id)).ToList(),
                newAddress.FirstName, newAddress.LastName, newAddress.PhoneNumber,
                newAddress.Email, newAddress.FaxNumber, newAddress.Company,
                newAddress.Address1, newAddress.Address2, newAddress.City,
                newAddress.County, newAddress.StateProvinceId, newAddress.ZipPostalCode,
                newAddress.CountryId, string.Empty);

            if (address == null)
            {
                address = newAddress.ToEntity();
                address.CustomAttributes = string.Empty;
                address.CreatedOnUtc = DateTime.UtcNow;

                if (address.CountryId == 0)
                    address.CountryId = null;
                if (address.StateProvinceId == 0)
                    address.StateProvinceId = null;

                await _addressService.InsertAddressAsync(address);
                await _customerService.InsertCustomerAddressAsync(customer, address);
            }

            customer.ShippingAddressId = address.Id;
            await _customerService.UpdateCustomerAsync(customer);
        }

        //load shipping method
        var shippingAddress = await _customerService.GetCustomerShippingAddressAsync(customer);
        var shippingMethodModel = await _checkoutModelFactory.PrepareShippingMethodModelAsync(cart, shippingAddress);
        response.ShippingMethodModel = shippingMethodModel;
        response.NextStep = 3; // shipping method step

        return Ok(new ApiResponse<CheckoutStepResponseDto> { Data = response });
    }

    /// <summary>
    /// POST /checkout/saveshippingmethod
    /// Save shipping method.
    /// </summary>
    [HttpPost("saveshippingmethod")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<CheckoutStepResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SaveShippingMethod([FromBody] SaveShippingMethodRequest request)
    {
        if (_orderSettings.CheckoutDisabled)
            return BadRequest(new { Message = await _localizationService.GetResourceAsync("Checkout.Disabled") });

        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

        if (!cart.Any())
            return BadRequest(new { Message = "Cart is empty" });

        var response = new CheckoutStepResponseDto();

        if (!await _shoppingCartService.ShoppingCartRequiresShippingAsync(cart))
        {
            await _genericAttributeService.SaveAttributeAsync<ShippingOption>(customer, NopCustomerDefaults.SelectedShippingOptionAttribute, null, store.Id);
            response.NextStep = 4; // payment method step
            return Ok(new ApiResponse<CheckoutStepResponseDto> { Data = response });
        }

        //pickup point
        if (_shippingSettings.AllowPickupInStore && request?.PickupInStore == true)
        {
            if (!string.IsNullOrEmpty(request.PickupPointsId))
            {
                var pickupPoint = request.PickupPointsId.Split(_separator, StringSplitOptions.None);
                if (pickupPoint.Length >= 2)
                {
                    var address = customer.BillingAddressId.HasValue
                        ? await _addressService.GetAddressByIdAsync(customer.BillingAddressId.Value)
                        : null;

                    var selectedPoint = (await _shippingService.GetPickupPointsAsync(cart, address, customer, pickupPoint[1], store.Id))
                        .PickupPoints.FirstOrDefault(x => x.Id.Equals(pickupPoint[0]));

                    if (selectedPoint != null)
                    {
                        await SavePickupOptionAsync(selectedPoint, customer, store.Id);
                        response.NextStep = 4; // payment method step
                        return Ok(new ApiResponse<CheckoutStepResponseDto> { Data = response });
                    }
                }
            }
            return BadRequest(new { Message = "Pickup point is not allowed" });
        }

        //set value indicating that "pick up in store" option has not been chosen
        await _genericAttributeService.SaveAttributeAsync<PickupPoint>(customer, NopCustomerDefaults.SelectedPickupPointAttribute, null, store.Id);

        //parse selected method
        if (string.IsNullOrEmpty(request?.ShippingOption))
            return BadRequest(new { Message = "Shipping option is required" });

        var splittedOption = request.ShippingOption.Split(_separator, StringSplitOptions.RemoveEmptyEntries);
        if (splittedOption.Length != 2)
            return BadRequest(new { Message = "Invalid shipping option format" });

        var selectedName = splittedOption[0];
        var shippingRateComputationMethodSystemName = splittedOption[1];

        //find it
        var shippingOptions = await _genericAttributeService.GetAttributeAsync<List<ShippingOption>>(customer,
            NopCustomerDefaults.OfferedShippingOptionsAttribute, store.Id);
        if (shippingOptions == null || !shippingOptions.Any())
        {
            var shippingAddress = await _customerService.GetCustomerShippingAddressAsync(customer);
            shippingOptions = (await _shippingService.GetShippingOptionsAsync(cart, shippingAddress, customer, shippingRateComputationMethodSystemName, store.Id))
                .ShippingOptions.ToList();
        }
        else
        {
            shippingOptions = shippingOptions.Where(so => so.ShippingRateComputationMethodSystemName.Equals(shippingRateComputationMethodSystemName, StringComparison.InvariantCultureIgnoreCase))
                .ToList();
        }

        var shippingOption = shippingOptions
            .FirstOrDefault(so => !string.IsNullOrEmpty(so.Name) && so.Name.Equals(selectedName, StringComparison.InvariantCultureIgnoreCase));
        if (shippingOption == null)
            return BadRequest(new { Message = "Shipping option not found" });

        //save
        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.SelectedShippingOptionAttribute, shippingOption, store.Id);

        response.NextStep = 4; // payment method step
        return Ok(new ApiResponse<CheckoutStepResponseDto> { Data = response });
    }

    /// <summary>
    /// POST /checkout/savepaymentmethod
    /// Save payment method.
    /// </summary>
    [HttpPost("savepaymentmethod")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<CheckoutStepResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SavePaymentMethod([FromBody] SavePaymentMethodRequest request)
    {
        if (_orderSettings.CheckoutDisabled)
            return BadRequest(new { Message = await _localizationService.GetResourceAsync("Checkout.Disabled") });

        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

        if (!cart.Any())
            return BadRequest(new { Message = "Cart is empty" });

        var response = new CheckoutStepResponseDto();

        //reward points
        if (_rewardPointsSettings.Enabled)
        {
            await _genericAttributeService.SaveAttributeAsync(customer,
                NopCustomerDefaults.UseRewardPointsDuringCheckoutAttribute, request?.UseRewardPoints ?? false,
                store.Id);
        }

        var vendorsInCart = await GetCartVendorsAsync(cart);
        var vendorPaymentSelections = request?.VendorPayments?.Where(vp => vp?.VendorId > 0).ToList() ?? new List<VendorPaymentSelectionDto>();
        var vendorPaymentMap = new Dictionary<int, string>();

        foreach (var selection in vendorPaymentSelections)
        {
            if (!vendorsInCart.ContainsKey(selection.VendorId))
                return BadRequest(new { Message = $"Vendor {selection.VendorId} is not present in the current cart" });

            if (string.IsNullOrWhiteSpace(selection.PaymentMethod))
                return BadRequest(new { Message = $"Payment method is required for vendor {selection.VendorId}" });

            if (!await _paymentPluginManager.IsPluginActiveAsync(selection.PaymentMethod, customer, store.Id))
                return BadRequest(new { Message = $"Payment method '{selection.PaymentMethod}' is not active for vendor {selection.VendorId}" });

            vendorPaymentMap[selection.VendorId] = selection.PaymentMethod;
        }

        //Check whether payment workflow is required
        var isPaymentWorkflowRequired = await _orderProcessingService.IsPaymentWorkflowRequiredAsync(cart);
        if (!isPaymentWorkflowRequired)
        {
            await _genericAttributeService.SaveAttributeAsync<string>(customer,
                NopCustomerDefaults.SelectedPaymentMethodAttribute, null, store.Id);
            response.NextStep = 6; // confirm order step
            return Ok(new ApiResponse<CheckoutStepResponseDto> { Data = response });
        }

        var selectedPaymentMethod = request?.PaymentMethod;
        if (string.IsNullOrWhiteSpace(selectedPaymentMethod) && vendorPaymentMap.Any())
            selectedPaymentMethod = vendorPaymentMap.First().Value;

        if (string.IsNullOrWhiteSpace(selectedPaymentMethod))
            return BadRequest(new { Message = "Payment method is required" });

        if (!await _paymentPluginManager.IsPluginActiveAsync(selectedPaymentMethod, customer, store.Id))
            return BadRequest(new { Message = "Payment method is not active" });

        if (!vendorPaymentMap.Any() && vendorsInCart.Any())
            vendorPaymentMap = vendorsInCart.Keys.ToDictionary(id => id, _ => selectedPaymentMethod);

        await _genericAttributeService.SaveAttributeAsync(customer,
            NopCustomerDefaults.SelectedPaymentMethodAttribute, selectedPaymentMethod, store.Id);

        await SaveVendorPaymentSelectionsAsync(customer, store.Id, vendorPaymentMap);

        var paymentMethod = await _paymentPluginManager.LoadPluginBySystemNameAsync(selectedPaymentMethod, customer, store.Id);
        if (paymentMethod != null)
        {
            if (paymentMethod.SkipPaymentInfo ||
                (paymentMethod.PaymentMethodType == PaymentMethodType.Redirection && _paymentSettings.SkipPaymentInfoStepForRedirectionPaymentMethods))
            {
                await _orderProcessingService.SetProcessPaymentRequestAsync(new ProcessPaymentRequest());
                response.NextStep = 6; // confirm order step
            }
            else
            {
                var paymentInfoModel = await _checkoutModelFactory.PreparePaymentInfoModelAsync(paymentMethod);
                response.PaymentInfoModel = paymentInfoModel;
                response.NextStep = 5; // payment info step
            }
        }
        else
        {
            response.NextStep = 6; // confirm order step
        }

        return Ok(new ApiResponse<CheckoutStepResponseDto> { Data = response });
    }

    /// <summary>
    /// GET /checkout/confirmorder
    /// Get confirm order summary.
    /// </summary>
    [HttpGet("confirmorder")]
    [ProducesResponseType(typeof(ApiResponse<CheckoutConfirmModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetConfirmOrder()
    {
        if (_orderSettings.CheckoutDisabled)
            return BadRequest(new { Message = await _localizationService.GetResourceAsync("Checkout.Disabled") });

        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

        if (!cart.Any())
            return BadRequest(new { Message = "Cart is empty" });

        var model = await _checkoutModelFactory.PrepareConfirmOrderModelAsync(cart);

        return Ok(new ApiResponse<CheckoutConfirmModel> { Data = model });
    }

    /// <summary>
    /// POST /checkout/confirmorder
    /// Confirm and place order.
    /// </summary>
    [HttpPost("confirmorder")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<CheckoutCompletedModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CheckoutConfirmModel>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmOrder()
    {
        return await ConfirmOrderInternal();
    }

    /// <summary>
    /// POST /checkout/confirmorder/selected
    /// Confirm and place order using only selected cart items.
    /// Body: { "itemIds": [1,2,3] }
    /// </summary>
    [HttpPost("confirmorder/selected")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<CheckoutCompletedModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CheckoutConfirmModel>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmSelectedOrder([FromBody] ConfirmSelectedOrderRequest request)
    {
        if (_orderSettings.CheckoutDisabled)
            return BadRequest(new { Message = await _localizationService.GetResourceAsync("Checkout.Disabled") });

        if (request?.ItemIds == null || !request.ItemIds.Any())
            return BadRequest(new { Message = "ItemIds is required" });

        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

        if (!cart.Any())
            return BadRequest(new { Message = "Cart is empty" });

        var selectedIds = request.ItemIds.Distinct().ToList();
        var missingIds = selectedIds.Except(cart.Select(x => x.Id)).ToList();
        if (missingIds.Any())
            return BadRequest(new { Message = $"Items not found in cart: {string.Join(", ", missingIds)}" });

        var itemsToKeep = cart.Where(x => selectedIds.Contains(x.Id)).ToList();
        if (!itemsToKeep.Any())
            return BadRequest(new { Message = "No matching items in cart" });

        var itemsToRestore = cart.Where(x => !selectedIds.Contains(x.Id)).ToList();

        // remove unselected items temporarily
        foreach (var item in itemsToRestore)
            await _shoppingCartService.DeleteShoppingCartItemAsync(item);

        IActionResult result;
        try
        {
            result = await ConfirmOrderInternal();
        }
        finally
        {
            // restore unselected items so the cart keeps them after checkout
            if (itemsToRestore.Any())
            {
                foreach (var item in itemsToRestore)
                {
                    var product = await _productService.GetProductByIdAsync(item.ProductId);
                    if (product == null || product.Deleted)
                        continue;

                    await _shoppingCartService.AddToCartAsync(
                        customer,
                        product,
                        ShoppingCartType.ShoppingCart,
                        store.Id,
                        item.AttributesXml,
                        item.CustomerEnteredPrice,
                        item.RentalStartDateUtc,
                        item.RentalEndDateUtc,
                        item.Quantity,
                        false);
                }
            }
        }

        return result;
    }

    private async Task<IActionResult> ConfirmOrderInternal()
    {
        if (_orderSettings.CheckoutDisabled)
            return BadRequest(new { Message = await _localizationService.GetResourceAsync("Checkout.Disabled") });

        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

        if (!cart.Any())
            return BadRequest(new { Message = "Cart is empty" });

        var model = await _checkoutModelFactory.PrepareConfirmOrderModelAsync(cart);

        //ensure shipping option is selected before placing the order to avoid shipping total calculation failure
        if (await _shoppingCartService.ShoppingCartRequiresShippingAsync(cart))
        {
            var selectedShippingOption = await _genericAttributeService.GetAttributeAsync<ShippingOption>(customer,
                NopCustomerDefaults.SelectedShippingOptionAttribute, store.Id);

            if (selectedShippingOption == null)
            {
                var offeredOptions = await _genericAttributeService
                    .GetAttributeAsync<List<ShippingOption>>(customer, NopCustomerDefaults.OfferedShippingOptionsAttribute, store.Id)
                    ?? new List<ShippingOption>();

                if (offeredOptions.Count == 1)
                {
                    selectedShippingOption = offeredOptions[0];
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.SelectedShippingOptionAttribute, selectedShippingOption, store.Id);
                }
                else
                {
                    model.Warnings.Add(await _localizationService.GetResourceAsync("Checkout.SelectShippingMethod"));
                    return BadRequest(new ApiResponse<CheckoutConfirmModel> { Data = model });
                }
            }
        }

        try
        {
            //prevent 2 orders being placed within an X seconds time frame
            if (_orderSettings.MinimumOrderPlacementInterval > 0)
            {
                var lastOrder = (await _orderService.SearchOrdersAsync(storeId: store.Id, customerId: customer.Id, pageSize: 1))
                    .FirstOrDefault();
                if (lastOrder != null)
                {
                    var interval = DateTime.UtcNow - lastOrder.CreatedOnUtc;
                    if (interval.TotalMinutes <= _orderSettings.MinimumOrderPlacementInterval)
                    {
                        model.Warnings.Add(await _localizationService.GetResourceAsync("Checkout.MinOrderPlacementInterval"));
                        return BadRequest(new ApiResponse<CheckoutConfirmModel> { Data = model });
                    }
                }
            }

            var isPaymentWorkflowRequired = await _orderProcessingService.IsPaymentWorkflowRequiredAsync(cart);
            var vendorsInCart = await GetCartVendorsAsync(cart);
            var vendorPaymentSelections = await LoadVendorPaymentSelectionsAsync(customer, store.Id);
            var vendorValidationWarnings = new List<string>();

            foreach (var vendor in vendorsInCart)
            {
                if (!vendorPaymentSelections.TryGetValue(vendor.Key, out var vendorMethod) || string.IsNullOrWhiteSpace(vendorMethod))
                {
                    var vendorLabel = string.IsNullOrWhiteSpace(vendor.Value) ? vendor.Key.ToString() : vendor.Value;
                    vendorValidationWarnings.Add($"Payment method is required for vendor {vendorLabel}");
                    continue;
                }

                if (isPaymentWorkflowRequired && !await _paymentPluginManager.IsPluginActiveAsync(vendorMethod, customer, store.Id))
                {
                    var vendorLabel = string.IsNullOrWhiteSpace(vendor.Value) ? vendor.Key.ToString() : vendor.Value;
                    vendorValidationWarnings.Add($"Payment method '{vendorMethod}' for vendor {vendorLabel} is not available");
                }
            }

            if (vendorValidationWarnings.Any())
            {
                foreach (var warning in vendorValidationWarnings)
                    model.Warnings.Add(warning);
                return BadRequest(new ApiResponse<CheckoutConfirmModel> { Data = model });
            }

            //place order
            var processPaymentRequest = await _orderProcessingService.GetProcessPaymentRequestAsync();
            if (processPaymentRequest == null)
            {
                if (isPaymentWorkflowRequired)
                {
                    var selectedPaymentMethod = await _genericAttributeService.GetAttributeAsync<string>(customer,
                        NopCustomerDefaults.SelectedPaymentMethodAttribute, store.Id);

                    if (string.IsNullOrWhiteSpace(selectedPaymentMethod))
                    {
                        model.Warnings.Add(await _localizationService.GetResourceAsync("Checkout.SelectPaymentMethod"));
                        return BadRequest(new ApiResponse<CheckoutConfirmModel> { Data = model });
                    }

                    if (!await _paymentPluginManager.IsPluginActiveAsync(selectedPaymentMethod, customer, store.Id))
                    {
                        model.Warnings.Add(await _localizationService.GetResourceAsync("Checkout.NoPaymentMethods"));
                        return BadRequest(new ApiResponse<CheckoutConfirmModel> { Data = model });
                    }

                    processPaymentRequest = new ProcessPaymentRequest
                    {
                        PaymentMethodSystemName = selectedPaymentMethod
                    };
                }
                else
                {
                    processPaymentRequest = new ProcessPaymentRequest();
                }
            }

            processPaymentRequest.StoreId = store.Id;
            processPaymentRequest.CustomerId = customer.Id;
            if (vendorPaymentSelections.Any())
                processPaymentRequest.CustomValues[WebApiFrontendDefaults.VendorPaymentMethodsCustomValue] = JsonSerializer.Serialize(vendorPaymentSelections);
            await _orderProcessingService.SetProcessPaymentRequestAsync(processPaymentRequest);

            var placeOrderResult = await _orderProcessingService.PlaceOrderAsync(processPaymentRequest);
            if (placeOrderResult.Success)
            {
                await _orderProcessingService.SetProcessPaymentRequestAsync(null);

                var postProcessPaymentRequest = new PostProcessPaymentRequest
                {
                    Order = placeOrderResult.PlacedOrder
                };
                await _paymentService.PostProcessPaymentAsync(postProcessPaymentRequest);
                await _genericAttributeService.SaveAttributeAsync<Dictionary<int, string>>(customer, WebApiFrontendDefaults.VendorPaymentMethodsAttribute, null, store.Id);

                var completedModel = await _checkoutModelFactory.PrepareCheckoutCompletedModelAsync(placeOrderResult.PlacedOrder!);
                return Ok(new ApiResponse<CheckoutCompletedModel> { Data = completedModel });
            }

            foreach (var error in placeOrderResult.Errors)
                model.Warnings.Add(error);
        }
        catch (Exception exc)
        {
            model.Warnings.Add(exc.Message);
        }

        return BadRequest(new ApiResponse<CheckoutConfirmModel> { Data = model });
    }

    /// <summary>
    /// GET /checkout/completed
    /// Get completed order information.
    /// </summary>
    [HttpGet("completed")]
    [ProducesResponseType(typeof(ApiResponse<CheckoutCompletedModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCompleted([FromQuery] int? orderId = null)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();

        Order? order = null;
        if (orderId.HasValue)
        {
            order = await _orderService.GetOrderByIdAsync(orderId.Value);
        }
        if (order == null)
        {
            order = (await _orderService.SearchOrdersAsync(storeId: store.Id, customerId: customer.Id, pageSize: 1))
                .FirstOrDefault();
        }
        if (order == null || order.Deleted || customer.Id != order.CustomerId)
        {
            return NotFound(new { Message = "Order not found" });
        }

        var model = await _checkoutModelFactory.PrepareCheckoutCompletedModelAsync(order);

        return Ok(new ApiResponse<CheckoutCompletedModel> { Data = model });
    }

    /// <summary>
    /// GET /checkout/paymentmethods
    /// Получить список доступных методов оплаты.
    /// </summary>
    [HttpGet("paymentmethods")]
    [ProducesResponseType(typeof(ApiResponse<CheckoutPaymentMethodModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPaymentMethods([FromQuery] int? countryId = null)
    {
        if (_orderSettings.CheckoutDisabled)
            return BadRequest(new { Message = await _localizationService.GetResourceAsync("Checkout.Disabled") });

        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

        if (!cart.Any())
            return BadRequest(new { Message = "Cart is empty" });

        var shippingAddress = await _customerService.GetCustomerShippingAddressAsync(customer);
        var billingAddress = await _customerService.GetCustomerBillingAddressAsync(customer);
        var filterCountryId = countryId ?? shippingAddress?.CountryId ?? billingAddress?.CountryId ?? 0;

        var model = await _checkoutModelFactory.PreparePaymentMethodModelAsync(cart, filterCountryId);

        return Ok(new ApiResponse<CheckoutPaymentMethodModel> { Data = model });
    }

    #region Private Methods

    private async Task<Dictionary<int, string>> GetCartVendorsAsync(IList<ShoppingCartItem> cart)
    {
        var productIds = cart.Select(item => item.ProductId).Distinct().ToArray();
        var products = await _productService.GetProductsByIdsAsync(productIds);

        return products
            .Where(product => product != null && product.VendorId > 0)
            .GroupBy(product => product.VendorId)
            .ToDictionary(group => group.Key, _ => string.Empty);
    }

    private async Task SavePickupOptionAsync(PickupPoint pickupPoint, Customer customer, int storeId)
    {
        var name = !string.IsNullOrEmpty(pickupPoint.Name) ?
            string.Format(await _localizationService.GetResourceAsync("Checkout.PickupPoints.Name"), pickupPoint.Name) :
            await _localizationService.GetResourceAsync("Checkout.PickupPoints.NullName");
        var pickUpInStoreShippingOption = new ShippingOption
        {
            Name = name,
            Rate = pickupPoint.PickupFee,
            Description = pickupPoint.Description,
            ShippingRateComputationMethodSystemName = pickupPoint.ProviderSystemName,
            IsPickupInStore = true
        };

        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.SelectedShippingOptionAttribute, pickUpInStoreShippingOption, storeId);
        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.SelectedPickupPointAttribute, pickupPoint, storeId);
    }

    private async Task<string> SaveCustomerVatNumberAsync(string fullVatNumber, Customer customer)
    {
        var (vatNumberStatus, _, _) = await _taxService.GetVatNumberStatusAsync(fullVatNumber);
        customer.VatNumberStatus = vatNumberStatus;
        customer.VatNumber = fullVatNumber;
        await _customerService.UpdateCustomerAsync(customer);

        if (vatNumberStatus != VatNumberStatus.Valid && !string.IsNullOrEmpty(fullVatNumber))
        {
            var warning = await _localizationService.GetResourceAsync("Checkout.VatNumber.Warning");
            return string.Format(warning, await _localizationService.GetLocalizedEnumAsync(vatNumberStatus));
        }

        return string.Empty;
    }

    private async Task<Dictionary<int, string>> LoadVendorPaymentSelectionsAsync(Customer customer, int storeId)
    {
        var rawValue = await _genericAttributeService.GetAttributeAsync<string>(customer, WebApiFrontendDefaults.VendorPaymentMethodsAttribute, storeId);
        if (string.IsNullOrWhiteSpace(rawValue))
            return new Dictionary<int, string>();

        try
        {
            return JsonSerializer.Deserialize<Dictionary<int, string>>(rawValue) ?? new Dictionary<int, string>();
        }
        catch
        {
            return new Dictionary<int, string>();
        }
    }

    private async Task SaveVendorPaymentSelectionsAsync(Customer customer, int storeId, Dictionary<int, string>? vendorPayments)
    {
        if (vendorPayments == null || vendorPayments.Count == 0)
        {
            await _genericAttributeService.SaveAttributeAsync<string>(customer, WebApiFrontendDefaults.VendorPaymentMethodsAttribute, null, storeId);
            return;
        }

        var serialized = JsonSerializer.Serialize(vendorPayments);
        await _genericAttributeService.SaveAttributeAsync(customer, WebApiFrontendDefaults.VendorPaymentMethodsAttribute, serialized, storeId);
    }

    #endregion
}

