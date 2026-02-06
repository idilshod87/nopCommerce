using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Http;
using Nop.Plugin.Misc.WebApi.Frontend.DTOs;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Shipping;
using Nop.Services.Vendors;
using System.Linq;
using Nop.Web.Factories;
using Nop.Web.Infrastructure;
using Nop.Web.Models.Common;
using Nop.Web.Models.Order;

namespace Nop.Plugin.Misc.WebApi.Frontend.Controllers;

/// <summary>
/// Order status grouping for filtering
/// </summary>
public enum OrderStatusGroup
{
    /// <summary>
    /// All orders regardless of status
    /// </summary>
    All = 0,

    /// <summary>
    /// Current/active orders (Pending, Processing)
    /// </summary>
    Current = 1,

    /// <summary>
    /// Completed orders (Complete, Cancelled)
    /// </summary>
    Completed = 2
}

/// <summary>
/// Public API for customer orders, aligned with NopStation Cart API routes.
/// </summary>
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[IgnoreAntiforgeryToken]
[Produces("application/json")]
[Route("public-api/order")]
public class OrderController : ControllerBase
{
    private readonly IWorkContext _workContext;
    private readonly IStoreContext _storeContext;
    private readonly ICustomerService _customerService;
    private readonly IOrderService _orderService;
    private readonly IShipmentService _shipmentService;
    private readonly IOrderModelFactory _orderModelFactory;
    private readonly IOrderProcessingService _orderProcessingService;
    private readonly IProductService _productService;
    private readonly IVendorService _vendorService;
    private readonly IDateTimeHelper _dateTimeHelper;
    private readonly ILocalizationService _localizationService;
    private readonly ICurrencyService _currencyService;
    private readonly IPriceFormatter _priceFormatter;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly OrderSettings _orderSettings;

    public OrderController(
        IWorkContext workContext,
        IStoreContext storeContext,
        ICustomerService customerService,
        IOrderService orderService,
        IShipmentService shipmentService,
        IOrderModelFactory orderModelFactory,
        IOrderProcessingService orderProcessingService,
        IProductService productService,
        IVendorService vendorService,
        IDateTimeHelper dateTimeHelper,
        ILocalizationService localizationService,
        ICurrencyService currencyService,
        IPriceFormatter priceFormatter,
        IShoppingCartService shoppingCartService,
        OrderSettings orderSettings)
    {
        _workContext = workContext;
        _storeContext = storeContext;
        _customerService = customerService;
        _orderService = orderService;
        _shipmentService = shipmentService;
        _orderModelFactory = orderModelFactory;
        _orderProcessingService = orderProcessingService;
        _productService = productService;
        _vendorService = vendorService;
        _dateTimeHelper = dateTimeHelper;
        _localizationService = localizationService;
        _currencyService = currencyService;
        _priceFormatter = priceFormatter;
        _shoppingCartService = shoppingCartService;
        _orderSettings = orderSettings;
    }

    private async Task<Customer> GetCurrentRegisteredCustomerAsync()
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        if (!await _customerService.IsRegisteredAsync(customer))
            return null;
        return customer;
    }

    /// <summary>
    /// GET /order/history
    /// Get all previously placed orders of a customer.
    /// </summary>
    /// <param name="pageNumber">Page number</param>
    /// <param name="limit">Order filtering period</param>
    /// <param name="status">Order statuses to filter by. Can specify multiple values (e.g., ?status=10&status=20). Valid values: 10=Pending, 20=Processing, 30=Complete, 40=Cancelled</param>
    /// <param name="statusGroup">Order status group filter. Valid values: 0=All, 1=Current (Pending+Processing), 2=Completed (Complete+Cancelled). Takes precedence over status parameter.</param>
    [HttpGet("history")]
    [ProducesResponseType(typeof(ApiResponse<CustomerOrderListModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetOrderHistory(
        [FromQuery] int? pageNumber = null,
        [FromQuery] OrderHistoryPeriods limit = OrderHistoryPeriods.All,
        [FromQuery] List<int> status = null,
        [FromQuery] OrderStatusGroup? statusGroup = null)
    {
        var customer = await GetCurrentRegisteredCustomerAsync();
        if (customer == null)
            return Unauthorized(new { Message = "Authentication required" });

        List<int> statusFilter = null;

        // If statusGroup is provided, it takes precedence
        if (statusGroup.HasValue)
        {
            statusFilter = statusGroup.Value switch
            {
                OrderStatusGroup.Current => new List<int> 
                { 
                    (int)OrderStatus.Pending, 
                    (int)OrderStatus.Processing 
                },
                OrderStatusGroup.Completed => new List<int> 
                { 
                    (int)OrderStatus.Complete, 
                    (int)OrderStatus.Cancelled 
                },
                OrderStatusGroup.All => null,
                _ => null
            };
        }
        // Otherwise use individual status filter if provided
        else if (status != null && status.Any())
        {
            statusFilter = status;
        }

        // If status filter is provided, use custom implementation
        if (statusFilter != null && statusFilter.Any())
        {
            var model = await PrepareCustomerOrderListModelWithStatusFilterAsync(pageNumber, limit, statusFilter);
            return Ok(new ApiResponse<CustomerOrderListModel> { Data = model });
        }

        // Default behavior without status filter
        var defaultModel = await _orderModelFactory.PrepareCustomerOrderListModelAsync(pageNumber, limit);
        return Ok(new ApiResponse<CustomerOrderListModel> { Data = defaultModel });
    }

    /// <summary>
    /// GET /order/orderdetails/{id}
    /// Get details of a previously placed order.
    /// </summary>
    [HttpGet("orderdetails/{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailsModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderDetails(int id)
    {
        var customer = await GetCurrentRegisteredCustomerAsync();
        if (customer == null)
            return Unauthorized(new { Message = "Authentication required" });

        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null)
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: await _localizationService.GetResourceAsync("Order.NotFound"),
                detail: await _localizationService.GetResourceAsync("Order.NotFound"));

        // Verify that the order belongs to the current customer
        if (order.CustomerId != customer.Id)
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: await _localizationService.GetResourceAsync("Order.NotFound"),
                detail: await _localizationService.GetResourceAsync("Order.NotFound"));

        var model = await _orderModelFactory.PrepareOrderDetailsModelAsync(order);

        return Ok(new ApiResponse<OrderDetailsModel> { Data = model });
    }

    /// <summary>
    /// GET /order/orderdetails/shipment/{id}
    /// Get shipment details for an order.
    /// </summary>
    [HttpGet("orderdetails/shipment/{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ShipmentDetailsModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetShipmentDetails(int id)
    {
        var customer = await GetCurrentRegisteredCustomerAsync();
        if (customer == null)
            return Unauthorized(new { Message = "Authentication required" });

        var shipment = await _shipmentService.GetShipmentByIdAsync(id);
        if (shipment == null)
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: await _localizationService.GetResourceAsync("Shipment.NotFound"),
                detail: await _localizationService.GetResourceAsync("Shipment.NotFound"));

        var order = await _orderService.GetOrderByIdAsync(shipment.OrderId);
        if (order == null)
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: await _localizationService.GetResourceAsync("Order.NotFound"),
                detail: await _localizationService.GetResourceAsync("Order.NotFound"));

        // Verify that the order belongs to the current customer
        if (order.CustomerId != customer.Id)
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: await _localizationService.GetResourceAsync("Shipment.NotFound"),
                detail: await _localizationService.GetResourceAsync("Shipment.NotFound"));

        var model = await _orderModelFactory.PrepareShipmentDetailsModelAsync(shipment);

        return Ok(new ApiResponse<ShipmentDetailsModel> { Data = model });
    }

    /// <summary>
    /// POST /order/cancel/{id}
    /// Cancel an order.
    /// </summary>
    [HttpPost("cancel/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelOrder(int id)
    {
        var customer = await GetCurrentRegisteredCustomerAsync();
        if (customer == null)
            return Unauthorized(new { Message = "Authentication required" });

        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null)
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: await _localizationService.GetResourceAsync("Order.NotFound"),
                detail: await _localizationService.GetResourceAsync("Order.NotFound"));

        // Verify that the order belongs to the current customer
        if (order.CustomerId != customer.Id)
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: await _localizationService.GetResourceAsync("Order.NotFound"),
                detail: await _localizationService.GetResourceAsync("Order.NotFound"));

        // Check if order can be cancelled
        if (order.OrderStatus == OrderStatus.Cancelled)
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: await _localizationService.GetResourceAsync("Order.AlreadyCancelled"),
                detail: await _localizationService.GetResourceAsync("Order.AlreadyCancelled"));

        if (order.OrderStatus == OrderStatus.Processing)
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: await _localizationService.GetResourceAsync("Order.CannotCancelProcessing"),
                detail: await _localizationService.GetResourceAsync("Order.CannotCancelProcessing"));

        if (order.OrderStatus == OrderStatus.Complete)
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: await _localizationService.GetResourceAsync("Order.CannotCancelCompleted"),
                detail: await _localizationService.GetResourceAsync("Order.CannotCancelCompleted"));

        try
        {
            await _orderProcessingService.CancelOrderAsync(order, true);
            return Ok(new { Message = await _localizationService.GetResourceAsync("Order.CancelledSuccessfully") });
        }
        catch (Exception ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: await _localizationService.GetResourceAsync("Order.CancelFailed"),
                detail: ex.Message);
        }
    }

    /// <summary>
    /// Prepare customer order list model with status filter
    /// </summary>
    private async Task<CustomerOrderListModel> PrepareCustomerOrderListModelWithStatusFilterAsync(
        int? page,
        OrderHistoryPeriods limit,
        List<int> status)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();
        var pageSize = _orderSettings.CustomerOrdersPageSize;
        var pageIndex = Math.Max((page ?? 0) - 1, 0);

        // Search orders with status filter
        var orders = await _orderService.SearchOrdersAsync(
            storeId: store.Id,
            customerId: customer.Id,
            createdFromUtc: limit == OrderHistoryPeriods.All ? null : DateTime.UtcNow.AddDays((int)limit * -1),
            createdToUtc: limit > 0 ? DateTime.UtcNow : null,
            osIds: status, // Apply status filter
            pageIndex: pageIndex,
            pageSize: pageSize);

        var periods = await Enum.GetValues<OrderHistoryPeriods>()
            .SelectAwait(async enumValue => new
            {
                ID = enumValue.ToString().ToLower(),
                Name = await _localizationService.GetLocalizedEnumAsync(enumValue)
            }).ToListAsync();

        var model = new CustomerOrderListModel
        {
            AvailableLimits = new SelectList(periods, "ID", "Name", limit.ToString()).ToList(),
            PagerModel = new PagerModel(_localizationService)
            {
                PageSize = orders.PageSize,
                TotalRecords = orders.TotalCount,
                PageIndex = orders.PageIndex,
                ShowTotalSummary = true,
                RouteActionName = NopRouteNames.Standard.CUSTOMER_ORDERS_PAGED,
                UseRouteLinks = true,
                RouteValues = new CustomerOrdersRouteValues { PageNumber = orders.PageIndex, Limit = limit.ToString().ToLower() }
            }
        };

        foreach (var order in orders)
        {
            var orderItems = await _orderService.GetOrderItemsAsync(order.Id);
            string vendorName = string.Empty;
            int vendorId = 0;
            if (orderItems.Count > 0)
            {
                var firstProduct = await _productService.GetProductByIdAsync(orderItems[0].ProductId);
                if (firstProduct != null)
                {
                    vendorId = firstProduct.VendorId;
                    var vendor = await _vendorService.GetVendorByIdAsync(firstProduct.VendorId);
                    vendorName = vendor?.Name ?? string.Empty;
                }
            }

            var orderTotalInCustomerCurrency = _currencyService.ConvertCurrency(order.OrderTotal, order.CurrencyRate);
            var orderModel = new CustomerOrderModel
            {
                Id = order.Id,
                CreatedOn = await _dateTimeHelper.ConvertToUserTimeAsync(order.CreatedOnUtc, DateTimeKind.Utc),
                OrderStatusEnum = order.OrderStatus,
                OrderStatus = await _localizationService.GetLocalizedEnumAsync(order.OrderStatus),
                PaymentStatus = await _localizationService.GetLocalizedEnumAsync(order.PaymentStatus),
                ShippingStatus = await _localizationService.GetLocalizedEnumAsync(order.ShippingStatus),
                IsReturnRequestAllowed = await _orderProcessingService.IsReturnRequestAllowedAsync(order),
                CustomOrderNumber = order.CustomOrderNumber,
                VendorName = vendorName,
                VendorId = vendorId,
                ItemCount = orderItems.Count,
                OrderTotalValue = orderTotalInCustomerCurrency,
                CurrencyCode = order.CustomerCurrencyCode
            };
            orderModel.OrderTotal = await _priceFormatter.FormatPriceAsync(
                orderTotalInCustomerCurrency,
                true,
                order.CustomerCurrencyCode,
                false,
                (await _workContext.GetWorkingLanguageAsync()).Id);

            model.Orders.Add(orderModel);
        }

        return model;
    }

    /// <summary>
    /// POST /order/reorder
    /// Reorder an item from a previous order by adding it to the shopping cart.
    /// </summary>
    /// <param name="request">Reorder request containing orderId and orderItemId</param>
    [HttpPost("reorder")]
    [ProducesResponseType(typeof(ApiResponse<ReorderResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReorderItem([FromBody] ReorderRequest request)
    {
        var customer = await GetCurrentRegisteredCustomerAsync();
        if (customer == null)
            return Unauthorized(new { Message = "Authentication required" });

        var order = await _orderService.GetOrderByIdAsync(request.OrderId);
        if (order == null)
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: await _localizationService.GetResourceAsync("Order.NotFound"),
                detail: await _localizationService.GetResourceAsync("Order.NotFound"));

        if (order.CustomerId != customer.Id)
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: await _localizationService.GetResourceAsync("Order.NotFound"),
                detail: await _localizationService.GetResourceAsync("Order.NotFound"));

        var orderItems = await _orderService.GetOrderItemsAsync(order.Id);
        var orderItem = orderItems.FirstOrDefault(x => x.Id == request.OrderItemId);
        
        if (orderItem == null)
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: await _localizationService.GetResourceAsync("Order.OrderItemNotFound"),
                detail: await _localizationService.GetResourceAsync("Order.OrderItemNotFound"));

        var product = await _productService.GetProductByIdAsync(orderItem.ProductId);
        if (product == null || product.Deleted)
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: await _localizationService.GetResourceAsync("Products.ProductNotFound"),
                detail: await _localizationService.GetResourceAsync("Products.ProductNotFound"));

        var store = await _storeContext.GetCurrentStoreAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

        var existingCartItem = cart.FirstOrDefault(sci =>
            sci.ProductId == orderItem.ProductId &&
            sci.AttributesXml == orderItem.AttributesXml);

        if (existingCartItem != null)
        {
            return Ok(new ApiResponse<ReorderResult>
            {
                Data = new ReorderResult
                {
                    CartItemId = existingCartItem.Id,
                    WasAlreadyInCart = true,
                    Message = await _localizationService.GetResourceAsync("ShoppingCart.ProductAlreadyInCart") 
                        ?? "This product with the same attributes is already in your cart"
                }
            });
        }

        var warnings = await _shoppingCartService.AddToCartAsync(
            customer: customer,
            product: product,
            shoppingCartType: ShoppingCartType.ShoppingCart,
            storeId: store.Id,
            attributesXml: orderItem.AttributesXml,
            quantity: orderItem.Quantity);

        if (warnings.Any())
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: await _localizationService.GetResourceAsync("ShoppingCart.AddToCartFailed") ?? "Failed to add product to cart",
                detail: string.Join(", ", warnings));
        }

        var updatedCart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
        var addedCartItem = updatedCart.FirstOrDefault(sci =>
            sci.ProductId == orderItem.ProductId &&
            sci.AttributesXml == orderItem.AttributesXml);

        if (addedCartItem == null)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: await _localizationService.GetResourceAsync("ShoppingCart.AddToCartFailed") ?? "Failed to add product to cart",
                detail: await _localizationService.GetResourceAsync("ShoppingCart.ProductNotAddedToCart") ?? "The product was not added to the cart");
        }

        return Ok(new ApiResponse<ReorderResult>
        {
            Data = new ReorderResult
            {
                CartItemId = addedCartItem.Id,
                WasAlreadyInCart = false,
                Message = await _localizationService.GetResourceAsync("ShoppingCart.ProductAddedToCart") 
                    ?? "Product has been added to your cart"
            }
        });
    }

    /// <summary>
    /// Record that has filter options for route values. Used for Customer orders pagination
    /// </summary>
    private sealed partial record CustomerOrdersRouteValues : BaseRouteValues
    {
        public string Limit { get; set; }
    }
}

