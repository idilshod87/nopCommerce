using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Misc.WebApi.Frontend.DTOs;
using Nop.Services.Attributes;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Shipping;
using Nop.Services.Payments;
using Nop.Services.Tax;
using Nop.Core.Domain.Shipping;
using Nop.Web.Factories;
using Nop.Web.Models.Catalog;
using Nop.Web.Models.ShoppingCart;

namespace Nop.Plugin.Misc.WebApi.Frontend.Controllers;

/// <summary>
/// Public API endpoints for shopping cart actions used on product details page.
/// Routes are aligned with NopStation Cart API (Add to cart, attribute change, estimate shipping).
/// </summary>
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[IgnoreAntiforgeryToken]
[Produces("application/json")]
[Route("public-api/shoppingCart")]
public class ShoppingCartController : ControllerBase
{
    #region DTOs

    public class FormValueDto
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class FormValuesRequest
    {
        public List<FormValueDto> FormValues { get; set; } = new();
    }

    #endregion

    #nullable enable
#region Fields

    private readonly IProductService _productService;
    private readonly IProductAttributeParser _productAttributeParser;
    private readonly IProductAttributeService _productAttributeService;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly IStoreContext _storeContext;
    private readonly IWorkContext _workContext;
    private readonly IShoppingCartModelFactory _shoppingCartModelFactory;
    private readonly IAttributeParser<CheckoutAttribute, CheckoutAttributeValue> _checkoutAttributeParser;
    private readonly IAttributeService<CheckoutAttribute, CheckoutAttributeValue> _checkoutAttributeService;
    private readonly IGenericAttributeService _genericAttributeService;
    private readonly IDiscountService _discountService;
    private readonly IGiftCardService _giftCardService;
    private readonly ICustomerService _customerService;
    private readonly ILocalizationService _localizationService;
    private readonly IShippingService _shippingService;
    private readonly ICurrencyService _currencyService;
    private readonly IPaymentPluginManager _paymentPluginManager;
    private readonly IPriceCalculationService _priceCalculationService;
    private readonly IPriceFormatter _priceFormatter;
    private readonly ITaxService _taxService;
    private readonly IProductModelFactory _productModelFactory;
    private readonly ShippingSettings _shippingSettings;
    private static readonly char[] _separator = [','];

    #endregion

    public ShoppingCartController(
        IProductService productService,
        IProductAttributeParser productAttributeParser,
        IProductAttributeService productAttributeService,
        IShoppingCartService shoppingCartService,
        IStoreContext storeContext,
        IWorkContext workContext,
        IShoppingCartModelFactory shoppingCartModelFactory,
        IAttributeParser<CheckoutAttribute, CheckoutAttributeValue> checkoutAttributeParser,
        IAttributeService<CheckoutAttribute, CheckoutAttributeValue> checkoutAttributeService,
        IGenericAttributeService genericAttributeService,
        IDiscountService discountService,
        IGiftCardService giftCardService,
        ICustomerService customerService,
        ILocalizationService localizationService,
        IShippingService shippingService,
        ICurrencyService currencyService,
        IPaymentPluginManager paymentPluginManager,
        IPriceCalculationService priceCalculationService,
        IPriceFormatter priceFormatter,
        ITaxService taxService,
        IProductModelFactory productModelFactory,
        ShippingSettings shippingSettings)
    {
        _productService = productService;
        _productAttributeParser = productAttributeParser;
        _productAttributeService = productAttributeService;
        _shoppingCartService = shoppingCartService;
        _storeContext = storeContext;
        _workContext = workContext;
        _shoppingCartModelFactory = shoppingCartModelFactory;
        _checkoutAttributeParser = checkoutAttributeParser;
        _checkoutAttributeService = checkoutAttributeService;
        _genericAttributeService = genericAttributeService;
        _discountService = discountService;
        _giftCardService = giftCardService;
        _customerService = customerService;
        _localizationService = localizationService;
        _shippingService = shippingService;
        _currencyService = currencyService;
        _paymentPluginManager = paymentPluginManager;
        _priceCalculationService = priceCalculationService;
        _priceFormatter = priceFormatter;
        _taxService = taxService;
        _productModelFactory = productModelFactory;
        _shippingSettings = shippingSettings;
    }

    private static IFormCollection BuildFormCollection(FormValuesRequest request)
    {
        var dict = new Dictionary<string, StringValues>(StringComparer.InvariantCultureIgnoreCase);

        if (request?.FormValues != null)
        {
            foreach (var pair in request.FormValues)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                    continue;

                dict[pair.Key] = new StringValues(pair.Value ?? string.Empty);
            }
        }

        return new FormCollection(dict);
    }

    /// <summary>
    /// POST /shoppingcart/addcart
    /// Simplified endpoint for adding product to shopping cart for mobile app.
    /// Full route: POST /public-api/shoppingCart/addcart
    /// Body: { "productId": 123, "quantity": 2 }
    /// </summary>
    [HttpPost("addcart")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<AddToCartResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartRequestDto request)
    {
        if (request == null)
            return BadRequest(new { Message = "Request body is required" });

        if (request.ProductId <= 0)
            return BadRequest(new { Message = "ProductId is required and must be greater than 0" });

        var product = await _productService.GetProductByIdAsync(request.ProductId);
        if (product == null || product.Deleted || !product.Published)
            return NotFound(new { Message = "Product not found" });

        var customer = await _workContext.GetCurrentCustomerAsync();
        if (customer == null || !await _customerService.IsRegisteredAsync(customer))
            return Unauthorized(new { Message = "Unauthorized" });

        var store = await _storeContext.GetCurrentStoreAsync();

        // Determine quantity (default 1)
        var quantity = request.Quantity ?? 1;
        if (quantity <= 0)
            quantity = product.OrderMinimumQuantity > 0 ? product.OrderMinimumQuantity : 1;

        // Build form collection for quantity in standard nopCommerce format
        var formValues = new List<FormValueDto>
        {
            new()
            {
                Key = $"addtocart_{request.ProductId}.EnteredQuantity",
                Value = quantity.ToString()
            }
        };

        // Add product attributes to form values
        if (request.ProductAttributes != null)
        {
            foreach (var attr in request.ProductAttributes)
            {
                if (attr.Id <= 0)
                    continue;

                var key = $"product_attribute_{attr.Id}";
                string? value = null;

                if (attr.Values != null && attr.Values.Any())
                    value = string.Join(",", attr.Values);
                else if (attr.Value.HasValue)
                    value = attr.Value.Value.ToString();
                else if (!string.IsNullOrWhiteSpace(attr.Text))
                    value = attr.Text.Trim();

                if (!string.IsNullOrWhiteSpace(value))
                    formValues.Add(new FormValueDto { Key = key, Value = value });
            }
        }

        decimal customerEnteredPriceConverted = decimal.Zero;
        if (product.CustomerEntersPrice)
        {
            var workingCurrency = await _workContext.GetWorkingCurrencyAsync();
            decimal priceInWorkingCurrency;

            if (request.CustomerEnteredPrice.HasValue && request.CustomerEnteredPrice.Value > decimal.Zero)
            {
                priceInWorkingCurrency = request.CustomerEnteredPrice.Value;
            }
            else
            {
                priceInWorkingCurrency = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(product.MinimumCustomerEnteredPrice, workingCurrency);
            }

            customerEnteredPriceConverted = await _currencyService.ConvertToPrimaryStoreCurrencyAsync(priceInWorkingCurrency, workingCurrency);
        }

        // Build form collection and parse attributes (empty attributes for now)
        var form = BuildFormCollection(new FormValuesRequest { FormValues = formValues });

        var addToCartWarnings = new List<string>();
        var attributesXml = await _productAttributeParser.ParseProductAttributesAsync(product, form, addToCartWarnings);

        // Add to cart (price will be calculated automatically from product catalog, or use customer entered price if required)
        addToCartWarnings.AddRange(await _shoppingCartService.AddToCartAsync(
            customer,
            product,
            ShoppingCartType.ShoppingCart,
            store.Id,
            attributesXml,
            customerEnteredPrice: customerEnteredPriceConverted,
            quantity: quantity));

        var success = !addToCartWarnings.Any();

        // Build cart summary
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
        var cartModel = new ShoppingCartModel();
        cartModel = await _shoppingCartModelFactory.PrepareShoppingCartModelAsync(cartModel, cart);

        var cartSummary = new CartSummaryDto
        {
            ItemsCount = cartModel.Items.Count,
            TotalQuantity = cartModel.Items.Sum(i => i.Quantity),
            // Subtotal information is not directly available on ShoppingCartModel used here,
            // so we only return quantities and leave monetary fields empty/default.
            Subtotal = string.Empty,
            SubtotalValue = 0,
            CurrencyCode = string.Empty
        };

        var response = new AddToCartResponseDto
        {
            Success = success,
            Message = success
                ? "Product added to cart successfully"
                : "Product added to cart with warnings",
            CartSummary = cartSummary,
            Warnings = addToCartWarnings
        };

        if (!success)
        {
            var vpd = new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                { "Warnings", addToCartWarnings.ToArray() }
            })
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation error"
            };
            return BadRequest(vpd);
        }

        return Ok(new ApiResponse<AddToCartResponseDto> { Data = response });
    }

    /// <summary>
    /// POST /shoppingcart/removecart
    /// Simplified endpoint for removing items from shopping cart for mobile app.
    /// Full route: POST /public-api/shoppingCart/removecart
    /// Body: { "itemIds": [123, 456] }
    /// </summary>
    [HttpPost("removecart")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<RemoveFromCartResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveFromCart([FromBody] RemoveFromCartRequestDto request)
    {
        if (request == null)
            return BadRequest(new { Message = "Request body is required" });

        if (request.ItemIds == null || !request.ItemIds.Any())
            return BadRequest(new { Message = "ItemIds is required and must contain at least one item ID" });

        var customer = await _workContext.GetCurrentCustomerAsync();
        if (customer == null || !await _customerService.IsRegisteredAsync(customer))
            return Unauthorized(new { Message = "Unauthorized" });

        var store = await _storeContext.GetCurrentStoreAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

        // Get distinct item IDs to remove
        var itemIdsToRemove = request.ItemIds.Distinct().ToList();
        var removedCount = 0;
        var notFoundIds = new List<int>();

        // Remove items from cart
        foreach (var itemId in itemIdsToRemove)
        {
            var cartItem = cart.FirstOrDefault(item => item.Id == itemId);
            if (cartItem != null)
            {
                await _shoppingCartService.DeleteShoppingCartItemAsync(cartItem);
                removedCount++;
            }
            else
            {
                notFoundIds.Add(itemId);
            }
        }

        // Get updated cart
        cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
        var cartModel = new ShoppingCartModel();
        cartModel = await _shoppingCartModelFactory.PrepareShoppingCartModelAsync(cartModel, cart);

        var cartSummary = new CartSummaryDto
        {
            ItemsCount = cartModel.Items.Count,
            TotalQuantity = cartModel.Items.Sum(i => i.Quantity),
            Subtotal = string.Empty,
            SubtotalValue = 0,
            CurrencyCode = string.Empty
        };

        var message = removedCount > 0
            ? $"Successfully removed {removedCount} item(s) from cart"
            : "No items were removed from cart";

        if (notFoundIds.Any())
        {
            message += $". Item IDs not found: {string.Join(", ", notFoundIds)}";
        }

        var response = new RemoveFromCartResponseDto
        {
            Success = removedCount > 0,
            Message = message,
            CartSummary = cartSummary
        };

        return Ok(new ApiResponse<RemoveFromCartResponseDto> { Data = response });
    }

    /// <summary>
    /// POST /shoppingcart/updatecartitem
    /// Simplified endpoint for updating cart item quantity for mobile app.
    /// Full route: POST /public-api/shoppingCart/updatecartitem
    /// Body: { "itemId": 123, "quantity": 5 }
    /// </summary>
    [HttpPost("updatecartitem")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<UpdateCartItemQuantityResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCartItemQuantity([FromBody] UpdateCartItemQuantityRequestDto request)
    {
        if (request == null)
            return BadRequest(new { Message = "Request body is required" });

        if (request.ItemId <= 0)
            return BadRequest(new { Message = "ItemId is required and must be greater than 0" });

        if (request.Quantity <= 0)
            return BadRequest(new { Message = "Quantity is required and must be greater than 0" });

        var customer = await _workContext.GetCurrentCustomerAsync();
        if (customer == null || !await _customerService.IsRegisteredAsync(customer))
            return Unauthorized(new { Message = "Unauthorized" });

        var store = await _storeContext.GetCurrentStoreAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

        // Find the cart item
        var cartItem = cart.FirstOrDefault(item => item.Id == request.ItemId);
        if (cartItem == null)
            return NotFound(new { Message = "Cart item not found" });

        // Update quantity
        var warnings = await _shoppingCartService.UpdateShoppingCartItemAsync(
            customer,
            cartItem.Id,
            cartItem.AttributesXml,
            cartItem.CustomerEnteredPrice,
            cartItem.RentalStartDateUtc,
            cartItem.RentalEndDateUtc,
            request.Quantity,
            true);

        // Get updated cart
        cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
        var cartModel = new ShoppingCartModel();
        cartModel = await _shoppingCartModelFactory.PrepareShoppingCartModelAsync(cartModel, cart);

        var cartSummary = new CartSummaryDto
        {
            ItemsCount = cartModel.Items.Count,
            TotalQuantity = cartModel.Items.Sum(i => i.Quantity),
            Subtotal = string.Empty,
            SubtotalValue = 0,
            CurrencyCode = string.Empty
        };

        var success = !warnings.Any();
        var message = success
            ? "Cart item quantity updated successfully"
            : "Cart item quantity updated with warnings";

        var response = new UpdateCartItemQuantityResponseDto
        {
            Success = success,
            Message = message,
            CartSummary = cartSummary,
            Warnings = warnings
        };

        return Ok(new ApiResponse<UpdateCartItemQuantityResponseDto> { Data = response });
    }

    /// <summary>
    /// POST /shoppingcart/product_attributechange/{productId}
    /// Strongly typed endpoint for updating product attributes.
    /// Body: ProductAttributeChangeTypedRequest
    /// </summary>
    [HttpPost("product_attributechange/{productId:int}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<ProductAttributeChangeResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ProductAttributeChangeTyped(int productId, [FromBody] ProductAttributeChangeTypedRequest request)
    {
        if (request == null)
            return BadRequest(new { Message = "Request body is required" });

        var product = await _productService.GetProductByIdAsync(productId);
        if (product == null || product.Deleted || !product.Published)
            return NotFound(new { Message = "Product not found" });

        var quantity = request.Quantity ?? 1;
        if (quantity <= 0)
            quantity = 1;

        var formValues = BuildFormValuesFromTypedRequest(productId, request);
        var form = BuildFormCollection(formValues);
        var result = await PrepareProductAttributeChangeResultAsync(product, form, quantity);

        return Ok(new ApiResponse<ProductAttributeChangeResultDto> { Data = result });
    }

    /// <summary>
    /// GET /shoppingcart/cart
    /// Get shopping cart.
    /// </summary>
    [HttpGet("cart")]
    [ProducesResponseType(typeof(ApiResponse<ShoppingCartResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCart()
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

        var model = new ShoppingCartModel();
        model = await _shoppingCartModelFactory.PrepareShoppingCartModelAsync(model, cart);

        // Создаём расширенную модель корзины с атрибутами
        var cartDto = new ShoppingCartDto
        {
            OnePageCheckoutEnabled = model.OnePageCheckoutEnabled,
            ShowSku = model.ShowSku,
            ShowProductImages = model.ShowProductImages,
            IsEditable = model.IsEditable,
            IsReadyToCheckout = false,
            CheckoutAttributes = model.CheckoutAttributes,
            OrderReviewData = model.OrderReviewData,
            DiscountBox = model.DiscountBox,
            GiftCardBox = model.GiftCardBox,
            CustomProperties = model.CustomProperties
        };

        // Конвертируем items с добавлением атрибутов
        foreach (var item in model.Items)
        {
            var cartItem = cart.FirstOrDefault(c => c.Id == item.Id);
            var attributes = cartItem != null ? await ParseCartItemAttributesAsync(cartItem) : new List<CartItemAttributeDto>();

            var allowedQuantitiesStr = item.AllowedQuantities != null && item.AllowedQuantities.Any() 
                ? string.Join(",", item.AllowedQuantities.Select(q => q.Value)) 
                : string.Empty;

            cartDto.Items.Add(new ShoppingCartItemDto
            {
                Id = item.Id,
                Sku = item.Sku,
                VendorId = item.VendorId,
                VendorName = item.VendorName,
                Picture = item.Picture,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                ProductSeName = item.ProductSeName,
                UnitPrice = item.UnitPrice,
                UnitPriceValue = item.UnitPriceValue,
                SubTotal = item.SubTotal,
                SubTotalValue = item.SubTotalValue,
                DiscountValue = item.DiscountValue,
                Quantity = item.Quantity,
                AllowedQuantities = allowedQuantitiesStr,
                AttributeInfo = item.AttributeInfo,
                AllowItemEditing = item.AllowItemEditing,
                DisableRemoval = item.DisableRemoval,
                Warnings = item.Warnings,
                Attributes = attributes,
                CustomProperties = item.CustomProperties
            });
        }

        var response = new ShoppingCartResponseDto
        {
            Cart = cartDto,
            Vendors = await PrepareVendorPaymentInfoAsync(model, cart)
        };

        return Ok(new ApiResponse<ShoppingCartResponseDto> { Data = response });
    }

    /// <summary>
    /// POST /shoppingcart/updatecart
    /// Update shopping cart (quantities, remove items, checkout attributes).
    /// Body: FormValuesRequest with keys like "itemquantity{id}", "removefromcart", "checkout_attribute_{id}"
    /// </summary>
    [HttpPost("updatecart")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<ShoppingCartModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateCart([FromBody] FormValuesRequest request)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

        var form = BuildFormCollection(request);

        //get identifiers of items to remove
        var itemIdsToRemove = form["removefromcart"]
            .SelectMany(value => value.Split(_separator, StringSplitOptions.RemoveEmptyEntries))
            .Select(idString => int.TryParse(idString, out var id) ? id : 0)
            .Distinct().ToList();

        var products = (await _productService.GetProductsByIdsAsync(cart.Select(item => item.ProductId).Distinct().ToArray()))
            .ToDictionary(item => item.Id, item => item);

        //get order items with changed quantity
        var itemsWithNewQuantity = cart.Select(item => new
        {
            NewQuantity = itemIdsToRemove.Contains(item.Id) ? 0 : int.TryParse(form[$"itemquantity{item.Id}"], out var quantity) ? quantity : item.Quantity,
            Item = item,
            Product = products.TryGetValue(item.ProductId, out var value) ? value : null
        }).Where(item => item.NewQuantity != item.Item.Quantity);

        //try to update cart items with new quantities
        var warnings = await itemsWithNewQuantity.SelectAwait(async cartItem => new
        {
            ItemId = cartItem.Item.Id,
            Warnings = await _shoppingCartService.UpdateShoppingCartItemAsync(customer,
                cartItem.Item.Id, cartItem.Item.AttributesXml, cartItem.Item.CustomerEnteredPrice,
                cartItem.Item.RentalStartDateUtc, cartItem.Item.RentalEndDateUtc, cartItem.NewQuantity, true)
        }).ToListAsync();

        //updated cart
        cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

        //parse and save checkout attributes
        await ParseAndSaveCheckoutAttributesAsync(cart, form);

        //prepare model
        var model = new ShoppingCartModel();
        model = await _shoppingCartModelFactory.PrepareShoppingCartModelAsync(model, cart);

        //update current warnings
        foreach (var warningItem in warnings.Where(warningItem => warningItem.Warnings.Any()))
        {
            var itemModel = model.Items.FirstOrDefault(item => item.Id == warningItem.ItemId);
            if (itemModel != null)
                itemModel.Warnings = warningItem.Warnings.Concat(itemModel.Warnings).Distinct().ToList();
        }

        return Ok(new ApiResponse<ShoppingCartModel> { Data = model });
    }

    /// <summary>
    /// GET /shoppingcart/wishlist
    /// Get wishlist.
    /// </summary>
    [HttpGet("wishlist")]
    [ProducesResponseType(typeof(ApiResponse<WishlistModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWishlist([FromQuery] int? list = null)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.Wishlist, store.Id, customWishlistId: list);

        var model = new WishlistModel();
        model = await _shoppingCartModelFactory.PrepareWishlistModelAsync(model, cart, true, list);

        return Ok(new ApiResponse<WishlistModel> { Data = model });
    }

    /// <summary>
    /// POST /shoppingcart/updatewishlist
    /// Update wishlist (quantities, remove items).
    /// Body: FormValuesRequest with keys like "itemquantity{id}", "removefromcart"
    /// </summary>
    [HttpPost("updatewishlist")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<WishlistModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateWishlist([FromBody] FormValuesRequest request, [FromQuery] int? list = null)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.Wishlist, store.Id, customWishlistId: list);

        var form = BuildFormCollection(request);

        var allIdsToRemove = form.ContainsKey("removefromcart")
            ? form["removefromcart"].ToString().Split(_separator, StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse)
                .ToList()
            : new List<int>();

        var innerWarnings = new Dictionary<int, IList<string>>();
        foreach (var sci in cart)
        {
            var remove = allIdsToRemove.Contains(sci.Id);
            if (remove)
                await _shoppingCartService.DeleteShoppingCartItemAsync(sci);
            else
            {
                foreach (var formKey in form.Keys)
                    if (formKey.Equals($"itemquantity{sci.Id}", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (int.TryParse(form[formKey], out var newQuantity))
                        {
                            var currSciWarnings = await _shoppingCartService.UpdateShoppingCartItemAsync(customer,
                                sci.Id, sci.AttributesXml, sci.CustomerEnteredPrice,
                                sci.RentalStartDateUtc, sci.RentalEndDateUtc,
                                newQuantity, true);
                            innerWarnings.Add(sci.Id, currSciWarnings);
                        }
                        break;
                    }
            }
        }

        //updated wishlist
        cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.Wishlist, store.Id, customWishlistId: list);
        var model = new WishlistModel();
        model = await _shoppingCartModelFactory.PrepareWishlistModelAsync(model, cart, list: list);

        //update current warnings
        foreach (var kvp in innerWarnings)
        {
            var sciModel = model.Items.FirstOrDefault(x => x.Id == kvp.Key);
            if (sciModel != null)
                foreach (var w in kvp.Value)
                    if (!sciModel.Warnings.Contains(w))
                        sciModel.Warnings.Add(w);
        }

        return Ok(new ApiResponse<WishlistModel> { Data = model });
    }

    /// <summary>
    /// POST /shoppingcart/additemstocartfromwishlist
    /// Add items from wishlist to cart.
    /// Body: FormValuesRequest with key "addtocart" containing comma-separated item IDs
    /// </summary>
    [HttpPost("additemstocartfromwishlist")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<WishlistModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddItemsToCartFromWishlist([FromBody] FormValuesRequest request, [FromQuery] int? list = null)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();
        var wishlistCart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.Wishlist, store.Id, customWishlistId: list);

        var form = BuildFormCollection(request);

        var allWarnings = new List<string>();
        var allIdsToAdd = form.ContainsKey("addtocart")
            ? form["addtocart"].ToString().Split(_separator, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList()
            : new List<int>();

        foreach (var sci in wishlistCart)
        {
            if (allIdsToAdd.Contains(sci.Id))
            {
                var product = await _productService.GetProductByIdAsync(sci.ProductId);
                var warnings = await _shoppingCartService.AddToCartAsync(customer,
                    product, ShoppingCartType.ShoppingCart,
                    store.Id,
                    sci.AttributesXml, sci.CustomerEnteredPrice,
                    sci.RentalStartDateUtc, sci.RentalEndDateUtc, sci.Quantity, true);
                allWarnings.AddRange(warnings);
            }
        }

        //updated wishlist
        wishlistCart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.Wishlist, store.Id, customWishlistId: list);
        var model = new WishlistModel();
        model = await _shoppingCartModelFactory.PrepareWishlistModelAsync(model, wishlistCart, list: list);
        model.Warnings = allWarnings;

        return Ok(new ApiResponse<WishlistModel> { Data = model });
    }

    /// <summary>
    /// POST /shoppingcart/applydiscountcoupon
    /// Apply discount coupon code.
    /// Body: { "discountcouponcode": "COUPON123" }
    /// </summary>
    [HttpPost("applydiscountcoupon")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<ShoppingCartModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApplyDiscountCoupon([FromBody] ApplyCouponRequest request)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

        var discountcouponcode = request?.DiscountCouponCode?.Trim();

        var model = new ShoppingCartModel();
        if (!string.IsNullOrWhiteSpace(discountcouponcode))
        {
            var discounts = (await _discountService.GetAllDiscountsAsync(couponCode: discountcouponcode, showHidden: true))
                .Where(d => d.RequiresCouponCode)
                .ToList();
            if (discounts.Any())
            {
                var userErrors = new List<string>();
                var anyValidDiscount = await discounts.AnyAwaitAsync(async discount =>
                {
                    var validationResult = await _discountService.ValidateDiscountAsync(discount, customer, [discountcouponcode]);
                    userErrors.AddRange(validationResult.Errors);
                    return validationResult.IsValid;
                });

                if (anyValidDiscount)
                {
                    await _customerService.ApplyDiscountCouponCodeAsync(customer, discountcouponcode);
                    model.DiscountBox.Messages.Add(await _localizationService.GetResourceAsync("ShoppingCart.DiscountCouponCode.Applied"));
                    model.DiscountBox.IsApplied = true;
                }
                else
                {
                    if (userErrors.Any())
                        model.DiscountBox.Messages = userErrors;
                    else
                        model.DiscountBox.Messages.Add(await _localizationService.GetResourceAsync("ShoppingCart.DiscountCouponCode.WrongDiscount"));
                }
            }
            else
                model.DiscountBox.Messages.Add(await _localizationService.GetResourceAsync("ShoppingCart.DiscountCouponCode.CannotBeFound"));
        }
        else
            model.DiscountBox.Messages.Add(await _localizationService.GetResourceAsync("ShoppingCart.DiscountCouponCode.Empty"));

        model = await _shoppingCartModelFactory.PrepareShoppingCartModelAsync(model, cart);

        return Ok(new ApiResponse<ShoppingCartModel> { Data = model });
    }

    /// <summary>
    /// POST /shoppingcart/removediscountcoupon
    /// Remove discount coupon code.
    /// Body: { "discountId": 1 }
    /// </summary>
    [HttpPost("removediscountcoupon")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<ShoppingCartModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveDiscountCoupon([FromBody] RemoveCouponRequest request)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();

        if (request?.DiscountId > 0)
        {
            var discount = await _discountService.GetDiscountByIdAsync(request.DiscountId);
            if (discount != null)
                await _customerService.RemoveDiscountCouponCodeAsync(customer, discount.CouponCode);
        }

        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
        var model = new ShoppingCartModel();
        model = await _shoppingCartModelFactory.PrepareShoppingCartModelAsync(model, cart);

        return Ok(new ApiResponse<ShoppingCartModel> { Data = model });
    }

    /// <summary>
    /// POST /shoppingcart/checkoutattributechange
    /// Update checkout attributes.
    /// Body: FormValuesRequest with keys like "checkout_attribute_{id}"
    /// </summary>
    [HttpPost("checkoutattributechange")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<ShoppingCartModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckoutAttributeChange([FromBody] FormValuesRequest request)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

        var form = BuildFormCollection(request);
        await ParseAndSaveCheckoutAttributesAsync(cart, form);

        var model = new ShoppingCartModel();
        model = await _shoppingCartModelFactory.PrepareShoppingCartModelAsync(model, cart);

        return Ok(new ApiResponse<ShoppingCartModel> { Data = model });
    }

    /// <summary>
    /// POST /shoppingcart/applygiftcard
    /// Apply gift card code.
    /// Body: { "giftcardcouponcode": "GIFT123" }
    /// </summary>
    [HttpPost("applygiftcard")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<ShoppingCartModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApplyGiftCard([FromBody] ApplyGiftCardRequest request)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

        var giftcardcouponcode = request?.GiftCardCouponCode?.Trim();

        var model = new ShoppingCartModel();
        var validationError = await GetGiftCardValidationErrorAsync(cart, giftcardcouponcode);

        if (string.IsNullOrEmpty(validationError))
        {
            await _customerService.ApplyGiftCardCouponCodeAsync(customer, giftcardcouponcode);
            model.GiftCardBox.Message = await _localizationService.GetResourceAsync("ShoppingCart.GiftCardCouponCode.Applied");
            model.GiftCardBox.IsApplied = true;
        }
        else
        {
            model.GiftCardBox.Message = validationError;
            model.GiftCardBox.IsApplied = false;
        }

        model = await _shoppingCartModelFactory.PrepareShoppingCartModelAsync(model, cart);

        return Ok(new ApiResponse<ShoppingCartModel> { Data = model });
    }

    /// <summary>
    /// POST /shoppingcart/removegiftcardcode
    /// Remove gift card code.
    /// Body: { "giftCardId": 1 }
    /// </summary>
    [HttpPost("removegiftcardcode")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<ShoppingCartModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveGiftCardCode([FromBody] RemoveGiftCardRequest request)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();

        if (request?.GiftCardId > 0)
        {
            var gc = await _giftCardService.GetGiftCardByIdAsync(request.GiftCardId);
            if (gc != null)
                await _customerService.RemoveGiftCardCouponCodeAsync(customer, gc.GiftCardCouponCode);
        }

        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
        var model = new ShoppingCartModel();
        model = await _shoppingCartModelFactory.PrepareShoppingCartModelAsync(model, cart);

        return Ok(new ApiResponse<ShoppingCartModel> { Data = model });
    }

    /// <summary>
    /// POST /shoppingcart/cart/estimateshipping
    /// Estimate shipping for cart.
    /// Body: EstimateShippingModel
    /// </summary>
    [HttpPost("cart/estimateshipping")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<EstimateShippingResultModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CartEstimateShipping([FromBody] EstimateShippingModel model)
    {
        if (model == null)
            model = new EstimateShippingModel();

        var errors = new List<string>();

        if (!_shippingSettings.EstimateShippingCityNameEnabled && string.IsNullOrEmpty(model.ZipPostalCode))
            errors.Add(await _localizationService.GetResourceAsync("Shipping.EstimateShipping.ZipPostalCode.Required"));

        if (_shippingSettings.EstimateShippingCityNameEnabled && string.IsNullOrEmpty(model.City))
            errors.Add(await _localizationService.GetResourceAsync("Shipping.EstimateShipping.City.Required"));

        if (model.CountryId == null || model.CountryId == 0)
            errors.Add(await _localizationService.GetResourceAsync("Shipping.EstimateShipping.Country.Required"));

        if (errors.Count > 0)
            return BadRequest(new { Errors = errors });

        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

        var result = await _shoppingCartModelFactory.PrepareEstimateShippingResultModelAsync(cart, model, true);

        return Ok(new ApiResponse<EstimateShippingResultModel> { Data = result });
    }

    #region Private Methods

    private async Task<List<CartItemAttributeDto>> ParseCartItemAttributesAsync(ShoppingCartItem cartItem)
    {
        var result = new List<CartItemAttributeDto>();
        
        if (string.IsNullOrEmpty(cartItem.AttributesXml))
            return result;
        
        var product = await _productService.GetProductByIdAsync(cartItem.ProductId);
        if (product == null)
            return result;
        
        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();
        
        // Получить все маппинги атрибутов из XML
        var attributeMappings = await _productAttributeParser
            .ParseProductAttributeMappingsAsync(cartItem.AttributesXml);
        
        foreach (var mapping in attributeMappings)
        {
            // Получить ProductAttribute из ProductAttributeMapping
            var productAttribute = await _productAttributeService.GetProductAttributeByIdAsync(mapping.ProductAttributeId);
            if (productAttribute == null)
                continue;
            
            var attributeName = await _localizationService
                .GetLocalizedAsync(productAttribute, x => x.Name);
            
            var attributeDto = new CartItemAttributeDto
            {
                AttributeId = mapping.Id,
                AttributeName = attributeName,
                ControlType = mapping.AttributeControlType.ToString()
            };
            
            // Для атрибутов со значениями (dropdown, radio, checkboxes, color/image squares)
            if (mapping.ShouldHaveValues())
            {
                var attributeValues = await _productAttributeParser
                    .ParseProductAttributeValuesAsync(cartItem.AttributesXml, mapping.Id);
                
                foreach (var value in attributeValues)
                {
                    attributeDto.SelectedValueIds.Add(value.Id);
                    
                    var valueName = await _localizationService
                        .GetLocalizedAsync(value, x => x.Name);
                    attributeDto.SelectedValues.Add(valueName);
                    
                    // Получить добавку к цене от этого значения
                    var priceAdjustment = await _priceCalculationService
                        .GetProductAttributeValuePriceAdjustmentAsync(
                            product,
                            value,
                            customer,
                            store);
                    
                    if (priceAdjustment != 0)
                    {
                        attributeDto.PriceAdjustmentValue += priceAdjustment;
                    }
                }
            }
            else
            {
                // Для текстовых атрибутов (textbox, multiline textbox)
                var textValues = _productAttributeParser
                    .ParseValues(cartItem.AttributesXml, mapping.Id);
                
                if (textValues.Any())
                {
                    attributeDto.TextValue = string.Join(", ", textValues);
                    attributeDto.SelectedValues.AddRange(textValues);
                }
            }
            
            // Форматировать цену
            if (attributeDto.PriceAdjustmentValue != 0)
            {
                var currency = await _workContext.GetWorkingCurrencyAsync();
                var priceStr = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(
                    attributeDto.PriceAdjustmentValue, 
                    currency);
                attributeDto.PriceAdjustment = $"{(attributeDto.PriceAdjustmentValue > 0 ? "+" : "")}{priceStr:N2} {currency.CurrencyCode}";
            }
            
            result.Add(attributeDto);
        }
        
        return result;
    }

    private async Task<ProductAttributeChangeResultDto> PrepareProductAttributeChangeResultAsync(Product product, IFormCollection form, int quantity = 1)
    {
        var errors = new List<string>();
        var attributesXml = await _productAttributeParser.ParseProductAttributesAsync(product, form, errors);
        var stockAvailability = await _productService.FormatStockMessageAsync(product, attributesXml);

        var productPrice = await PrepareProductPriceForAttributeChangeAsync(product, attributesXml, quantity);

        // Calculate subtotal using the price value that already includes attribute adjustments
        var priceValue = productPrice.PriceValue ?? 0m;
        var subtotalValue = priceValue * quantity;
        var subtotalFormatted = await _priceFormatter.FormatPriceAsync(subtotalValue);

        return new ProductAttributeChangeResultDto
        {
            ProductId = product.Id,
            ProductPrice = productPrice,
            SubTotal = subtotalFormatted,
            SubTotalValue = subtotalValue,
            StockAvailability = stockAvailability,
            Errors = errors
        };
    }

    private async Task<ProductPriceModel> PrepareProductPriceForAttributeChangeAsync(Product product, string attributesXml, int quantity = 1)
    {
        var productDetails = await _productModelFactory.PrepareProductDetailsModelAsync(product);
        var model = productDetails.ProductPrice ?? new ProductPriceModel { ProductId = product.Id };

        if (model.HidePrices || model.CustomerEntersPrice || model.CallForPrice)
            return model;

        var currentCurrency = await _workContext.GetWorkingCurrencyAsync();
        var currentStore = await _storeContext.GetCurrentStoreAsync();
        var currentCustomer = await _workContext.GetCurrentCustomerAsync();

        // Use the ready-made GetUnitPriceAsync which already calculates price with attributes
        var (unitPrice, _, _) = await _shoppingCartService.GetUnitPriceAsync(
            product,
            currentCustomer,
            currentStore,
            ShoppingCartType.ShoppingCart,
            quantity,
            attributesXml,
            0,
            null,
            null,
            includeDiscounts: true);

        var finalPriceWithDiscount = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(unitPrice, currentCurrency);

        model.Price = await _priceFormatter.FormatPriceAsync(finalPriceWithDiscount);
        model.PriceValue = finalPriceWithDiscount;

        // Calculate weight for base price per unit
        var attributeValues = await _productAttributeParser.ParseProductAttributeValuesAsync(attributesXml);
        var totalWeight = product.BasepriceAmount;
        foreach (var attributeValue in attributeValues)
        {
            switch (attributeValue.AttributeValueType)
            {
                case AttributeValueType.Simple:
                    totalWeight += attributeValue.WeightAdjustment;
                    break;
                case AttributeValueType.AssociatedToProduct:
                    var associatedProduct = await _productService.GetProductByIdAsync(attributeValue.AssociatedProductId);
                    if (associatedProduct != null)
                        totalWeight += associatedProduct.BasepriceAmount * attributeValue.Quantity;
                    break;
            }
        }

        if (product.IsRental)
        {
            model.IsRental = true;
            model.Price = await _priceFormatter.FormatRentalProductPeriodAsync(product, model.Price);
            var priceStr = await _priceFormatter.FormatPriceAsync(finalPriceWithDiscount);
            model.RentalPrice = await _priceFormatter.FormatRentalProductPeriodAsync(product, priceStr);
            model.RentalPriceValue = finalPriceWithDiscount;
        }

        // Get base price for formatting (without attributes, just base price)
        var basePrice = (await _priceCalculationService.GetFinalPriceAsync(product, currentCustomer, currentStore, quantity: quantity, includeDiscounts: true)).finalPrice;
        model.BasePricePAngV = await _priceFormatter.FormatBasePriceAsync(product, basePrice, totalWeight);
        model.BasePricePAngVValue = basePrice;

        return model;
    }

    private static int GetEnteredQuantity(IFormCollection form, int productId)
    {
        var quantityValue = form[$"addtocart_{productId}.EnteredQuantity"];
        return int.TryParse(quantityValue, out var quantity) && quantity > 0 ? quantity : 1;
    }

    private static FormValuesRequest BuildFormValuesFromTypedRequest(int productId, ProductAttributeChangeTypedRequest request)
    {
        var formValues = new List<FormValueDto>();

        if (request.Quantity.HasValue && request.Quantity.Value > 0)
        {
            formValues.Add(new FormValueDto
            {
                Key = $"addtocart_{productId}.EnteredQuantity",
                Value = request.Quantity.Value.ToString()
            });
        }

        if (request.ProductAttributes != null)
        {
            foreach (var attribute in request.ProductAttributes)
                AddAttributeSelectionToFormValues(attribute, formValues);
        }

        return new FormValuesRequest { FormValues = formValues };
    }

    private static void AddAttributeSelectionToFormValues(ProductAttributeSelectionRequest? selection, ICollection<FormValueDto> formValues)
    {
        if (selection == null || selection.Id <= 0)
            return;

        var key = $"product_attribute_{selection.Id}";
        string? value = null;

        if (selection.Values != null && selection.Values.Any())
            value = string.Join(",", selection.Values);
        else if (selection.Value.HasValue)
            value = selection.Value.Value.ToString();
        else if (!string.IsNullOrWhiteSpace(selection.Text))
            value = selection.Text.Trim();

        if (!string.IsNullOrWhiteSpace(value))
            formValues.Add(new FormValueDto { Key = key, Value = value });

        if (selection.Date != null)
        {
            if (selection.Date.Day > 0)
                formValues.Add(new FormValueDto { Key = $"{key}_day", Value = selection.Date.Day.ToString() });
            if (selection.Date.Month > 0)
                formValues.Add(new FormValueDto { Key = $"{key}_month", Value = selection.Date.Month.ToString() });
            if (selection.Date.Year > 0)
                formValues.Add(new FormValueDto { Key = $"{key}_year", Value = selection.Date.Year.ToString() });
        }
    }

    private async Task<Dictionary<int, string>> GetVendorPaymentSelectionsAsync()
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();

        // Always get as string first to avoid cast exceptions
        var raw = await _genericAttributeService.GetAttributeAsync<string>(customer,
            WebApiFrontendDefaults.VendorPaymentMethodsAttribute,
            store.Id);

        if (string.IsNullOrWhiteSpace(raw))
            return new();

        try
        {
            // Try to deserialize as JSON
            var parsed = JsonSerializer.Deserialize<Dictionary<int, string>>(raw);
            if (parsed != null)
                return parsed;
        }
        catch
        {
            // If deserialization fails, reset the attribute
            await _genericAttributeService.SaveAttributeAsync<Dictionary<int, string>>(customer,
                WebApiFrontendDefaults.VendorPaymentMethodsAttribute,
                null,
                store.Id);
        }

        return new();
    }

    private async Task<IList<VendorPaymentInfoDto>> PrepareVendorPaymentInfoAsync(ShoppingCartModel model, IList<ShoppingCartItem> cart)
    {
        // Group items by VendorId (including VendorId=0 for store items)
        var vendorGroups = model.Items
            .GroupBy(item => item.VendorId > 0 ? item.VendorId : 0)
            .ToList();

        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();
        var languageId = (await _workContext.GetWorkingLanguageAsync()).Id;
        var vendorPaymentSelections = await GetVendorPaymentSelectionsAsync();
        var defaultSelectedPaymentMethod = await _genericAttributeService.GetAttributeAsync<string>(customer,
            NopCustomerDefaults.SelectedPaymentMethodAttribute, store.Id);

        // If no vendor-specific selections saved, but default payment method exists, use it for all vendors
        if (!vendorPaymentSelections.Any() && !string.IsNullOrWhiteSpace(defaultSelectedPaymentMethod))
        {
            // Apply default payment method to all vendor groups
            foreach (var group in vendorGroups)
            {
                vendorPaymentSelections[group.Key] = defaultSelectedPaymentMethod;
            }
        }

        var result = new List<VendorPaymentInfoDto>();
        var activePaymentMethods = await (await _paymentPluginManager.LoadActivePluginsAsync(customer, store.Id))
            .Where(pm => pm.PaymentMethodType == PaymentMethodType.Standard || pm.PaymentMethodType == PaymentMethodType.Redirection)
            .WhereAwait(async pm => !await pm.HidePaymentMethodAsync(cart))
            .ToListAsync();

        foreach (var group in vendorGroups)
        {
            var vendorId = group.Key;
            vendorPaymentSelections.TryGetValue(vendorId, out var systemName);

            // If no payment method found for this vendor, try to get the first available one (for single vendor scenarios)
            if (string.IsNullOrWhiteSpace(systemName) && vendorPaymentSelections.Any())
                systemName = vendorPaymentSelections.First().Value;

            // Fallback to default selected payment method if available
            if (string.IsNullOrWhiteSpace(systemName) && !string.IsNullOrWhiteSpace(defaultSelectedPaymentMethod))
                systemName = defaultSelectedPaymentMethod;

            string? paymentMethodName = null;
            if (!string.IsNullOrWhiteSpace(systemName))
            {
                var plugin = await _paymentPluginManager.LoadPluginBySystemNameAsync(systemName, customer, store.Id);
                if (plugin != null)
                    paymentMethodName = await _localizationService.GetLocalizedFriendlyNameAsync(plugin, languageId);
            }

            var availableMethods = new List<VendorPaymentMethodDto>();
            foreach (var pm in activePaymentMethods)
            {
                if (await _shoppingCartService.ShoppingCartIsRecurringAsync(cart) && pm.RecurringPaymentType == RecurringPaymentType.NotSupported)
                    continue;

                var pmSystemName = pm.PluginDescriptor.SystemName;
                var name = await _localizationService.GetLocalizedFriendlyNameAsync(pm, languageId);
                var logo = await _paymentPluginManager.GetPluginLogoUrlAsync(pm);
                var description = string.Empty;

                availableMethods.Add(new VendorPaymentMethodDto
                {
                    SystemName = pmSystemName,
                    Name = name,
                    LogoUrl = logo,
                    Description = description,
                    Selected = systemName != null && pmSystemName.Equals(systemName, StringComparison.InvariantCultureIgnoreCase)
                });
            }

            result.Add(new VendorPaymentInfoDto
            {
                VendorId = vendorId,
                VendorName = vendorId == 0 
                    ? string.Empty 
                    : group.Select(item => item.VendorName).FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)) ?? string.Empty,
                PaymentMethodSystemName = systemName ?? string.Empty,
                PaymentMethodName = paymentMethodName ?? string.Empty,
                AvailablePaymentMethods = availableMethods
            });
        }

        return result;
    }

    private async Task ParseAndSaveCheckoutAttributesAsync(IList<ShoppingCartItem> cart, IFormCollection form)
    {
        var attributesXml = string.Empty;
        var excludeShippableAttributes = !await _shoppingCartService.ShoppingCartRequiresShippingAsync(cart);
        var store = await _storeContext.GetCurrentStoreAsync();
        var checkoutAttributes = await _checkoutAttributeService.GetAllAttributesAsync(null, null, store.Id, excludeShippableAttributes);

        foreach (var attribute in checkoutAttributes)
        {
            var controlId = $"checkout_attribute_{attribute.Id}";
            switch (attribute.AttributeControlType)
            {
                case AttributeControlType.DropdownList:
                case AttributeControlType.RadioList:
                case AttributeControlType.ColorSquares:
                case AttributeControlType.ImageSquares:
                {
                    var ctrlAttributes = form[controlId];
                    if (!StringValues.IsNullOrEmpty(ctrlAttributes))
                    {
                        var selectedAttributeId = int.Parse(ctrlAttributes);
                        if (selectedAttributeId > 0)
                            attributesXml = _checkoutAttributeParser.AddAttribute(attributesXml, attribute, selectedAttributeId.ToString());
                    }
                    break;
                }
                case AttributeControlType.Checkboxes:
                {
                    var ctrlAttributes = form[controlId];
                    if (!StringValues.IsNullOrEmpty(ctrlAttributes))
                    {
                        foreach (var attributeValue in ctrlAttributes.ToString().Split(_separator, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (int.TryParse(attributeValue, out var selectedAttributeId) && selectedAttributeId > 0)
                                attributesXml = _checkoutAttributeParser.AddAttribute(attributesXml, attribute, selectedAttributeId.ToString());
                        }
                    }
                    break;
                }
                case AttributeControlType.TextBox:
                case AttributeControlType.MultilineTextbox:
                {
                    var ctrlAttributes = form[controlId];
                    if (!StringValues.IsNullOrEmpty(ctrlAttributes))
                    {
                        var enteredText = ctrlAttributes.ToString().Trim();
                        attributesXml = _checkoutAttributeParser.AddAttribute(attributesXml, attribute, enteredText);
                    }
                    break;
                }
                case AttributeControlType.Datepicker:
                {
                    var date = form[controlId + "_day"];
                    var month = form[controlId + "_month"];
                    var year = form[controlId + "_year"];
                    DateTime? selectedDate = null;
                    try
                    {
                        if (!StringValues.IsNullOrEmpty(date) && !StringValues.IsNullOrEmpty(month) && !StringValues.IsNullOrEmpty(year))
                            selectedDate = new DateTime(int.Parse(year), int.Parse(month), int.Parse(date));
                    }
                    catch
                    {
                        // ignored
                    }

                    if (selectedDate.HasValue)
                        attributesXml = _checkoutAttributeParser.AddAttribute(attributesXml, attribute, selectedDate.Value.ToString("D"));
                    break;
                }
            }
        }

        var customer = await _workContext.GetCurrentCustomerAsync();
        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.CheckoutAttributes, attributesXml, store.Id);
    }

    private async Task<string> GetGiftCardValidationErrorAsync(IList<ShoppingCartItem> cart, string giftcardcouponcode)
    {
        if (string.IsNullOrWhiteSpace(giftcardcouponcode))
            return await _localizationService.GetResourceAsync("ShoppingCart.GiftCardCouponCode.WrongGiftCard");

        if (await _shoppingCartService.ShoppingCartIsRecurringAsync(cart))
            return await _localizationService.GetResourceAsync("ShoppingCart.GiftCardCouponCode.DontWorkWithAutoshipProducts");

        var giftCard = (await _giftCardService.GetAllGiftCardsAsync(giftCardCouponCode: giftcardcouponcode)).FirstOrDefault();

        if (giftCard == null || !await _giftCardService.IsGiftCardValidAsync(giftCard))
            return await _localizationService.GetResourceAsync("ShoppingCart.GiftCardCouponCode.WrongGiftCard");

        if (await _productService.HasAnyGiftCardProductAsync(cart.Select(c => c.ProductId).ToArray()))
            return await _localizationService.GetResourceAsync("ShoppingCart.GiftCardCouponCode.DontWorkWithGiftCards");

        return string.Empty;
    }

    #endregion
}

#region Request DTOs

public class ApplyCouponRequest
{
    public string? DiscountCouponCode { get; set; }
}

public class RemoveCouponRequest
{
    public int DiscountId { get; set; }
}

public class ApplyGiftCardRequest
{
    public string? GiftCardCouponCode { get; set; }
}

public class RemoveGiftCardRequest
{
    public int GiftCardId { get; set; }
}

public class ProductAttributeChangeTypedRequest
{
    /// <summary>
    /// List of product attribute selections.
    /// </summary>
    public List<ProductAttributeSelectionRequest>? ProductAttributes { get; set; }
    
    /// <summary>
    /// Quantity for add to cart calculation.
    /// </summary>
    public int? Quantity { get; set; }
}

public class ProductAttributeSelectionRequest
{
    /// <summary>
    /// Product attribute mapping ID.
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Single value ID (for dropdown, radio, color/image squares).
    /// </summary>
    public int? Value { get; set; }
    
    /// <summary>
    /// Multiple value IDs (for checkboxes).
    /// </summary>
    public List<int>? Values { get; set; }
    
    /// <summary>
    /// Text value (for textbox, multiline textbox).
    /// </summary>
    public string? Text { get; set; }
    
    /// <summary>
    /// Date value (for datepicker).
    /// </summary>
    public ProductAttributeDateValueRequest? Date { get; set; }
}

public class ProductAttributeDateValueRequest
{
    public int Day { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
}

#endregion
