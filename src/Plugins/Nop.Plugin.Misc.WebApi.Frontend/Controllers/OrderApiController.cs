using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Misc.WebApi.Frontend.DTOs;
using Nop.Services.Customers;
using Nop.Services.Orders;
using Nop.Services.Shipping;
using Nop.Web.Factories;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Web.Models.Order;

namespace Nop.Plugin.Misc.WebApi.Frontend.Controllers;

/// <summary>
/// Public API for customer orders, aligned with NopStation Cart API routes.
/// </summary>
[ApiController]
[IgnoreAntiforgeryToken]
[Produces("application/json")]
[Route("public-api/order")]
public class OrderController : ControllerBase
{
    private readonly IWorkContext _workContext;
    private readonly ICustomerService _customerService;
    private readonly IOrderService _orderService;
    private readonly IShipmentService _shipmentService;
    private readonly IOrderModelFactory _orderModelFactory;

    public OrderController(
        IWorkContext workContext,
        ICustomerService customerService,
        IOrderService orderService,
        IShipmentService shipmentService,
        IOrderModelFactory orderModelFactory)
    {
        _workContext = workContext;
        _customerService = customerService;
        _orderService = orderService;
        _shipmentService = shipmentService;
        _orderModelFactory = orderModelFactory;
    }

    private async Task<Customer?> GetCurrentRegisteredCustomerAsync()
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
    [HttpGet("history")]
    [ProducesResponseType(typeof(ApiResponse<CustomerOrderListModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetOrderHistory([FromQuery] int? pageNumber = null, [FromQuery] OrderHistoryPeriods limit = OrderHistoryPeriods.All)
    {
        var customer = await GetCurrentRegisteredCustomerAsync();
        if (customer == null)
            return Unauthorized(new { Message = "Authentication required" });

        var model = await _orderModelFactory.PrepareCustomerOrderListModelAsync(pageNumber, limit);

        return Ok(new ApiResponse<CustomerOrderListModel> { Data = model });
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
            return NotFound(new { Message = "Order not found" });

        // Verify that the order belongs to the current customer
        if (order.CustomerId != customer.Id)
            return NotFound(new { Message = "Order not found" });

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
            return NotFound(new { Message = "Shipment not found" });

        var order = await _orderService.GetOrderByIdAsync(shipment.OrderId);
        if (order == null)
            return NotFound(new { Message = "Order not found" });

        // Verify that the order belongs to the current customer
        if (order.CustomerId != customer.Id)
            return NotFound(new { Message = "Shipment not found" });

        var model = await _orderModelFactory.PrepareShipmentDetailsModelAsync(shipment);

        return Ok(new ApiResponse<ShipmentDetailsModel> { Data = model });
    }
}

