using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Misc.WebApi.Frontend.DTOs;
using Nop.Services.Attributes;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Shipping;
using Nop.Core.Domain.Shipping;
using Nop.Web.Factories;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Web.Models.ShoppingCart;

namespace Nop.Plugin.Misc.WebApi.Frontend.Controllers;

/// <summary>
/// Public API endpoints for shopping cart actions used on product details page.
/// Routes are aligned with NopStation Cart API (Add to cart, attribute change, estimate shipping).
/// </summary>
[ApiController]
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

    #region Fields

    private readonly IProductService _productService;
    private readonly IProductAttributeParser _productAttributeParser;
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
    private readonly ShippingSettings _shippingSettings;
    private static readonly char[] _separator = [','];

    #endregion

    public ShoppingCartController(
        IProductService productService,
        IProductAttributeParser productAttributeParser,
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
        ShippingSettings shippingSettings)
    {
        _productService = productService;
        _productAttributeParser = productAttributeParser;
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
    /// POST /shoppingCart/AddProductToCart/details/{productId}/{cartType}
    /// cartType: 1 - shopping cart, 2 - wishlist (aligned with NopStation docs).
    /// Body: { "FormValues": [ { "Key": "product_attribute_{id}", "Value": "{valueId}" }, { "Key": "addtocart_{productId}.EnteredQuantity", "Value": "2" } ] }
    /// </summary>
    [HttpPost("AddProductToCart/details/{productId:int}/{cartType:int}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<AddToCartResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddProductToCartDetails(int productId, int cartType, [FromBody] FormValuesRequest request)
    {
        var product = await _productService.GetProductByIdAsync(productId);
        if (product == null || product.Deleted || !product.Published)
            return NotFound(new { Message = "Product not found" });

        var form = BuildFormCollection(request);

        var addToCartWarnings = new List<string>();

        // attributes
        var attributesXml = await _productAttributeParser.ParseProductAttributesAsync(product, form, addToCartWarnings);

        // quantity
        var quantity = _productAttributeParser.ParseEnteredQuantity(product, form);
        if (quantity <= 0)
            quantity = product.OrderMinimumQuantity > 0 ? product.OrderMinimumQuantity : 1;

        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();

        var cartTypeEnum = cartType == (int)ShoppingCartType.Wishlist
            ? ShoppingCartType.Wishlist
            : ShoppingCartType.ShoppingCart;

        addToCartWarnings.AddRange(await _shoppingCartService.AddToCartAsync(
            customer,
            product,
            cartTypeEnum,
            store.Id,
            attributesXml,
            quantity: quantity));

        var success = !addToCartWarnings.Any();

        var result = new AddToCartResultDto
        {
            Success = success,
            Warnings = addToCartWarnings
        };

        return Ok(new ApiResponse<AddToCartResultDto> { Data = result });
    }

    /// <summary>
    /// POST /shoppingcart/productattributechange/{productId}
    /// Body: { "FormValues": [ { "Key": "product_attribute_{id}", "Value": "{valueId}" }, ... ] }
    /// Returns updated price and stock message for selected attributes.
    /// </summary>
    [HttpPost("productattributechange/{productId:int}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<ProductAttributeChangeResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ProductAttributeChange(int productId, [FromBody] FormValuesRequest request)
    {
        var product = await _productService.GetProductByIdAsync(productId);
        if (product == null || product.Deleted || !product.Published)
            return NotFound(new { Message = "Product not found" });

        var form = BuildFormCollection(request);
        var errors = new List<string>();

        var attributesXml = await _productAttributeParser.ParseProductAttributesAsync(product, form, errors);

        // stock message with selected attributes
        var stockAvailability = await _productService.FormatStockMessageAsync(product, attributesXml);

        var result = new ProductAttributeChangeResultDto
        {
            ProductId = product.Id,
            StockAvailability = stockAvailability,
            Errors = errors
        };

        return Ok(new ApiResponse<ProductAttributeChangeResultDto> { Data = result });
    }

    /// <summary>
    /// GET /shoppingcart/cart
    /// Get shopping cart.
    /// </summary>
    [HttpGet("cart")]
    [ProducesResponseType(typeof(ApiResponse<ShoppingCartModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCart()
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

        var model = new ShoppingCartModel();
        model = await _shoppingCartModelFactory.PrepareShoppingCartModelAsync(model, cart);

        return Ok(new ApiResponse<ShoppingCartModel> { Data = model });
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

#endregion
